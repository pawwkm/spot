﻿using Pote;
using Pote.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Spot.Ebnf
{
    /// <summary>
    /// Parser for ISO/IEC 14977 : 1996(E) Ebnf
    /// </summary>
    internal sealed class Parser
    {
        private LexicalAnalyzer<TokenType> source;

        /// <summary>
        /// Parses the syntax expressed in ISO/IEC 14977 : 1996(E) Ebnf
        /// </summary>
        /// <param name="analyzer">The lexical analyzer that provides the tokens of the syntax.</param>
        /// <returns>The parsed syntax.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.3 for details.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="analyzer"/> is null.
        /// </exception>
        /// <exception cref="ParsingException">
        /// An error happend while parsing.
        /// </exception>
        public Syntax Parse(LexicalAnalyzer<TokenType> analyzer)
        {
            if (analyzer == null)
                throw new ArgumentNullException(nameof(analyzer));

            source = analyzer;

            List<Rule> rules = new List<Rule>();
            while (!analyzer.EndOfInput)
            {
                if (analyzer.LookAhead().Type == TokenType.EndOfInput)
                    break;

                Rule rule = Rule();
                if (rules.Any(x => x.Name == rule.Name))
                    throw new ParsingException(rule.DefinedAt.ToString("Rule '" + rule.Name + "' is redefined."));

                rules.Add(rule);
            }

            return new Syntax(rules);
        }

        /// <summary>
        /// Parses a rule. 
        /// </summary>
        /// <returns>The parsed rule.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.3 for details.
        /// </remarks>
        private Rule Rule()
        {
            Token<TokenType> identifier = source.Next();
            if (identifier.Type != TokenType.MetaIdentifier)
                throw new ParsingException(identifier.Position.ToString("Expected a meta identifier."));

            Rule rule = new Rule(identifier.Text, identifier.Position);

            Token<TokenType> equal = source.Next();
            if (equal.Text != equal.Text)
                throw new ParsingException(equal.Position.ToString("Expected =."));

            while (source.LookAhead().Type != TokenType.EndOfInput)
            {
                if (source.LookAhead().Text.IsOneOf(";", "."))
                    break;

                if (source.LookAhead().Text == "|")
                    source.Next();

                rule.Branches.AddRange(DefinitionList());
            }

            Token<TokenType> endSymbol = source.Next();
            if (!endSymbol.Text.IsOneOf(";", "."))
                throw new ParsingException(endSymbol.Position.ToString("Expected ';' or '.'"));

            return rule;
        }

        /// <summary>
        /// Parses a syntactic exception. 
        /// </summary>
        /// <returns>The parsed definition list.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.4 for details.
        /// </remarks>
        private DefinitionList DefinitionList()
        {
            DefinitionList list = new DefinitionList();
            list.Add(SingleDefinition());

            while (source.LookAhead().Text.IsOneOf(",", "/", "!"))
            {
                source.Next();
                list.Add(SingleDefinition());
            }

            return list;
        }

        /// <summary>
        /// Parses a single definition. 
        /// </summary>
        /// <returns>The parsed single definition.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.5 for details.
        /// </remarks>
        private SingleDefinition SingleDefinition()
        {
            SingleDefinition definition = new SingleDefinition(source.LookAhead().Position);

            definition.SyntacticTerms.Add(SyntacticTerm());
            while (source.LookAhead().Text == ",")
            {
                source.Next();
                definition.SyntacticTerms.Add(SyntacticTerm());
            }

            return definition;
        }

        /// <summary>
        /// Parses a syntactic term. 
        /// </summary>
        /// <returns>The parsed syntactic term.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.6 for details.
        /// </remarks>
        private SyntacticTerm SyntacticTerm()
        {
            SyntacticTerm term = new SyntacticTerm(SyntacticFactor(), source.LookAhead().Position);
            if (source.LookAhead().Text == "-")
            {
                source.Next();
                term.Exception = SyntacticException();
            }

            return term;
        }

        /// <summary>
        /// Parses a syntactic exception. 
        /// </summary>
        /// <returns>The parsed syntactic exception.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.7 for details.
        /// </remarks>
        private SyntacticFactor SyntacticException()
        {
            SyntacticFactor factor = SyntacticFactor();
            CheckForReferencesInException(factor);

            return factor;
        }

        /// <summary>
        /// Parses a syntactic factor. 
        /// </summary>
        /// <returns>The parsed syntactic factor.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.8 for details.
        /// </remarks>
        private SyntacticFactor SyntacticFactor()
        {
            Token<TokenType> token = source.LookAhead();
            if (token.Type == TokenType.Integer)
            {
                source.Next();
                uint repetitions = uint.Parse(token.Text, CultureInfo.InvariantCulture);

                token = source.Next();
                if (token.Text != "*")
                    throw new ParsingException(token.Position.ToString("Expected repetition symbol (*)."));

                return new SyntacticFactor(SyntacticPrimary(), token.Position, repetitions);
            }
            
            return new SyntacticFactor(SyntacticPrimary(), token.Position);
        }

        /// <summary>
        /// Parses a syntactic primary. 
        /// </summary>
        /// <returns>The parsed syntactic primary.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.10 for details.
        /// </remarks>
        private Definition SyntacticPrimary()
        {
            Token<TokenType> token = source.LookAhead();
            Definition definition = null;

            if (token.Type == TokenType.MetaIdentifier)
                definition = new MetaIdentifier(token.Text, token.Position);
            else if (token.Type == TokenType.TerminalString)
                definition = new TerminalString(token.Text, token.Position);
            else if (token.Type == TokenType.SpecialSequence)
                definition = new SpecialSequence(token.Text, token.Position);
            else if (token.Text.IsOneOf("[", "(/"))
                return OptionalSequence();
            else if (token.Text.IsOneOf("{", "(:"))
                return RepeatedSequence();
            else if (token.Text == "(")
                return GroupedSequence();
            else if (token.Text == ";")
                definition = new EmptySequence(token.Position);
            else
                throw new ParsingException(token.Position.ToString("Expected a syntactic primary"));

            source.Next();

            return definition;
        }

        /// <summary>
        /// Parses an optional sequence. 
        /// </summary>
        /// <returns>The parsed optional sequence.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.11 for details.
        /// </remarks>
        /// <exception cref="ParsingException">
        /// Expected optional sequence start or end symbol.
        /// </exception>
        private OptionalSequence OptionalSequence()
        {
            Token<TokenType> token = source.Next();
            OptionalSequence sequence = new OptionalSequence(token.Position);

            if (!token.Text.IsOneOf("[", "(/"))
                throw new ParsingException(token.Position.ToString("Expected optional sequence start symbol."));

            token = source.LookAhead();
            if (token.Text.IsOneOf("]", "/)"))
                throw new ParsingException(token.Position.ToString("The sequence must contain at least 1 syntactic primary."));

            sequence.Branches.AddRange(DefinitionList());
            while (source.LookAhead().Text == "|")
            {
                source.Next();
                sequence.Branches.AddRange(DefinitionList());
            }

            token = source.Next();
            if (!token.Text.IsOneOf("]", "/)"))
                throw new ParsingException(token.Position.ToString("Expected optional sequence end symbol."));

            return sequence;
        }

        /// <summary>
        /// Parses a repeated sequence. 
        /// </summary>
        /// <returns>The parsed repeated sequence.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.12 for details.
        /// </remarks>
        /// <exception cref="ParsingException">
        /// Expected repeat sequence start or end symbol.
        /// </exception>
        private RepeatedSequence RepeatedSequence()
        {
            Token<TokenType> token = source.Next();
            RepeatedSequence sequence = new RepeatedSequence(token.Position);

            if (!token.Text.IsOneOf("{", "(:"))
                throw new ParsingException(token.Position.ToString("Expected repeat sequence start symbol."));

            token = source.LookAhead();
            if (token.Text.IsOneOf("}", ":)"))
                throw new ParsingException(token.Position.ToString("The sequence must contain at least 1 syntactic primary."));

            sequence.Branches.AddRange(DefinitionList());
            while (source.LookAhead().Text == "|")
            {
                source.Next();
                sequence.Branches.AddRange(DefinitionList());
            }

            token = source.Next();
            if (!token.Text.IsOneOf("}", ":)"))
                throw new ParsingException(token.Position.ToString("Expected repeat sequence end symbol."));

            return sequence;
        }

        /// <summary>
        /// Parses a grouped sequence. 
        /// </summary>
        /// <returns>The parsed grouped sequence.</returns>
        /// <remarks>
        /// Read ISO/IEC 14977 : 1996(E) section 4.13 for details.
        /// </remarks>
        /// <exception cref="ParsingException">
        /// Expected group sequence start or end symbol.
        /// </exception>
        private GroupedSequence GroupedSequence()
        {
            Token<TokenType> token = source.Next();
            GroupedSequence sequence = new GroupedSequence(token.Position);

            if (token.Text != "(")
                throw new ParsingException(token.Position.ToString("Expected group sequence start symbol."));

            token = source.LookAhead();
            if (token.Text == ")")
                throw new ParsingException(token.Position.ToString("The sequence must contain at least 1 syntactic primary."));

            sequence.Branches.AddRange(DefinitionList());
            while (source.LookAhead().Text == "|")
            {
                source.Next();
                sequence.Branches.AddRange(DefinitionList());
            }

            token = source.Next();
            if (token.Text != ")")
                throw new ParsingException(token.Position.ToString("Expected group sequence end symbol."));

            return sequence;
        }

        /// <summary>
        /// Checks if the <paramref name="factor"/> contains meta identifiers.
        /// </summary>
        /// <param name="factor">THe factor to check.</param>
        /// <exception cref="ParsingException">
        /// <paramref name="factor"/> contains one or more meta identifiers.
        /// </exception>
        private void CheckForReferencesInException(SyntacticFactor factor)
        {
            var identifier = factor.SyntacticPrimary as MetaIdentifier;
            if (identifier != null)
                throw new ParsingException(identifier.DefinedAt.ToString("Rule '" + identifier.Name + "' referenced in exception."));

            var sequence = factor.SyntacticPrimary as Sequence;
            if (sequence != null)
            {
                foreach (var branch in sequence.Branches)
                {
                    var single = branch as SingleDefinition;
                    if (single == null)
                        continue;

                    foreach (var term in single.SyntacticTerms)
                        CheckForReferencesInException(term.Factor);
                }
            }
        }
    }
}
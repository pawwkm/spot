﻿using NSubstitute;
using NUnit.Framework;
using Pote;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Spot.Ebnf
{
    /// <summary>
    /// Provides tests for the <see cref="FuzzyTestGenerator"/> class.
    /// </summary>
    [TestFixture]
    public class FuzzyTestGeneratorTests
    {
        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle an empty syntax
        /// </summary>
        [Test]
        public void Generate_EmptySyntax_Success()
        {
            var text = "syntax = ;";

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new string[0];
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle a rule consisting of a single terminal.
        /// </summary>
        [Test]
        public void Generate_SingleTerminal_Success()
        {
            var text = "syntax = 'abc' ;";

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "abc" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle a rule consisting of a single terminal x times.
        /// </summary>
        [Test]
        public void Generate_RepeatedSingleTerminal_Success()
        {
            var text = "syntax = 3 * 'abc' ;";

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "abcabcabc" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle nested rules consisting of a single terminal.
        /// </summary>
        [Test]
        public void Generate_NestedRuleWithSingleTerminal_Success()
        {
            var text = new StringBuilder();
            text.AppendLine("syntax = rule ;");
            text.AppendLine("rule = 'abc' ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "abc" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle nested rules consisting of a single terminal x times.
        /// </summary>
        [Test]
        public void Generate_NestedRuleWithRepeatedSingleTerminal_Success()
        {
            var text = new StringBuilder();
            text.AppendLine("syntax = 3 * rule ;");
            text.AppendLine("rule = 'abc' ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "abcabcabc" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle branches.
        /// </summary>
        [Test]
        public void Generate_RuleWithBranches_Success()
        {
            var text = "syntax = 'abc' | 'def' ;";

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "abc", "def" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle rules consisting of terminals and a group of terminals.
        /// </summary>
        [Test]
        public void Generate_RuleConsistingOfAGroupOfTerminals_Success()
        {
            var text = "syntax = 'a', ( 'b' | 'c' ), 'd' ;";

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "abd", "acd" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle nested rules consisting of groups with terminals.
        /// </summary>
        [Test]
        public void Generate_NestedRuleConsistingOfGroupOfTerminals_Success()
        {
            var text = new StringBuilder();
            text.AppendLine("syntax = 'a', rule, 'd' ;");
            text.AppendLine("rule = ( 'b' | 'c' ) ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "abd", "acd" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle rules consisting of optional terminals.
        /// </summary>
        [Test]
        public void Generate_RuleConsistingOfOptionalOfTerminals_Success()
        {
            var text = "syntax = 'a', [ 'b' | 'c' ], 'd' ;";

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "ad", "abd", "acd" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle nested rules consisting of optional terminals.
        /// </summary>
        [Test]
        public void Generate_NestedRuleConsistingOfOptionalOfTerminals_Success()
        {
            var text = new StringBuilder();
            text.AppendLine("syntax = 'a', rule, 'd' ;");
            text.AppendLine("rule = [ 'b' | 'c' ] ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "ad", "abd", "acd" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle rules consisting of a repeated sequence terminals.
        /// </summary>
        [Test]
        public void Generate_RuleConsistingOfARepeatedSequenceOfTerminals_Success()
        {
            var text = "syntax = 'a', { 'b' | 'c' }, 'd' ;";

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "ad", "abd", "acd", "abbd", "abcd", "acbd", "accd" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle nested rules consisting of a repeated sequence terminals.
        /// </summary>
        [Test]
        public void Generate_NestedRuleConsistingOfRepeatedSequenceOfTerminals_Success()
        {
            var text = new StringBuilder();
            text.AppendLine("syntax = 'a', rule, 'd' ;");
            text.AppendLine("rule = { 'b' | 'c' } ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "ad", "abd", "acd", "abbd", "abcd", "acbd", "accd" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle nested rules with exceptions.
        /// </summary>
        [Test]
        public void Generate_NestedRuleWithExceptions_Success()
        {
            var text = new StringBuilder();
            text.AppendLine("syntax = rule - 'abc' ;");
            text.AppendLine("rule = ( 'abc' | 'def' | 'ghi' ) - 'ghi', 'jkl' ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();

            var expected = new[] { "abcjkl", "defjkl" };
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// can handle special sequences.
        /// </summary>
        [Test]
        public void Generate_SepcialSequence_Success()
        {
            var text = "syntax = ? digit ? ;";
            var expected = new string[]
            {
                "0", "1", "2", "3", "4",
                "5", "6", "7", "8", "9"
            };

            var sequence = Substitute.For<ISpecialSequenceGenerator>();
            sequence.IsValid(" digit ").Returns(true);
            sequence.Generate(" digit ").Returns(expected);

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();
            generator.SpecialSequenceGenerators.Add(sequence);

            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// generates all the variations of a rule consisting definition list 
        /// of the same rule repeated three times.
        /// </summary>
        [Test]
        public void Generate_TheSameRuleRepeated3Times_Success()
        {
            var expected = new List<string>();
            for (var d1 = 0; d1 < 10; d1++)
            {
                for (var d2 = 0; d2 < 10; d2++)
                {
                    for (var d3 = 0; d3 < 10; d3++)
                        expected.Add(d1.ToString() + d2.ToString() + d3.ToString());
                }
            }

            var text = new StringBuilder();
            text.AppendLine("number = digit, digit, digit ;")
                .AppendLine("digit = '0' | '1'| '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// generates all the variations of a rule consisting definition list 
        /// of the same rule repeated three times using the '*' symbol.
        /// </summary>
        [Test]
        public void Generate_TheSameRuleUsingRepetition_Success()
        {
            var expected = new List<string>();
            for (var d1 = 0; d1 < 10; d1++)
            {
                for (var d2 = 0; d2 < 10; d2++)
                {
                    for (var d3 = 0; d3 < 10; d3++)
                        expected.Add(d1.ToString() + d2.ToString() + d3.ToString());
                }
            }

            var text = new StringBuilder();
            text.AppendLine("number = 3 * digit ;")
                .AppendLine("digit = '0' | '1'| '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// generates all the variations of a rule consisting definition list 
        /// of identical branched groups of terminals.
        /// </summary>
        [Test]
        public void Generate_3IndenticleBranchedGroups_Success()
        {
            var expected = new List<string>();
            for (var d1 = 0; d1 < 10; d1++)
            {
                for (var d2 = 0; d2 < 10; d2++)
                {
                    for (var d3 = 0; d3 < 10; d3++)
                        expected.Add(d1.ToString() + d2.ToString() + d3.ToString());
                }
            }

            var text = new StringBuilder();
            text.AppendLine("number = ('0' | '1'| '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9'),")
                .AppendLine("         ('0' | '1'| '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9'),")
                .AppendLine("         ('0' | '1'| '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9');");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// generates all the variations of a rule consisting definition list 
        /// of two rules. The first has less branches than the second.
        /// </summary>
        [Test]
        public void Generate_FirstRuleHasLessBranchesThanTheSecond_Success()
        {
            var expected = new List<string>();
            for (var c = 'A'; c < 'E'; c++)
            {
                for (var d = 0; d < 10; d++)
                    expected.Add(c.ToString() + d.ToString());
            }

            var text = new StringBuilder();
            text.AppendLine("number = letter, digit ;")
                .AppendLine("digit = '0' | '1'| '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' ;")
                .AppendLine("letter = 'A' | 'B'| 'C' | 'D' ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        /// <summary>
        /// Tests that <see cref="FuzzyTestGenerator.Generate(Syntax)"/>
        /// generates all the variations of a rule consisting definition list 
        /// of two rules. The first has more branches than the second.
        /// </summary>
        [Test]
        public void Generate_FirstRuleHasMoreBranchesThanTheSecond_Success()
        {
            var expected = new List<string>();
            for (var d = 0; d < 10; d++)
            {
                for (var c = 'A'; c < 'E'; c++)
                    expected.Add(d.ToString() + c.ToString());
            }

            var text = new StringBuilder();
            text.AppendLine("number = digit, letter ;")
                .AppendLine("digit = '0' | '1'| '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' ;")
                .AppendLine("letter = 'A' | 'B'| 'C' | 'D' ;");

            var reader = new SyntaxReader();
            var syntax = reader.Read(text.ToStream());
            var generator = new FuzzyTestGenerator();
            var actual = generator.Generate(syntax);

            CollectionAssert.AreEquivalent(expected, actual);
        }
    }
}
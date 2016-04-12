﻿using Pote.Text;
using System;

namespace Spot.SrtL
{
    /// <summary>
    /// Token builder for ebnf tokens.
    /// </summary>
    internal sealed class TokenBuilder : TokenBuilder<TokenBuilder, TokenType>
    {
        private InputPosition position = new InputPosition();

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenBuilder"/> class.
        /// </summary>
        public TokenBuilder()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenBuilder"/> class
        /// with position data.
        /// </summary>
        /// <param name="line">Specifies the line number of the first token.</param>
        /// <param name="column">Specifies the column number of the first token.</param>
        /// <param name="index">Specifies the index of the first token.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="line"/> or <paramref name="column"/>
        /// is less than one. <paramref name="index"/> is less 
        /// than zero.
        /// </exception>
        public TokenBuilder(int line, int column, int index)
        {
            position = new InputPosition(line, column, index);
        }

        /// <summary>
        /// Creates a token of the 'test' keyword.
        /// </summary>
        /// <returns>This builder.</returns>
        public TokenBuilder Test()
        {
            var token = Token("test", TokenType.Keyword, position.DeepCopy());
            position.Advance("test\n");

            return token;
        }

        /// <summary>
        /// Creates a token of the 'input' keyword.
        /// </summary>
        /// <returns>This builder.</returns>
        public TokenBuilder Input()
        {
            var token = Token("input", TokenType.Keyword, position.DeepCopy());
            position.Advance("\tinput ");

            return token;
        }

        /// <summary>
        /// Creates a token of the 'is' keyword.
        /// </summary>
        /// <returns>This builder.</returns>
        public TokenBuilder Is()
        {
            var token = Token("is", TokenType.Keyword, position.DeepCopy());
            position.Advance("\tis ");

            return token;
        }

        /// <summary>
        /// Creates a token of the 'not' keyword.
        /// </summary>
        /// <returns>This builder.</returns>
        public TokenBuilder Not()
        {
            var token = Token("not", TokenType.Keyword, position.DeepCopy());
            position.Advance("not ");

            return token;
        }

        /// <summary>
        /// Creates a token of the 'valid' keyword.
        /// </summary>
        /// <returns>This builder.</returns>
        public TokenBuilder Valid()
        {
            var token = Token("valid", TokenType.Keyword, position.DeepCopy());
            position.Advance("valid\n\n");

            return token;
        }

        /// <summary>
        /// Creates a string token.
        /// </summary>
        /// <param name="text">The content of the string.</param>
        /// <returns>This builder.</returns>
        public TokenBuilder String(string text)
        {
            var token = Token(text, TokenType.String, position.DeepCopy());

            position.Advance('"');
            position.Advance(text);
            position.Advance('"');

            return token;
        }

        /// <summary>
        /// Creates an unknown token.
        /// </summary>
        /// <param name="text">The content of the token.</param>
        /// <returns>This builder.</returns>
        public TokenBuilder Unknown(string text)
        {
            var token = Token(text, TokenType.Unknown, position.DeepCopy());

            position.Advance(text);
            position.Advance(' ');

            return token;
        }
    }
}
// ***********************************************************************
// Copyright (c) 2020 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Missing XML Docs
#pragma warning disable 1591

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter
{
    public class TestFilterParser
    {
        private Tokenizer _tokenizer;

        private static readonly Token LPAREN = new Token(TokenKind.Symbol, "(");
        private static readonly Token RPAREN = new Token(TokenKind.Symbol, ")");
        private static readonly Token AND_OP = new Token(TokenKind.Symbol, "&");
        private static readonly Token OR_OP = new Token(TokenKind.Symbol, "|");
        private static readonly Token NOT_OP = new Token(TokenKind.Symbol, "!");

        private static readonly Token EQ_OP = new Token(TokenKind.Symbol, "=");
        private static readonly Token NE_OP = new Token(TokenKind.Symbol, "!=");
        private static readonly Token CONTAINS_OP = new Token(TokenKind.Symbol, "~");
        private static readonly Token NOTCONTAINS_OP = new Token(TokenKind.Symbol, "!~");

        private static readonly Token[] AND_OPS = new Token[] { AND_OP };
        private static readonly Token[] OR_OPS = new Token[] { OR_OP };
        private static readonly Token[] EQ_OPS = new Token[] { EQ_OP };
        private static readonly Token[] REL_OPS = new Token[] { EQ_OP, NE_OP, CONTAINS_OP, NOTCONTAINS_OP };

        private static readonly Token EOF = new Token(TokenKind.Eof);

        public string Parse(string input)
        {
            _tokenizer = new Tokenizer(input);

            if (_tokenizer.LookAhead == EOF)
                throw new TestFilterParserException("No input provided for test selection.");

            var result = ParseFilterExpression();

            Expect(EOF);
            return result;
        }

        /// <summary>
        /// Parse a single term or an or expression, returning the xml
        /// </summary>
        /// <returns></returns>
        public string ParseFilterExpression()
        {
            var terms = new List<string>();
            terms.Add(ParseFilterTerm());

            while (LookingAt(OR_OPS))
            {
                NextToken();
                terms.Add(ParseFilterTerm());
            }

            if (terms.Count == 1)
                return terms[0];

            var sb = new StringBuilder("<or>");

            foreach (string term in terms)
                sb.Append(term);

            sb.Append("</or>");

            return sb.ToString();
        }

        /// <summary>
        /// Parse a single element or an and expression and return the xml
        /// </summary>
        public string ParseFilterTerm()
        {
            var elements = new List<string>();
            elements.Add(ParseFilterCondition());

            while (LookingAt(AND_OPS))
            {
                NextToken();
                elements.Add(ParseFilterCondition());
            }

            if (elements.Count == 1)
                return elements[0];

            var sb = new StringBuilder("<and>");

            foreach (string element in elements)
                sb.Append(element);

            sb.Append("</and>");

            return sb.ToString();
        }

        /// <summary>
        /// Parse a single filter element such as a category expression
        /// and return the xml representation of the filter.
        /// </summary>
        public string ParseFilterCondition()
        {
            if (LookingAt(LPAREN, NOT_OP))
                return ParseExpressionInParentheses();

            Token lhs = Expect(TokenKind.Word);

            if (!LookingAt(REL_OPS))
                return EmitFullNameFilter(CONTAINS_OP, lhs.Text);

            Token op = Expect(REL_OPS);
            Token rhs;

            switch (lhs.Text)
            {
                case "FullyQualifiedName":
                    rhs = Expect(TokenKind.FQN, TokenKind.Word);
                    return EmitFullNameFilter(op, rhs.Text);
                case "TestCategory":
                    rhs = Expect(TokenKind.Word);
                    return EmitCategoryFilter(op, rhs.Text);
                case "Priority":
                    rhs = Expect(TokenKind.Word);
                    return EmitPropertyFilter(op, lhs.Text, rhs.Text);
                case "Name":
                    rhs = Expect(TokenKind.FQN, TokenKind.Word);
                    return EmitNameFilter(op, rhs.Text);

                default:
                    // Assume it's a property name
                    // op = Expect(REL_OPS);
                    // rhs = Expect(TokenKind.String, TokenKind.Word);
                    // return EmitPropertyElement(op, lhs, rhs);
                    throw InvalidTokenError(lhs);
            }
        }

        private static string EmitFullNameFilter(Token op, string value)
        {
            return EmitFilter("test", op, value);
        }

        private static string EmitCategoryFilter(Token op, string value)
        {
            return EmitFilter("cat", op, value);
        }

        private static string EmitNameFilter(Token op, string value)
        {
            return EmitFilter("name", op, value);
        }
        
        private static string EmitFilter(string lhs, Token op, string rhs)
        {
            rhs = EscapeRhsValue(op, rhs);

            if (op == EQ_OP)
                return $"<{lhs}>{rhs}</{lhs}>";
            if (op == NE_OP)
                return $"<not><{lhs}>{rhs}</{lhs}></not>";
            if (op == CONTAINS_OP)
                return $"<{lhs} re='1'>{rhs}</{lhs}>";
            if (op == NOTCONTAINS_OP)
                return $"<not><{lhs} re='1'>{rhs}</{lhs}></not>";

            throw new TestFilterParserException($"Invalid operator {op.Text} at position {op.Pos}");
        }

        private static string EmitPropertyFilter(Token op, string name, string value)
        {
            value = EscapeRhsValue(op, value);

            if (op == EQ_OP)
                return $"<prop name='{name}'>{value}</prop>";
            if (op == NE_OP)
                return $"<not><prop name='{name}'>{value}</prop></not>";
            if (op == CONTAINS_OP)
                return $"<prop name='{name}' re='1'>{value}</prop>";
            if (op == NOTCONTAINS_OP)
                return $"<not><prop name='{name}' re='1'>{value}</prop></not>";

            throw new TestFilterParserException($"Invalid operator {op.Text} at position {op.Pos}");
        }

        private static string EscapeRhsValue(Token op, string rhs)
        {
            if (op == CONTAINS_OP || op == NOTCONTAINS_OP)
                rhs = EscapeRegexChars(rhs);

            return XmlEscape(rhs);
        }

        private string ParseExpressionInParentheses()
        {
            Token op = Expect(LPAREN, NOT_OP);

            if (op == NOT_OP) Expect(LPAREN);

            string result = ParseFilterExpression();

            Expect(RPAREN);

            if (op == NOT_OP)
                result = "<not>" + result + "</not>";

            return result;
        }

        // Require a token of one or more kinds
        private Token Expect(params TokenKind[] kinds)
        {
            Token token = NextToken();

            foreach (TokenKind kind in kinds)
                if (token.Kind == kind)
                    return token;

            throw InvalidTokenError(token);
        }

        // Require a token from a list of tokens
        private Token Expect(params Token[] valid)
        {
            Token token = NextToken();

            foreach (Token item in valid)
                if (token == item)
                    return token;

            throw InvalidTokenError(token);
        }

        private Exception InvalidTokenError(Token token)
        {
            return new TestFilterParserException(string.Format(
                $"Unexpected {token.Kind} '{token.Text}' at position {token.Pos} in selection expression."));
        }

        private Token LookAhead
        {
            get { return _tokenizer.LookAhead; }
        }

        private bool LookingAt(params Token[] tokens)
        {
            foreach (Token token in tokens)
                if (LookAhead == token)
                    return true;

            return false;
        }

        private Token NextToken()
        {
            return _tokenizer.NextToken();
        }

        // Since we use a regular expression to implement
        // the contains operator, we must escape all chars
        // that have a special meaning in a regex.
        private const string REGEX_CHARS = @".[]{}()*+?|^$\";

        private static string EscapeRegexChars(string input)
        {
            var sb = new StringBuilder();

            foreach (char c in input)
            {
                if (REGEX_CHARS.Contains(c))
                    sb.Append('\\');
                sb.Append(c);
            }

            return sb.ToString();
        }

        // Since the NUnit filter is represented in XML
        // contents of each element must be escaped.
        private static string XmlEscape(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&apos;");
        }
    }
}

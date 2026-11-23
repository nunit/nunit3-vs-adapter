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
using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

// Missing XML Docs
#pragma warning disable 1591

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

public class LegacyTestFilterParser : ITestFilterParser
{
    private Tokenizer _tokenizer;

    private static readonly Token LPAREN = new(TokenKind.Symbol, "(");
    private static readonly Token RPAREN = new(TokenKind.Symbol, ")");
    private static readonly Token AND_OP = new(TokenKind.Symbol, "&");
    private static readonly Token OR_OP = new(TokenKind.Symbol, "|");
    private static readonly Token NOT_OP = new(TokenKind.Symbol, "!");

    private static readonly Token EQ_OP = new(TokenKind.Symbol, "=");
    private static readonly Token NE_OP = new(TokenKind.Symbol, "!=");
    private static readonly Token CONTAINS_OP = new(TokenKind.Symbol, "~");
    private static readonly Token NOTCONTAINS_OP = new(TokenKind.Symbol, "!~");

    private static readonly Token[] AND_OPS = { AND_OP };
    private static readonly Token[] OR_OPS = { OR_OP };
    private static readonly Token[] EQ_OPS = { EQ_OP };
    private static readonly Token[] REL_OPS = { EQ_OP, NE_OP, CONTAINS_OP, NOTCONTAINS_OP };

    private static readonly Token EOF = new(TokenKind.Eof);

    public ITestFilter Parse(string filter)
    {
            _tokenizer = new Tokenizer(filter);

            if (_tokenizer.LookAhead == EOF)
                throw new TestFilterParserException("No input provided for test selection.");

            var expression = ParseFilterExpression();

            Expect(EOF);
            return new TestFilter(expression);
        }

    /// <summary>
    /// Parse a single term or an or expression, returning the xml.
    /// </summary>
    public ITestFilterExpression ParseFilterExpression()
    {
            var terms = new List<ITestFilterTerm> { ParseFilterTerm() };

            while (LookingAt(OR_OPS))
            {
                NextToken();
                terms.Add(ParseFilterTerm());
            }

            return new TestFilterExpression(terms);
        }

    /// <summary>
    /// Parse a single element or an and expression and return the xml.
    /// </summary>
    public ITestFilterTerm ParseFilterTerm()
    {
            var factors = new List<ITestFilterFactor> { ParseFilterCondition() };

            while (LookingAt(AND_OPS))
            {
                NextToken();
                factors.Add(ParseFilterCondition());
            }

            return new TestFilterTerm(factors);
        }

    /// <summary>
    /// Parse a single filter element such as a category expression
    /// and return the xml representation of the filter.
    /// </summary>
    public ITestFilterFactor ParseFilterCondition()
    {
            if (LookingAt(LPAREN, NOT_OP))
                return ParseExpressionInParentheses();

            var lhs = Expect(TokenKind.Word);

            if (!LookingAt(REL_OPS))
            {
                return new TestFilterCondition(
                    "FullyQualifiedName",
                    TestFilterOperations.Contains,
                    UnEscape(lhs.Text));
            }

            var op = Expect(REL_OPS);
            Token rhs;
            if (lhs.Text is "FullyQualifiedName" or "TestCategory" or "Priority" or "Name")
            {
                rhs = Expect(TokenKind.FQN, TokenKind.Word);
            }
            else
            {
                rhs = Expect(TokenKind.String, TokenKind.FQN, TokenKind.Word);
            }

            return new TestFilterCondition(
                lhs.Text,
                GetOperation(op),
                UnEscape(rhs.Text));
        }
    private static string UnEscape(string rhs) => FullyQualifiedNameFilterParser.Unescape(rhs, strict: true);

    private static ITestFilterOperation GetOperation(Token op)
    {
            if (op == EQ_OP)
                return TestFilterOperations.Equal;
            if (op == NE_OP)
                return TestFilterOperations.NotEqual;
            if (op == CONTAINS_OP)
                return TestFilterOperations.Contains;
            if (op == NOTCONTAINS_OP)
                return TestFilterOperations.NotContains;

            throw new TestFilterParserException($"Invalid operator {op.Text} at position {op.Pos}");
        }

    private ITestFilterFactor ParseExpressionInParentheses()
    {
            var op = Expect(LPAREN, NOT_OP);

            if (op == NOT_OP) Expect(LPAREN);

            var expression = ParseFilterExpression();

            Expect(RPAREN);

            return op == NOT_OP
                ? new TestFilterNegation(expression)
                : new TestFilterGroup(expression);
        }

    // Require a token of one or more kinds
    private Token Expect(params TokenKind[] kinds)
    {
            var token = NextToken();

            if (kinds.Any(kind => token.Kind == kind))
            {
                return token;
            }

            throw InvalidTokenError(token);
        }

    // Require a token from a list of tokens
    private Token Expect(params Token[] valid)
    {
            var token = NextToken();

            if (valid.Any(item => token == item))
            {
                return token;
            }

            throw InvalidTokenError(token);
        }

    private Exception InvalidTokenError(Token token)
    {
            return new TestFilterParserException(string.Format(
                $"Unexpected {token.Kind} '{token.Text}' at position {token.Pos} in selection expression."));
        }

    private Token LookAhead => _tokenizer.LookAhead;

    private bool LookingAt(params Token[] tokens)
    {
            return tokens.Any(token => LookAhead == token);
        }

    private Token NextToken()
    {
            return _tokenizer.NextToken();
        }
}

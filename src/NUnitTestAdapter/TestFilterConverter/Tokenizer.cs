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
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

#if !NET6_0_OR_GREATER
#pragma warning disable CA1510
#endif


public enum TokenKind
{
    Eof,
    Word,
    FQN,
    String, // Unused
    Symbol
}

public class Token(TokenKind kind, string text)
{
    public Token(TokenKind kind) : this(kind, string.Empty) { }

    public Token(TokenKind kind, char ch) : this(kind, ch.ToString()) { }

    public TokenKind Kind { get; } = kind;

    public string Text { get; } = text;

    public int Pos { get; set; }

    #region Equality Overrides

    public override bool Equals(object obj) => obj is Token token && this == token;

    public override int GetHashCode()
    {
        return Text.GetHashCode();
    }

    public override string ToString()
    {
        return Text != null
            ? Kind + ":" + Text
            : Kind.ToString();
    }

    public static bool operator ==(Token t1, Token t2)
    {
        bool t1Null = ReferenceEquals(t1, null);
        bool t2Null = ReferenceEquals(t2, null);

        return (t1Null && t2Null) || (!t1Null && !t2Null && (t1.Kind == t2.Kind && t1.Text == t2.Text));
    }

    public static bool operator !=(Token t1, Token t2) => !(t1 == t2);

    #endregion
}

/// <summary>
/// Tokenizer class performs lexical analysis for the TestSelectionParser.
/// It recognizes a very limited set of tokens: words, symbols and
/// quoted strings. This is sufficient for the simple DSL we use to
/// select which tests to run.
/// </summary>
public class Tokenizer
{
    private readonly string input;
    private int index;

    private const char EOF_CHAR = '\0';
    private const string WORD_BREAK_CHARS = "=~!()&|";
    private const string VSTEST_ESCAPED_CHARS = @"\()&|=!~";
    private readonly string[] doubleCharSymbols = { "!=", "!~" };

    private Token lookahead;

    /// <summary>
    /// The Tokenizer class performs lexical analysis for the TestSelectionParser.
    /// It recognizes a very limited set of tokens: words, symbols and
    /// quoted strings. This is sufficient for the simple DSL we use to
    /// select which tests to run.
    /// </summary>
    public Tokenizer(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        this.input = input;
    }

    public Token LookAhead
    {
        get
        {
            if (lookahead == null)
                lookahead = GetNextToken();

            return lookahead;
        }
    }

    public Token NextToken()
    {
        Token result = lookahead ?? GetNextToken();
        lookahead = null;
        return result;
    }

    private Token GetNextToken()
    {
        SkipBlanks();

        var ch = NextChar;
        int pos = index;

        switch (ch)
        {
            case EOF_CHAR:
                return new Token(TokenKind.Eof) { Pos = pos };

            // Single char symbols
            case '(':
            case ')':
            case '~':
            case '&':
            case '|':
            case '=':
                GetChar();
                return new Token(TokenKind.Symbol, ch) { Pos = pos };

            // Could be alone or start of a double char symbol
            case '!':
                GetChar();
                foreach (string dbl in doubleCharSymbols)
                {
                    if (ch != dbl[0] || NextChar != dbl[1])
                        continue;
                    GetChar();
                    return new Token(TokenKind.Symbol, dbl) { Pos = pos };
                }

                return new Token(TokenKind.Symbol, ch);

            default:
                // This is the only place in the tokenizer where
                // we don't know what we are getting at the start
                // of the input string. To avoid modifying the
                // overall design of the parser, the tokenizer
                // will return either a Word or an FQN and the
                // parser grammar has been changed to accept either
                // one of them in certain places.
                return GetWordOrFqn();
        }
    }

    /// <summary>
    /// Collects a value up to the next unescaped operator, as the test platform does:
    /// a '\x' escape pair is always part of the value, and spaces are too. The value is
    /// trimmed. An unescaped '(' inside a value starts an argument list that is balanced
    /// as before, so unescaped filters such as Name=Foo(1) keep working. The result is an
    /// FQN token when it has such an argument list, otherwise a Word.
    /// </summary>
    private Token GetWordOrFqn()
    {
        var sb = new StringBuilder();
        int pos = index;
        bool hasArgumentList = false;

        while (NextChar != EOF_CHAR)
        {
            char c = NextChar;
            if (c == '\\')
            {
                sb.Append(GetChar());
                if (NextChar != EOF_CHAR)
                    sb.Append(GetChar());
            }
            else if (c == '(')
            {
                hasArgumentList = true;
                CollectBalancedParentheticalExpression(sb);
            }
            else if (WORD_BREAK_CHARS.IndexOf(c) >= 0)
            {
                break;
            }
            else
            {
                sb.Append(GetChar());
            }
        }

        return new Token(hasArgumentList ? TokenKind.FQN : TokenKind.Word, sb.ToString().TrimEnd()) { Pos = pos };
    }

    /// <summary>
    /// Collects an argument list. A '"' starts a string argument, so parentheses inside it do
    /// not count. Not every '"' does: a char argument '"', or a custom test name with a single
    /// quote, has no closing one. When a string runs to the end of input, the argument list is
    /// collected again without string handling, so the rest of the filter keeps its conditions.
    /// </summary>
    private void CollectBalancedParentheticalExpression(StringBuilder sb)
    {
        if (NextChar != '(')
            return;

        int start = index;
        int length = sb.Length;
        if (!TryCollectParenthetical(sb, withStrings: true))
        {
            index = start;
            sb.Length = length;
            TryCollectParenthetical(sb, withStrings: false);
        }
    }

    /// <returns>False if a string argument was not closed before end of input.</returns>
    private bool TryCollectParenthetical(StringBuilder sb, bool withStrings)
    {
        int depth = 0;
        do
        {
            var c = GetChar();
            sb.Append(c);
            if (c == '\\' && NextChar != EOF_CHAR)
                sb.Append(GetChar());
            else if (c == '(')
                ++depth;
            else if (c == ')')
                --depth;
            else if (c == '"' && withStrings && !CollectQuotedString(sb))
                return false;
        }
        while (depth > 0 && NextChar != EOF_CHAR);
        return true;
    }

    /// <summary>
    /// Copies a quoted argument as is.
    /// The text is escaped twice: NUnit escapes '\' and '"' in string arguments,
    /// then VSTest escapes '\' and the filter operators. VSTest pairs are read
    /// as one char, and only an NUnit '\"' does not end the string.
    /// </summary>
    /// <returns>False if end of input came before the closing quote.</returns>
    private bool CollectQuotedString(StringBuilder sb)
    {
        bool nunitEscape = false;
        while (NextChar != EOF_CHAR)
        {
            char c = GetChar();
            sb.Append(c);
            if (c == '\\' && VSTEST_ESCAPED_CHARS.IndexOf(NextChar) >= 0)
                sb.Append(c = GetChar());

            if (c == '"' && !nunitEscape)
                return true;
            nunitEscape = c == '\\' && !nunitEscape;
        }
        return false;
    }

    /// <summary>
    /// Get the next character in the input, consuming it.
    /// </summary>
    /// <returns>The next char.</returns>
    private char GetChar() => index < input.Length ? input[index++] : EOF_CHAR;

    /// <summary>
    /// Peek ahead at the next character in input.
    /// </summary>
    private char NextChar => index < input.Length ? input[index] : EOF_CHAR;

    private void SkipBlanks()
    {
        while (char.IsWhiteSpace(NextChar))
            index++;
    }
}


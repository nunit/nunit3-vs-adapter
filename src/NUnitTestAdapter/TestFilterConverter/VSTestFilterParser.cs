#nullable enable

using System.Collections.Generic;
using System.Text;

using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

public class VSTestFilterParser : ITestFilterParser
{
    public ITestFilter Parse(string filter)
    {
        if (filter is null)
        {
            throw Error("A filter cannot be null", 0);
        }

        return new Parser(filter, SplitExpression(filter)).Parse();
    }

    private static List<Part> SplitExpression(string input)
    {
        var parts = new List<Part>();
        int start = 0;
        bool escaped = false;

        for (int i = 0; i < input.Length; i++)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            char character = input[i];
            if (character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character is '(' or ')' or '&' or '|')
            {
                AddExpressionPart(parts, input.Substring(start, i - start), start);
                parts.Add(new Part(character.ToString(), i, isOperator: true));
                start = i + 1;
            }
        }

        AddExpressionPart(parts, input.Substring(start), start);
        return parts;
    }

    private static void AddExpressionPart(List<Part> parts, string text, int position)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            parts.Add(new Part(text, position, isOperator: false));
        }
    }

    private static TestFilterParserException Error(string message, int position)
    {
        return new TestFilterParserException($"{message} at position {position}.");
    }

    private sealed class Parser
    {
        private readonly string _input;
        private readonly IReadOnlyList<Part> _parts;
        private int _index;

        public Parser(string input, IReadOnlyList<Part> parts)
        {
            _input = input;
            _parts = parts;
        }

        public ITestFilter Parse()
        {
            if (!HasPartsToConsume)
            {
                throw Error("No filter expression was provided", 0);
            }

            var expression = ParseExpression();
            if (HasPartsToConsume)
            {
                throw Error($"Unexpected token '{CurrentPartToConsume.Text}'", CurrentPartToConsume.Position);
            }

            return new TestFilter(expression);
        }

        private ITestFilterExpression ParseExpression()
        {
            var terms = new List<ITestFilterTerm> { ParseTerm() };
            while (IsOperator("|"))
            {
                _index++;
                terms.Add(ParseTerm());
            }

            return new TestFilterExpression(terms);
        }

        private ITestFilterTerm ParseTerm()
        {
            var factors = new List<ITestFilterFactor> { ParseFactor() };
            while (IsOperator("&"))
            {
                _index++;
                factors.Add(ParseFactor());
            }

            return new TestFilterTerm(factors);
        }

        private ITestFilterFactor ParseFactor()
        {
            if (!HasPartsToConsume)
            {
                throw Error("Expected a filter condition", _input.Length);
            }

            if (IsOperator("("))
            {
                _index++;
                if (!HasPartsToConsume || IsOperator(")"))
                {
                    throw Error("An empty group is not allowed", HasPartsToConsume ? CurrentPartToConsume.Position : _input.Length);
                }

                var expression = ParseExpression();
                if (!IsOperator(")"))
                {
                    throw Error("Expected a closing parenthesis", HasPartsToConsume ? CurrentPartToConsume.Position : _input.Length);
                }

                _index++;
                return new TestFilterGroup(expression);
            }

            if (CurrentPartToConsume.IsOperator)
            {
                throw Error($"Unexpected token '{CurrentPartToConsume.Text}'", CurrentPartToConsume.Position);
            }

            var condition = CurrentPartToConsume;
            _index++;
            return ParseCondition(condition);
        }

        private static ITestFilterFactor ParseCondition(Part condition)
        {
            var parts = SplitCondition(condition.Text, condition.Position);
            if (parts.Count is 1)
            {
                return new TestFilterCondition(
                    property: "FullyQualifiedName",
                    comparison: TestFilterComparison.Contains,
                    value: Unescape(parts[0]));
            }

            if (parts.Count != 3 || !parts[1].IsOperator)
            {
                throw Error("A condition must contain one property, operator, and value", condition.Position);
            }

            if (parts[0].Text.Length == 0)
            {
                throw Error("A condition is missing its property name", parts[1].Position);
            }

            if (parts[2].Text.Length == 0)
            {
                throw Error("A condition is missing its value", parts[1].Position + parts[1].Text.Length);
            }

            return VSTestFilterOperators.CreateCondition(
                property: Unescape(parts[0]),
                symbol: parts[1].Text,
                value: Unescape(parts[2]));
        }

        private static List<Part> SplitCondition(string input, int position)
        {
            var parts = new List<Part>();
            int start = 0;
            bool escaped = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                char character = input[i];
                if (character == '\\')
                {
                    escaped = true;
                    continue;
                }

                int length = 0;
                if (character is '=' or '~')
                {
                    length = 1;
                }
                else if (character == '!' && i + 1 < input.Length && input[i + 1] is '=' or '~')
                {
                    length = 2;
                }

                if (length is 0)
                {
                    continue;
                }

                AddConditionPart(parts, input.Substring(start, i - start), position + start);
                parts.Add(new Part(input.Substring(i, length), position + i, isOperator: true));
                i += length - 1;
                start = i + 1;
            }

            AddConditionPart(parts, input.Substring(start), position + start);
            return parts;
        }

        private static void AddConditionPart(List<Part> parts, string text, int position)
        {
            int leadingWhitespace = text.Length - text.TrimStart().Length;
            parts.Add(new Part(text.Trim(), position + leadingWhitespace, isOperator: false));
        }

        private static string Unescape(Part part)
        {
            var value = new StringBuilder();
            for (int i = 0; i < part.Text.Length; i++)
            {
                char character = part.Text[i];
                if (character != '\\')
                {
                    value.Append(character);
                    continue;
                }

                if (i + 1 >= part.Text.Length)
                {
                    throw Error("A filter cannot end with an escape character", part.Position + i);
                }

                // Escape sequences are only defined in this MSDN:
                // https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=nunit#character-escaping
                char escaped = part.Text[++i];
                if (escaped is not '\\'
                    and not '('
                    and not ')'
                    and not '&'
                    and not '|'
                    and not '='
                    and not '!'
                    and not '~')
                {
                    throw Error($"Invalid escape sequence '\\{escaped}'", part.Position + i - 1);
                }

                value.Append(escaped);
            }

            return value.ToString();
        }

        private bool HasPartsToConsume => _index < _parts.Count;

        private Part CurrentPartToConsume => _parts[_index];

        private bool IsOperator(string value)
        {
            return HasPartsToConsume && CurrentPartToConsume.IsOperator && CurrentPartToConsume.Text == value;
        }
    }

    private sealed class Part
    {
        public Part(string text, int position, bool isOperator)
        {
            Text = text;
            Position = position;
            IsOperator = isOperator;
        }

        public string Text { get; }

        public int Position { get; }

        public bool IsOperator { get; }
    }
}

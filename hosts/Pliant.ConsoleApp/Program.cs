﻿using Pliant.Builders;
using Pliant.Grammars;
using Pliant.Tokens;
using System.IO;
using System.Text;

namespace Pliant.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var whitespaceTerminal = new WhitespaceTerminal();
            var whitespace = new GrammarLexerRule(
                "whitespace",
                new GrammarBuilder("whitespace")
                    .Production("whitespace", r => r
                        .Rule(whitespaceTerminal, "whitespace")
                        .Rule(whitespaceTerminal))
                    .ToGrammar());

            var ruleName = new GrammarLexerRule(
                "rule-name",
                new GrammarBuilder("rule-name")
                    .Production("rule-name", r => r
                        .Rule("letter", "zeroOrManyLetterOrDigit"))
                    .Production("zeroOrManyLetterOrDigit", r => r
                        .Rule("letterOrDigit", "zeroOrManyLetterOrDigit")
                        .Lambda())
                    .Production("letterOrDigit", r => r
                        .Rule("letter")
                        .Rule("digit")
                        .Rule("special"))
                    .Production("letter", r => r
                        .Rule(new RangeTerminal('a', 'z'))
                        .Rule(new RangeTerminal('A', 'Z')))
                    .Production("digit", r => r
                        .Rule(new DigitTerminal()))
                    .Production("special", r => r
                        .Rule(new SetTerminal('-', '_')))
                    .ToGrammar());

            var implements = new StringLiteralLexerRule("::=", new TokenType("implements"));
            var eol = new StringLiteralLexerRule("\r\n", new TokenType("eol"));

            var grammarBuilder = new GrammarBuilder("syntax")
                .Production("syntax", r => r
                    .Rule("syntax")
                    .Rule("rule", "syntax"))
                .Production("rule", r => r
                    .Rule("identifier", implements, "expression", "line-end"))
                .Production("expression", r => r
                    .Rule("list")
                    .Rule("list", '|', "expression"))
                .Production("line-end", r => r
                    .Rule(eol)
                    .Rule("line-end", "line-end"))
                .Production("list", r => r
                    .Rule("term")
                    .Rule("term", "list"))
                .Production("term", r => r
                    .Rule("literal")
                    .Rule("identifier"))
                .Production("identifier", r => r
                    .Rule('<', ruleName, '>'))
                .Production("literal", r => r
                    .Rule('"', "doubleQuoteText", '"')
                    .Rule('\'', "singleQuoteText", '\''))
                .Production("doubleQuoteText", r => r
                    .Rule("doubleQuoteText", new NegationTerminal(new Terminal('"')))
                    .Lambda())
                .Production("singleQuoteText", r => r
                    .Rule("singleQuoteText", new NegationTerminal(new Terminal('\'')))
                    .Lambda())
                .Ignore(whitespace);
            var grammar = grammarBuilder.ToGrammar();

            var sampleBnf = @"
            <syntax>         ::= <rule> | <rule> <syntax>
            <rule>           ::= <identifier> ""::="" <expression> <line-end>
            <expression>     ::= <list> | <list> ""|"" <expression>
            <line-end>       ::= <EOL> | <line-end> <line-end>
            <list>           ::= <term > | <term> <list>
            <term>           ::= <literal > | <identifier>
            <identifier>     ::= ""<"" <rule-name> "">""
            <literal>        ::= '""' <text> '""' | ""'"" <text> ""'""";
            var parseEngine = new ParseEngine(grammar);
            var parseInterface = new ParseInterface(parseEngine, sampleBnf);
            var stringReader = new StringReader(sampleBnf);

            while (!parseInterface.EndOfStream())
            {
                if (!parseInterface.Read())
                {
                    var position = parseInterface.Position;
                    var startIndex = 0;
                    for (int i = position; i >= 0; i--)
                    {
                        if (sampleBnf[i] == '\n' && i > 0)
                            if (sampleBnf[i - 1] == '\r')
                            {
                                startIndex = i;
                                break;
                            }
                    }
                    var endIndex = sampleBnf.IndexOf("\r\n", position);
                    endIndex = endIndex < 0 ? sampleBnf.Length : endIndex;
                    var length = endIndex - startIndex;
                    var stringBuilder = new StringBuilder();
                    stringBuilder
                        .AppendFormat("Error parsing input string at position {0}.", parseInterface.Position)
                        .AppendLine()
                        .AppendFormat("start: {0}", startIndex)
                        .AppendLine()
                        .AppendLine(sampleBnf.Substring(startIndex, length));
                }
            }
            parseInterface.ParseEngine.IsAccepted();
        }
    }
}

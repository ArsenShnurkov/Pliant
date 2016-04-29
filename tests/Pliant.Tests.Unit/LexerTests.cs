﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pliant.Builders;
using Pliant.Charts;
using Pliant.Grammars;
using Pliant.Tokens;
using System;

namespace Pliant.Tests.Unit
{
    [TestClass]
    public class LexerTests
    {
        private GrammarLexerRule _whitespaceRule;
        private GrammarLexerRule _wordRule;

        public LexerTests()
        {
            _whitespaceRule = CreateWhitespaceRule();
            _wordRule = CreateWordRule();
        }

        private static GrammarLexerRule CreateWhitespaceRule()
        {
            ProductionBuilder S = "S", whitespace = "whitespace";

            S.Definition =
                whitespace
                | whitespace + S;
            whitespace.Definition =
                new WhitespaceTerminal();

            var grammar = new GrammarBuilder(S, new[] { S, whitespace }).ToGrammar();
            return new GrammarLexerRule(nameof(whitespace), grammar);
        }

        private static GrammarLexerRule CreateWordRule()
        {
            ProductionBuilder W = "W", word = "word";
            W.Definition =
                word
                | word + W;
            word.Definition = (_)
                new RangeTerminal('a', 'z')
                | new RangeTerminal('A', 'Z')
                | new RangeTerminal('0', '9');

            var wordGrammar = new GrammarBuilder(W, new[] { W, word }).ToGrammar();
            return new GrammarLexerRule(nameof(word), wordGrammar);
        }

        [TestMethod]
        public void LexerShouldParseSimpleWordSentence()
        {
            ProductionBuilder S = "S";
            S.Definition =
                _whitespaceRule
                | _whitespaceRule + S
                | _wordRule
                | _wordRule + S;
            var grammar = new GrammarBuilder(S, new[] { S }).ToGrammar();
            var input = "this is input";
            var parseEngine = new ParseEngine(grammar);
            RunParse(parseEngine, input);
        }

        [TestMethod]
        public void LexerShouldIgnoreWhitespace()
        {
            // a <word boundary> abc <word boundary> a <word boundary> a
            const string input = "a abc a a";
            ProductionBuilder A = "A";
            A.Definition =
                _wordRule + A
                | _wordRule;
            var grammar = new GrammarBuilder(
                A,
                new[] { A },
                new[] { _whitespaceRule })
                .ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            RunParse(parseEngine, input);
        }

        [TestMethod]
        public void LexerShouldEmitTokenBetweenLexerRulesAndEndOfFile()
        {
            const string input = "aa";
            ProductionBuilder S = "S";
            S.Definition = 'a' + S | 'a';
            var grammar = new GrammarBuilder(S, new[] { S }).ToGrammar();
            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, input);

            var chart = GetParseEngineChart(parseEngine);
            Assert.IsTrue(lexer.Read());
            Assert.AreEqual(1, chart.EarleySets.Count);
            Assert.IsTrue(lexer.Read());
            Assert.AreEqual(3, chart.EarleySets.Count);
        }

        [TestMethod]
        public void LexerShouldUseExistingMatchingLexemesToPerformMatch()
        {
            const string input = "aaaa";

            ProductionBuilder A = "A";
            A.Definition = (_)'a' + 'a';
            var aGrammar = new GrammarBuilder(A, new[] { A }).ToGrammar();
            var a = new GrammarLexerRule("a", aGrammar);

            ProductionBuilder S = "S";
            S.Definition = a + S | a;
            var grammar = new GrammarBuilder(S, new[] { S }).ToGrammar();
            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, input);

            var chart = GetParseEngineChart(parseEngine);
            Assert.IsTrue(lexer.Read());
            Assert.AreEqual(1, chart.EarleySets.Count);
            Assert.IsTrue(lexer.Read());
            Assert.AreEqual(1, chart.EarleySets.Count);
        }

        [TestMethod]
        public void LexerWhenNoLexemesMatchCharacterShouldCreateNewLexeme()
        {
            const string input = "aaaa";

            ProductionBuilder A = "A", S = "S";

            A.Definition = (_)'a' + 'a';
            var aGrammar = new GrammarBuilder(A, new[] { A }).ToGrammar();
            var a = new GrammarLexerRule("a", aGrammar);

            S.Definition = a + S | a;
            var grammar = new GrammarBuilder(S, new[] { S }).ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, input);

            var chart = GetParseEngineChart(parseEngine);
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(lexer.Read());
            Assert.AreEqual(2, chart.EarleySets.Count);
        }

        [TestMethod]
        public void LexerShouldEmitTokenWhenIgnoreCharacterIsEncountered()
        {
            const string input = "aa aa";
            ProductionBuilder S = "S";

            S.Definition = _wordRule + S | _wordRule;

            var grammar = new GrammarBuilder(
                S,
                new[] { S },
                new[] { _whitespaceRule })
                .ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, input);
            var chart = GetParseEngineChart(parseEngine);
            for (int i = 0; i < 2; i++)
                Assert.IsTrue(lexer.Read());
            Assert.IsTrue(lexer.Read());
            Assert.AreEqual(2, chart.EarleySets.Count);
        }

        [TestMethod]
        public void LexerShouldEmitTokenWhenCharacterMatchesNextProduction()
        {
            const string input = "aabb";
            ProductionBuilder A = "A";
            A.Definition =
                'a' + A
                | 'a';
            var aGrammar = new GrammarBuilder(A, new[] { A }).ToGrammar();
            var a = new GrammarLexerRule("a", aGrammar);

            ProductionBuilder B = "B";
            B.Definition =
                'b' + B
                | 'b';
            var bGrammar = new GrammarBuilder(B, new[] { B }).ToGrammar();
            var b = new GrammarLexerRule("b", bGrammar);

            ProductionBuilder S = "S";
            S.Definition = (_)
                a + b;
            var grammar = new GrammarBuilder(S, new[] { S }).ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            var chart = GetParseEngineChart(parseEngine);
            var parseRunner = new ParseRunner(parseEngine, input);
            for (int i = 0; i < input.Length; i++)
            {
                Assert.IsTrue(lexer.Read());
                if (i < 2)
                    Assert.AreEqual(1, chart.Count);
                else if (i < 3)
                    Assert.AreEqual(2, chart.Count);
                else
                    Assert.AreEqual(3, chart.Count);
            }
        }

        [TestMethod]
        public void LexerGivenIgnoreCharactersWhenOverlapWithTerminalShouldChooseTerminal()
        {
            var input = "word \t\r\n word";

            var endOfLine = new StringLiteralLexerRule(
                Environment.NewLine,
                new TokenType("EOL"));
            ProductionBuilder S = "S";
            S.Definition = (_)_wordRule + endOfLine + _wordRule;
            var grammar = new GrammarBuilder(
                S,
                new[] { S },
                new[] { _whitespaceRule })
                .ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            RunParse(parseEngine, input);
        }

        private static Chart GetParseEngineChart(ParseEngine parseEngine)
        {
            return new PrivateObject(parseEngine).GetField("_chart") as Chart;
        }

        private static void RunParse(ParseEngine parseEngine, string input)
        {
            var parseRunner = new ParseRunner(parseEngine, input);
            for (int i = 0; i < input.Length; i++)
                Assert.IsTrue(lexer.Read(), $"Error parsing at position {i}");
            Assert.IsTrue(lexer.ParseEngine.IsAccepted());
        }
    }
}
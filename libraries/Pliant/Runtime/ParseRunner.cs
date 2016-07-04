﻿using Pliant.Automata;
using Pliant.Collections;
using Pliant.Grammars;
using Pliant.Lexemes;
using Pliant.Tokens;
using Pliant.Utilities;
using System.Collections.Generic;
using System.IO;

namespace Pliant.Runtime
{
    public class ParseRunner : IParseRunner
    {
        private List<ILexeme> _existingLexemes;
        private List<ILexeme> _ignoreLexemes;
        private readonly ILexemeFactoryRegistry _lexemeFactoryRegistry;
        private readonly ObjectPool<List<ILexeme>> _lexemeListPool;
        private readonly ObjectPool<List<ILexerRule>> _lexerRuleListPool;

        private readonly TextReader _textReader;

        public IParseEngine ParseEngine { get; private set; }

        public int Position { get; private set; }

        public ParseRunner(IParseEngine parseEngine, string input)
                            : this(parseEngine, new StringReader(input))
        {
        }

        public ParseRunner(IParseEngine parseEngine, TextReader input)
        {
            _textReader = input;

            _lexemeFactoryRegistry = new LexemeFactoryRegistry();
            RegisterDefaultLexemeFactories(_lexemeFactoryRegistry);

            _ignoreLexemes = new List<ILexeme>();
            _existingLexemes = new List<ILexeme>();

            _lexemeListPool = new ObjectPool<List<ILexeme>>();
            _lexerRuleListPool = new ObjectPool<List<ILexerRule>>();

            Position = 0;
            ParseEngine = parseEngine;
        }

        public bool EndOfStream()
        {
            return _textReader.Peek() == -1;
        }

        public bool Read()
        {
            if (EndOfStream())
                return false;

            var character = ReadCharacter();

            if (MatchesExistingLexemes(character))
            {
                if (!EndOfStream())
                    return true;
                return TryParseExistingToken();
            }

            if (AnyExistingLexemes())
                if (!TryParseExistingToken())
                    return false;

            if (MatchesNewLexemes(character))
            {
                if (!EndOfStream())
                    return true;
                return TryParseExistingToken();
            }

            if (MatchesExistingIgnoreLexemes(character))
                return true;

            ClearExistingIngoreLexemes();

            if (MatchesNewIgnoreLexemes(character))
                return true;

            return false;
        }

        private static void RegisterDefaultLexemeFactories(ILexemeFactoryRegistry lexemeFactoryRegistry)
        {
            lexemeFactoryRegistry.Register(new TerminalLexemeFactory());
            lexemeFactoryRegistry.Register(new ParseEngineLexemeFactory());
            lexemeFactoryRegistry.Register(new StringLiteralLexemeFactory());
            lexemeFactoryRegistry.Register(new DfaLexemeFactory());
        }
        private bool AnyExistingLexemes()
        {
            return _existingLexemes.Count > 0;
        }

        private void ClearExistingIngoreLexemes()
        {
            _ignoreLexemes.Clear();
        }

        private void ClearExistingLexemes()
        {
            _existingLexemes.Clear();
        }

        private ILexeme CreateLexemeForLexerRule(ILexerRule lexerRule)
        {
            return _lexemeFactoryRegistry
                .Get(lexerRule.LexerRuleType)
                .Create(lexerRule);
        }

        private IToken CreateTokenFromLexeme(ILexeme lexeme)
        {
            return new Token(
                lexeme.Capture,
                Position - lexeme.Capture.Length - 1,
                lexeme.TokenType);
        }

        private bool MatchesExistingIgnoreLexemes(char character)
        {
            if (_ignoreLexemes.Count == 0)
                return false;

            var anyMatchedIgnoreLexemes = false;
            foreach (var existingLexeme in _ignoreLexemes)
            {
                if (existingLexeme.Scan(character))
                {
                    anyMatchedIgnoreLexemes = true;
                }
            }
            return anyMatchedIgnoreLexemes;
        }

        private bool MatchesExistingLexemes(char character)
        {
            if (!AnyExistingLexemes())
                return false;
            var matchedLexemes = _lexemeListPool.AllocateAndClear();
            var anyMatchedLexemes = false;
            foreach (var existingLexeme in _existingLexemes)
            {
                if (existingLexeme.Scan(character))
                {
                    matchedLexemes.Add(existingLexeme);
                    anyMatchedLexemes = true;
                }
            }
            if (!anyMatchedLexemes)
                return false;
            _lexemeListPool.Free(_existingLexemes);
            _existingLexemes = matchedLexemes;
            return true;
        }

        private bool MatchesNewIgnoreLexemes(char character)
        {
            if (ParseEngine.Grammar.Ignores.Count == 0)
                return false;

            var ignoreLexerRules = _lexerRuleListPool.AllocateAndClear();
            // PERF: Avoid IEnumerable<T> boxing by calling AddRange
            // PERF: Avoid foreach loop due to non struct boxing
            for (int i = 0; i < ParseEngine.Grammar.Ignores.Count; i++)
            {
                var ignore = ParseEngine.Grammar.Ignores[i];
                ignoreLexerRules.Add(ignore);
            }

            var matchingIgnoreLexemes = _lexemeListPool.Allocate();
            var anyMatchingIgnoreLexemes = false;
            foreach (var ignoreLexerRule in ignoreLexerRules)
            {
                var lexeme = CreateLexemeForLexerRule(ignoreLexerRule);
                if (lexeme.Scan(character))
                {
                    matchingIgnoreLexemes.Add(lexeme);
                    anyMatchingIgnoreLexemes = true;
                }
            }
            _lexerRuleListPool.Free(ignoreLexerRules);

            if (anyMatchingIgnoreLexemes)
            {
                _lexemeListPool.Free(_ignoreLexemes);
                _ignoreLexemes = matchingIgnoreLexemes;
                return true;
            }
            return false;
        }

        private bool MatchesNewLexemes(char character)
        {
            var newLexemes = _lexemeListPool.AllocateAndClear();
            var anyLexemeScanned = false;
            
            var expectedLexerRules = ParseEngine.GetExpectedLexerRules();
            // PERF: Avoid foreach due to boxing IEnumerable<T>
#pragma warning disable CC0006 // Use foreach
            for (var l = 0; l< expectedLexerRules.Count; l++)
            {
                var lexerRule = expectedLexerRules[l];
                var lexeme = CreateLexemeForLexerRule(lexerRule);
                if (lexeme.Scan(character))
                {
                    anyLexemeScanned = true;
                    newLexemes.Add(lexeme);
                }
            }
#pragma warning restore CC0006 // Use foreach
                        
            if (!anyLexemeScanned)
                return false;
            _lexemeListPool.Free(_existingLexemes);
            _existingLexemes = newLexemes;
            return true;
        }
        private char ReadCharacter()
        {
            var character = (char)_textReader.Read();
            Position++;
            return character;
        }

        private bool TryParseExistingToken()
        {
            // PERF: Avoid Linq FirstOrDefault due to lambda allocation
            ILexeme longestAcceptedMatch = null;
            foreach (var lexeme in _existingLexemes)
                if (lexeme.IsAccepted())
                {
                    longestAcceptedMatch = lexeme;
                    break;
                }

            if (longestAcceptedMatch == null)
                return false;

            var token = CreateTokenFromLexeme(longestAcceptedMatch);
            if (token == null)
                return false;

            if (!ParseEngine.Pulse(token))
                return false;

            ClearExistingLexemes();
            return true;
        }
    }
}
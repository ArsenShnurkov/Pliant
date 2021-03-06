﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pliant.Grammars;
using Pliant.Tokens;

namespace Pliant.Tests.Unit
{
    [TestClass]
    public class TerminalLexemeTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TerminalLexemShouldWhileAcceptedContinuesToMatch()
        {
            var terminalLexeme = new TerminalLexeme(
                new CharacterTerminal('c'),
                new TokenType("c"),
                0);
            Assert.IsFalse(terminalLexeme.IsAccepted());
            Assert.IsTrue(terminalLexeme.Scan('c'));
            Assert.IsTrue(terminalLexeme.IsAccepted());
            Assert.IsFalse(terminalLexeme.Scan('c'));
        }

        [TestMethod]
        public void TerminalLexemeShouldInitializeCatpureToEmptyString()
        {
            var terminalLexeme = new TerminalLexeme(new CharacterTerminal('c'), new TokenType("c"), 0);
            Assert.AreEqual(string.Empty, terminalLexeme.Value);
        }
    }
}
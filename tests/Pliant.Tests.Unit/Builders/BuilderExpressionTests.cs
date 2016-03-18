﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pliant.Builders;

namespace Pliant.Tests.Unit.Builders
{
    [TestClass]
    public class BuilderExpressionTests
    {
        [TestMethod]
        public void BuilderExpressionShouldCastBuilderNonTerminalToBuilderExpression()
        {
            ProductionBuilder term = null;
            RuleBuilder expression = null;
            expression = term;
            Assert.AreEqual(1, expression.Data.Count);
            Assert.AreEqual(1, expression.Data[0].Count);
        }

        [TestMethod]
        public void BuilderExpressionShouldCastStringToBuilderExpression()
        {
            var input = "";
            RuleBuilder expression = null;
            expression = input;
            Assert.AreEqual(1, expression.Data.Count);
            Assert.AreEqual(1, expression.Data[0].Count);
        }

        [TestMethod]
        public void BuilderExpressionShouldCastStringAndString()
        {
            var input1 = "";
            var input2 = "";
            var expression = (_)input1 + input2;
            Assert.AreEqual(1, expression.Data.Count);
            Assert.AreEqual(2, expression.Data[0].Count);
        }

        [TestMethod]
        public void BuilderExpressionShouldCastStringOrString()
        {
            var input1 = "";
            var input2 = "";
            var expression = (_)input1 | input2;
            Assert.AreEqual(2, expression.Data.Count);
            Assert.AreEqual(1, expression.Data[0].Count);
            Assert.AreEqual(1, expression.Data[1].Count);
        }

        [TestMethod]
        public void BuilderExpressionShouldCastComplexRule()
        {
            ProductionBuilder A = "A";
            var expression = (_)"abc" + "def" | "abc" + A;
            Assert.AreEqual(2, expression.Data.Count);
            Assert.AreEqual(2, expression.Data[0].Count);
            Assert.AreEqual(2, expression.Data[1].Count);
        }
    }
}
﻿using Pliant.Builders;
using Pliant.Grammars;
using System.Collections.Generic;

namespace Pliant.Builders.Expressions
{
    public class GrammarExpression
    {
        public GrammarModel GrammarModel { get; private set; }

        public GrammarExpression(
            ProductionExpression start, 
            IEnumerable<ProductionExpression> productions = null, 
            IEnumerable<LexerRuleModel> ignore = null)
        {
            GrammarModel = new GrammarModel
            {
                Start = start.ProductionModel
            };

            if(productions != null)
                foreach (var production in productions)
                    GrammarModel.Productions.Add(production.ProductionModel);

            if(ignore != null)
                foreach (var ignoreRule in ignore)
                    GrammarModel.IgnoreRules.Add(ignoreRule);
        }

        public GrammarExpression(
            ProductionExpression start,
            IEnumerable<ProductionExpression> productions,
            IEnumerable<ILexerRule> ignore)            
        {
            GrammarModel = new GrammarModel
            {
                Start = start.ProductionModel
            };

            if (productions != null)
                foreach (var production in productions)
                    GrammarModel.Productions.Add(production.ProductionModel);
            if (ignore != null)
                foreach (var ignoreRule in ignore)
                    GrammarModel.IgnoreRules.Add(new LexerRuleModel(ignoreRule));
        }

        public IGrammar ToGrammar()
        {
            return GrammarModel.ToGrammar();
        }
    }
}

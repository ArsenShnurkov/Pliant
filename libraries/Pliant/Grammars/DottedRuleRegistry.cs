﻿using System.Collections.Generic;
using Pliant.Collections;

namespace Pliant.Grammars
{
    public abstract class DottedRuleRegistry : IDottedRuleRegistry
    {
        private Dictionary<IProduction, Dictionary<int, IDottedRule>> _dottedRuleIndex;

        public DottedRuleRegistry()
        {
            _dottedRuleIndex = new Dictionary<IProduction, Dictionary<int, IDottedRule>>();
        }

        public void Register(IDottedRule dottedRule)
        {
            var positionIndex = _dottedRuleIndex.AddOrGetExisting(dottedRule.Production);
            positionIndex[dottedRule.Position] = dottedRule;
        }

        public IDottedRule Get(IProduction production, int position)
        {
            Dictionary<int, IDottedRule> positionIndex;
            if (!_dottedRuleIndex.TryGetValue(production, out positionIndex))
                return null;
            IDottedRule dottedRule;
            if (!positionIndex.TryGetValue(position, out dottedRule))
                return null;
            return dottedRule;
        }
    }
}

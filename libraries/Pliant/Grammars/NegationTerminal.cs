﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pliant.Grammars
{
    public class NegationTerminal : BaseTerminal, ITerminal
    {
        ITerminal _innerTerminal;
        public NegationTerminal(ITerminal innerTerminal)
        {
            _innerTerminal = innerTerminal;
        }

        public override bool IsMatch(char character)
        {
            return !_innerTerminal.IsMatch(character);
        }       
    }
}

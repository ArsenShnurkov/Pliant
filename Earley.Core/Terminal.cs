﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Earley
{
    public class Terminal : Symbol
    {
        public Terminal(string value)
            : base(SymbolType.Terminal, value)
        { }
    }
}

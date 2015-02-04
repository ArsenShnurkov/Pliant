﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Earley
{
    public class State : Earley.IState
    {
        private int _count = -1;

        public IProduction Production { get; private set; }
        
        public int Position { get; private set; }
        
        public int Origin { get; private set; }

        public State(IProduction production, int position, int start)
        {
            Assert.IsNotNull(production, "production");
            Assert.IsGreaterThanZero(position, "position");
            Assert.IsGreaterThanZero(start, "start");
            Production = production;
            Position = position;
            Origin = start;
        }
        public bool IsComplete()
        {
            // cache the count because productions are immutable
            if (_count < 0)
                _count = Production.RightHandSide.Count();
            return _count <= Position;
        }

        public ISymbol CurrentSymbol()
        {
            if (IsComplete())
                return null;
            return Production.RightHandSide[Position];
        }

        public ISymbol CompletedSymbol()
        {
            if (Position == 0)
                return null;
            return Production.RightHandSide[Position - 1];
        }

        public override bool Equals(object obj)
        {
            var state = obj as State;
            if (state == null)
                return false;
            return Position == state.Position
                && Origin == state.Origin
                && Production == state.Production;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode()
                ^ Origin.GetHashCode()
                ^ Production.GetHashCode();
        }
    }
}

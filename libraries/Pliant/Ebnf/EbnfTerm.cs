﻿using System;

namespace Pliant.Ebnf
{
    public class EbnfTerm : EbnfNode
    {
        private readonly int _hashCode;

        public EbnfFactor Factor { get; private set; }
        
        public EbnfTerm(EbnfFactor factor)
        {
            Factor = factor;
            _hashCode = ComputeHashCode();
        }

        public override EbnfNodeType NodeType
        {
            get
            {
                return EbnfNodeType.EbnfTerm;
            }
        }

        int ComputeHashCode()
        {
            return HashUtil.ComputeHash(
                NodeType.GetHashCode(),
                Factor.GetHashCode());
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null)
                return false;
            var term = obj as EbnfTerm;
            if ((object)term == null)
                return false;
            return term.NodeType == NodeType
                && term.Factor.Equals(Factor);
        }
    }

    public class EbnfTermConcatenation : EbnfTerm
    {
        private readonly int _hashCode;

        public EbnfTerm Term { get; private set; }

        public EbnfTermConcatenation(EbnfFactor factor, EbnfTerm term)
            : base(factor)
        {
            Term = term;
            _hashCode = ComputeHashCode();
        }

        public override EbnfNodeType NodeType
        {
            get
            {
                return EbnfNodeType.EbnfTermRepetition;
            }
        }

        int ComputeHashCode()
        {
            return HashUtil.ComputeHash(
                NodeType.GetHashCode(),
                Factor.GetHashCode(),
                Term.GetHashCode());
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null)
                return false;
            var term = obj as EbnfTermConcatenation;
            if ((object)term == null)
                return false;
            return term.NodeType == NodeType
                && term.Factor.Equals(Factor)
                && term.Term.Equals(Term);
        }
    }
}
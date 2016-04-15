﻿using Pliant.Charts;
using Pliant.Collections;
using Pliant.Grammars;
using System.Collections.Generic;

namespace Pliant.Forest
{
    public class VirtualNode : NodeBase, ISymbolNode
    {
        private ITransitionState _transitionState;
        private INode _completedParseNode;
        private ReadWriteList<IAndNode> _children;

        /// <summary>
        /// A single AND node. Virtual nodes are leo nodes and by nature don't have ambiguity.
        /// </summary>
        private AndNode _andNode;

        public ISymbol Symbol { get; private set; }

        public VirtualNode(
            int location,
            ITransitionState transitionState,
            INode completedParseNode)
        {
            _transitionState = transitionState;
            _completedParseNode = completedParseNode;
            _children = new ReadWriteList<IAndNode>();
            Location = location;
            NodeType = NodeType.Symbol;
            Initialize(transitionState);
        }

        public IReadOnlyList<IAndNode> Children
        {
            get
            {
                if (!ResultCached())
                    LazyLoadChildren();
                return _children;
            }
        }

        private void Initialize(ITransitionState transitionState)
        {
            IState transitionStateToUse = transitionState;

            var parameterTransitionStateHasNoParseNode = transitionState.ParseNode == null;

            if (parameterTransitionStateHasNoParseNode)
                transitionStateToUse = _transitionState.Reduction;

            Symbol = transitionStateToUse.Production.LeftHandSide;
            Origin = transitionStateToUse.Origin;
        }

        private void LazyLoadChildren()
        {
            if (_transitionState.NextTransition != null)
            {
                var virtualNode = new VirtualNode(Location, _transitionState.NextTransition, _completedParseNode);

                if (_transitionState.Reduction.ParseNode == null)
                    AddUniqueFamily(virtualNode);
                else
                    AddUniqueFamily(_transitionState.Reduction.ParseNode, virtualNode);
            }
            else if (_transitionState.Reduction.ParseNode != null)
            {
                AddUniqueFamily(_transitionState.Reduction.ParseNode, _completedParseNode);
            }
            else
            {
                AddUniqueFamily(_completedParseNode);
            }
        }

        private bool ResultCached()
        {
            return _andNode != null;
        }

        public void AddUniqueFamily(INode trigger)
        {
            if (_andNode != null)
                return;
            _andNode = new AndNode();
            _andNode.AddChild(trigger);
            _children.Add(_andNode);
        }

        public void AddUniqueFamily(INode source, INode trigger)
        {
            if (_andNode != null)
                return;
            _andNode = new AndNode();
            _andNode.AddChild(source);
            _andNode.AddChild(trigger);
            _children.Add(_andNode);
        }

        public override string ToString()
        {
            return $"({Symbol}, {Origin}, {Location})";
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
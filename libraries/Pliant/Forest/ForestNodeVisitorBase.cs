﻿namespace Pliant.Forest
{
    public abstract class ForestNodeVisitorBase : IForestNodeVisitor
    {
        public IForestNodeVisitorStateManager StateManager { get; private set; }

        protected ForestNodeVisitorBase(IForestNodeVisitorStateManager stateManager)
        {
            StateManager = stateManager;
        }

        public virtual void Visit(IIntermediateForestNode intermediateNode)
        {
            var currentAndNode = StateManager.GetCurrentAndNode(intermediateNode);
            Visit(currentAndNode);
            StateManager.MarkAsTraversed(intermediateNode);
        }

        public virtual void Visit(ITokenForestNode tokenNode)
        { }

        public virtual void Visit(IAndForestNode andNode)
        {
            for (var c = 0; c < andNode.Children.Count; c++)
            {
                var child = andNode.Children[c];
                child.Accept(this);
            }
        }

        public virtual void Visit(ISymbolForestNode symbolNode)
        {
            var currentAndNode = StateManager.GetCurrentAndNode(symbolNode);
            Visit(currentAndNode);
            StateManager.MarkAsTraversed(symbolNode);
        }

        public virtual void Visit(ITerminalForestNode terminalNode)
        { }
    }
}
﻿using Pliant.Forest;
using Pliant.Runtime;
using Pliant.Tree;
using System;

namespace Pliant.RegularExpressions
{
    public class RegexParser
    {
        public Regex Parse(string regularExpression)
        {
            var grammar = new RegexGrammar();
            var parseEngine = new ParseEngine(grammar, new ParseEngineOptions(optimizeRightRecursion: true));
            var parseRunner = new ParseRunner(parseEngine, regularExpression);
            while (!parseRunner.EndOfStream())
            {
                if (!parseRunner.Read())
                    throw new Exception(
                        $"Unable to parse regular expression. Error at position {parseRunner.Position}.");
            }
            if (!parseEngine.IsAccepted())
                throw new Exception(
                    $"Error parsing regular expression. Error at position {parseRunner.Position}");
            
            var parseForestRoot = parseEngine.GetParseForestRootNode();
            var parseForest = parseForestRoot.Children[0].Children[0];

            var parseTree = new InternalTreeNode(
                    parseForest as IInternalForestNode,
                    new SinglePassForestNodeVisitorStateManager());

            var regexVisitor = new RegexVisitor();
            parseTree.Accept(regexVisitor);

            return regexVisitor.Regex;            
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pliant
{
    public class PulseRecognizer
    {
        /// <summary>
        /// The grammar used in the parse
        /// </summary>
        public IGrammar Grammar { get; private set; }

        /// <summary>
        /// The state of the parse
        /// </summary>
        public Chart Chart { get; private set; }

        /// <summary>
        /// The current location within the parse
        /// </summary>
        public int Location { get; private set; }

        public PulseRecognizer(IGrammar grammar)
        {
            Grammar = grammar;
            Initialization();
        }
        
        private void Initialization()
        {
            Location = 0;
            Chart = new Chart();
            foreach (var startProduction in Grammar.StartProductions())
            {
                var startState = new State(startProduction, 0, 0);
                if (Chart.Enqueue(0, startState))
                    Log("Start", 0, startState);
            }

            ReductionPass(Location);
        }

        public bool Pulse(char token)
        {
            ScanPass(Location, token);

            bool tokenNotRecognized = Chart.Earlemes.Count <= Location + 1;
            if (tokenNotRecognized)
                return false;  

            // Move to next earlmeme
            Location++;          
            
            ReductionPass(Location);
            
            return true;
        }
        
        private void ScanPass(int location, char token)
        {
            IEarleme earleme = Chart.Earlemes[location];
            for (int s = 0; s < earleme.Scans.Count; s++)
            {
                var scanState = earleme.Scans[s];
                Scan(scanState, location, token);
            }
        }

        private void ReductionPass(int location)
        {
            IEarleme earleme = Chart.Earlemes[location];
            var resume = true;

            int p = 0, 
                c = 0;

            while (resume)
            {
                if (c < earleme.Completions.Count)
                {
                    var completion = earleme.Completions[c];
                    Complete(completion, location);
                    c++;
                }
                else if (p < earleme.Predictions.Count)
                {
                    var prediction = earleme.Predictions[p];
                    Predict(prediction, location);
                    p++;
                }
                else
                    resume = false;
            }
        }

        private void Predict(IState sourceState, int j)
        {
            var nonTerminal = sourceState.CurrentSymbol() as INonTerminal;
            foreach (var production in Grammar.RulesFor(nonTerminal))
            {
                PredictProduction(sourceState, j, production);
            }
        }

        private void PredictProduction(IState sourceState, int j, IProduction production)
        {
            var state = new State(production, 0, j);
            if (Chart.Enqueue(j, state))
                Log("Predict", j, state);

            var stateIsNullable = state.Production.RightHandSide.Count == 0;
            if (stateIsNullable)
            {
                var aycockHorspoolState = new State(sourceState.Production, sourceState.Position + 1, j);
                Chart.Enqueue(j, aycockHorspoolState);
                Log("Predict", j, aycockHorspoolState);
            }
        }

        private void Scan(IState scan, int j, char token)
        {
            int i = scan.Origin;
            var currentSymbol = scan.CurrentSymbol();
            var terminal = currentSymbol as ITerminal;
            if (terminal.IsMatch(token))
            {
                var scanState = new ScanState(
                    scan.Production,
                    scan.Position + 1,
                    i,
                    token);
                if (Chart.Enqueue(j + 1, scanState))
                    LogScan(j + 1, scanState, token);
            }
        }
        
        private void Complete(IState completed, int k)
        {
            var earleme = Chart.Earlemes[k];
            var searchSymbol = completed.Production.LeftHandSide;
            OptimizeReductionPath(searchSymbol, k, Chart);
            var transitiveState = FindTransitiveState(earleme, searchSymbol);
            if (transitiveState != null)
            {
                var topmostItem = new State(
                    transitiveState.Production, 
                    transitiveState.Position, 
                    transitiveState.Origin);
                if (Chart.Enqueue(k, topmostItem))
                    Log("Complete", k, topmostItem);
            }
            else
            {
                int j = completed.Origin;
                var sourceEarleme = Chart.Earlemes[j];
                for (int p = 0; p < sourceEarleme.Predictions.Count; p++)
                {
                    var state = sourceEarleme.Predictions[p];
                    if (IsSourceState(completed.Production.LeftHandSide, state))
                    {
                        int i = state.Origin;
                        var nextState = new State(state.Production, state.Position + 1, i);
                        if (Chart.Enqueue(k, nextState))
                            Log("Complete", k, nextState);
                    }
                }
            }
        }

        private void OptimizeReductionPath(ISymbol searchSymbol, int k, Chart chart)
        {
            IState t_rule = null;
            OptimizeReductionPathRecursive(searchSymbol, k, chart, ref t_rule);
        }

        private void OptimizeReductionPathRecursive(ISymbol searchSymbol, int k, Chart chart, ref IState t_rule)
        {
            var earleme = chart.Earlemes[k];
            var transitiveState = FindTransitiveState(earleme, searchSymbol);
            if (transitiveState != null)
            {
                t_rule = transitiveState;
            }
            else
            {
                var sourceState = FindSourceState(earleme, searchSymbol);

                if (sourceState != null)
                {
                    var sourceStateNext = sourceState.NextState();
                    if (IsQuasiComplete(sourceStateNext))
                    {
                        t_rule = sourceStateNext;
                        OptimizeReductionPathRecursive(sourceState.Production.LeftHandSide, sourceState.Origin, chart, ref t_rule);
                        if (t_rule != null)
                        {
                            var transitionItem = new TransitionState(
                                searchSymbol,
                                t_rule.Production,
                                t_rule.Production.RightHandSide.Count,
                                t_rule.Origin);
                            if (chart.Enqueue(k, transitionItem))
                                Log("Transition", k, transitionItem);
                        }
                    }
                }
            }
        }

        bool IsQuasiComplete(IState state)
        {            
            return state.IsComplete();
        }

        IState FindSourceState(IEarleme earleme, ISymbol searchSymbol)
        {
            var sourceItemCount = 0;
            IState sourceItem = null;
            for (int s = 0; s < earleme.Predictions.Count; s++)
            {
                var state = earleme.Predictions[s];
                if (IsSourceState(searchSymbol, state))
                {
                    bool moreThanOneSourceItemExists = sourceItemCount > 0;
                    if (moreThanOneSourceItemExists)
                        return null;
                    sourceItemCount++;
                    sourceItem = state;
                }
            }
            return sourceItem;
        }

        private bool IsSourceState(ISymbol searchSymbol, IState state)
        {
            if (state.IsComplete())
                return false;
            return state.CurrentSymbol().Equals(searchSymbol);
        }

        private IState FindTransitiveState(IEarleme earleme, ISymbol searchSymbol)
        {
            for (int t = 0; t < earleme.Transitions.Count; t++)
            {
                var transitionState = earleme.Transitions[t] as TransitionState;
                if (transitionState.Recognized.Equals(searchSymbol))
                    return transitionState;
            }
            return null;
        }
        
        public void Reset()
        {
            Initialization();
        }

        public bool IsAccepted()
        {
            var lastEarleme = Chart.Earlemes[Chart.Count - 1];
            var startStateSymbol = Grammar.Start;
            return lastEarleme
                .Completions
                .Any(x =>
                    x.Origin == 0
                    && x.Production.LeftHandSide.Value == startStateSymbol.Value);
        }

        private void Log(string operation, int origin, IState state)
        {
            Debug.Write(string.Format("{0}\t{1}", origin, state));
            Debug.WriteLine(string.Format("\t # {0}", operation));
        }

        private void LogScan(int origin, IState state, char token)
        {
            Debug.Write(string.Format("{0}\t{1}", origin, state));
            Debug.WriteLine(string.Format("\t # Scan {0}", token));
        }
    }
}

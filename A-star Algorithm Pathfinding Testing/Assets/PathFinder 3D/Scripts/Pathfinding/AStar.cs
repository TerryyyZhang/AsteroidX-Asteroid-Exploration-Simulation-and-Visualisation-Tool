using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathFinder3D
{
    public class AStar : IPathfindingAlg
    {
        private double omega;
        private PriorityQueue Open;
        CancellationToken cTInstance;
        SpaceManager spaceManagerInstance;
        private List<IVertex> Close;
        private IGraph graph;
        private IVertex start;
        private IVertex goal;
        int pathfindingLevel;

        public AStar(double omega, SpaceManager spaceManagerInstance, object cTInstance, IGraph graph, IVertex start, IVertex goal,int pathfindingLevel)
        {
            if (omega > 1 || omega < 0)
                throw new ArgumentException();
            this.omega = omega;
            this.graph = graph;
            this.start = start;
            this.goal = goal;
            this.cTInstance = (CancellationToken)cTInstance;
            this.spaceManagerInstance = spaceManagerInstance;
            this.pathfindingLevel = pathfindingLevel;
        }

        // Pathfinding
        public List<Vector3> GetWay()
        {
            Open = new PriorityQueue();
            Close = new List<IVertex>();
            var s = new List<IVertex>();
            s.Add(start);
            Open.Add(FFunction(s), s);
            while (Open.Count != 0)
            {
                if (CheckForCancellationRequested())
                    return null;
                var p = Open.First();
                Open.RemoveFirst();
                var x = p.Last();
                if (Close.Contains(x))
                    continue;
                if (x.Equals(goal))
                {
                    spaceManagerInstance.pathfindingThreadsCurCount--;
                    return p.Select(el => SpaceGraph.GetCellCenterCoordFromIndexOnLevel(((Cube)el).index, pathfindingLevel)).ToList(); //converting List<IVertex> to Lis<Vector3>
                }
                Close.Add(x);
                foreach (var y in graph.Adjacent(x))
                {
                    if (CheckForCancellationRequested())
                        return null;
                    if (!Close.Contains(y))
                    {
                        var newPath = new List<IVertex>(p);
                        newPath.Add(y);
                        Open.Add(FFunction(newPath), newPath);
                    }
                }
            }
            spaceManagerInstance.pathfindingThreadsCurCount--;
            return null;
        }


        bool CheckForCancellationRequested()
        {
            if (cTInstance.IsCancellationRequested)
            {
                cTInstance.IsCancellationRequested = false;
                spaceManagerInstance.pathfindingThreadsCurCount--;
                return true;
            }
            return false;
        }

        // Heuristics function
        public double FFunction(List<IVertex> path)
        {
            double g = 0;
            for (int i = 1; i < path.Count; i++)
                g += graph.Cost(path[i - 1], path[i]);
            return (1 - omega) * g + omega * path.Last().HFunction();
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinder3D
{
    public class Graph : IGraph
    {
        Cube start;
        Cube goal;
        SpaceConstraints constraints;
        SpaceManager spaceManagerInstance;
        object cTInstance;
        AStar astar;
        double omega;
        bool diags;
        int pathfindingLevel;

        public Graph(SpaceManager spaceManagerInstance, object cTInstance, Vector3 start, Vector3 goal,int pathfindingLevel , SpaceConstraints constraints, double omega, bool diags = false)
        {
            if (!constraints.Check(start) || !constraints.Check(goal))
                throw new ArgumentOutOfRangeException();
            this.goal = new Cube(SpaceGraph.GetCellIndexFromCoordOnLevel(goal, pathfindingLevel));
            this.start = new Cube(SpaceGraph.GetCellIndexFromCoordOnLevel(start, pathfindingLevel), this.goal);
            this.constraints = constraints;
            this.omega = omega;
            this.diags = diags;
            this.cTInstance = cTInstance;
            this.spaceManagerInstance = spaceManagerInstance;
            this.pathfindingLevel = pathfindingLevel;
        }

        public List<IVertex> Adjacent(IVertex vertex)
        {
            int x = (int)((Cube)vertex).index.x;
            int y = (int)((Cube)vertex).index.y;
            int z = (int)((Cube)vertex).index.z;
            List<IVertex> adj = new List<IVertex>();
            if (diags)
            {
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        for (int k = -1; k <= 1; k++)
                            try
                            {
                                if (!IsIndexInRange(x + i, y + j, z + k)) throw new IndexOutOfRangeException();
                                Vector3 tryingCube = new Vector3(x + i, y + j, z + k);
                                    if (!SpaceGraph.IsCellOccAtIndexOnLevel(tryingCube, pathfindingLevel))
                                        adj.Add(new Cube(tryingCube, goal));
                            }
                            catch (IndexOutOfRangeException)
                            {
                            }
            }
            else
            {
                for (int i = -1; i <= 1; i+=2)
                    try
                    {
                        if (!IsIndexInRange(x + i, y, z)) throw new IndexOutOfRangeException();
                        Vector3 tryingCube = new Vector3(x + i, y, z);
                            if (!SpaceGraph.IsCellOccAtIndexOnLevel(tryingCube, pathfindingLevel))
                                adj.Add(new Cube(tryingCube, goal));
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
                for (int j = -1; j <= 1; j += 2)
                    try
                    {
                        if (!IsIndexInRange(x, y + j, z)) throw new IndexOutOfRangeException();
                        Vector3 tryingCube = new Vector3(x, y + j, z);
                            if (!SpaceGraph.IsCellOccAtIndexOnLevel(tryingCube, pathfindingLevel))
                                adj.Add(new Cube(tryingCube, goal));
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
                for (int k = -1; k <= 1; k += 2)
                    try
                    {
                        if (!IsIndexInRange(x, y, z + k)) throw new IndexOutOfRangeException();
                        Vector3 tryingCube = new Vector3(x, y, z + k);
                            if (!SpaceGraph.IsCellOccAtIndexOnLevel(tryingCube, pathfindingLevel))
                                adj.Add(new Cube(tryingCube, goal));
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }
            }
            return adj;
        }


        public List<Vector3> GetWay()
        {
            spaceManagerInstance.pathfindingThreadsCurCount++;
            astar = new AStar(omega, spaceManagerInstance, cTInstance, this, start, goal, pathfindingLevel);
            return astar.GetWay();
        }

        bool IsIndexInRange(int x, int y, int z)
        {
            Vector3 v = new Vector3(x, y, z);
            return constraints.Check(SpaceGraph.GetCellCenterCoordFromIndexOnLevel(v, pathfindingLevel));
        }

        public double Cost(IVertex a, IVertex b)
        {
            Cube aa = (Cube)a;
            Cube bb = (Cube)b;
            double dx = (aa.index.x - bb.index.x);
            double dy = (aa.index.y - bb.index.y);
            double dz = (aa.index.z - bb.index.z);
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    public struct SpaceConstraints
    {
        public float xMin, xMax, yMin, yMax, zMin, zMax;

        public SpaceConstraints(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
        {
            this.xMin = xMin;
            this.xMax = xMax;
            this.yMin = yMin;
            this.yMax = yMax;
            this.zMin = zMin;
            this.zMax = zMax;
        }

        public bool Check(Vector3 v)
        {
            if (v.x < xMin || v.x > xMax)
                return false;
            if (v.y < yMin || v.y > yMax)
                return false;
            if (v.z < zMin || v.z > zMax)
                return false;
            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PathFinder3D;

public static class PathHandler
{
    static float deltaDistCheck;
    public static void Init(float deltaDistCheck)
    {
        PathHandler.deltaDistCheck = deltaDistCheck;
    }
    public static List<Vector3> PathFancification(List<Vector3> path, int level)
    {
        path = ChooseFreeAdjacents(path, level);
        if (SpaceGraph.amountOfSpatialGraphLevels == 1) return path;

        List<Vector3> newPath = new List<Vector3>();
        newPath.Add(path[0]);
        for (int i = 1; i < path.Count - 1; i++)
        {
            newPath.Add(SpaceGraph.FindCoordMaxCellCenter(path[i], level));
        }
        newPath.Distinct();
        newPath.Add(path[path.Count - 1]);
        return newPath;
    }
    public static List<Vector3> PathSmoothing(List<Vector3> path, int level)
    {
        //Interpolation with the Catmull-Rom spline
        CatmullRom catmullRomInstance = new CatmullRom(path, SpaceGraph.GetLevelSideLength(level));
        return catmullRomInstance.GetSplineCall();
    }
    public static List<Vector3> PathOptimization(List<Vector3> path, int level)
    {
        path = InternalPathOptimization(path, level);
        path.Reverse();
        path = InternalPathOptimization(path, level);
        path.Reverse();
        return path;
    }

    //Internal & accessory methods 
    static List<Vector3> InternalPathOptimization(List<Vector3> path, int level)
    {
        if (OnLineOfSight(SpaceGraph.GetCellIndexFromCoordOnLevel(path[0], level), SpaceGraph.GetCellIndexFromCoordOnLevel(path[path.Count - 1], level), level))
            return new List<Vector3> { path[0], path[path.Count - 1] };
        for (int j = 2; j <= path.Count - 3; j++)
            for (int i = 2; i < path.Count; i += 2)
            {
                if (i >= path.Count) return path;
                if (OnLineOfSight(SpaceGraph.GetCellIndexFromCoordOnLevel(path[i - 2], level), SpaceGraph.GetCellIndexFromCoordOnLevel(path[i], level), level))
                {
                    path.RemoveAt(i - 1);
                    return InternalPathOptimization(path, level);
                }
            }
        return path;
    }
    static List<Vector3> ChooseFreeAdjacents(List<Vector3> path, int level)
    {
        List<Vector3> newPath = new List<Vector3>();
        newPath.Add(path[0]);
        for (int i = 1; i < path.Count - 1; i++)
        {
            newPath.Add(GetFreeAdjacentCellCoord(SpaceGraph.GetCellIndexFromCoordOnLevel(path[i], level), level));
        }
        newPath.Distinct();
        newPath.Add(path[path.Count - 1]);
        return newPath;
    }
    static Vector3 GetFreeAdjacentCellCoord(Vector3 index, int level)
    {
        if (IsAdjacentsFree(index, level)) return SpaceGraph.GetCellCenterCoordFromIndexOnLevel(index, level);
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 tmpIndex = new Vector3(index.x + x, index.y + y, index.z + z);
                    if (IsAdjacentsFree(tmpIndex, level)) return SpaceGraph.GetCellCenterCoordFromIndexOnLevel(tmpIndex, level);
                }
        return SpaceGraph.GetCellCenterCoordFromIndexOnLevel(index, level);
    }
    //Is thre is no any adjacents occupied cells
    static bool IsAdjacentsFree(Vector3 index, int level)
    {
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                    if (SpaceGraph.IsCellOccAtIndexOnLevel(new Vector3(index.x + x, index.y + y, index.z + z), level)) return false;
        return true;
    }
    //Is line from i cell to j cell free?
    static bool OnLineOfSight(Vector3 i, Vector3 j, int level)
    {
        var n = (int)Vector3.Distance(i, j) / Math.Min(1f, deltaDistCheck);
        var tempVector = new Vector3();

        var direction = (j - i).normalized;

        for (var k = 0; k < n; k++)
        {
            i += direction * Math.Min(1f, deltaDistCheck);
            tempVector.Set((int)(i.x), (int)(i.y), (int)(i.z));
            //  lock (spaceGraphInstance.occCells)
            if (SpaceGraph.IsCellOccAtIndexOnLevel(tempVector, level)) return false;
        }

        return true;
    }
}

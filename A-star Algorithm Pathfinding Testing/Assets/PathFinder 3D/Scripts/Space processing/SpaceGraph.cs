using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading;
using PathFinder3D;
using System.Collections.Concurrent;

/// <summary>
/// Class, that implements the storage of spatial graphs. 
/// It contains methods that operate with cells of graphs of any levels
/// </summary>
public static class SpaceGraph
{
    public static Int16 amountOfSpatialGraphLevels;

    /// <summary>
    /// The length of the side of the cells that make up the zero level graph
    /// </summary>
    public static float cellMinSideLength;

    /// <summary>
    /// The amount by which the lengths of the sides of the cells of two adjacent levels differ. 
    /// Each level contains cells that are larger than the cells of the previous level by this value.
    /// </summary>
    static float sizeCellsRiseStep;

    /// <summary>
    /// Occupied cells storage. 
    /// <see cref="List{}"/> - spatial graph levels;
    /// <see cref="Vector3"/> - index of cell;
    /// <see cref="ConcurrentDictionary{}"/> - transform instance unique id list,
    /// that occupy this cell list of objects, defined upper.
    /// Rising indexes from 0 to 1,2,.. etc. defines to rising of cells size.
    /// </summary>
    public static volatile List<ConcurrentDictionary<Vector3, ConcurrentDictionary<int, int>>> occCellsLevels;

    public static void SetFields(float cellMinSideLength, int gridLevelsCount, int maxAliveThreadsCount)
    {
        SpaceGraph.amountOfSpatialGraphLevels = (Int16) gridLevelsCount;
        SpaceGraph.cellMinSideLength = cellMinSideLength;
        SpaceGraph.sizeCellsRiseStep = cellMinSideLength * .33f;
        occCellsLevels = new List<ConcurrentDictionary<Vector3, ConcurrentDictionary<int, int>>>(gridLevelsCount);
        for (var i = 0; i < gridLevelsCount; ++i)
            occCellsLevels.Add(new ConcurrentDictionary<Vector3, ConcurrentDictionary<int, int>>(maxAliveThreadsCount, 50000));
    }

    /// <summary>
    /// Finds the highest level at which the cell covering the coordinate is free from obstacles.
    /// It then calculates the coordinate of the center of this cell.
    /// </summary>
    /// <param name="coord">Coordinate on the scene</param>
    /// <param name="level">The lowest level index</param>
    /// <returns>The center of the found cell</returns>
    public static Vector3 FindCoordMaxCellCenter(Vector3 coord, int level)
    {
        Vector3 tmpCoord = GetEmbracingCellCenterOnLevel(coord, 0);
        for (var i = level; i < amountOfSpatialGraphLevels; ++i)
        {
            Vector3 tmpIndex = GetCellIndexFromCoordOnLevel(coord, i);
            if (occCellsLevels[i].ContainsKey(tmpIndex))
            {
                return tmpCoord;
            }
            else
            {
                tmpCoord = GetCellCenterCoordFromIndexOnLevel(tmpIndex, i);
            }
        }
        return tmpCoord;
    }

    /// <summary>
    /// At a specific level, finds the cell index embracing the coordinate.
    /// </summary>
    /// <param name="coord">Coordinate on the scene</param>
    /// <param name="level">Specific level index</param>
    /// <returns>Cell index</returns>
    public static Vector3 GetCellIndexFromCoordOnLevel(Vector3 coord, int level)
    {
        float cellSize = cellMinSideLength + sizeCellsRiseStep * level;
        float xI = ((int) ((Mathf.Abs(coord.x) / cellSize + 0.5F))) * Mathf.Sign(coord.x);
        float yI = ((int) ((Mathf.Abs(coord.y) / cellSize + 0.5F))) * Mathf.Sign(coord.y);
        float zI = ((int) ((Mathf.Abs(coord.z) / cellSize + 0.5F))) * Mathf.Sign(coord.z);
        return new Vector3(xI, yI, zI);
    }

    /// <summary>
    /// At a specific level, find the coordinate of the center of the cell by its index.
    /// </summary>
    /// <param name="index">Coordinate on the scene</param>
    /// <param name="level">Specific level index</param>
    /// <returns>The center of the found cell</returns>
    public static Vector3 GetCellCenterCoordFromIndexOnLevel(Vector3 index, int level)
    {
        float cellSize = cellMinSideLength + sizeCellsRiseStep * level;
        float x = index.x * cellSize;
        float y = index.y * cellSize;
        float z = index.z * cellSize;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// For each level, the method determines the cell that embraces the coordinate, 
    /// and mark it like occupied by game object with specific instance ID.
    /// </summary>
    /// <param name="coord">Coordinate on the scene</param>
    /// <param name="instanceID">Specific ID of the transform instance of the game object, that occupies the cell</param>
    public static void OccupyCellsInCoordAtAllLevels(Vector3 coord, int instanceID)
    {
        for (int i = 0; i < amountOfSpatialGraphLevels; i++)
        {
            try
            {
                Vector3 tmpIndex = GetCellIndexFromCoordOnLevel(coord, i);
                if (occCellsLevels[i].ContainsKey(tmpIndex))
                        occCellsLevels[i][tmpIndex].TryAdd(instanceID, 0);
                else
                {
                    occCellsLevels[i].TryAdd(tmpIndex, new ConcurrentDictionary<int, int>());
                    occCellsLevels[i][tmpIndex].TryAdd(instanceID, 0);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex.Message);
            }
        }
    }

    /// <summary>
    /// Determines whether a cell that embraces the specific coord is occupied or not, at a specific level.
    /// </summary>
    /// <param name="coord">Specific index</param>
    /// <param name="level">Specific level</param>
    /// <returns>Is cell occupied or not</returns>
    public static bool IsCellOccAtCoordOnLevel(Vector3 coord, int level)
    {
        return occCellsLevels[level].ContainsKey(GetCellIndexFromCoordOnLevel(coord, level));
    }

    /// <summary>
    /// Determines whether a cell with a specific index is occupied or not, at a specific level.
    /// </summary>
    /// <param name="index">Specific index</param>
    /// <param name="level">Specific level</param>
    /// <returns>Is cell occupied or not</returns>
    public static bool IsCellOccAtIndexOnLevel(Vector3 index, int level)
    {
        return occCellsLevels[level].ContainsKey(index);
    }

    /// <summary>
    /// Determines whether a cell that embraces the specific coord is occupied by any static obstacle or not, at a specific level.
    /// </summary>
    /// <param name="coord">Specific index</param>
    /// <param name="level">Specific level</param>
    /// <returns>Is cell occupied by any static obstacle or not</returns>
    public static bool IsCellOccStaticOnLevel(Vector3 coord, int level)
    {
        //заглушка, т.к. статичные и динамические препятствия не описаны
        ConcurrentDictionary<int, int> instancesID;
        Vector3 cellIndex = GetCellIndexFromCoordOnLevel(coord, level);
        if (occCellsLevels[level].ContainsKey(cellIndex))
            instancesID = occCellsLevels[level][cellIndex];
        else
            return false;
        return true;
    }

    /// <summary>
    /// Determines side length of cell at specific level.
    /// </summary>
    /// <param name="level">Specific level</param>
    /// <returns>Side length</returns>
    public static float GetLevelSideLength(int level)
    {
        return cellMinSideLength + (cellMinSideLength * .33f) * level;
    }

    /// <summary>
    /// At a specific level, find the coordinate of the center of the cell that embraces specific coordinate.
    /// </summary>
    /// <param name="coord">Specific coordinate</param>
    /// <param name="level">Specific level</param>
    /// <returns>Cell center coordinate</returns>
    static Vector3 GetEmbracingCellCenterOnLevel(Vector3 coord, int level)
    {
        float cellSideLength = cellMinSideLength + sizeCellsRiseStep * level;
        float x = (Mathf.Abs(coord.x) + cellSideLength) * Mathf.Sign(coord.x);
        float y = (Mathf.Abs(coord.y) + cellSideLength) * Mathf.Sign(coord.y);
        float z = (Mathf.Abs(coord.z) + cellSideLength) * Mathf.Sign(coord.z);
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Removes all occurrences of "instanceID" from each cell
    /// </summary>
    /// <param name="instanceID">Specific ID of the transform instance of the game object, that occupies the cell</param>
    public static void ReleaseCellsFromObstcID(int instanceID)
    {
        ConcurrentDictionary<int, int> val;
        for (var i = 0; i < amountOfSpatialGraphLevels; ++i)
        {
            if (occCellsLevels[i] == null || occCellsLevels[i].Count == 0) continue;
            Vector3[] indexList = occCellsLevels[i].Where(el => el.Value.ContainsKey(instanceID)).Select(el => el.Key).ToArray();
            foreach (var cellIndex in indexList)
            {
                if (occCellsLevels[i].ContainsKey(cellIndex))
                {
                    int tmpVar;
                    occCellsLevels[i][cellIndex].TryRemove(instanceID,out tmpVar);
                    if (occCellsLevels[i][cellIndex].Count == 0) occCellsLevels[i].TryRemove(cellIndex, out val);
                }
            }
        }
    }
}
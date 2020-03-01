using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PathFinder3D;

public class WaveTrace : IPathfindingAlg
{
    private Vector3 start;
    private Vector3 goal;
    SpaceConstraints spaceConstarintsInst;
    CancellationToken cTInstance;
    public volatile List<HashSet<Vector3>> waves;

    public WaveTrace(SpaceConstraints spaceConstraintsInp, object cTInstance, Vector3 start, Vector3 goal)
    {
        this.cTInstance = (CancellationToken)cTInstance;
        spaceConstarintsInst = spaceConstraintsInp;
        this.start = SpaceGraph.GetCellIndexFromCoordOnLevel(start,0);
        this.goal = SpaceGraph.GetCellIndexFromCoordOnLevel(goal,0);
        waves = new List<HashSet<Vector3>>();
    }

    public List<Vector3> GetWay()
    {
        waves.Add(new HashSet<Vector3> { start });
        waves.Add(new HashSet<Vector3>());

        while (waves[waves.Count - 2].Count != 0)
        {
            foreach (Vector3 index in waves[waves.Count - 2])
            {
                if (cTInstance.IsCancellationRequested) return null;
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 cell = new Vector3(index.x + i, index.y, index.z);
                    if (cell == goal) return BackwardDescent();
                    if (!spaceConstarintsInst.Check(SpaceGraph.GetCellCenterCoordFromIndexOnLevel(cell,0))) continue;
                    if (SpaceGraph.IsCellOccAtIndexOnLevel(cell,0) || waves[waves.Count - 2].Contains(cell)) continue;
                    if (waves.Count > 2) if (waves[waves.Count - 3].Contains(cell)) continue;
                        lock (waves)
                            waves[waves.Count - 1].Add(cell);
                }
                for (int j = -1; j <= 1; j += 2)
                {
                    Vector3 cell = new Vector3(index.x, index.y + j, index.z);
                    if (cell == goal) return BackwardDescent();
                    if (!spaceConstarintsInst.Check(SpaceGraph.GetCellCenterCoordFromIndexOnLevel(cell,0))) continue;
                    if (SpaceGraph.IsCellOccAtIndexOnLevel(cell,0) || waves[waves.Count - 2].Contains(cell)) continue;
                    if (waves.Count > 2) if (waves[waves.Count - 3].Contains(cell)) continue;
                    lock (waves)
                        waves[waves.Count - 1].Add(cell);
                }
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 cell = new Vector3(index.x, index.y, index.z + k);
                    if (cell == goal) return BackwardDescent();
                    if (!spaceConstarintsInst.Check(SpaceGraph.GetCellCenterCoordFromIndexOnLevel(cell,0))) continue;
                    if (SpaceGraph.IsCellOccAtIndexOnLevel(cell,0) || waves[waves.Count - 2].Contains(cell)) continue;
                    if (waves.Count > 2) if (waves[waves.Count - 3].Contains(cell)) continue;
                    lock (waves)
                        waves[waves.Count - 1].Add(cell);
                }
            }
            HashSet<Vector3> newHSet = new HashSet<Vector3>();
            waves.Add(newHSet);
        }
        return null;
    }

    List<Vector3> BackwardDescent()
    {
        List<Vector3> way = new List<Vector3>(waves.Count + 1);
        way.Add(goal);

        for (int waveI = waves.Count - 1; waveI >= 0; waveI--)
        {
            if (cTInstance.IsCancellationRequested) return null;
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 cell = new Vector3(way[way.Count - 1].x + i, way[way.Count - 1].y, way[way.Count - 1].z);
                if (waves[waveI].Contains(cell)) { way.Add(cell); goto exitLabel; }
            }
            for (int j = -1; j <= 1; j += 2)
            {
                Vector3 cell = new Vector3(way[way.Count - 1].x, way[way.Count - 1].y + j, way[way.Count - 1].z);
                if (waves[waveI].Contains(cell)) { way.Add(cell); goto exitLabel; }
            }
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 cell = new Vector3(way[way.Count - 1].x, way[way.Count - 1].y, way[way.Count - 1].z + k);
                if (waves[waveI].Contains(cell)) { way.Add(cell); goto exitLabel; }
            }
            exitLabel:;
        }

        way = way.Select(index => SpaceGraph.GetCellCenterCoordFromIndexOnLevel(index,0)).ToList();
        way.Reverse();
        return way;
    }
}

using System.Collections.Generic;
using UnityEngine;
using PathFinder3D;

public class CatmullRom
{
    List<Vector3> way;
    int segmentsCount;
    float cellMinSize;
    float sqrCellMinSize;
    //class constructor
    public CatmullRom(List<Vector3> wayToSmooth, float sideLength)
    {
        //calculation number of joints for each spline curve
        segmentsCount = (int)(sideLength * 8F);
        way = wayToSmooth;
        cellMinSize = sideLength;
        sqrCellMinSize = cellMinSize * cellMinSize*2;
    }
    //Polyline construction on spline curves
    public List<Vector3> GetSplineCall()
    {
        List<Vector3> splinePoints = GetSpline();
        List<Vector3> optimizedSplinePoints = new List<Vector3>();

        optimizedSplinePoints.Add(splinePoints[0]);
        int optPrePointIndex = 0;
        for(int i = 0; i < splinePoints.Count-1; i++)
        {
            Vector3 optPrePoint = optimizedSplinePoints[optPrePointIndex];
            Vector3 curSplinePoint = splinePoints[i];
            if (Vector3.SqrMagnitude(optPrePoint - curSplinePoint) > sqrCellMinSize)
            {
                optimizedSplinePoints.Add(curSplinePoint);
                optPrePointIndex++;
            }
        }
        optimizedSplinePoints.Add(splinePoints[splinePoints.Count-1]);
        
        //return optimizedSplinePoints;
        return BuildSpline(optimizedSplinePoints);

    }

    List<Vector3> GetSpline()
    {
        Vector3 endPoint = way[way.Count - 1];
        way = InfixPolyline(way, SpaceGraph.cellMinSideLength * 10);

        // if (way[way.Count - 1] != endPoint) way.Add(endPoint);
        return BuildSpline(way);
    }

    List<Vector3> BuildSpline(List<Vector3> way)
    {
        //accessory extreme points adding
        way.Insert(0, way[0] + (way[0] - way[1]).normalized);
        way.Insert(way.Count, way[way.Count - 1] + (way[way.Count - 1] - way[way.Count - 2]).normalized);

        List<Vector3> splinePoints = new List<Vector3>((way.Count - 3) * segmentsCount);
        float step = 1f / (float)segmentsCount;

        Vector3 nextPoint;
        //spline points obtaining
        for (int i = 1; i <= way.Count - 3; i++)
        {
            for (float t = 0; t <= 1; t += step)
            {
                nextPoint = CatmullRomEq(way[i - 1], way[i], way[i + 1], way[i + 2], t);
                if (!SpaceGraph.IsCellOccAtCoordOnLevel(nextPoint, 0))
                    splinePoints.Add(nextPoint);
            }
        }
        // if (way[way.Count - 1] != endPoint) way.Add(endPoint);
        return splinePoints;
    }

    //Adding additional points to a polyline
    List<Vector3> InfixPolyline(List<Vector3> way, float step)
    {
        float sqrStep = step * step;
        for (int i = 0; i < way.Count - 1; i++)
        {
            if (Vector3.SqrMagnitude(way[i] - way[i + 1]) > sqrStep)
            {
                Vector3 newPoint = way[i] + (way[i + 1] - way[i]) * .5f;
                way.Insert(i + 1, newPoint);
                return InfixPolyline(way,step);
            }
        }
        return way;
    }
    /*
    List<Vector3> InfixPolyline(List<Vector3> way, float step)
    {
        float totalLength = 0;
        float offset = 0;
        Vector3 curPoint = way[0];
        float distToNext;
        for (int j = 0; j < way.Count - 1; j++)
            totalLength += Vector3.Distance(way[j], way[j + 1]);

        int i = 0;
        while (offset < totalLength)
        {
            if (totalLength - offset < step) break;
            distToNext = Vector3.Distance(curPoint, way[i + 1]);
            if (distToNext > step)
            {
                curPoint += (way[i + 1] - curPoint).normalized * step;
                i++;
                way.Insert(i, curPoint);
                offset += step;
                continue;
            }
            else
            {
                offset += distToNext;
                i++;
                curPoint = way[i];
            }
        }
        return way;
    }
    */
    //Catmull-Rom spline equation
    public Vector3 CatmullRomEq(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        return .5f * (
            -t * (1 - t) * (1 - t) * P0
            + (2 - 5 * t * t + 3 * t * t * t) * P1
            + t * (1 + 4 * t - 3 * t * t) * P2
            - t * t * (1 - t) * P3
            );
    }
}
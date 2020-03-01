using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;

[RequireComponent(typeof(Pursuer))]
public class PathfindingController : MonoBehaviour {
    public Transform startTrans;
    public Transform targetTrans;
    List<Vector3> foundRoute = new List<Vector3>();
    Stopwatch stopWatch;
    TimeSpan ts;
    GameObject lineContainer;
    LineRenderer line;
    private void Awake()
    {
        lineContainer = new GameObject();
        lineContainer.name = "spline";
        line = lineContainer.AddComponent<LineRenderer>();
        line.loop = false;
    }

    public void PathfindingCall()
    {
        if (gameObject.GetComponent<Pursuer>().spaceManagerInstance.isPrimaryProcessingCompleted)
        {
            GetComponent<Pursuer>().StopAllAsyncTasks();
            stopWatch = new Stopwatch();
            gameObject.GetComponent<Pursuer>().FindWay(startTrans.position, targetTrans.position, gameObject.GetComponent<Pursuer>().pathfindingLevel, foundRoute, GetComponent<Pursuer>().selectedPFAlg, null, new Action(() => { gameObject.SendMessage("PathIsReady"); }), GetComponent<Pursuer>().trajectoryOptimization, GetComponent<Pursuer>().trajectorySmoothing);
            stopWatch.Start();
        }
    }
    public void PathIsReady() {
        line.positionCount = foundRoute.Count;
        line.SetPositions(foundRoute.ToArray());
        line.startWidth = 0.25f;
        line.endWidth = 0.25f;
        stopWatch.Stop();
        ts = stopWatch.Elapsed;
        UnityEngine.Debug.Log("Path was found in: " + ts.TotalMilliseconds);
    }
}

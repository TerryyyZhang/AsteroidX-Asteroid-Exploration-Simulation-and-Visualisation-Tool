using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Pursuer))]
public class PathController : MonoBehaviour {
    public Transform toPoint;
    public Material lineMaterial;
    public float lineWidth = 0.1f;
    Vector3 toOldPos;
    Vector3 fromOldPos;
    volatile List<Vector3> trajectory;
    Pursuer thisPursuerInstance;
    SpaceManager spaceManagerinstance;
    GameObject lineContainer;
    LineRenderer line;
    private void Start()
    {
        lineContainer = new GameObject();
        lineContainer.name = "spline";
        line = lineContainer.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.loop = false;
        thisPursuerInstance = GetComponent<Pursuer>();
        spaceManagerinstance = Component.FindObjectOfType<SpaceManager>();
        trajectory = new List<Vector3>();
        toOldPos = toPoint.position;
        fromOldPos = transform.position;
    }
    private void Update()
    {
     if((toOldPos!=toPoint.position || fromOldPos!=transform.position) && spaceManagerinstance.isPrimaryProcessingCompleted)
        {
            fromOldPos = transform.position;
            toOldPos = toPoint.position;
            thisPursuerInstance.StopAllAsyncTasks();
            thisPursuerInstance.FindWay(fromOldPos, toOldPos, thisPursuerInstance.pathfindingLevel, trajectory,thisPursuerInstance.selectedPFAlg, null, new Action(() => { gameObject.SendMessage("PathWasUpdated"); }), thisPursuerInstance.trajectoryOptimization, thisPursuerInstance.trajectorySmoothing);
        }
    }
    public void PathWasUpdated()
    {
        line.positionCount = trajectory.Count;
        line.SetPositions(trajectory.ToArray());
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
    }
}

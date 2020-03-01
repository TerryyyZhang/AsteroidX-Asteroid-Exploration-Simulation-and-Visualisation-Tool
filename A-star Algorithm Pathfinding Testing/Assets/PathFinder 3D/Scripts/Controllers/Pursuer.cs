using System.Collections.Generic;
using UnityEngine;
#if NETFX_CORE
    using Windows.System.Threading;
#else
using System.Threading;
#endif
using System;
using System.Linq;
using PathFinder3D;

public class Pursuer : MonoBehaviour
{
    //Game zone constraints
    public float xMin;
    public float xMax;
    public float yMin;
    public float yMax;
    public float zMin;

    public float zMax;

    //Pathfinding settings
    public PathfindingAlgorithm selectedPFAlg;
    public int pathfindingLevel;
    public bool inEditorPathfindingTraverce;
    public float heuristicFactor = 0.9f;

    public bool trajectoryOptimization, trajectorySmoothing;

    //Movement settings
    public float speed;
    public bool moveVectorOrientation;
    public float turnSpeed;

    public bool reckDynamicObstacles;

    //Other settings
    public bool generateEventMessages;

    public bool generateCondMessages;

    //Debug settings
    public bool showPathfindingZone;
    public bool tracePath;
    public float lineWidth;
    public Material lineMaterial;


    #region Internal variables & classes 

    /// <summary>
    /// Pathfinding algorithm type for selecting algorithm possability.
    /// </summary>
    public enum PathfindingAlgorithm
    {
        AStar,
        waveTrace
    };

    /// <summary>
    /// The delegate who should display the current state of the pursuer, using the storage of a specific procedure.
    /// </summary>
    delegate void condition();

    /// <summary>
    /// The variable, that keeps current Pursuer's condition.
    /// </summary>
    volatile condition curCond;

    /// <summary>
    /// SpaceManager instance. Uses for responding graph metadata and appeal to the queue of pursuers.
    /// </summary>
    [HideInInspector]
    public SpaceManager spaceManagerInstance;

    /// <summary>
    /// Keeps original finden path. 
    /// Each point is the center of the cell at a specific pathfinding level (The level at which the pathfinding was performed).
    /// (the path is stored in its original form, obtained before optimization and smoothing)
    /// </summary>
    volatile List<Vector3> foundPath;

    /// <summary>
    /// Keeps finden path at its final form.
    /// (after optimization and smoothing has perfomed)
    /// </summary>
    volatile List<Vector3> finalPath;

    /// <summary>
    /// Keeps refined path, that obtains after calling RefinePath procedure.
    /// Variable is temporary, and is used for obtaining union of obsolate path part and refined path.
    /// </summary>
    volatile List<Vector3> refinedPath;

    /// <summary>
    /// Keeps anonimous functions, that must be invoced ath the main thread. 
    /// (Invocation occurs at Update event)
    /// </summary>
    volatile Queue<Action> actionsQueue;

    /// <summary>
    /// Means, is refining process in progress right now or not.
    /// </summary>
    bool isRefiningInProgress;

    /// <summary>
    /// Keeps transform instance of this GameObject.
    /// (variable is needed in order to avoid frequent "GetComponent" (the same bad as gameObject.transform) request)
    /// </summary>
    Transform thisTransformInstance;

    /// <summary>
    /// Game object - container for line renderer component, that visualize finden trajectory. 
    /// </summary>
    GameObject trajectoryVisualLine;

    /// <summary>
    /// Keeps data about suppositional position of Pursuer during its moving across trajectory.
    /// (used in MotionMethod)
    /// </summary>
    Vector3 curPos;

    /// <summary>
    /// Used to index the points on which the pursuer moves.
    /// (used in MotionMethod)
    /// </summary>
    int prePointI;

    /// <summary>
    /// Keeps current path length.
    /// </summary>
    volatile float totalLength;

    /// <summary>
    /// Keep current distance, that covered by Pursuer due moving along the path.
    /// </summary>
    float offset;

    /// <summary>
    /// Keeps the position of the target, that was transfered to MoveTo at the moment of its call.
    /// </summary>
    Vector3 targetCoord;

    /// <summary>
    /// Keeps the actual position of the target, that was transfered to RefinePath at the moment of its call.
    /// </summary>
    Vector3 refinedTargetCoord;

    /// <summary>
    /// Token for asycn operations cancelling.
    /// </summary>
    public CancellationTokenSource cTSInstance;

    /// <summary>
    /// Instructions for async pathfinding task, for case when there is no path between Pursuer and target.
    /// </summary>
    Action defaultFailureAct;

    /// <summary>
    /// Instructions for async pathfinding task, for case when path was succesfuly finden.
    /// </summary>
    Action defaultSuccesAct;

    /// <summary>
    /// Keep waves data in case of WaveTrace algorithm using.
    /// (used for visualizing waves in editor mode)
    /// </summary>
    List<HashSet<Vector3>> waves;

    /// <summary>
    /// Exception for the case, when pathfinding was requested before obstacles primary processing was finished.
    /// </summary>
    class GraphNotReadyException : Exception
    {
    }

    /// <summary>
    /// Exception, for the case, when pathfinding request takes place with level argument, which value is out of levels range (from 0 to levels count, set in SpaceManager).
    /// </summary>
    class PathfindingLevelOutOfRangeException : Exception
    {
    }

    /// <summary>
    /// Class- container, uses for transferring parametrs into pathfinding async task (PathfindingThreadMethod).
    /// </summary>
    class PathfindingParamContainer
    {
        public Vector3 start;
        public Vector3 target;
        public List<Vector3> outPath;
        public PathfindingAlgorithm usingAlg;
        public int pathfindingLevel;
        public bool inThreadOptimizePath;
        public bool inThreadSmoothPath;
        public Action failureActions;
        public Action succesActions;
        public bool generateEvents;
    }

    //Container for passing parametrs into path-processing thread 
    /// <summary>
    /// Class-container for transferring parametrs into path processing async task (PathProcessThreadMethod).
    /// </summary>
    class PathProcessParamContainer
    {
        public bool trajectoryOptimization;
        public bool trajectorySmoothing;
        public int pathfindingLevel;
        public List<Vector3> pathToProcess;
        public Vector3 startPos;
        public Action furtherActions;
    }

    #endregion

    #region Unity's events

    void Awake()
    {
        thisTransformInstance = transform;
        spaceManagerInstance = Component.FindObjectOfType<SpaceManager>();
        defaultFailureAct = () =>
        {
            curCond = WaitingForRequest;
            ThrowUniversalEvent("EventPathWasNotFound");
        };
        defaultSuccesAct = () =>
        {
            curCond = WaitingForWayProcessing;
            ThrowUniversalEvent("EventPathWasFound");
            ThrowUniversalEvent("EventProcessingStarted");
            ProcessPath(foundPath);
        };
        actionsQueue = new Queue<Action>();
        foundPath = new List<Vector3>();
        finalPath = new List<Vector3>();
        refinedPath = new List<Vector3>();
        cTSInstance = new CancellationTokenSource();
        curCond = WaitingForRequest;
    }

    void Update()
    {
        while (actionsQueue.Count > 0)
        {
            Action someFunc = actionsQueue.Dequeue();
            try
            {
                someFunc.Invoke();
            }
            catch
            {
            }
        }
    }

    private void FixedUpdate()
    {
        curCond.Invoke();
    }

    private void OnApplicationQuit()
    {
        cTSInstance.Cancel();
    }

    private void OnDestroy()
    {
        if (trajectoryVisualLine != null)
        {
            Destroy(trajectoryVisualLine);
            trajectoryVisualLine = null;
        }

        cTSInstance.Cancel();
        LeavePathfindingQueue();
    }

    #endregion

    #region User's calls

    /// <summary>
    /// Returns total length of the actual path.
    /// </summary>
    /// <returns>Path length</returns>
    public float GetTotalPathLength()
    {
        return totalLength;
    }

    /// <summary>
    /// Returns actual original finden path.
    /// (Returns the copy of the foundPath list)
    /// </summary>
    /// <returns>The list of the points, that make up the path</returns>
    public List<Vector3> GetFoundPath()
    {
        List<Vector3> newVector3List = new List<Vector3>();
        newVector3List.AddRange(foundPath);
        return newVector3List;
    }

    /// <summary>
    /// Returns actual final path, after applying optimization and smoothing procedures.
    /// (Returns the copy of the finalPath list)</summary>
    /// <returns>The list of the points, that make up the final path</returns>
    public List<Vector3> GetFinalPath()
    {
        return finalPath;
    }

    //Sets constraints of pathfinding area
    /// <summary>
    /// Sets the edges of the zone, pathfinding is allowed in.
    /// </summary>
    /// <param name="xMin">Left edge of the x axis</param>
    /// <param name="xMax">Righy edge of the x axis</param>
    /// <param name="yMin">Left edge of the y axis</param>
    /// <param name="yMax">Righy edge of the y axis</param>
    /// <param name="zMin">Left edge of the z axis</param>
    /// <param name="zMax">Righy edge of the z axis</param>
    /// <returns>False, if edges are incorect, else - true.</returns>
    public bool SetConstraints(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
    {
        if (xMin >= xMax || yMin >= yMax || zMin >= zMax) return false;
        this.xMin = xMin;
        this.xMax = xMax;
        this.yMin = yMin;
        this.yMax = yMax;
        this.zMin = zMin;
        this.zMax = zMax;
        return true;
    }

    /// <summary>
    /// Returns the name of the current Pursur's condition.
    /// </summary>
    /// <returns>The name of the method, that curCond var is keeping at the moment.</returns>
    public string GetCurCondition()
    {
        return curCond == null ? "none" : curCond.Method.Name;
    }

    /// <summary>
    /// Returns the ratio of the passed path part to total path length.
    /// </summary>
    /// <returns>The ratio of the passed path part to total length</returns>
    float GetCurWayProgress()
    {
        return offset / totalLength;
    }

    /// <summary>
    /// Performs a refinement of the trajectory to the target.
    /// Allows to perform trajectory correction without interrupting the movement of the pursuer.
    /// (It is useful in case the target moves during the pursuit)
    /// </summary>
    /// <param name="target">Target's transform component.</param>
    /// <returns>False, if pursuer not in Movement condition at the call moment.</returns>
    public bool RefinePath(Transform target)
    {
        return RefinePath(target.position);
    }

    public bool RefinePath(Vector3 newPos)
    {
        ThrowUniversalEvent("EventPathRefiningRequested");
        if (curCond == Movement)
        {
            if (isRefiningInProgress ||
                !IsAtConstraintsArea(newPos) ||
                !IsAtConstraintsArea(thisTransformInstance.position) ||
                SpaceGraph.IsCellOccStaticOnLevel(thisTransformInstance.position, 0) ||
                SpaceGraph.IsCellOccStaticOnLevel(newPos, 0))
                return false;

            LeavePathfindingQueue();
            isRefiningInProgress = true;
            refinedTargetCoord = newPos;
            Action failureAct = () =>
            {
                ThrowUniversalEvent("EventPathWasNotRefined");
                isRefiningInProgress = false;
            };
            Action succesAct = () => { ThrowUniversalEvent("EventPathWasRefined"); };
            ThrowUniversalEvent("EventPathRefiningStarted");
            FindWay(thisTransformInstance.position, newPos, pathfindingLevel, refinedPath, selectedPFAlg, failureAct,
                succesAct, trajectoryOptimization, trajectorySmoothing);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Makes pursuer to serach for a path to the target, and moves along it.
    /// Places the pursuer in the WaitingForPursuersQueue state.
    /// Throws MovementToTheTargetRequested event.
    /// </summary>
    /// <param name="target">Target's transform component.</param>
    /// <param name="topPriority">Put the pursuer in the first place in the pathfinding queue?</param>
    public void MoveTo(Transform target, bool topPriority = false)
    {
        MoveTo(target.position, topPriority);
    }

    public void MoveTo(Vector3 targetPos, bool topPriority = false)
    {
        ResetValues();
        ThrowUniversalEvent("EventMovementToTheTargetRequested");

        if (!spaceManagerInstance.isPrimaryProcessingCompleted)
            throw new GraphNotReadyException();

        if (Vector3.Distance(thisTransformInstance.position, targetPos) < SpaceGraph.cellMinSideLength)
        {
            foundPath = new List<Vector3> { thisTransformInstance.position, targetPos };
            finalPath = new List<Vector3>(foundPath);
            curCond = WaitingForWayProcessing;
            targetCoord = targetPos;
            ThrowUniversalEvent("EventPathWasFound");
            ThrowUniversalEvent("EventProcessingStarted");
            ProcessPath(foundPath);
            return;
        }

        ThrowUniversalEvent("EventPathfindingRequested");
        curCond = WaitingForPursuersQueue;
        curPos = thisTransformInstance.position;
        targetCoord = targetPos;
        QueueUpForPathfinding(topPriority);
    }

    /// <summary>
    /// Interrupts pursuer's movement.
    /// </summary>
    /// <returns>False, if pursuer not in Movement state at the call moment.</returns>
    public bool InterruptMovement()
    {
        if (curCond == Movement)
        {
            ThrowUniversalEvent("EventMovementWasInterrupted");
            curCond = WaitingForTheContinuation;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Places pursuer to the WaitingForRequest state.
    /// Resets all accessory pathfinding variables, destroys line visualization, if it's existing.
    /// Stops all async tasks.
    /// </summary>
    public void ResetCondition()
    {
        ResetValues();
    }

    /// <summary>
    /// Resumes pursuer's movement.
    /// </summary>
    /// <returns>False, if pursuer not in WaitingForTheContinuation state at the call moment.</returns>
    public bool ResumeMovement()
    {
        if (curCond == WaitingForTheContinuation)
        {
            ThrowUniversalEvent("EventMovementWasResumed");
            curCond = Movement;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Stops all async tasks.
    /// </summary>
    public void StopAllAsyncTasks()
    {
        cTSInstance.Cancel();
        cTSInstance = new CancellationTokenSource();
    }

    /// <summary>
    /// Starts pathfinding task from <paramref name="startPos" /> to <paramref name="targetPos" /> asyncronously.
    /// </summary>
    /// <param name="startPos">Coordinate of the start pathfinding point at space.</param>
    /// <param name="targetPos">Coordinate of the target pathfinding point at space.</param>
    /// <param name="pathfindingLvl">Level of the pathfinding graph, which pathfinding will performed on.</param>
    /// <param name="foundPath">Variable, which found path will puted in.</param>
    /// <param name="usingAlg">Algorithm, that must be using for pathfinding.</param>
    /// <param name="failurePathfindingActions">Instructions, that must be invoked in the case, when pathfinding unsucced.</param>
    /// <param name="succesPathfindingActions">Instructions, that must be invoked in the case, when pathfinding succed.</param>
    /// <param name="inThreadOptimization">Is needed to perform finden path optimization?</param>
    /// <param name="inThreadSmoothing">Is needed to perform finden path smoothing?</param>
    public void FindWay(Vector3 startPos, Vector3 targetPos, int pathfindingLvl, List<Vector3> foundPath,
        PathfindingAlgorithm usingAlg, Action failurePathfindingActions = null, Action succesPathfindingActions = null,
        bool inThreadOptimization = false, bool inThreadSmoothing = false)
    {
        if (!spaceManagerInstance.isPrimaryProcessingCompleted)
            throw new GraphNotReadyException();
        if (pathfindingLevel >= spaceManagerInstance.gridDetailLevelsCount || pathfindingLevel < 0)
            throw new PathfindingLevelOutOfRangeException();
        PathfindingParamContainer pathfindingData = new PathfindingParamContainer
        {
            start = startPos,
            target = targetPos,
            outPath = foundPath,
            usingAlg = selectedPFAlg,
            pathfindingLevel = pathfindingLvl,
            inThreadOptimizePath = inThreadOptimization,
            inThreadSmoothPath = inThreadSmoothing,
            failureActions = failurePathfindingActions,
            succesActions = succesPathfindingActions,
            generateEvents = true
        };
        ThreadPool.QueueUserWorkItem(PathfindingThreadMethod, pathfindingData);
    }

    #endregion

    #region Internal methods definition

    /// <summary>
    /// Places pursuer to the WaitingForRequest state.
    /// Resets all non-public & internal variables, destroys line visualization, if it's existing.
    /// Stops all async tasks.
    /// </summary>
    void ResetValues()
    {
        LeavePathfindingQueue();
        if (trajectoryVisualLine != null)
        {
            Destroy(trajectoryVisualLine);
            trajectoryVisualLine = null;
        }

        cTSInstance.Cancel();
        cTSInstance = new CancellationTokenSource();
        curCond = WaitingForRequest;
        finalPath.Clear();
        foundPath.Clear();
        refinedPath.Clear();
        totalLength = 0;
        offset = 0;
        actionsQueue.Clear();
        isRefiningInProgress = false;
        prePointI = 0;
        if (spaceManagerInstance.gridDetailLevelsCount <= pathfindingLevel)
            pathfindingLevel = spaceManagerInstance.gridDetailLevelsCount - 1;
    }

    /// <summary>
    /// Starts trajectory processing procedures (optimization & smoothing) asyncronously. 
    /// </summary>
    /// <param name="path">Path to process.</param>
    void ProcessPath(List<Vector3> path)
    {
        PathProcessParamContainer pPPCInstance = new PathProcessParamContainer
        {
            trajectoryOptimization = trajectoryOptimization,
            trajectorySmoothing = trajectorySmoothing,
            pathfindingLevel = this.pathfindingLevel,
            pathToProcess = foundPath,
            startPos = thisTransformInstance.position,
            furtherActions = () =>
            {
                if (tracePath) DrawAPath(finalPath);
                ThrowUniversalEvent("EventProcessingFinished");
                ThrowUniversalEvent("EventMovementStarted");
                curCond = Movement;
            }
        };
        ThreadPool.QueueUserWorkItem(PathProcessThreadMethod, pPPCInstance);
    }

    #region Threading methods

    /// <summary>
    /// Internal method.
    /// Performs trajectory processing procedures.
    /// (asynchronous execution is assumed)
    /// </summary>
    /// <param name="inpData"></param>
    void PathProcessThreadMethod(object inpData)
    {
        CancellationToken cTInstance = cTSInstance.Token;
        PathProcessParamContainer pPPCInstance = (PathProcessParamContainer)inpData;

        List<Vector3> newPath = new List<Vector3>();
        lock (pPPCInstance.pathToProcess)
            newPath.AddRange(pPPCInstance.pathToProcess);

        newPath[newPath.Count - 1] = targetCoord;
        newPath.Insert(0, pPPCInstance.startPos);
        if (cTInstance.IsCancellationRequested) return;
        //call optimization functions and smooth trajectory
        if (pPPCInstance.trajectoryOptimization)
        {
            newPath = PathHandler.PathFancification(newPath, pPPCInstance.pathfindingLevel);
            newPath = PathHandler.PathOptimization(newPath, pPPCInstance.pathfindingLevel);
        }

        if (pPPCInstance.trajectorySmoothing)
            newPath = PathHandler.PathSmoothing(newPath, pPPCInstance.pathfindingLevel);

        if (cTInstance.IsCancellationRequested) return;

        for (int i = 1; i <= newPath.Count - 1; i++)
            totalLength += Vector3.Distance(newPath[i - 1], newPath[i]);

        lock (finalPath)
        {
            finalPath.AddRange(newPath);
        }

        if (cTInstance.IsCancellationRequested) return;
        lock (actionsQueue)
            actionsQueue.Enqueue(pPPCInstance.furtherActions);
    }

    /// <summary>
    /// Internal method.
    /// Performs pathfinding.
    /// (asynchronous execution is assumed)
    /// </summary>
    /// <param name="inpData"></param>
    void PathfindingThreadMethod(object inpData)
    {
        PathfindingParamContainer paramsData = (PathfindingParamContainer)inpData;
        try
        {
            if (paramsData.generateEvents)
            {
                if (!IsAtConstraintsArea(paramsData.target))
                {
                    lock (actionsQueue)
                        actionsQueue.Enqueue(new Action(() =>
                        {
                            curCond = WaitingForRequest;
                            ThrowUniversalEvent("EventPathfindingToOutwardPointRequested");
                        }));
                    return;
                }

                if (!IsAtConstraintsArea(paramsData.start))
                {
                    actionsQueue.Enqueue(new Action(() =>
                    {
                        curCond = WaitingForRequest;
                        ThrowUniversalEvent("EventPathfindingFromOutwardPointRequested");
                    }));

                    return;
                }

                if (SpaceGraph.IsCellOccStaticOnLevel(paramsData.start, 0))
                {
                    actionsQueue.Enqueue(new Action(() =>
                    {
                        curCond = WaitingForRequest;
                        ThrowUniversalEvent("EventPathfindingFromStaticOccupiedCellRequested");
                    }));
                    return;
                }

                if (SpaceGraph.IsCellOccStaticOnLevel(paramsData.target, 0))
                {
                    actionsQueue.Enqueue(new Action(() =>
                    {
                        curCond = WaitingForRequest;
                        ThrowUniversalEvent("EventPathfindingToStaticOccupiedCellRequested");
                    }));
                    return;
                }
            }

            List<Vector3> foundPath;
            // поиск пути, результат кладется в переменную foundPath
            if (paramsData.usingAlg == PathfindingAlgorithm.AStar)
            {
                Graph graph = new Graph(spaceManagerInstance, cTSInstance.Token, paramsData.start, paramsData.target,
                    paramsData.pathfindingLevel,
                    new SpaceConstraints { xMin = xMin, xMax = xMax, yMin = yMin, yMax = yMax, zMin = zMin, zMax = zMax },
                    heuristicFactor
                );
                foundPath = graph.GetWay();
            }
            else
            {
                WaveTrace waveTraceInst =
                    new WaveTrace(
                        new SpaceConstraints
                        {
                            xMin = xMin,
                            xMax = xMax,
                            yMin = yMin,
                            yMax = yMax,
                            zMin = zMin,
                            zMax = zMax
                        }, cTSInstance.Token, paramsData.start, paramsData.target);
                waves = waveTraceInst.waves;
                foundPath = waveTraceInst.GetWay();
            }

            if (foundPath == null)
            {
                lock (actionsQueue) actionsQueue.Enqueue(paramsData.failureActions);
                return;
            }

            if (paramsData.inThreadOptimizePath)
            {
                foundPath = PathHandler.PathFancification(foundPath, paramsData.pathfindingLevel);
                foundPath = PathHandler.PathOptimization(foundPath, paramsData.pathfindingLevel);
            }

            if (paramsData.inThreadSmoothPath)
                foundPath = PathHandler.PathSmoothing(foundPath, paramsData.pathfindingLevel);
            if (paramsData.inThreadOptimizePath || paramsData.inThreadSmoothPath)
            {
                totalLength = 0;
                for (int i = 0; i < foundPath.Count - 1; i++)
                    totalLength += Vector3.Distance(foundPath[i], foundPath[i + 1]);
            }

            lock (paramsData.outPath)
            {
                paramsData.outPath.Clear();
                paramsData.outPath.AddRange(foundPath);
            }

            lock (actionsQueue) actionsQueue.Enqueue(paramsData.succesActions);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex.Message);
        }
    }

    #endregion

    /// <summary>
    /// Draws a line along trajectory.
    /// </summary>
    /// <param name="path">Trajectory to be drawn.</param>
    void DrawAPath(List<Vector3> path)
    {
        GameObject lineContainer = new GameObject();
        lineContainer.name = "trajectory for GO" + gameObject.name;
        LineRenderer line = lineContainer.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.loop = false;

        line.positionCount = path.Count;
        line.SetPositions(path.ToArray());
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        trajectoryVisualLine = lineContainer;
        return;
    }

    /// <summary>
    /// Moves pursuer across the pass.
    /// Occurs each tick of Movement condition.
    /// </summary>
    void MotionMethod()
    {
        float curOffset = 0;
        float distToNext = 0, deltaDist;
        while (curOffset < speed)
        {

            distToNext = Vector3.Distance(finalPath[prePointI + 1], curPos);
            deltaDist = speed - curOffset;
            if (distToNext < deltaDist)
            {
                prePointI++;
                curOffset += distToNext;
                curPos = finalPath[prePointI];
                if (prePointI == finalPath.Count - 1)
                {
                    if (moveVectorOrientation) FollowTheDirection(thisTransformInstance.position, curPos);
                    thisTransformInstance.position = curPos;
                    offset = totalLength;
                    ThrowUniversalEvent("EventTargetReached");
                    return;
                }
            }
            else
            {
                curOffset += deltaDist;
                curPos += (finalPath[prePointI + 1] - curPos).normalized * deltaDist;
            }
        }

        if (moveVectorOrientation) FollowTheDirection(thisTransformInstance.position, curPos);
        thisTransformInstance.position = curPos;
        offset += curOffset;
    }

    /// <summary>
    /// Allows to start pathfinding method.
    /// (Calls from SpaceManager, when the queue is pumping to the moment, the turn comes to this Pursuer)
    /// </summary>
    public void AllowPathfinding()
    {
        if (GetCurCondition() == "WaitingForPursuersQueue")
        {
            ThrowUniversalEvent("EventPathfindingQueueCameUp");
            curCond = WaitingForAWay;

            ThrowUniversalEvent("EventPathfindingStarted");
            FindWay(thisTransformInstance.position, targetCoord, pathfindingLevel, foundPath, selectedPFAlg,
                defaultFailureAct, defaultSuccesAct);
        }
    }

    /// <summary>
    /// Adds this Pursuer instance to the pathfindong queue (at SpaceManager).
    /// </summary>
    /// <param name="topPriority">Put the pursuer in the first place in the pathfinding queue?</param>
    void QueueUpForPathfinding(bool topPriority = false)
    {
        if (topPriority)
            spaceManagerInstance.pathfindingQueue.Insert(0, this);
        else
            spaceManagerInstance.pathfindingQueue.Add(this);
        ThrowUniversalEvent("EventAddedToPathfindingQueue");
    }

    /// <summary>
    /// Removes this Pursuer instance from pathfinding queue.
    /// </summary>
    void LeavePathfindingQueue()
    {
        try
        {
            spaceManagerInstance.pathfindingQueue.Remove(this);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Rotates loacal z-axis of this pursuer's transform instance towards the vector = <paramref name="newPos"/>-<paramref name="prePos"/>.
    /// </summary>
    /// <param name="prePos">Previous pursuer's position.</param>
    /// <param name="newPos">Actual pursuer's position.</param>
    void FollowTheDirection(Vector3 prePos, Vector3 newPos)
    {
        thisTransformInstance.rotation = Quaternion.RotateTowards(thisTransformInstance.rotation,
            Quaternion.LookRotation(newPos - prePos), turnSpeed);
    }

    /// <summary>
    /// Determines whether a point is inside the pathfinding area.
    /// </summary>
    /// <param name="point"></param>
    /// <returns>True, if point is inside the area, esle - false.</returns>
    bool IsAtConstraintsArea(Vector3 point)
    {
        return (
            (point.x > xMin && point.x < xMax) &&
            (point.y > yMin && point.y < yMax) &&
            (point.z > zMin && point.z < zMax)
        );
    }

    #endregion

    /// <summary>
    /// Pursuer's FSM conditions. 
    /// All conditions descripted in User's manual.
    /// </summary>

    #region Conditions definition

    void WaitingForRequest()
    {
        if (generateCondMessages)
            gameObject.SendMessage("CondWaitingForRequest", SendMessageOptions.DontRequireReceiver);
    }

    void WaitingForPursuersQueue()
    {
        if (generateCondMessages)
            gameObject.SendMessage("CondWaitingForPursuersQueue", SendMessageOptions.DontRequireReceiver);
    }

    void WaitingForAWay()
    {
        if (generateCondMessages) gameObject.SendMessage("CondWaitingForAWay", SendMessageOptions.DontRequireReceiver);
    }

    void WaitingForWayProcessing()
    {
        if (generateCondMessages)
            gameObject.SendMessage("CondWaitingForWayProcessing", SendMessageOptions.DontRequireReceiver);
    }

    void WaitingForTheContinuation()
    {
        if (generateCondMessages)
            gameObject.SendMessage("CondWaitingForTheContinuation", SendMessageOptions.DontRequireReceiver);
    }

    void Movement()
    {
        MotionMethod();
        if (generateCondMessages) gameObject.SendMessage("CondMovement", SendMessageOptions.DontRequireReceiver);
    }

    #endregion

    /// <summary>
    /// Throws event with name, defined by <paramref name="eventDescr"/> argument.
    /// </summary>
    /// <param name="eventDescr">The name of the event to be thrown.</param>
    void ThrowUniversalEvent(string eventDescr)
    {
        switch (eventDescr)
        {
            case "EventTargetReached":
                if (trajectoryVisualLine != null)
                {
                    Destroy(trajectoryVisualLine);
                    trajectoryVisualLine = null;
                }

                curCond = WaitingForRequest;
                break;
            case "EventPathWasRefined":
                int startI = 0;
                float sqrMagnPre = Vector3.SqrMagnitude(thisTransformInstance.position - refinedPath[0]);
                for (var i = 1; i < refinedPath.Count; ++i)
                {
                    float newSqrMgn = Vector3.SqrMagnitude(thisTransformInstance.position - refinedPath[i]);
                    if (newSqrMgn <= sqrMagnPre)
                    {
                        sqrMagnPre = newSqrMgn;
                        startI = i;
                    }
                    else break;
                }

                for (var i = startI + 1; i < refinedPath.Count - 1; ++i)
                    if (Vector3.Angle(refinedPath[i] - thisTransformInstance.position,
                            refinedPath[i + 1] - refinedPath[i]) < 10)
                    {
                        startI = i;
                        break;
                    }

                refinedPath = refinedPath.Skip(startI).ToList();
                refinedPath.Insert(0, thisTransformInstance.position);
                refinedPath.Insert(refinedPath.Count - 1, refinedTargetCoord);

                totalLength += Vector3.Distance(refinedPath[0], refinedPath[1]) +
                               Vector3.Distance(refinedPath[refinedPath.Count - 2], refinedPath[refinedPath.Count - 1]);

                prePointI = 0;
                offset = 0;
                targetCoord = refinedTargetCoord;
                isRefiningInProgress = false;
                finalPath.Clear();
                finalPath.AddRange(refinedPath);
                if (tracePath)
                {
                    if (trajectoryVisualLine != null)
                    {
                        Destroy(trajectoryVisualLine);
                        trajectoryVisualLine = null;
                    }

                    DrawAPath(finalPath);
                }

                break;
            default:
                break;
        }

        if (generateEventMessages) gameObject.SendMessage(eventDescr, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// In-editor highlighting of the wave-trace algorithm executing.
    /// </summary>

    #region Pathfinding trace

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (showPathfindingZone)
        {
            Gizmos.color = Color.green;
            Vector3 zoneCenter = new Vector3(xMin + .5f * (xMax - xMin), yMin + .5f * (yMax - yMin),
                zMin + .5f * (zMax - zMin));
            Gizmos.DrawWireCube(zoneCenter, new Vector3((xMax - xMin), (yMax - yMin), (zMax - zMin)));
        }

        if (!inEditorPathfindingTraverce) return;

        Gizmos.color = Color.cyan;

        if (waves == null) return;

        lock (waves)
        {
            if (waves.Count <= 0) return;
            foreach (Vector3 cell in waves[waves.Count - 1])
            {
                Vector3 coord = SpaceGraph.GetCellCenterCoordFromIndexOnLevel(cell, 0);
                Gizmos.DrawWireCube(coord,
                    new Vector3(spaceManagerInstance.cellMinSize, spaceManagerInstance.cellMinSize,
                        spaceManagerInstance.cellMinSize));
            }
        }
    }
#endif

    #endregion
}

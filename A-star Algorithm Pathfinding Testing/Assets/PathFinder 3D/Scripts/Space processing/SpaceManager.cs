using System.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;
#if NETFX_CORE
    using Windows.System.Threading;
#else
using System.Threading;
#endif
using UnityEngine;
using PathFinder3D;

//Class providing the processing of the scene space and pumping the pursuers queue
public class SpaceManager : MonoBehaviour
{
    /// <summary>
    /// A list of game object tags that must be considered static obstacles.
    /// </summary>
    public List<string> staticObstacleTags;

    /// <summary>
    /// Number of spatial graph levels.
    /// </summary>
    public int gridDetailLevelsCount;

    /// <summary>
    /// The size of the side length of the cells, composing lowest spatial grap level.
    /// </summary>
    public float cellMinSize;

    /// <summary>
    /// Obtain graph at the scene start?
    /// </summary>
    public bool obtainGraphAtStart = true;

    /// <summary>
    /// Is in-editor occupied cells highlighting enabled?
    /// </summary>
    public bool traceCellsInEditor;

    /// <summary>
    /// Use this setting at your own risk
    /// </summary>
    public bool agressiveUseMultithreading;

    /// <summary>
    /// Number of the level, whose cells have to highlight.
    /// </summary>
    public int levelToTrace;

    /// <summary>
    /// Was the graph obtaining at the scene start done?
    /// </summary>
    public bool isPrimaryProcessingCompleted;

    /// <summary>
    /// Current selected graph obtaining method.
    /// </summary>
    public GraphObtainingMethods graphObtainingMethod;

    /// <summary>
    /// Var, that must be .bytes file, containing serialized graph data.
    /// </summary>
    public TextAsset serializedGraphToImport;

    /// <summary>
    /// The number of the pathfinding tasks, that may be runing simultaneousely.
    /// </summary>
    int pathfindingTasksCountLimit;

    /// <summary>
    /// Current count of the alive pathfinding tasks.
    /// </summary>
    public volatile int pathfindingThreadsCurCount = 0;

    /// <summary>
    /// The queue containing Pursuer.cs instances pending pathfinding permission.
    /// </summary>
    public List<Pursuer> pathfindingQueue;

    public SpaceHandler spaceHandlerInstance;

    /// <summary>
    /// Cancellation token, that passes to the all async tasks.
    /// The signal to stop the asynchronous task comes when the event is resolved: OnDestroy, OnApplicationQuit
    /// </summary>
    CancellationTokenSource cTSInstance;

    /// <summary>
    /// The method of selecting the maximum number of asynchronous tasks. (0,1,2)
    /// Changes from SpaceManagerEditor.cs
    /// </summary>
    public int threadsCountMode;
    /// <summary>
    /// The multiplier of cores count, that applys, if threadsCountMode is equal to 1
    /// </summary>
    public float coresCountMultiplier;

    /// <summary>
    /// The maximum allowed count of alive work threads for the ThreadPool class.
    /// </summary>
    public int allowedProcessorCoresCount;

    /// <summary>
    /// Maximum possible number of processor cores to use.
    /// Leave one free core, if possible, so as not to interfere with other processes.
    /// </summary>
    public static int maxPossibleProcessorCores = Math.Max(Environment.ProcessorCount - 1, 1);

    /// <summary>
    /// Timer for measuring of any durations of any operations.
    /// Using for debugging purposes.
    /// </summary>
    Stopwatch timer;

    /// <summary>
    /// The methods of the spatial graph obtaining.
    /// </summary>
    public enum GraphObtainingMethods
    {
        InGameProcessing,
        DeserializingFromFile
    };

    #region User's calls

    /// <summary>
    /// Primary scene processing.
    /// The method must be called once at start. 
    /// Calling this method provides the pathfinding ability in the future.
    /// </summary>
    /// <returns></returns>
    public bool PrimaryProcessing()
    {
        if (!isPrimaryProcessingCompleted)
        {
            timer = Stopwatch.StartNew();
            spaceHandlerInstance = new SpaceHandler(this, cellMinSize, gridDetailLevelsCount, staticObstacleTags,
                cTSInstance.Token, allowedProcessorCoresCount,agressiveUseMultithreading);
            if (graphObtainingMethod == GraphObtainingMethods.InGameProcessing)
                spaceHandlerInstance.HandleAllObstaclesOnScene();
            else
            {
                if (serializedGraphToImport == null) throw new Exception("BinaryFileNullReferenceException");
                BinaryGraphStorageSerializer.DeserializeBinaryAsync(new MemoryStream(serializedGraphToImport.bytes),
                    this, new Action(() => { spaceHandlerInstance.NotifyRediness(); }));
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Must be called during primary processing.
    /// Returns the ratio of processed trises count to total tris count.
    /// </summary>
    /// <returns>Ratio value</returns>
    public float GetProcessingProgress()
    {
        return (float) spaceHandlerInstance.processedTrisCount / (float) spaceHandlerInstance.totalTrisCount;
    }

    /// <summary>
    /// Must be called during primary processing.
    /// Returns the number of all trises over the all obstacels on the scene.
    /// </summary>
    /// <returns>Number of all trises.</returns>
    public int GetTotalTrisCountToProcess()
    {
        return spaceHandlerInstance.totalTrisCount;
    }

    /// <summary>
    /// Must be called during primary processing.
    /// Returns the number of curently processed trises.
    /// </summary>
    /// <returns>Number of processed trises.</returns>
    public int GetCurentProcessedTrisCount()
    {
        return spaceHandlerInstance.processedTrisCount;
    }

    /// <summary>
    /// Process new obstacle, instantiated after primary processing.
    /// Or update existing (may be useful, if obstacle changed its position, rotation,etc.)</summary>
    /// <param name="obstacleGOToHandle">Obstacle game object to handle</param>
    public void HandleAnObstacle(GameObject obstacleGOToHandle)
    {
        spaceHandlerInstance.UpdateGraphForObstacle(obstacleGOToHandle, cTSInstance.Token);
    }

    /// <summary>
    /// Removes obstacle from search graph.
    /// </summary>
    /// <param name="obstacleGOToRemove">Obstacle game object to remove.</param>
    /// <param name="destroyGOAfter">Destroy the game object after removing from the graph?</param>
    public void RemoveAnObstacle(GameObject obstacleGOToRemove, bool destroyGOAfter = false)
    {
        spaceHandlerInstance.ClearGraphForObstacle(obstacleGOToRemove);
        if (destroyGOAfter) Destroy(obstacleGOToRemove);
    }

    /// <summary>
    /// Returns the number of currently alive handling tasks, that runs asynchronously
    /// </summary>
    /// <returns>Number of currently alive handeling tasks</returns>
    public int GetAliveHandlingTasksCount()
    {
        return spaceHandlerInstance.aliveHandelingTasksCount;
    }

    /// <summary>
    /// Returns the size of the Queue of the entityes to be handeled as async task 
    /// </summary>
    /// <returns>Handling tasks queue size</returns>
    public int GetHandlingTasksQueueSize()
    {
        return spaceHandlerInstance.asyncTasksQueue.Count;
    }
    public List<string> GetHandlingTasksInfo()
    {
        List<string> tasksStrings = new List<string>();
        foreach (KeyValuePair<int, SpaceHandler.AsyncTaskDataContainer> pair in spaceHandlerInstance.aliveTasksDict)
        {
            string newStr = "";
            newStr += pair.Key + " ";
            newStr += pair.Value.gameObjectName + " ";
            newStr += pair.Value.trisCount + " ";
            newStr += pair.Value.verticesCount + " ";
            tasksStrings.Add(newStr);
        }
        return tasksStrings;
    }
    #endregion

    #region Other methods defenition

    private void Awake()
    {
        cTSInstance = new CancellationTokenSource();
        isPrimaryProcessingCompleted = false;
    }

    void Start()
    {
        pathfindingQueue = new List<Pursuer>();
        PathHandler.Init(cellMinSize * .25f);

        switch (threadsCountMode)
        {
            case 0:
                allowedProcessorCoresCount = maxPossibleProcessorCores;
                break;
            case 1:
                allowedProcessorCoresCount = Mathf.CeilToInt(Environment.ProcessorCount * coresCountMultiplier);
                break;
            case 2:
                break;
            default:
                break;
        }
        allowedProcessorCoresCount = Mathf.Min(
                    Mathf.Max(allowedProcessorCoresCount, 1), // To not be less than 1
                    maxPossibleProcessorCores); // To not be more than the number of available processor cores.
        pathfindingTasksCountLimit = allowedProcessorCoresCount;
        if (obtainGraphAtStart)
        {
            cTSInstance = new CancellationTokenSource();
            PrimaryProcessing();
        }
    }

    private void OnApplicationQuit()
    {
        cTSInstance.Cancel();
    }

    private void OnDestroy()
    {
        cTSInstance.Cancel();
    }

    /// <summary>
    /// Executes when primary processing is done.
    /// </summary>
    void PrimaryProcessingFinished()
    {
        spaceHandlerInstance.SetInternalTimerTickrate(50);
        timer.Stop();
        UnityEngine.Debug.Log("Time taken to obtain a graph is : " + timer.ElapsedMilliseconds);
    }

    void Update()
    {
        //Execution of instructions assigned by the SpaceGraph
        if (spaceHandlerInstance != null)
            while (spaceHandlerInstance.inUpdateInvocationsQueue.Count > 0)
            {
                Action someFunc = spaceHandlerInstance.inUpdateInvocationsQueue.Dequeue();
                try
                {
                    someFunc.Invoke();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log(ex.Message);
                }
            }

        //Permission to pathfinding for Pursuers in the queue
        if (pathfindingQueue.Count > 0 && pathfindingThreadsCurCount < pathfindingTasksCountLimit)
        {
            pathfindingQueue[0].AllowPathfinding();
            pathfindingQueue.RemoveAt(0);
        }
    }

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// Highlights all occupied cells, if appropriate toggle is active at inspector.
    /// </summary>
    public void OnDrawGizmos()
    {
        if (traceCellsInEditor && spaceHandlerInstance != null)
        {
            Gizmos.color = new Color(1, .46f, .46f, 1);
            float cellSize = SpaceGraph.GetLevelSideLength(levelToTrace);
            foreach (Vector3 index in SpaceGraph.occCellsLevels[levelToTrace].Keys)
                Gizmos.DrawWireCube(SpaceGraph.GetCellCenterCoordFromIndexOnLevel(index, levelToTrace),
                    new Vector3(cellSize * .99f, cellSize * .99f, cellSize * .99f));
        }
    }
#endif
}

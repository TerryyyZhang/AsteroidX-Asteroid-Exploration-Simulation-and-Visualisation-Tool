using System.Collections.Generic;
using System.Linq;
#if NETFX_CORE
    using Windows.System.Threading;
#else
using System.Threading;
#endif
using System;
using System.Diagnostics;
using UnityEngine;
using System.Collections.Concurrent;

namespace PathFinder3D
{
    /// <summary>
    /// The class implements the handling of obstacles in the game scene. 
    /// By processing game obstacles, here we mean updating the search graph under the current position of obstacles on the scene. 
    /// The main operations are the handling of a new obstacle and the removal of an existing one.
    /// </summary>
    public class SpaceHandler
    {
        /// <summary>
        /// The types of the entity to handle as the asynchronous task.
        /// </summary>
        enum EntityType
        {
            Mesh,
            Terrain
        };
        enum TaskType
        {
            Handling,
            Removing
        };

        /// <summary>
        /// Queue of the tasks, that must be invoced in the main thread.
        /// Invocations calling from Update event at SpaceManager class.
        /// </summary>
        public Queue<Action> inUpdateInvocationsQueue;

        /// <summary>
        /// Queue of the entityes to be handeled as async task.
        /// Dequeing occurs at StartAsyncTasks proc. during internalTimer tick.
        /// </summary>
        public ConcurrentQueue<object[]> asyncTasksQueue;

        public ConcurrentDictionary<int, AsyncTaskDataContainer> aliveTasksDict;

        /// <summary>
        /// Count of the current handeling tasks, that runs asyncronously. 
        /// </summary>
        public int aliveHandelingTasksCount;

        public int uniqeHandlingTaskID;

        /// <summary>
        /// A var-flag indicating the completion of scene primary processing.
        /// </summary>
        public bool isPrimaryProcessingCompleted;

        /// <summary>
        /// Use this setting at your own risk
        /// </summary>
        public bool agressiveUseMultithreading;

        /// <summary>
        /// A list of game object tags that must be considered static obstacles.
        /// It is a reference to the list, that defined at Spacemanager.
        /// </summary>
        List<string> staticObstacleTags;

        /// <summary>
        /// General cancellation token for all asyncronous tasks.
        /// </summary>
        CancellationToken broadcastCancToken;

        SpaceManager spaceManagerInstance;

        /// <summary>
        /// Offset value, using for triangles traversing.
        /// Depends on min cells min size.
        /// </summary>
        float shiftStep;

        int maxAliveAsyncTasksCount;
        TimerCallback tmCallback;

        /// <summary>
        /// The timer used for pumping asyncTasksQueue.
        /// </summary>
        Timer internalTimer;

        volatile public int totalTrisCount;
        public int processedTrisCount;

        /// <summary>
        /// Class - container. Using for transferring mesh object data to asyncronous task. 
        /// </summary>
        class MeshDataContainer
        {
            public string gameObjectName;
            public CustomTransform transformData;
            public Vector3[] vertices;
            public int[] triangles;
            public int instanceID;
            public object CTInstance;

            public MeshDataContainer Clone()
            {
                MeshDataContainer cloneInstance = new MeshDataContainer
                {
                    transformData = this.transformData
                };

                cloneInstance.vertices = new Vector3[this.vertices.Length];
                this.vertices.CopyTo(cloneInstance.vertices, 0);

                cloneInstance.triangles = new int[this.triangles.Length];
                this.triangles.CopyTo(cloneInstance.triangles, 0);


                cloneInstance.instanceID = this.instanceID;
                cloneInstance.CTInstance = this.CTInstance;
                cloneInstance.gameObjectName = this.gameObjectName;
                return cloneInstance;
            }
        }

        /// <summary>
        /// Class - container. Using for transferring terrain object data to asyncronous task. 
        /// </summary>
        class TerrainDataContainer
        {
            public string gameObjectName;
            public float maxH;
            public float[,] hMArray;
            public int xStart;
            public int xEnd;
            public int zStart;
            public int zEnd;
            public float hX;
            public float hZ;
            public int hMWidth;
            public Vector3 terrainPos;
            public float hMapScaleX;
            public float hMapScaleZ;
            public int instanceID;
            public object CTInstance;

            public TerrainDataContainer Clone()
            {
                TerrainDataContainer cloneInstance = new TerrainDataContainer
                {
                    maxH = this.maxH,
                    hMArray = this.hMArray,
                    xStart = this.xStart,
                    xEnd = this.xEnd,
                    zStart = this.zStart,
                    zEnd = this.zEnd,
                    hX = this.hX,
                    hZ = this.hZ,
                    hMWidth = this.hMWidth,
                    terrainPos = new Vector3(this.terrainPos.x, this.terrainPos.y, this.terrainPos.z),
                    hMapScaleX = this.hMapScaleX,
                    hMapScaleZ = this.hMapScaleZ,
                    instanceID = this.instanceID,
                    CTInstance = this.CTInstance,
                    gameObjectName = this.gameObjectName
                };


                return cloneInstance;
            }
        }

        public class AsyncTaskDataContainer
        {
            public string gameObjectName;
            public string threadName;
            public int uniqeTaskID;
            public int trisCount;
            public int verticesCount;
        }

        public SpaceHandler(SpaceManager spaceManagerInstance, float minSideLength, int gridLevelsCount,
            List<string> staticObstacleTags, object cancellationTokenInstance, int maxAliveThreadsCount, bool agressiveUseOfMultithreading)
        {
            processedTrisCount = 0;
            SpaceGraph.SetFields(minSideLength, gridLevelsCount, maxAliveThreadsCount);
            tmCallback = StartAsyncTasks;
            internalTimer = new Timer(tmCallback, null, 0, 4);
            inUpdateInvocationsQueue = new Queue<Action>();
            asyncTasksQueue = new ConcurrentQueue<object[]>();
            aliveTasksDict = new ConcurrentDictionary<int, AsyncTaskDataContainer>();
            this.staticObstacleTags = staticObstacleTags;
            this.spaceManagerInstance = spaceManagerInstance;
            this.maxAliveAsyncTasksCount = maxAliveThreadsCount;
            broadcastCancToken = (CancellationToken)cancellationTokenInstance;
            shiftStep = Mathf.Min(minSideLength * minSideLength, minSideLength * .5f);
            agressiveUseMultithreading = agressiveUseOfMultithreading;
        }


        #region Public methods

        /// <summary>
        /// Executes handeling of all obstacles on scene.
        /// </summary>
        /// <returns>Returns true if handeling succeded, false if no obstacles are found on the scene. </returns>
        public bool HandleAllObstaclesOnScene()
        {
            //selection all GameObjects at scene
            List<GameObject> generalGOList = GameObject.FindObjectsOfType(typeof(GameObject))
                .Select(item => (GameObject)item).ToList();

            //selection of objects, having mesh collider,terrain, or prohibited tag
            generalGOList = generalGOList.Where(go =>
                !go.GetComponent<Pursuer>() && go.activeInHierarchy && (
                    go.GetComponent<MeshCollider>() && go.GetComponent<MeshCollider>().sharedMesh &&
                    go.GetComponent<MeshCollider>().enabled == true ||
                    staticObstacleTags.Contains(go.tag) && go.GetComponent<MeshFilter>() &&
                    go.GetComponent<MeshFilter>().sharedMesh ||
                    go.GetComponent<Terrain>() && go.GetComponent<Terrain>().enabled == true
                )).ToList();

            if (generalGOList.Count == 0)
            {
                NotifyRediness();
                return false;
            }

            totalTrisCount = CountTheTotalNumOfTris(generalGOList);

            foreach (GameObject go in generalGOList)
                UpdateGraphForObstacle(go, broadcastCancToken);

            return true;
        }

        int CountTheTotalNumOfTris(List<GameObject> generalGOList)
        {
            int totalTrisCount = 0;
            foreach (GameObject obstcToCountTris in generalGOList)
            {
                MeshCollider meshColliderInst = obstcToCountTris.GetComponent<MeshCollider>();
                MeshFilter meshFilterInst = obstcToCountTris.GetComponent<MeshFilter>();
                Terrain terrainInst = obstcToCountTris.GetComponent<Terrain>();
                if (meshColliderInst && meshColliderInst.sharedMesh)
                    totalTrisCount += meshColliderInst.sharedMesh.triangles.Length / 3;

                if (meshFilterInst && meshFilterInst.sharedMesh)
                    totalTrisCount += meshFilterInst.sharedMesh.triangles.Length / 3;

                if (terrainInst)
                    totalTrisCount += (terrainInst.terrainData.heightmapWidth - 1) * (terrainInst.terrainData.heightmapWidth - 1) * 2;
            }
            return totalTrisCount;
        }
        /// <summary>
        /// Builds the navigation graph for obstacle. 
        /// If updateObstc is set to true cond., firstly, remove occuranse of obstacle in the graph.
        /// </summary>
        /// <param name="obstacleGOToUpdate">GameObject-obstacle, for which you want to update the graph.</param>
        /// <param name="cancToken">Cancellation token for controll to async handeling</param>
        /// <param name="entitiesTasksToParallelize">List, you want to put handelling task in (if you want)</param>
        /// <param name="updateObstc">Is obstc graph updating for, already was handeled once.</param>
        public void UpdateGraphForObstacle(GameObject obstacleGOToUpdate, CancellationToken cancToken, bool updateObstc = false)
        {
            MeshCollider meshColliderInst = obstacleGOToUpdate.GetComponent<MeshCollider>();
            MeshFilter meshFilterInst = obstacleGOToUpdate.GetComponent<MeshFilter>();
            Terrain terrainInst = obstacleGOToUpdate.GetComponent<Terrain>();
            if (updateObstc)
                SpaceGraph.ReleaseCellsFromObstcID(obstacleGOToUpdate.transform.GetInstanceID());
            MeshDataContainer mDCInst = new MeshDataContainer()
            {
                gameObjectName = obstacleGOToUpdate.name,
                transformData = obstacleGOToUpdate.transform,
                instanceID = obstacleGOToUpdate.transform.GetInstanceID(),
                CTInstance = cancToken
            };
            if (meshColliderInst && meshColliderInst.sharedMesh)
            {
                mDCInst.triangles = meshColliderInst.sharedMesh.triangles;
                mDCInst.vertices = meshColliderInst.sharedMesh.vertices;
                HandleEntity(mDCInst, EntityType.Mesh);
            }

            if (meshFilterInst && meshFilterInst.sharedMesh)
            {
                mDCInst.triangles = meshFilterInst.sharedMesh.triangles;
                mDCInst.vertices = meshFilterInst.sharedMesh.vertices;
                HandleEntity(mDCInst, EntityType.Mesh);
            }

            if (terrainInst)
            {
                float[,] hmArr = terrainInst.terrainData.GetHeights(0, 0, terrainInst.terrainData.heightmapWidth,
                    terrainInst.terrainData.heightmapWidth);
                TerrainDataContainer tDCInst = new TerrainDataContainer()
                {
                    gameObjectName = obstacleGOToUpdate.name,
                    maxH = terrainInst.terrainData.size.y,
                    hMArray = hmArr,
                    hMWidth = terrainInst.terrainData.heightmapWidth,
                    xStart = 1,
                    xEnd = terrainInst.terrainData.heightmapWidth,
                    zStart = 1,
                    zEnd = terrainInst.terrainData.heightmapWidth,
                    hX = terrainInst.terrainData.size.x,
                    hZ = terrainInst.terrainData.size.z,
                    terrainPos = terrainInst.GetPosition(),
                    hMapScaleX = terrainInst.terrainData.heightmapScale.x,
                    hMapScaleZ = terrainInst.terrainData.heightmapScale.z,
                    instanceID = terrainInst.transform.GetInstanceID(),
                    CTInstance = cancToken
                };
                HandleEntity(tDCInst, EntityType.Terrain);
            }
        }

        public void ClearGraphForObstacle(GameObject obstacleTORemove)
        {
            asyncTasksQueue.Enqueue(new object[] { TaskType.Removing, obstacleTORemove.transform.GetInstanceID() });
        }

        /// <summary>
        /// Calls after primary processing finished. 
        /// Initiates PrimaryProcessingFinished event at SpaceManager instance. 
        /// Initiates TheGraphIsReady event at each Pursuer instance. 
        /// </summary>
        public void NotifyRediness()
        {
            isPrimaryProcessingCompleted = true;
            spaceManagerInstance.isPrimaryProcessingCompleted = true;
            inUpdateInvocationsQueue.Enqueue(() =>
            {
                Component.FindObjectOfType<SpaceManager>().SendMessage("PrimaryProcessingFinished",
                    SendMessageOptions.DontRequireReceiver);
                List<GameObject> pursuersGOList = GameObject.FindObjectsOfType(typeof(GameObject))
                    .Select(item => (GameObject)item).Where(obj => obj.GetComponent<Pursuer>() != null).ToList();
                foreach (GameObject pursuer in pursuersGOList)
                {
                    pursuer.SendMessage("TheGraphIsReady", SendMessageOptions.DontRequireReceiver);
                }
            });
        }

        public void SetInternalTimerTickrate(int newMsTickRate)
        {
            internalTimer = new Timer(tmCallback, null, 0, newMsTickRate);
        }
        #endregion

        #region Class intenal methods

        /// <summary>
        /// Performs the distribution of obstacle handeling into several subtasks for their further asynchronous execution.
        /// </summary>
        /// <param name="entityData">MeshDataContainer or TerrainDataContainer, packed as object.</param>
        /// <param name="entityType">Represent type of the <paramref name="entityData" /> to provide correct unpack from object type.</param>
        /// <param name="entitiesTasksToParallelize">List in which subtasks puts.</param>
        void HandleEntity(object entityData, EntityType entityType)
        {
            if (entityType == EntityType.Mesh)
            {
                MeshDataContainer mDCInst = ((MeshDataContainer)entityData).Clone();
                int tripletsNum = Mathf.CeilToInt(((float)mDCInst.triangles.Length / 3) / maxAliveAsyncTasksCount);
                if (tripletsNum == 0) tripletsNum = 1;
                while (mDCInst.triangles.Length > 0)
                {
                    int trisIntsNum = Mathf.Min(mDCInst.triangles.Length, tripletsNum * 3);
                    int[] distrTrisArr = mDCInst.triangles.Take(trisIntsNum).ToArray();
                    mDCInst.triangles = mDCInst.triangles.Skip(trisIntsNum).ToArray();

                    MeshDataContainer newMDCInst = ((MeshDataContainer)entityData).Clone();
                    newMDCInst.triangles = distrTrisArr;
                    asyncTasksQueue.Enqueue(new object[] { TaskType.Handling, entityType, newMDCInst });
                }
            }
            else
            {
                int xDelta, n;
                int hMWidth = ((TerrainDataContainer)entityData).hMWidth;
                if (maxAliveAsyncTasksCount == 1)
                {
                    xDelta = hMWidth;
                    n = 1;
                }
                else
                {
                    xDelta = Mathf.CeilToInt((float)hMWidth / maxAliveAsyncTasksCount /16);
                    n = Mathf.CeilToInt((float)hMWidth / xDelta);
                }
                for (var i = 0; i < n; ++i)
                {
                    TerrainDataContainer tDCInst = ((TerrainDataContainer)entityData).Clone();
                    tDCInst.xStart = i * xDelta + 1;
                    tDCInst.xEnd = Mathf.Min(i * xDelta + xDelta + 1, hMWidth);
                    asyncTasksQueue.Enqueue(new object[] { TaskType.Handling, entityType, tDCInst });
                }
            }
        }

        /// <summary>
        /// Perfoms handeling of a mesh. Must be running asyncronously.
        /// </summary>
        /// <param name="inpData">Mesh to handle data, packed from MeshDataContainer instance. </param>
        void AsyncMeshMethod(object inpData)
        {
            try
            {
                if (agressiveUseMultithreading)
                    Interlocked.Increment(ref aliveHandelingTasksCount);
                MeshDataContainer mDCInstance = (MeshDataContainer)inpData;
                int curTaskId = uniqeHandlingTaskID;
                Interlocked.Increment(ref uniqeHandlingTaskID);
                AsyncTaskDataContainer aTDCInstance = new AsyncTaskDataContainer
                {
                    gameObjectName = mDCInstance.gameObjectName,
                    threadName = Thread.CurrentThread.Name,
                    uniqeTaskID = curTaskId,
                    trisCount = mDCInstance.triangles.Length / 3,
                    verticesCount = mDCInstance.vertices.Length
                };
                aliveTasksDict.TryAdd(curTaskId, aTDCInstance);
                int[] triangles = null;
                int p0i = 0;
                int p1i = 0;
                int p2i = 0;

                Vector3[] vertices = mDCInstance.vertices;
                triangles = mDCInstance.triangles;
                CustomTransform meshCTransf = mDCInstance.transformData;
                int instanceID = mDCInstance.instanceID;


                for (var i = 1; i <= mDCInstance.triangles.Length / 3; ++i)
                {
                    try
                    {
                        if ((broadcastCancToken.IsCancellationRequested) ||
                            ((CancellationToken)mDCInstance.CTInstance).IsCancellationRequested) return;
                        p0i = triangles[(i - 1) * 3];
                        p1i = triangles[(i - 1) * 3 + 1];
                        p2i = triangles[(i - 1) * 3 + 2];
                        Vector3 p0 = vertices[triangles[(i - 1) * 3]];
                        Vector3 p1 = vertices[triangles[(i - 1) * 3 + 1]];
                        Vector3 p2 = vertices[triangles[(i - 1) * 3 + 2]];

                        OccupyCellsForATriangle(
                            meshCTransf.position + meshCTransf.rotation * (Vector3.Scale(meshCTransf.localScale, p0)),
                            meshCTransf.position + meshCTransf.rotation * (Vector3.Scale(meshCTransf.localScale, p1)),
                            meshCTransf.position + meshCTransf.rotation * (Vector3.Scale(meshCTransf.localScale, p2)),
                            instanceID
                        );

                    }
                    catch (Exception ex)
                    {
                        if (!isPrimaryProcessingCompleted) Interlocked.Increment(ref processedTrisCount);
                        throw (ex);
                    }
                }

                aliveTasksDict.TryRemove(curTaskId, out aTDCInstance);
                Interlocked.Decrement(ref aliveHandelingTasksCount);
                if (!isPrimaryProcessingCompleted && processedTrisCount >= totalTrisCount && aliveHandelingTasksCount == 0) NotifyRediness();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex.Message);
            }
        }

        /// <summary>
        /// Perfoms handeling of the terrain. Must be running asyncronously.
        /// </summary>
        /// <param name="inpData">Terrain to handle data, packed from MeshDataContainer instance.</param>
        void AsyncTerrainMethod(object inpData)
        {
            if (agressiveUseMultithreading)
                Interlocked.Increment(ref aliveHandelingTasksCount);
            TerrainDataContainer tDCInst = (TerrainDataContainer)inpData;
            int curTaskId = uniqeHandlingTaskID;
            Interlocked.Increment(ref uniqeHandlingTaskID);
            AsyncTaskDataContainer aTDCInstance = new AsyncTaskDataContainer
            {
                gameObjectName = tDCInst.gameObjectName,
                threadName = Thread.CurrentThread.Name,
                uniqeTaskID = curTaskId,
                trisCount = (tDCInst.xEnd - tDCInst.xStart) * (tDCInst.zEnd - tDCInst.zStart) * 2,
                verticesCount = tDCInst.hMArray.Length
            };
            aliveTasksDict.TryAdd(curTaskId, aTDCInstance);
            try
            {
                CancellationToken localCancToken = (CancellationToken)tDCInst.CTInstance;
                Vector3 p0, p1, p2, p3;
                int step = 1;
                int hMWidth = tDCInst.hMWidth;
                Vector3 terrPos = tDCInst.terrainPos;
                float hZ = tDCInst.hZ;
                float hX = tDCInst.hX;
                float[,] hMArray = tDCInst.hMArray;
                float maxH = tDCInst.maxH;
                for (var x = tDCInst.xStart; x < tDCInst.xEnd; x += step)
                {
                    if (broadcastCancToken.IsCancellationRequested || localCancToken.IsCancellationRequested) return;
                    for (var z = tDCInst.zStart; z < tDCInst.zEnd; z += step)
                    {
                        p0 = new Vector3(((float)(z - step) / hMWidth) * hZ + terrPos.x,
                            (hMArray[x - step, z - step] * maxH) + terrPos.y,
                            ((float)(x - step) / hMWidth) * hX + terrPos.z);
                        p1 = new Vector3(((float)(z - step) / hMWidth) * hZ + terrPos.x,
                            (hMArray[x, z - step] * maxH) + terrPos.y, ((float)x / hMWidth) * hX + terrPos.z);
                        p2 = new Vector3(((float)z / hMWidth) * hZ + terrPos.x, (hMArray[x - step, z] * maxH) + terrPos.y,
                            ((float)(x - step) / hMWidth) * hX + terrPos.z);
                        p3 = new Vector3(((float)z / hMWidth) * hZ + terrPos.x, (hMArray[x, z] * maxH) + terrPos.y,
                            ((float)x / hMWidth) * hX + terrPos.z);
                        OccupyCellsForATriangle(p0, p1, p2, tDCInst.instanceID);
                        OccupyCellsForATriangle(p1, p2, p3, tDCInst.instanceID);
                    }
                }
                aliveTasksDict.TryRemove(curTaskId, out aTDCInstance);
                Interlocked.Decrement(ref aliveHandelingTasksCount);
                if (!isPrimaryProcessingCompleted && processedTrisCount >= totalTrisCount && aliveHandelingTasksCount == 0) NotifyRediness();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        void AsyncRemoveMethod(object inpData)
        {
            int instanceID = (int)inpData;
            SpaceGraph.ReleaseCellsFromObstcID(instanceID);
        }

        /// <summary>
        /// Injects instanceID into the cells of the graph occupied by a triangle
        /// consisting of points <paramref name="p0" />, <paramref name="p1" />, <paramref name="p2" />.
        /// </summary>
        /// <param name="p0">First triangle point.</param>
        /// <param name="p1">Second triangle point.</param>
        /// <param name="p2">Third triangle point.</param>
        /// <param name="instanceID">Specific ID of the transform instance of the game object, whose triangle is handling.</param>
        void OccupyCellsForATriangle(Vector3 p0, Vector3 p1, Vector3 p2, int instanceID)
        {
            if (!isPrimaryProcessingCompleted) Interlocked.Increment(ref processedTrisCount);

            float dist12 = Vector3.Distance(p2, p1);
            float offs12 = 0;
            float offs012;
            Vector3 point12 = p1;
            Vector3 point012;
            while (offs12 < dist12)
            {
                point12 = p1 + ((p2 - p1).normalized) * offs12;
                SpaceGraph.OccupyCellsInCoordAtAllLevels(point12, instanceID);
                offs012 = 0;
                while (offs012 * offs012 < Vector3.SqrMagnitude(p0 - point12))
                {
                    point012 = p0 + ((point12 - p0).normalized) * offs012;
                    SpaceGraph.OccupyCellsInCoordAtAllLevels(point012, instanceID);
                    offs012 += shiftStep;
                }

                offs12 += shiftStep;
            }

        }

        /// <summary>
        /// Performs async starting of the handeling tasks for entities, queued in the asyncTasksQueue.
        /// </summary>
        /// <param name="obj"></param>
        void StartAsyncTasks(object obj)
        {
            while (asyncTasksQueue.Count > 0 && aliveHandelingTasksCount < maxAliveAsyncTasksCount)
            {
                object[] curObjTask;
                bool result = asyncTasksQueue.TryDequeue(out curObjTask);
                if (result && !agressiveUseMultithreading)
                    Interlocked.Increment(ref aliveHandelingTasksCount);
                EntityType entityTaskType = (EntityType)curObjTask[1];
                TaskType taskType = (TaskType)curObjTask[0];
                if (taskType == TaskType.Handling)
                {
                    if (entityTaskType == EntityType.Mesh)
                        ThreadPool.QueueUserWorkItem(AsyncMeshMethod, (MeshDataContainer)curObjTask[2]);
                    else
                        ThreadPool.QueueUserWorkItem(AsyncTerrainMethod, (TerrainDataContainer)curObjTask[2]);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(AsyncRemoveMethod, (int)curObjTask[1]);
                }
            }
        }

        #endregion
    }
}
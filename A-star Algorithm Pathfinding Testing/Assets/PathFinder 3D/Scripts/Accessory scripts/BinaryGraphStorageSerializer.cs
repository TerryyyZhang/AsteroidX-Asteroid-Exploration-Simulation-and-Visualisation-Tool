using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
#if NETFX_CORE
    using Windows.System.Threading;
#else
using System.Threading;
#endif
using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using PathFinder3D;

public static class BinaryGraphStorageSerializer
{
    public static void DeserializeBinaryAsync(Stream sReader, SpaceManager sPMInst, Action doAfter)
    {
        ThreadPool.QueueUserWorkItem(DeserializeBinaryThreading,new object[] { sReader,sPMInst,doAfter });
      /*  Thread deserializeThread = new Thread(DeserializeBinaryThreading);
        deserializeThread.Priority = System.Threading.ThreadPriority.Highest;
        deserializeThread.Start(new object[] { sReader, sPMInst, doAfter });*/
    }
    public static void SerializeBinary(Stream sWriter, List<ConcurrentDictionary<Vector3, ConcurrentDictionary<int,int>>> storage, SpaceManager sPMInst)
    {
        List<List<IndexHSPair>> listedStorage = new List<List<IndexHSPair>>(storage.Count);
        for (int i = 0; i < storage.Count; i++)
        {
            listedStorage.Add(new List<IndexHSPair>(storage[i].Count));
            foreach (Vector3 key in storage[i].Keys)
            {
                listedStorage[i].Add(new IndexHSPair(new Index3(key), storage[i][key].Keys.ToArray()));
            }
        }
        listedStorage.Add(new List<IndexHSPair>());

        string strCellMinSize = Convert.ToString(sPMInst.cellMinSize);
        int dotPos = strCellMinSize.IndexOf(',');
        if (dotPos == -1) dotPos = strCellMinSize.IndexOf('.');
        strCellMinSize = dotPos > -1 ? strCellMinSize.Remove(dotPos, 1) : strCellMinSize;
        char[] strArr = strCellMinSize.ToCharArray();
        Array.Reverse(strArr);
        strCellMinSize = new string(strArr);
        
        List<int> cellSizeData = new List<int>();
        cellSizeData.Add(dotPos);
        cellSizeData.Add(Convert.ToInt32(strCellMinSize));

        listedStorage[listedStorage.Count - 1].Add(new IndexHSPair(new Index3(Vector3.zero),cellSizeData.ToArray()));

        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(sWriter, listedStorage);
        
    }
    public static void DeserializeBinary(Stream sReader, SpaceManager sPMInst)
    {
        Stopwatch timer;
        timer = Stopwatch.StartNew();
        sReader.Position = 0;
        BinaryFormatter formatter = new BinaryFormatter();
        List<List<IndexHSPair>> listedStorage = (List<List<IndexHSPair>>)formatter.Deserialize(sReader);

        UnityEngine.Debug.Log("Deserialization takes: " + timer.ElapsedMilliseconds);
        timer = Stopwatch.StartNew();

        try
        {
            int[] unboxedArr = listedStorage[listedStorage.Count - 1][0].Value;
            int dotPos = unboxedArr[0];
            int tmpInt = unboxedArr[1];

            string strCellMinSize = "";

            strCellMinSize = Convert.ToString(tmpInt);
            char[] strArr = strCellMinSize.ToCharArray();
            Array.Reverse(strArr);
            strCellMinSize = new string(strArr);
            strCellMinSize = dotPos > -1 ? strCellMinSize.Insert(dotPos, ",") : strCellMinSize;

            sPMInst.cellMinSize = Convert.ToSingle(strCellMinSize);
            SpaceGraph.cellMinSideLength = sPMInst.cellMinSize;
            List<ConcurrentDictionary<Vector3, ConcurrentDictionary<int,int>>> storage = new List<ConcurrentDictionary<Vector3, ConcurrentDictionary<int,int>>>(listedStorage.Count);


            for (int i = 0; i < listedStorage.Count; i++)
            {
                storage.Add(new ConcurrentDictionary<Vector3, ConcurrentDictionary<int,int>>(sPMInst.allowedProcessorCoresCount, listedStorage[i].Count));
                foreach (IndexHSPair entry in listedStorage[i])
                {
                    ConcurrentDictionary<int,int> tmpHS = new ConcurrentDictionary<int,int>();
                    for (int j = 0; j < entry.Value.Length; j++)
                        tmpHS.TryAdd(entry.Value[j],0);
                    
                       bool result = storage[i].TryAdd((Vector3)entry.Key, tmpHS);
                        if (!result)
                            foreach (int key in tmpHS.Keys)
                                storage[i][(Vector3)entry.Key].TryAdd(key,0);
                }
            }


            SpaceGraph.occCellsLevels = storage;
            sPMInst.gridDetailLevelsCount = storage.Count-1;
            SpaceGraph.amountOfSpatialGraphLevels = (Int16)sPMInst.gridDetailLevelsCount;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex.Message);
        }
        UnityEngine.Debug.Log("Collections transformations takes: " + timer.ElapsedMilliseconds);
    }
    static void DeserializeBinaryThreading(object inpData)
    {
        DeserializeBinary((Stream)((object[])inpData)[0],(SpaceManager)((object[])inpData)[1]);
        ((Action)((object[])inpData)[2]).Invoke();
//        lock(((SpaceManager)((object[])inpData)[1]).spaceHandlerInstance.listToDo)

 /*       ((SpaceManager)((object[])inpData)[1]).spaceHandlerInstance.listToDo.Add(new Action(() =>
        {
            System.Reflection.MethodInfo method = typeof(SpaceHandler).GetMethod("NotifyRediness");
            method.Invoke(((SpaceManager)((object[])inpData)[1]).spaceHandlerInstance, null);
        }));*/
    }
    [Serializable]
    public class IndexHSPair
    {
        public Index3 Key;
        public int[] Value;
        public IndexHSPair()
        {
        }

        public IndexHSPair(Index3 key, int[] value)
        {
            Key = key;
            Value = value;
        }
    }
    [Serializable]
    public class Index3
    {
        public short x;
        public short y;
        public short z;
        public Index3()
        {
        }
        public Index3(Vector3 inpV3)
        {
            this.x = (short)inpV3.x;
            this.y = (short)inpV3.y;
            this.z = (short)inpV3.z;
        }
        public static explicit operator Vector3(Index3 i)
        {
            return(new Vector3(i.x,i.y,i.z));
        }
    }
}

using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using PathFinder3D;

[CustomEditor(typeof(SpaceManager))]
public class SpaceManagerEditor : Editor
{
    string newTagStr = "Untagged";
    bool isHandelingInProgress;
    SpaceManager thisSpaceManagerInst;
    SpaceHandler spaceHandlerInstance;

    CancellationTokenSource CTSInstance;

    //    TextAsset serializedFileToImport = null;
    SerializedProperty _cellMinSize,
        _gridDetailLevelsCount,
        _obtainGraphAtStart,
        _traceCellsInEditor,
        _allowedProcessorCoresCount,
        _graphObtainingMethod,
        _serilizedGraphToImport,
        _levelToTrace,
        _threadsCountMode,
        _coresCountMultiplier,
        _agressiveUseMultithreading;

    private void OnEnable()
    {
        _gridDetailLevelsCount = serializedObject.FindProperty("gridDetailLevelsCount");
        _cellMinSize = serializedObject.FindProperty("cellMinSize");
        _obtainGraphAtStart = serializedObject.FindProperty("obtainGraphAtStart");
        _traceCellsInEditor = serializedObject.FindProperty("traceCellsInEditor");
        _graphObtainingMethod = serializedObject.FindProperty("graphObtainingMethod");
        _serilizedGraphToImport = serializedObject.FindProperty("serializedGraphToImport");
        _levelToTrace = serializedObject.FindProperty("levelToTrace");
        _allowedProcessorCoresCount = serializedObject.FindProperty("allowedProcessorCoresCount");
        _threadsCountMode = serializedObject.FindProperty("threadsCountMode");
        _coresCountMultiplier = serializedObject.FindProperty("coresCountMultiplier");
        _agressiveUseMultithreading = serializedObject.FindProperty("agressiveUseMultithreading");
    }

    public override void OnInspectorGUI()
    {
        thisSpaceManagerInst = (SpaceManager) target;
        EditorGUILayout.BeginVertical();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Assign obstacle tags:", EditorStyles.boldLabel);
        if (thisSpaceManagerInst.staticObstacleTags != null)
            for (int i = 0; i < thisSpaceManagerInst.staticObstacleTags.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                thisSpaceManagerInst.staticObstacleTags[i] =
                    EditorGUILayout.TagField(thisSpaceManagerInst.staticObstacleTags[i]);
                if (GUILayout.Button("Delete tag")) thisSpaceManagerInst.staticObstacleTags.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }

        EditorGUILayout.Separator();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Select an obstacle tag:", EditorStyles.wordWrappedLabel);
        newTagStr = EditorGUILayout.TagField(newTagStr);
        if (GUILayout.Button("Add tag"))
            if (newTagStr != "Untagged" && !thisSpaceManagerInst.staticObstacleTags.Contains(newTagStr))
                thisSpaceManagerInst.staticObstacleTags.Add(newTagStr);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Space processing settings:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Minimum size of cells, composing the space", EditorStyles.wordWrappedLabel);
        _cellMinSize.floatValue = Mathf.Max(EditorGUILayout.FloatField(thisSpaceManagerInst.cellMinSize), .05f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Number of spatial graph levels", EditorStyles.wordWrappedLabel);
        _gridDetailLevelsCount.intValue = EditorGUILayout.IntSlider(thisSpaceManagerInst.gridDetailLevelsCount, 1, 15);
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Show obstacle cells in editor", EditorStyles.wordWrappedLabel);
        _traceCellsInEditor.boolValue = EditorGUILayout.Toggle(_traceCellsInEditor.boolValue);
        EditorGUILayout.EndHorizontal();
        if (thisSpaceManagerInst.traceCellsInEditor)
            _levelToTrace.intValue = EditorGUILayout.IntSlider(thisSpaceManagerInst.levelToTrace, 0,
                thisSpaceManagerInst.gridDetailLevelsCount - 1);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Graph obtaining at scene start", EditorStyles.wordWrappedLabel);
        _obtainGraphAtStart.boolValue = EditorGUILayout.Toggle(_obtainGraphAtStart.boolValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Source of graph obtaining:");
        SpaceManager.GraphObtainingMethods newEnum =
            (SpaceManager.GraphObtainingMethods) _graphObtainingMethod.enumValueIndex;
        newEnum = (SpaceManager.GraphObtainingMethods) EditorGUILayout.EnumPopup(newEnum);
        _graphObtainingMethod.enumValueIndex = (int) newEnum;
        EditorGUILayout.EndHorizontal();

        if (thisSpaceManagerInst.graphObtainingMethod == SpaceManager.GraphObtainingMethods.DeserializingFromFile)
        {
            EditorGUILayout.BeginHorizontal();
            _serilizedGraphToImport.objectReferenceValue = (TextAsset) EditorGUILayout.ObjectField(
                "Choose .bytes file:", _serilizedGraphToImport.objectReferenceValue, typeof(TextAsset), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (!isHandelingInProgress)
            {
                if (GUILayout.Button("Handle obstacles and serialize"))
                {
                    CTSInstance = new CancellationTokenSource();
                    spaceHandlerInstance = new SpaceHandler(thisSpaceManagerInst, thisSpaceManagerInst.cellMinSize,
                        thisSpaceManagerInst.gridDetailLevelsCount, thisSpaceManagerInst.staticObstacleTags,
                        CTSInstance.Token, Environment.ProcessorCount,false);
                    if (!spaceHandlerInstance.HandleAllObstaclesOnScene())
                        Debug.Log("There is no any obstacles at the scene");
                    else
                        isHandelingInProgress = true;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Obstacles handeling in progress..", EditorStyles.wordWrappedLabel);
                if (EditorUtility.DisplayCancelableProgressBar("Obstacles handeling progress",
                    "Tris handeling: " + spaceHandlerInstance.processedTrisCount + "/" +
                    spaceHandlerInstance.totalTrisCount,
                    (float) spaceHandlerInstance.processedTrisCount / (float) spaceHandlerInstance.totalTrisCount))
                {
                    FinishInEditorHandeling();
                }

                if (spaceHandlerInstance != null && spaceHandlerInstance.isPrimaryProcessingCompleted)
                {
                    FinishInEditorHandeling();
                    string filePath = EditorUtility.SaveFilePanelInProject("Save serialized spatial graph data",
                        SceneManager.GetActiveScene().name + "_" + thisSpaceManagerInst.cellMinSize + "_" +
                        thisSpaceManagerInst.gridDetailLevelsCount, "bytes",
                        "Please enter a file name to save serialized data to");
                    using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
                    {
                        BinaryGraphStorageSerializer.SerializeBinary(fs, SpaceGraph.occCellsLevels,
                            thisSpaceManagerInst);
                    }

                    AssetDatabase.Refresh();
                    _serilizedGraphToImport.objectReferenceValue =
                        (TextAsset) AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset));
                }

                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.LabelField("Threading settings:", EditorStyles.boldLabel);
        int ProcessorCount = Environment.ProcessorCount - 1;
        if (ProcessorCount <= 0) ProcessorCount = 1;
        EditorGUILayout.LabelField("Number of available processor cores is " + ProcessorCount + ".\n(1 core will be left for other processes whenever possible.)", EditorStyles.wordWrappedLabel);

        EditorGUILayout.LabelField("The number of processor cores allowed for use by the asset:", EditorStyles.wordWrappedLabel);
        string[] selectionStrings = { "All processor cores", "Number of Processor cores with a multiplier", "Fixed value" };
        _threadsCountMode.intValue = GUILayout.SelectionGrid(thisSpaceManagerInst.threadsCountMode, selectionStrings, 1);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Aggressive use of multi-threading", EditorStyles.wordWrappedLabel);
        _agressiveUseMultithreading.boolValue = EditorGUILayout.Toggle(_agressiveUseMultithreading.boolValue);
        EditorGUILayout.EndHorizontal();

        if (thisSpaceManagerInst.threadsCountMode == 1)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Multiplier: ");
            _coresCountMultiplier.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(thisSpaceManagerInst.coresCountMultiplier), .05f, 1f);
            EditorGUILayout.EndHorizontal();
        }
        if (thisSpaceManagerInst.threadsCountMode == 2)
        {
            EditorGUILayout.LabelField("This value will be reduced on the user's device if it has a smaller number of cores.", EditorStyles.wordWrappedLabel);
            _allowedProcessorCoresCount.intValue = EditorGUILayout.IntSlider(thisSpaceManagerInst.allowedProcessorCoresCount, 1, ProcessorCount);
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
        Undo.RecordObject(target, "Static obstacle tag added");
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    #region accesory funcs

    void FinishInEditorHandeling()
    {
        EditorUtility.ClearProgressBar();
        isHandelingInProgress = false;
        thisSpaceManagerInst.isPrimaryProcessingCompleted = false;
        CTSInstance.Cancel();
    }

    #endregion
}

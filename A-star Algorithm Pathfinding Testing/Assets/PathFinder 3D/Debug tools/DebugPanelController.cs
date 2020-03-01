using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanelController : MonoBehaviour
{
    SpaceManager spaceManagerInstance;
    public Slider progressbarSlider;
    public Text processingProgressText;
    public Text taskCountText;
    public Text tasksQueueSizeText;
    public Text tasksInfoListText;
    public Text additionalText;
    // Start is called before the first frame update
    void Start()
    {
        spaceManagerInstance = Component.FindObjectOfType<SpaceManager>();
        if (!spaceManagerInstance.obtainGraphAtStart) Destroy(this);
    }

    // Update is called once per frame
    void Update()
    {
        
        tasksQueueSizeText.text = spaceManagerInstance.GetHandlingTasksQueueSize().ToString();
        progressbarSlider.value = spaceManagerInstance.GetProcessingProgress();

        taskCountText.text = spaceManagerInstance.GetAliveHandlingTasksCount().ToString();
        tasksInfoListText.text = "";
        List<string> taskInfoList = spaceManagerInstance.GetHandlingTasksInfo();
        foreach (string taskString in taskInfoList)
        {
            tasksInfoListText.text += taskString + "\n";
        }

        processingProgressText.text = "Tris processed: " + spaceManagerInstance.GetCurentProcessedTrisCount() + " / " + spaceManagerInstance.GetTotalTrisCountToProcess();
     //   if (progressbarSlider.value == 1)
     //       Destroy(this);
    }
    
}

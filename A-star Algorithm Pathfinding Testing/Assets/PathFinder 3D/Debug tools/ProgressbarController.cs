using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PathFinder3D;

public class ProgressbarController : MonoBehaviour
{

    SpaceManager spaceManagerInstance;
    Slider progressbarSlider;
    Text processingProgressText;
    void Start()
    {
        spaceManagerInstance = Component.FindObjectOfType<SpaceManager>();
        progressbarSlider = gameObject.GetComponentInChildren<Slider>();
        processingProgressText = progressbarSlider.GetComponentInChildren<Text>();
        if (!spaceManagerInstance.obtainGraphAtStart) Destroy(this);
    }

    // Update is called once per frame
    void Update()
    {

        progressbarSlider.value = spaceManagerInstance.GetProcessingProgress();
        processingProgressText.text = "Tris processed: " + spaceManagerInstance.GetCurentProcessedTrisCount() + " / " + spaceManagerInstance.GetTotalTrisCountToProcess();
        if (progressbarSlider.value == 1)
            Destroy(this);

    }
}

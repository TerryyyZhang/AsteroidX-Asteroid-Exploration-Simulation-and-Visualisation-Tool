using System.Collections;
using System.Collections.Generic;
#if NETFX_CORE
    using Windows.System.Threading;
#else
using System.Threading;
#endif
using UnityEngine;
using PathFinder3D;

//This class is not used in this version of the asset
public class DynamicObstacleController : MonoBehaviour
{

    public float updatePeriod = 0.5f;
    public bool canUpdate = true;

    CancellationTokenSource cTSInstance;
    public SpaceHandler spaceHandlerInstance;
    public SpaceManager spaceManagerInstance;
    Vector3 oldPos;
    Vector3 oldScale;
    Quaternion oldRot;
    bool isProcessingAllowed;
    bool isProcessing;
    bool isInQueue;

    private void Awake()
    {
        if (!spaceManagerInstance) spaceManagerInstance = Component.FindObjectOfType<SpaceManager>();
        cTSInstance = new CancellationTokenSource();
        oldPos = transform.position;
        oldRot = transform.rotation;
        oldScale = transform.localScale;
    }

    private void OnDestroy()
    {
        cTSInstance.Cancel();
        SpaceGraph.ReleaseCellsFromObstcID(transform.GetInstanceID());
    }

    void Update()
    {
        if (canUpdate && spaceHandlerInstance.isPrimaryProcessingCompleted)
        {
            if (Vector3.Distance(oldPos, transform.position) > SpaceGraph.cellMinSideLength)
            {
                //                Debug.Log("position transformation happens");
                oldPos = transform.position;
                spaceHandlerInstance.UpdateGraphForObstacle(gameObject, cTSInstance.Token);
                canUpdate = false;
                StartCoroutine(allowUpdateTimer());
                return;
            }

            if (oldRot != transform.rotation)
            {
                //                   Debug.Log("rotation transformation happens");
                oldRot = transform.rotation;
                spaceHandlerInstance.UpdateGraphForObstacle(gameObject, cTSInstance.Token);
                canUpdate = false;
                StartCoroutine(allowUpdateTimer());
                return;
            }

            if (oldScale != transform.localScale)
            {
                //                Debug.Log("scale transformation happens");
                oldScale = transform.localScale;
                spaceHandlerInstance.UpdateGraphForObstacle(gameObject, cTSInstance.Token);
                canUpdate = false;
                StartCoroutine(allowUpdateTimer());
                return;
            }
        }
    }

    public void SetUpdatePeriod(float seconds)
    {
        updatePeriod = seconds;
    }

    IEnumerator allowUpdateTimer()
    {
        yield return new WaitForSeconds(updatePeriod);
        canUpdate = true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Pursuer))]
public class MissleController : MonoBehaviour
{
    public Transform target;
    Vector3 targetOldPos;
    public DefenderController dCInstance;
    public float targetLesionAreaRadius;
    public float targetPathUpdateOffset;
    Pursuer thisPursuerInstance;

    //Start pursuit of the target
    public void BeginPursuit(GameObject target)
    {
        this.target = target.transform;
        thisPursuerInstance = GetComponent<Pursuer>();
        targetOldPos = target.transform.position;
        targetPathUpdateOffset = 8;
        thisPursuerInstance.MoveTo(target.transform);
    }
    private void Update()
    {
        //if the target for some reason was destroyed, destroy missle
        if (target == null) { Destroy(gameObject); return; }

        //if the target has moved from the previous coordinate(targetOldPos) to more than "targetPathUpdateOffset", update the path to the target
        if (Vector3.Distance(targetOldPos, target.position) > targetPathUpdateOffset)
        {
            targetOldPos = target.position;
            if (thisPursuerInstance.GetCurCondition() == "Movement")
                thisPursuerInstance.RefinePath(target);
            else
                thisPursuerInstance.MoveTo(target, true);
        }
    }
    //Destroy the target and the missile if the missile entered the target lesion area
    public void EventTargetReached()
    {
        if (Vector3.SqrMagnitude(target.position- transform.position) <= targetLesionAreaRadius * targetLesionAreaRadius)
        {
            dCInstance.enemyDict.Remove(target);
            Destroy(target.gameObject);
            Destroy(gameObject);
        }
    }
    //If the missile was inside a cell occupied by a static obstacle and tried to find a path, then destroy the missile and make the target accessible for pursuit
    public void EventPathfindingFromStaticOccupiedCellRequested()
    {
        dCInstance.enemyDict[target] = false;
        Destroy(gameObject);
    }
    //If the target is in a cell occupied by a dynamic obstacle, repeat the search for it after a while (perhaps by that time the target will be in the free cell)
    public void EventPathfindingToDynamicOccupiedCellRequested()
    {
        StartCoroutine(RepeatMoveTo());
    }
    //If the target is in a cell occupied by a static obstacle, repeat the search for it after a while (perhaps by that time the target will be in the free cell)
    public void EventPathfindingToStaticOccupiedCellRequested()
    {
        StartCoroutine(RepeatMoveTo());
    }
    //Retry the move to the target after a while
    IEnumerator RepeatMoveTo()
    {
        yield return new WaitForSecondsRealtime(1);
        GetComponent<Pursuer>().MoveTo(target);
    }
}

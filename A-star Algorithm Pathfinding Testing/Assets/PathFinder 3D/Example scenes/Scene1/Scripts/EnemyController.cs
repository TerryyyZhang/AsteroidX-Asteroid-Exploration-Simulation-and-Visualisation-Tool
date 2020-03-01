using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathFinder3D;

[RequireComponent(typeof(Pursuer))]
public class EnemyController : MonoBehaviour {
    public void EventTargetReached()
    {
        Destroy(gameObject);
    }
    public void EventPathfindingFromStaticOccupiedCellRequested()
    {
        Destroy(gameObject);
    }
}

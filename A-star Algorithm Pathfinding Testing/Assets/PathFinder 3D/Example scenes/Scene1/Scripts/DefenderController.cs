using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DefenderController : MonoBehaviour
{
    
    public List<Transform> wayPoints = new List<Transform>();
    [HideInInspector]
    //Key- enemy Transform, bool - flag, that show if the enemy captured by any missle 
    public Dictionary<Transform, bool> enemyDict = new Dictionary<Transform, bool>();
    public GameObject misslePrefab;
    public Transform missleSpawn;
    Vector3 curVector;
    public float speed = 1f;
    public int leftWayPointI;
    public int rightWayPointI;
    public float launchDelay = 0.2f;

    bool drawTrajectory = true;
    public bool optimizeTrajectory = true;
    public bool smoothTrajectory = true;
    void Start()
    {
        if (wayPoints.Count >= 2)
        {
            curVector = wayPoints[1].position - wayPoints[0].position;
            leftWayPointI = 0;
            rightWayPointI = 1;
            transform.position = wayPoints[0].position + curVector.normalized * Vector3.Distance(wayPoints[1].position, wayPoints[0].position) * 0.5f;
            transform.rotation = Quaternion.LookRotation(curVector);
        }
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && enemyDict.Values.Any(v => !v))
        {
            float dist = 500;
            Transform selectedTarget=null;
            foreach (KeyValuePair<Transform,bool> pair in enemyDict.Where(en => !en.Value))
            {
                Transform target=pair.Key;
                if (!target) continue;
                float curDist = Vector3.Distance(target.position, missleSpawn.transform.position);
                if (curDist <= dist)
                {
                    selectedTarget = target;
                    dist = curDist;
                }
            }
            if (selectedTarget)
            {
                GameObject missleGO = Instantiate(misslePrefab, missleSpawn.transform.position, missleSpawn.rotation);
                missleGO.GetComponent<MissleController>().BeginPursuit(selectedTarget.gameObject);
                missleGO.GetComponent<MissleController>().dCInstance = this;
                missleGO.GetComponent<Pursuer>().trajectoryOptimization = optimizeTrajectory;
                missleGO.GetComponent<Pursuer>().trajectorySmoothing = smoothTrajectory;
                missleGO.GetComponent<Pursuer>().tracePath = drawTrajectory;
                enemyDict[selectedTarget] = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += curVector.normalized * speed;
            if (Vector3.Distance(transform.position, wayPoints[leftWayPointI].position) > Vector3.Distance(wayPoints[rightWayPointI].position, wayPoints[leftWayPointI].position))
            {
                if (rightWayPointI == wayPoints.Count - 1) { transform.position = wayPoints[rightWayPointI].position; return; }
                leftWayPointI++;
                rightWayPointI++;
                curVector = wayPoints[rightWayPointI].position - wayPoints[leftWayPointI].position;
                transform.position = wayPoints[leftWayPointI].position;
            }
            transform.rotation = Quaternion.LookRotation(curVector);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= curVector.normalized * speed;
            if (Vector3.Distance(transform.position, wayPoints[rightWayPointI].position) > Vector3.Distance(wayPoints[rightWayPointI].position, wayPoints[leftWayPointI].position))
            {
                if (leftWayPointI == 0) { transform.position = wayPoints[leftWayPointI].position; return; }
                leftWayPointI--;
                rightWayPointI--;
                curVector = wayPoints[rightWayPointI].position - wayPoints[leftWayPointI].position;
                transform.position = wayPoints[rightWayPointI].position;
            }
            transform.rotation = Quaternion.LookRotation(curVector);
        }
    }

    public void InverseOptimization()
    {
        optimizeTrajectory = !optimizeTrajectory;
    }

    public void InverseSmoothing()
    {
        smoothTrajectory = !smoothTrajectory;
    }

    public void InverseDrawing()
    {
        drawTrajectory = !drawTrajectory;
    }

}

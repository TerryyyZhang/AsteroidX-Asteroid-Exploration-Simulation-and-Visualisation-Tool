using System.Collections;
using UnityEngine;
using PathFinder3D;

public class EnemySpawner : MonoBehaviour {
    bool isSceneReady;
    bool canSpawn=true;
    SpaceManager spaceManagerInstance;
    public float spawnPeriod = 0.5f;
    public GameObject enemyPrefab;
    public Transform target;

    bool drawTrajectory = true;
    bool optimizeTrajectory = true;
    bool smoothTrajectory = true;
    // Use this for initialization
    void Start () {
        spaceManagerInstance = Component.FindObjectOfType<SpaceManager>();
    }
	
	// Update is called once per frame
	void Update () {
        if (!isSceneReady)
            isSceneReady = spaceManagerInstance.isPrimaryProcessingCompleted;
               
        if (isSceneReady && canSpawn)
        {
            float y = Random.Range(15, 140);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            GameObject spawnedEnemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            GameObject.Find("defender").GetComponent<DefenderController>().enemyDict.Add(spawnedEnemy.transform,false);
            Pursuer spawnedPursuerInstance = spawnedEnemy.GetComponent<Pursuer>();
            spawnedPursuerInstance.trajectoryOptimization = optimizeTrajectory;
            spawnedPursuerInstance.trajectorySmoothing = smoothTrajectory;
            spawnedPursuerInstance.tracePath = drawTrajectory;
            spawnedPursuerInstance.MoveTo(target);
            spawnedEnemy.transform.rotation = Quaternion.LookRotation(target.position-spawnedEnemy.transform.position);
            spawnedPursuerInstance.yMax = y +  15;
            spawnedPursuerInstance.speed = Random.Range(.15f,.5f);
            canSpawn = false;
            StartCoroutine(SpawnAllower());
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
    IEnumerator SpawnAllower()
    {
        yield return new WaitForSecondsRealtime(spawnPeriod);
        canSpawn = true;
    }
}

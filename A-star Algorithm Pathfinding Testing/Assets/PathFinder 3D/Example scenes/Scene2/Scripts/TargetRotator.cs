using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetRotator : MonoBehaviour {
    float alpha;
    public float circleRadius = 5;
    // Use this for initialization
    void Start () {
        alpha = 0;

    }
	
	// Update is called once per frame
	void Update () {
        alpha += 5;
        float radAlpha = (alpha * (float)Mathf.PI) / 180.0F;
        float x = circleRadius * (float)Mathf.Cos(radAlpha);
        float z = circleRadius * (float)Mathf.Sin(radAlpha);
        transform.position = new Vector3(x,0,z);
        if (alpha >= 360) alpha = 0;
    }
}

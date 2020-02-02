using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipTesting : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            GravityEngine.Instance().ApplyImpulse(GetComponent<NBody>(), 0.1f *Vector3.up);
        }
    }
}

using Polarith.AI.Package;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class inputControl : MonoBehaviour
{
    public GameObject spaceship1;
    public GameObject spaceship2;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
            spaceship1.GetComponent<ToggleNBody>().ToTarget1();
        if (Input.GetKeyDown(KeyCode.P))
            spaceship1.GetComponent<ToggleNBody>().ToTarget2();
        if (Input.GetKeyDown(KeyCode.K))
            spaceship2.GetComponent<ToggleNBody>().ToTarget1();
        if (Input.GetKeyDown(KeyCode.L))
            spaceship2.GetComponent<ToggleNBody>().ToTarget2();
    }
}

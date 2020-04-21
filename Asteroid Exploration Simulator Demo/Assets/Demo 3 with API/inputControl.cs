using Polarith.AI.Package;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class inputControl : MonoBehaviour
{
    public GameObject ship1;
    public GameObject ship2;

    public static GameObject spaceship1;
    public static GameObject spaceship2;

    // Start is called before the first frame update
    void Start()
    {
        spaceship1 = ship1;
        spaceship2 = ship2;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
            Ask1Go1();
        if (Input.GetKeyDown(KeyCode.P))
            Ask1Go2();
        if (Input.GetKeyDown(KeyCode.K))
            Ask2Go1();
        if (Input.GetKeyDown(KeyCode.L))
            Ask2Go2();
    }

    public static void Ask1Go1()
    {
        spaceship1.GetComponent<ToggleNBody>().ToTarget1();
    }
    public static void Ask1Go2()
    {
        spaceship1.GetComponent<ToggleNBody>().ToTarget2();
    }
    public static void Ask2Go1()
    {
        spaceship2.GetComponent<ToggleNBody>().ToTarget1();
    }
    public static void Ask2Go2()
    {
        spaceship2.GetComponent<ToggleNBody>().ToTarget2();
    }
}

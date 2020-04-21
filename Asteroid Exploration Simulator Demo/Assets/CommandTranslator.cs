using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Polarith.AI.Package;

public class CommandTranslator : MonoBehaviour
{
    public GameObject ship1;
    public GameObject ship2;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void onReceiveCommand(string incoming)
    {
        if(incoming.ToLower().Equals("agent 1 to site 1"))
        {
            ship1.GetComponent<ToggleNBody>().ToTarget1();
        }
        if (incoming.ToLower().Equals("agent 1 to site 2"))
        {
            ship1.GetComponent<ToggleNBody>().ToTarget2();
        }
        if (incoming.ToLower().Equals("agent 2 to site 1"))
        {
            ship2.GetComponent<ToggleNBody>().ToTarget1();
        }
        if (incoming.ToLower().Equals("agent 2 to site 2"))
        {
            ship2.GetComponent<ToggleNBody>().ToTarget2();
        }
    }

}

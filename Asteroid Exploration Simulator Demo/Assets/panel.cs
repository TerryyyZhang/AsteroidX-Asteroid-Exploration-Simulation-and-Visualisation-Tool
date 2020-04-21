using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Polarith.AI.Package
{
    public class panel : MonoBehaviour
    {
        private GameObject spacecraft;
        private string thisName;
        private GameObject asteroid1;
        private GameObject asteroid2;
        private ToggleNBody toggle;

        private double distance1;
        private double distance2;
        // Start is called before the first frame update
        void Start()
        {
            spacecraft = GetComponentInParent<NBody>().gameObject;
            thisName = spacecraft.name;
            toggle = spacecraft.GetComponent<ToggleNBody>();
            asteroid1 = toggle.keyK.gameObject;
            asteroid2 = toggle.keyL.gameObject;

        }

        // Update is called once per frame
        void Update()
        {
            try
            {
                distance1 = (asteroid1.transform.position - spacecraft.transform.position).magnitude;
                distance2 = (asteroid2.transform.position - spacecraft.transform.position).magnitude;
            }
            catch (System.Exception e) { }

            GetComponent<Text>().text = thisName + "\n\n" +
                "State: " + getState() + "\n\n" +
                "Radius " + asteroid1.name + ": " + distance1.ToString("F1") + " km\n" +
                "Radius " + asteroid2.name + ": " + distance2.ToString("F1") + " km";

        }

        private string getState()
        {
            if(toggle.countManeuver() == 1)
            {
                return "transit orbit to " + toggle.centerNbody.name;
            }
            else if(!toggle.stateIndicator){
                return "path-finding for " + toggle.centerNbody.name;
            }
            else if (toggle.stateIndicator)
            {
                return "orbiting " + toggle.centerNbody.name;
            }
            else { return "error"; }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Polarith.AI.Move;

namespace Polarith.AI.Package {
    /// <summary>
    /// There are two states to switch between in the spaceship. 
    /// State 1: "Auto Pilot State" is for the spaceship seeking long range target. 
    /// State 2: "Orbit Transfering State" is for the spaceship to transfer into a specific orbit using Lambert transfer algorithm, 
    /// which will be activated when the spaceship is in short range.
    /// 
    /// Function 1:
    /// This script will have an attribute "dominantRange". 
    /// When the spaceship is within range of the destination planet, the ship will start Lanbert orbit entering maneuver immediately. 
    /// 
    /// Function 2: 
    /// This script provides an interface "changeCenterObject" which will change the references of some scripts attached in this spaceship and 
    /// provide consistency of the orbiting center of this spacecraft.
    /// </summary>
    /// 
    /// Imposed rules:
    /// 1. Target orbit game object (with OrbitUniversal component) must be attached to the goal game object.
    /// 2. Camera game object (with CameraFollow component) must be attached to the spaceship game object. 
    /// 3. Orbit predictor game object (with OrbitPredictor component) must be attached to the spaceship game object. 
    public class ToggleNBody : MonoBehaviour
    {
        public NBody centerNbody;
        public float dominantRange = 500f;

        public NBody keyK;//temp testing
        public NBody KeyL;//temp testing
        public string GoalAIMEnvironmentTag = "Goal";

        private NBody thisNBody;
        private TransferShip transferShip;
        private bool doneDoTransfer;

        // Start is called before the first frame update
        void Start()
        {
            thisNBody = GetComponent<NBody>();
            transferShip = GetComponent<TransferShip>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SwitchToAutoPilotState();
                
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                SwitchToOrbitingState();
            }

            /// If the spaceship is close to the target planet, switch to orbiting state. 
            if (CheckWithInDominantRange() && !doneDoTransfer)
            {
                SwitchToOrbitingState();

                transferShip.ResetOrbitTransfer();

                transferShip.DoTransfer(null);
                doneDoTransfer = true;
                print("Goal in range. Initiate orbit entering protocal.");
            }

            
        }

        public void ToTarget1()
        {
            ChangeCenterObject(keyK);
            SwitchToAutoPilotState();
            print("Set course to " + centerNbody.gameObject.name + " .");
            transferShip.ClearManeuvers();
        }

        public void ToTarget2()
        {
            ChangeCenterObject(KeyL);
            SwitchToAutoPilotState();
            print("Set course to " + centerNbody.gameObject.name + " .");
            transferShip.ClearManeuvers();
        }

        public void SwitchToAutoPilotState()
        {
            GravityEngine.Instance().InactivateBody(gameObject);
            GetComponent<SpaceshipPhysics>().enabled = true;
            GetComponent<SpaceshipController>().enabled = true;
            doneDoTransfer = false; //?? place here?
        }

        public void SwitchToOrbitingState()
        {
            Vector3 pos = transform.position;
            Vector3 vel = new Vector3(-10, 0, 0);
            GravityEngine.Instance().UpdatePositionAndVelocity(thisNBody, pos, vel);
            GravityEngine.Instance().ActivateBody(gameObject);
            GetComponent<SpaceshipPhysics>().enabled = false;
            GetComponent<SpaceshipController>().enabled = false;
        }

        
        public void ChangeCenterObject(NBody newCenterObject)
        {
            //For orbit transfering
            centerNbody = newCenterObject;
            transferShip.SetTargetNbody(getTargetObjectOrbit(newCenterObject).GetComponent<NBody>());
            GetComponentInChildren<OrbitPredictor>().SetCenterObject(newCenterObject.gameObject);
            GetComponentInChildren<CameraFollow>().GoalObject = newCenterObject.gameObject.transform; 

            //For auto-pilot
            ///for all in getcomponent<AIMEnvironment> where label=goal
            ///replace its goal game objects to newCenterObject
            AIMEnvironment[] AIMEnvs = GetComponentsInChildren<AIMEnvironment>();
            foreach (AIMEnvironment env in AIMEnvs)
            {
                if(env.Label.Equals(GoalAIMEnvironmentTag))
                {
                    env.GameObjects = new List<GameObject> { CreateReferenceObject(newCenterObject) };
                }
            }
        }

        /// <summary>
        /// check that the spaceship is within the dominant gravity range of the target planet.
        /// </summary>
        /// <returns></returns>
        private bool CheckWithInDominantRange()
        {
            if (Vector3.Distance(transform.position, centerNbody.gameObject.transform.position) < dominantRange)
                return true;
            else
                return false;
        }
        /// <summary>
        /// Return the orbit game object which should be attached as a child of target game object.
        /// </summary>
        /// <returns></returns>
        private GameObject getTargetObjectOrbit(NBody newCenterObject)
        {
            return newCenterObject.gameObject.GetComponentInChildren<OrbitUniversal>().gameObject;
        }

        /// <summary>
        /// THIS METHOD DOES NOT WORK AND SHOULD BE REMOVED.
        /// In order to avoid the transit orbit crashing the goal planet, we specify the periapsis of Lambertr transfer orbit to be the same as goal orbit.
        /// This function help us calculate the periapsis points we would like to achieve in the back of the target planet. 
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        private Vector3d ComputePeriapsis()
        {
            Vector3 headingDirection = centerNbody.transform.position - transform.position;
            double targetOrbitSemiParameter = getTargetObjectOrbit(centerNbody).GetComponent<OrbitUniversal>().GetSemiParam();
            Vector3d periapsis = new Vector3d(headingDirection).normalized * targetOrbitSemiParameter *2 + new Vector3d(centerNbody.transform.position);
            return periapsis;
        }

        private double GetCenterSemiParam()
        {
            return getTargetObjectOrbit(centerNbody).GetComponent<OrbitUniversal>().GetSemiParam(); 
        }
        /// <summary>
        /// As the spaceship 
        /// </summary>
        /// <returns></returns>
        private GameObject CreateReferenceObject(NBody newCenterObject)
        {
            //float semiParameter = (float) getTargetObjectOrbit(newCenterObject).GetComponent<OrbitUniversal>().GetSemiParam();
            GameObject reference = new GameObject("Reference Goal");
            reference.transform.SetParent(newCenterObject.transform);
            reference.transform.localPosition = new Vector3(0, 1.5f, 0);
            reference.transform.rotation = Quaternion.Euler(0, 0, 0);
            return reference;
        }
    }
}
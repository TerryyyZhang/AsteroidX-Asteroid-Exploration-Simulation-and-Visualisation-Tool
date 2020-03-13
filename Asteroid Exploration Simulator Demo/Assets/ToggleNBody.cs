using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Polarith.AI.Move;

namespace Polarith.AI.Package {
    /// <summary>
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
                //The following doesn't work and to be deleted. 
                //In order to avoid crashing the goal asteroid, a perpendicular direction of transfer orbit need to be specified.
                //Vector3 perpendicularPoint = ComputeRandomPerpendicular(centerNbody.transform.position) + transform.position;
                //Vector3d perpendicularPoint2 = new Vector3d( perpendicularPoint.x, perpendicularPoint.y, perpendicularPoint.z ); 
                //transferShip.SetTargetPoint(perpendicularPoint2);
                
                transferShip.DoTransfer(null);
                doneDoTransfer = true;
                print("Goal in range. Initiate orbit entering protocal.");
            }

            ///temp testing
            if (Input.GetKeyDown(KeyCode.K))
            {
                ChangeCenterObject(keyK);
                SwitchToAutoPilotState();
                print("Set course to " + centerNbody.gameObject.name + " .");
                transferShip.ClearManeuvers();
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                ChangeCenterObject(KeyL);
                SwitchToAutoPilotState();
                print("Set course to " + centerNbody.gameObject.name + " .");
                transferShip.ClearManeuvers();
            }
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
            print(pos);
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
                    env.GameObjects = new List<GameObject> { newCenterObject.gameObject };
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
        /// !!!It doesn't work and to be deleted.
        /// In order to avoid crashing the goal asteroid, a perpendicular direction of transfer orbit need to be specified.
        /// see https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        private Vector3 ComputeRandomPerpendicular(Vector3 destination)
        {
            Vector3 side1 = transform.position - destination;
            Vector3 side2 = Vector3.zero - destination; // for random generation
            return Vector3.Cross(side1, side2).normalized*100;
        }
    }
}
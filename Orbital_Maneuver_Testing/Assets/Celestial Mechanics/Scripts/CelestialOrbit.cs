using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CelestialMechanics {
	public class CelestialOrbit : MonoBehaviour, ISimulation {
		const double Tau = Math.PI*2.0;
		const double Deg2Rad = Math.PI/180.0;

		#region Fields
		//input fields
		[SerializeField] double _periapsis = 1.0; //[m]
		public double periapsis {
			get {return _periapsis;}
			set {
				_periapsis = value;
				semiLatusRectum = Kepler.ComputeSemiLatusRectum(_periapsis, _eccentricity);
			}
		}

		[SerializeField] double _eccentricity = 0.4; //[1]
		public double eccentricity {
			get {return _eccentricity;}
			set {
				_eccentricity = value;
				semiLatusRectum = Kepler.ComputeSemiLatusRectum(_periapsis, _eccentricity);
			}
		}

		[SerializeField] float _argument = 0.0f; //[degrees]
		public float argument {
			get {return _argument;}
			set {
				_argument = value;
				orientation = Kepler.ComputeOrientation(_argument, _longitude, _inclination);
			}
		}

		[SerializeField] float _longitude = 0.0f; //[degrees]
		public float longitude {
			get {return _longitude;}
			set {
				_longitude = value;
				orientation = Kepler.ComputeOrientation(_argument, _longitude, _inclination);
			}
		}

		[SerializeField] float _inclination = 0.0f; //[degrees]
		public float inclination {
			get {return _inclination;}
			set {
				_inclination = value;
				orientation = Kepler.ComputeOrientation(_argument, _longitude, _inclination);
			}
		}

		//control fields
		[SerializeField] bool _simulate = true;
		public bool simulate {
			get {return _simulate;}
			set {
				if (_simulate != value) {
					_simulate = value;
					if (_simulate) OnOrbitStart.Invoke();
					else OnOrbitEnd.Invoke();
				}
			}
		}

		public double startEpoch = 0.0; //[seconds]

		[SerializeField] Vector2 _limits = new Vector2(-180,180); //[degrees]
		public Vector2 limits {
			get {return _limits;}
			set {
				_limits = value;
				rate = Kepler.ComputeRate(_period, _limits.x*Deg2Rad, _limits.y*Deg2Rad);
			}
		}

		//public WrapMode ending = WrapMode.Loop;

		public double meanAnomaly = 0.0; //[degrees]

		//time fields
		[SerializeField] double _period = 10.0; //[seconds/orbit]
		public double period {
			get {return _period;}
			set {
				_period = value;
				rate = Kepler.ComputeRate(_period, _limits.x*Deg2Rad, _limits.y*Deg2Rad);
			}
		}

		public double timeScale = 1.0;

		//events
		public UnityEvent OnOrbitStart = new UnityEvent();
		public OrbitEvent OnOrbitUpdate = new OrbitEvent();
		public UnityEvent OnOrbitEnd = new UnityEvent();
		#endregion

		#region Properties
		//static properties
		public Quaternion orientation {get; private set;}
		public double semiLatusRectum {get; private set;} //[m]
		public double semiMajorAxis {get; private set;} //[m]
		public double rate {get; private set;} //[rad/second]

		//dynamic properties
		public double anomaly {get; private set;} //[rad]
		public double eccentricAnomaly {get; private set;} //[rad]
		public double trueAnomaly {get; private set;} //[rad]
		public double radius {get; private set;} //[m]
		public Vector3 position {get; private set;}
		public Vector3 velocity {get; private set;}
		#endregion

		#region Messages
		void Reset() {
			_periapsis = 1.0;
			_eccentricity = 0.4;
			_argument = 0.0f;
			_longitude = 0.0f;
			_inclination = 0.0f;

			_simulate = true;
			_limits = new Vector2(-180f, 180f);
			meanAnomaly = 0.0;

			_period = 10;
			timeScale = 1.0;
			startEpoch = 0.0;

			OnOrbitStart = new UnityEvent();
			OnOrbitUpdate = new OrbitEvent();
			OnOrbitEnd = new UnityEvent();

			OnEnable();
		}

		void Start() {
			ResetSimulation();
			if (simulate) OnOrbitStart.Invoke();
		}

		void OnEnable() {
			ComputeStaticProperties();
			ComputeDynamicProperties(anomaly);
			transform.localRotation = orientation;
			transform.localPosition = position;
		}

		void Update() {
			if (simulate) {UpdateSimulation();}
		}

		void OnValidate() {
			if (_eccentricity < 0) _eccentricity = 0.0;
			if (_periapsis < 0) _periapsis = 0.0;

			Start();
			OnEnable();
		}
		#endregion

		#region Computation
		/// <summary>Compute static properties for orbit shape and speed</summary>
		public void ComputeStaticProperties() {
			orientation = Kepler.ComputeOrientation(argument, longitude, inclination);
			semiLatusRectum = Kepler.ComputeSemiLatusRectum(_periapsis, _eccentricity);
			semiMajorAxis = Kepler.ComputeSemiMajorAxis(_periapsis, _eccentricity);
			rate = Kepler.ComputeRate(_period, _limits.x*Deg2Rad, _limits.y*Deg2Rad);
		}

		/// <summary>Compute dynamic properties that change over the anomaly</summary>
		/// <param name="M">The anomaly to evaluate properties at</param>
		public void ComputeDynamicProperties(double M) {
			eccentricAnomaly = Kepler.ComputeEccentricAnomaly(M, _eccentricity);
			trueAnomaly = Kepler.ComputeTrueAnomaly(eccentricAnomaly, _eccentricity);
			radius = Kepler.ComputeRadius(semiLatusRectum, _eccentricity, trueAnomaly);
			position = orientation * Kepler.ComputePosition(radius, trueAnomaly);
			velocity = orientation * Kepler.ComputeVelocity(_periapsis, radius, rate, eccentricAnomaly, trueAnomaly, _eccentricity);
		}
		#endregion

		#region Simulation
		public void StartSimulation() {simulate = true;}
		public void StopSimulation() {simulate = false;}

		public void ResetSimulation() {
			if (_eccentricity < 1) { //closed orbit
				anomaly = meanAnomaly*Deg2Rad + startEpoch * rate;
				anomaly = ((anomaly % Tau) + Tau) % Tau; //wraps to [0, Tau]

			} else { //open orbit
				anomaly = meanAnomaly*Deg2Rad;
			}
		}

		public void UpdateSimulation() {
			anomaly += Time.deltaTime * rate * timeScale;

			if (_eccentricity < 1) { //closed orbit
				if (anomaly > Tau) {
					anomaly = anomaly % Tau;
				} else if (anomaly < 0) {
					anomaly = (anomaly % Tau) + Tau;
				}

			} else { //open orbit
				if (anomaly > _limits.y*Deg2Rad) {
					anomaly = _limits.y*Deg2Rad;
					simulate = false; //bounds reached on open orbit
				}
			}

			ComputeDynamicProperties(anomaly);
			transform.localPosition = position;

			OnOrbitUpdate.Invoke(trueAnomaly);
		}
		#endregion

		#region Gizmos
		void OnDrawGizmosSelected() {
			//OnValidate should always be called before this, meaning appropriate values for properties are available

			//variables required
			Vector3[] path = new Vector3[51];
			Vector3 periapsisV = orientation * Vector3.right * (float)_periapsis;
			Vector3 positionV = orientation*Kepler.ComputePosition(Kepler.ComputeRadius(semiLatusRectum, _eccentricity, trueAnomaly), trueAnomaly);

			//elliptical and circular
			//build list of vectors for path
			double step, lower, upper;
			double r;

			lower = Kepler.ComputeTrueAnomaly(Kepler.ComputeEccentricAnomaly(_limits.x*Deg2Rad, _eccentricity), _eccentricity);
			upper = Kepler.ComputeTrueAnomaly(Kepler.ComputeEccentricAnomaly(_limits.y*Deg2Rad, _eccentricity), _eccentricity);
			step = (upper - lower)/50;
			
			for (int i = 0; i <= 50; i++) {
				r = Kepler.ComputeRadius(semiLatusRectum, _eccentricity, lower + i*step);
				path[i] = Kepler.ComputePosition(r, lower + i*step);
			}

			//Set the gizmos to draw in parent space
			Gizmos.matrix = (transform.parent)? transform.parent.localToWorldMatrix : Matrix4x4.identity;

			//draw the path of the orbit
			Gizmos.color = Color.cyan;
			for (int i = 0; i < 50; i++) {
				Gizmos.DrawLine(orientation*path[i], orientation*path[i+1]);
			}

			//draw periapsis vector
			Gizmos.DrawLine(Vector3.zero, periapsisV);

			//draw position vector
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(Vector3.zero, positionV);

			//draw velocity vector
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(positionV, positionV + velocity);
		}
		#endregion
	}
}
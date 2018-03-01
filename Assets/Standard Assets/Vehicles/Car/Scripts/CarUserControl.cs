using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityEngine.Networking;
namespace UnityStandardAssets.Vehicles.Car
{
	[RequireComponent(typeof (CarController))]
	public class CarUserControl : NetworkBehaviour
	{
		private CarController m_Car; // the car controller we want to use
		private NetworkStartPosition [] spawnPoints;

		private void Awake()
		{
			// get the car controller
			m_Car = GetComponent<CarController>();
		}



		private void FixedUpdate()
		{
			
			// pass the input to the car!
			float h = CrossPlatformInputManager.GetAxis("Horizontal");
			float v = CrossPlatformInputManager.GetAxis("Vertical");

			if (Input.GetKeyDown(KeyCode.U)) m_Car.changeGear("up");
			if (Input.GetKeyDown(KeyCode.J)) m_Car.changeGear("down");
			if (Input.GetKeyDown (KeyCode.R)) RpcRespawn ();

			#if !MOBILE_INPUT
			float handbrake = CrossPlatformInputManager.GetAxis("Jump");
			m_Car.Move(h, v, v, handbrake);
			#else
			m_Car.Move(h, v, v, 0f);
			#endif
		}


		//Added Network
		[RPC]
		void RpcRespawn()
		{
			Debug.Log ("Respawning");
			Vector3 spawnPoint = Vector3.zero;
			if (spawnPoints != null && spawnPoints.Length > 0)
			{
				spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
			}

			transform.position = spawnPoint;
		}
	}
}

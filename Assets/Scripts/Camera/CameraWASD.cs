using UnityEngine;
using System.Collections;

public class CameraWASD : MonoBehaviour {

  
  public float MoveSpeed;

	// Use this for initialization
	void Start () {
  }
	
	// Update is called once per frame
	void Update () {
    Vector3 forward = Vector3.zero;
    Vector3 strafe = Vector3.zero;
    if (Input.GetKey(KeyCode.W)) {
      forward = transform.forward * MoveSpeed;
    }
    else if (Input.GetKey(KeyCode.S)) {
      //forward = transform.TransformDirection(Vector3.forward) * -MoveSpeed;
      forward = transform.forward * -MoveSpeed;
    }

    if (Input.GetKey(KeyCode.A)) {
      strafe = transform.right * -MoveSpeed;
    }
    else if (Input.GetKey(KeyCode.D)) {
      strafe = transform.right * MoveSpeed;
    }

    transform.position = transform.position + (forward + strafe) * Time.deltaTime;
  }
}

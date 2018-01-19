using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class NURRPAvatarController : MonoBehaviour {

	// Use this for initialization
	private void Start() {
		getMyStuff();

		// Get the rallying Point
		rallyingPoint = GameObject.FindGameObjectWithTag("Rallying Point");
		rallyingPointPos = rallyingPoint.transform.position;

		currTargetPos = rallyingPointPos;

		// Init NavMesh
		animator = gameObject.GetComponent<Animator>();
		navComponent = gameObject.GetComponent<NavMeshAgent>();
		navComponent.SetDestination(currTargetPos);
	}

	// Update is called once per framee
	private void Update() {

		getMyStuff();

		//Check if I'm close enough to the Target, then stop
		if (Vector3.Distance(myPos, currTargetPos) < 2) {

			//If this is the rallying Point, then stay, else move on to the rallying point
			if (Vector3.Distance(myPos, rallyingPointPos) < 2) {

				animator.SetBool("closeEnough", true);
				navComponent.Stop();
				stopped = true;
			} else {

				currTarget = rallyingPoint;
				currTargetPos = rallyingPointPos;
				navComponent.SetDestination(currTargetPos);
			}
		} 

		else if(!stopped) { // Still going

			animator.SetBool("closeEnough", false);
		}


	}

	private void getMyStuff() {

		myPos = transform.position;

		// What is my Floor
		if (myPos.y > 12) {
			myFloor = 4;
		} else if (myPos.y > 9) {
			myFloor = 3;
		} else if (myPos.y > 6) {
			myFloor = 2;
		} else if (myPos.y > 3) {
			myFloor = 1;
		} else
			myFloor = 0;
	}

	public void burn() {

		// Try another destination
		int index = sortedDests.IndexOfValue(currTarget);
		
		currTarget = sortedDests.Values[index + 1];
		
		Debug.Log(currTarget.name);
		currTargetPos = currTarget.transform.position;
		navComponent.SetDestination(currTargetPos);
	}

	private Animator animator;
	private Vector3 currTargetPos;
	private NavMeshAgent navComponent;
	private GameObject[] destinations;
	private bool stopped;
	private int myFloor;
	private SortedList<float, GameObject> sortedDests;
	private Vector3 myPos;
	private GameObject currTarget;
	private GameObject rallyingPoint;
	private Vector3 rallyingPointPos;
}

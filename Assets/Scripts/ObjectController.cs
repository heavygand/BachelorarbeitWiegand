#pragma warning disable 1587
/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using System.Collections;
using UnityEngine;

public class ObjectController : MonoBehaviour {

    private RegionController myRegion;
	private bool withOtherPerson;
	private ActivityController avatar;
	private int internalLoops;

	public Vector3 WorkPlace
	{
		get
		{
			return getFootOfFirstCollider();
		}
	}

	public Vector3 MoveVector
	{
		get
		{
			return rotateVector(moveVector);
		}
	}

	public bool isWithOtherPerson {
		get {
			return withOtherPerson;
		}
	}

	public ActivityController getAvatar(){

		return avatar;
	}

	public enum Activities {
		sitDown,
		layDown,
		stand,
		sport,
		rummage,
		bartending,
		brooming,
		talking,
		eating,
		handMovements,
		ironing
	}

	[Tooltip("The general animation to fulfill here")]
	public Activities activity;

	[Tooltip("Rotation relative to the Object")]
	public int turnAngle;

	[Tooltip("The time in seconds to use this activity")]
	public int time;

	[Tooltip("A direction to slide to, when arrived (seen from the center of the stopper-collider)")]
	public Vector3 moveVector;

	[Tooltip("Indicates if this should be visible ingame")]
	public bool isPhysical;

	[Tooltip("Indicates if the user has to be able to \"go through\" things to use this")]
	public bool makeGhost;

	[Tooltip("The tool to use for this activity")]
	public GameObject toolToUse;

	public enum HandUsage {
		leftHand,
		rightHand,
		bothHands
	}

	[Tooltip("Wich hands shall the avatar use with this tool?")]
	public HandUsage handToUse;

	[Tooltip("How often shall the Avatar start again with the destination-tree")]
	public int loops;

	[Tooltip("Only for child-destinations: Indicates if the user shall look at the next destination, when arrived")]
	public bool lookAtNext;

	void Start() {

		if (time < 1) time = 100;
		internalLoops = loops;

		avatar = gameObject.GetComponentInParent<ActivityController>();

		if (avatar != null) {

			withOtherPerson = true;
		}
		else withOtherPerson = false;
		StartCoroutine(checkIfOutside());
	}

	private IEnumerator checkIfOutside() {

		yield return new WaitForSeconds(0.25f);
		if (myRegion == null) {

			GameObject.Find("GameActivityController").GetComponentInChildren<RegionController>().registerActivity(this);
		}
	}

	public Vector3 getFootOfFirstCollider() {

		Bounds bounds = GetComponents<Collider>()[0].bounds;
		Vector3 center = new Vector3(bounds.center.x, bounds.center.y - bounds.size.y/2, bounds.center.z);
		return center;
	}

	private Vector3 rotateVector(Vector3 unturnedVector) {

        return transform.localRotation * unturnedVector;
    }

    public void setRegion(RegionController rc) {

        myRegion = rc;
    }

	public void resetLoops() {

		loops = internalLoops;
	}
}
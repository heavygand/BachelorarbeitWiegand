﻿#pragma warning disable 1587
/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using System.Collections;
using UnityEngine;

public class ObjectController : MonoBehaviour {

    private RegionController myRegion;
	private ActivityController avatar;
	private int internalLoops;

	public Vector3 MoveVector
	{
		get
		{
			return rotateVector(moveVector);
		}
	}

	private bool withOtherPerson;
	public bool isWithOther {
		get {
			return avatar!=null;
		}
	}

	private ActivityController user;
	public ActivityController currentUser {
		get {

			// If the user doesn't have this as current activity anymore, then set and return null
			if (user != null && user.CurrentActivity != this) {

				if (logging) Debug.Log($"{name}: has no user anymore");
				user = null;
			}

			return user;
		}
		set {

			if (logging) Debug.Log($"{user.name}: is now the user of {name}");
			user = value;
		}
	}

	// This is the avatar, that IS the activity, not the current user of this activity (like the talkDestination)
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
		ironing,
		typing,
		call
	}

	[Tooltip("When an avatar (A) wants to interrupt someone else (B), then this is only possible if the activity of avatar (A) is more important than the one of (B)")]
	public int Priority;

	[Tooltip("The general animation to fulfill here")]
	public Activities activity;

	[Tooltip("Rotation relative to the Object, wich the avatar will perform when arrived there")]
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
		noHand,
		leftHand,
		rightHand,
		bothHands
	}

	[Tooltip("Wich hands shall the avatar use with this tool?")]
	public HandUsage handToUse;

	[Tooltip("Indicates if this is an activity with more avatars involved, wich will be picked randomly in the current region and will be interrupted and will look for destinations under the parent of this object (those should be deactivated)")]
	public bool isGroupActivity;

	[Tooltip("How often shall the Avatar start again with the destination-tree")]
	public int loops;

	[Tooltip("Only for child-destinations: Indicates if the user shall look at the next destination, when arrived")]
	public bool lookAtNext;

	private bool logging = false;

	public Vector3 WorkPlace
	{
		get
		{
			return getFootOfFirstCollider();
		}
	}

	void Start() {

		if (time < 1) time = 100;
		internalLoops = loops;

		avatar = gameObject.GetComponentInParent<ActivityController>();

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
#pragma warning disable 1587
/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using System.Collections;
using UnityEngine;

public class ObjectController : MonoBehaviour {

	// This is the avatar, that IS the activity, not the current user of this activity
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
		call,
        throwOneHand,
        pushButton
    }

    [Tooltip("Indicates if an avatar can get this as a random destination. Should be activated on childdestinations or when the destination is for only one user")]
    public bool cannotBeFound;

    [Tooltip("When an avatar (A) wants to interrupt someone else (B), then this is only possible if the activity of avatar (A) is more important than the one of (B)")]
	public int Priority;

	[Tooltip("The general animation to fulfill here")]
	public Activities wichAnimation;

	[Tooltip("Rotation relative to the Object, wich the avatar will perform when arrived there. Turnangle 0 therefore means 'look where the object looks'. If no rotation shall be performed, then please activate 'No Turning'")]
	public int turnAngle;

    [Tooltip("Only for child-destinations: Indicates if the user shall look at the next destination, when arrived")]
    public bool lookAtNext;

    [Tooltip("Indicates if the avatar shall look at the target when arrived")]
    public bool lookAtTarget;

    [Tooltip("Indicates, that the avatar shall not rotate when arrived. -> Deactivates 'Look At Next' and resets the turnAngle")]
    public bool noTurning;

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

	public Vector3 WorkPlace => getFootOfFirstCollider();

    [Tooltip("The time in seconds, after wich this object is activated (may be sound or firealarm, etc)")]
    public int activationDelay;

    [Tooltip("The time to wait before starting this (includes start going)")]
    public int startDelay;

    [Tooltip("The discription text for FUNGUS. Sollte passen zu: \"Ich war gerade am...\" und \"Ich war gerade auf dem weg zum...\"")]
    public string discription;

    private RegionController myRegion;
    private int internalLoops;
    public Vector3 MoveVector => rotateVector(moveVector);

    private ActivityController avatar;
    public bool isAvatar => avatar != null;

    private bool withOtherPerson;

    private bool isActivated;
    public bool IsActivated => isActivated;

    public bool isMovable => isAvatar;

    private ActivityController user;
    public bool started;

    public ActivityController CurrentUser
    {
        get
        {
            // If the user doesn't have this as current activity anymore, then set and return null
            if (user != null && user.CurrentActivity != this && user.NextActivity != this) {

                user.log4Me("has no user anymore#Detail10Log");
                isActivated = false;
                user = null;
            }

            return user;
        }
        set
        {
            isActivated = false;
            user = value;
            if (user != null) user.log4Me($"I'm now the user of {name}#Detail10Log");
        }
    }

    public void Start() {

        if(started) return;
        started = true;

		if (time < 1) time = 100;
		internalLoops = loops;

		avatar = gameObject.GetComponentInParent<ActivityController>();

        if(isAvatar) name = $"{name} with {avatar.name}";

		StartCoroutine(checkIfOutside());

        if (noTurning && turnAngle != 0) {

            Debug.LogWarning($"Achtung: bei {name} ist 'No Turning' aktiviert und eine Rotation eingetragen. Der Avatar wird nicht rotieren.");
        }

        if (LayerMask.LayerToName(gameObject.layer) != "Feuermelder" && tag != "FireFighter Point" && tag != "rallying Point") {

            StartCoroutine(warnDiscription()); 
        }
    }

    private IEnumerator warnDiscription() {

        yield return new WaitForSeconds(1);
        if (discription == "") {

            Debug.LogWarning($"Warnung: {name} in {myRegion.name} hat keine Tätigkeitsbeschreibung");
        }
    }

    private IEnumerator checkIfOutside() {

		yield return new WaitForSeconds(0.25f);
		if (myRegion == null) {

            // Register outside
            GameObject.Find("GameLogic").GetComponent<GameLogic>().outside.registerActivity(this);
		}
    }

    public void setRegion(RegionController rc) {
        
        if (isAvatar) {

            avatar.log4Me($"{name}'s region is now {(rc != null ? rc.name : "null")} (was {(myRegion != null ? myRegion.name : "null")})#Detail10Log");
        }

        myRegion = rc;

        StartCoroutine(checkIfOutside());
    }

    public Vector3 getFootOfFirstCollider() {

        if(transform.parent != null && transform.parent.gameObject.tag == "Player" || tag == "Player") {
            
            return transform.position;
        }

		Bounds bounds = GetComponents<Collider>()[0].bounds;
		Vector3 center = new Vector3(bounds.center.x, bounds.center.y - bounds.size.y/2, bounds.center.z);
		return center;
	}

	private Vector3 rotateVector(Vector3 unturnedVector) {

        return transform.rotation * unturnedVector;
    }

    public RegionController getRegion() {

        return myRegion;
    }

    public void resetLoops() {

		loops = internalLoops;
	}

    // This can bo other users than the current user, to make the rallying point possible, wich is a multi user object
    public IEnumerator activate(ActivityController userHere) {

        yield return new WaitForSeconds(activationDelay);

        // If there's a sound, then play it
        AudioSource sound = GetComponent<AudioSource>();
        if (sound != null) sound.Play();

        // When it's a firealarm, then set alarm and panic
        if (LayerMask.LayerToName(gameObject.layer) == "Feuermelder") {

            myRegion.HasAlarm = true;
            userHere.Panic = true;
        }
        // When it's a firealarm, then set alarm and panic
        if (tag == "FireFighter Point") {

            //Debug.Log("Reached firefighterpoint");
            userHere.stopNow = true;
            StartCoroutine(myRegion.getMaster().activateFirstPerson(transform.parent.GetComponent<RegionController>()));
        }
        // When it's a rallying point
        if (tag == "rallying Point") {
            
            userHere.doRallyingPointStuff();
        }
        // When it's the players talkdestination
        if (tag == "playersTalkDestination") {

            userHere.Panic = false;
        }

        isActivated = true;
        userHere.log4Me($"{name} activated");
    }
}
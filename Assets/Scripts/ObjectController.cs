using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the necessary data for the usage of this activity.
/// 
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>
public class ObjectController : MonoBehaviour {

    /// <summary>
    /// This is the avatar, that IS the activity, not the current user of this activity
    /// </summary>
    /// <returns>The avatar, who represents this activity</returns>
    public ActivityController getAvatar(){

		return avatar;
	}

    /// <summary>
    /// The available activities for all avatars
    /// The names of the enums have to be exactly the names of the bool-parameters of the avatar animator
    /// </summary>
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
        pushButton,
        guitarPlaying
    }

    /// <summary>
    /// Indicates if an avatar can get this as a random destination. Should be activated on childdestinations or when the destination is for only one user
    /// </summary>
    [Tooltip("Indicates if an avatar can get this as a random destination. Should be activated on childdestinations or when the destination is for only one user")]
    public bool cannotBeFound;

    /// <summary>
    /// When an avatar (A) wants to interrupt someone else (B), then this is only possible if the activity of avatar (A) is more important than the one of (B)
    /// </summary>
    [Tooltip("When an avatar (A) wants to interrupt someone else (B), then this is only possible if the activity of avatar (A) is more important than the one of (B)")]
	public int Priority;

    /// <summary>
    /// The general animation to fulfill here
    /// </summary>
	[Tooltip("The general animation to fulfill here")]
	public Activities wichAnimation;

    /// <summary>
    /// Rotation relative to the Object, wich the avatar will perform when arrived there. Turnangle 0 therefore means 'look where the object looks'. If no rotation shall be performed, then please activate 'No Turning'
    /// </summary>
	[Tooltip("Rotation relative to the Object, wich the avatar will perform when arrived there. Turnangle 0 therefore means 'look where the object looks'. If no rotation shall be performed, then please activate 'No Turning'")]
	public int turnAngle;

    /// <summary>
    /// Only for child-destinations: Indicates if the user shall look at the next destination, when arrived
    /// </summary>
    [Tooltip("Only for child-destinations: Indicates if the user shall look at the next destination, when arrived")]
    public bool lookAtNext;

    /// <summary>
    /// Indicates if the avatar shall look at the target when arrived
    /// </summary>
    [Tooltip("Indicates if the avatar shall look at the target when arrived")]
    public bool lookAtTarget;

    /// <summary>
    /// Indicates, that the avatar shall not rotate when arrived. -> Deactivates 'Look At Next' and resets the turnAngle
    /// </summary>
    [Tooltip("Indicates, that the avatar shall not rotate when arrived. -> Deactivates 'Look At Next' and resets the turnAngle")]
    public bool noTurning;

    /// <summary>
    /// The time in seconds to use this activity
    /// </summary>
    [Tooltip("The time in seconds to use this activity")]
	public int time;

    /// <summary>
    /// A direction to slide to, when arrived (seen from the center of the stopper-collider)
    /// </summary>
	[Tooltip("A direction to slide to, when arrived (seen from the center of the stopper-collider)")]
	public Vector3 moveVector;

    /// <summary>
    /// Indicates if this should be visible ingame
    /// </summary>
	[Tooltip("Indicates if this should be visible ingame")]
	public bool isPhysical;

    /// <summary>
    /// Indicates if the user has to be able to \"go through\" things to use this
    /// </summary>
	[Tooltip("Indicates if the user has to be able to \"go through\" things to use this")]
	public bool makeGhost;

    /// <summary>
    /// The tool to use for this activity
    /// </summary>
	[Tooltip("The tool to use for this activity")]
	public GameObject toolToUse;

    /// <summary>
    /// The possible hand-combinations
    /// </summary>
	public enum HandUsage {
		noHand,
		leftHand,
		rightHand,
		bothHands
	}

    /// <summary>
    /// Wich hands shall the avatar use with this tool?
    /// </summary>
	[Tooltip("Wich hands shall the avatar use with this tool?")]
	public HandUsage handToUse;

    /// <summary>
    /// Indicates if this is an activity with more avatars involved, wich will be picked randomly in the current region and will be interrupted and will look for destinations under the parent of this object (those should be deactivated)
    /// </summary>
    [Tooltip("Indicates if this is an activity with more avatars involved, wich will be picked randomly in the current region and will be interrupted and will look for destinations under the parent of this object (those should be deactivated)")]
	public bool isGroupActivity;

    /// <summary>
    /// How often shall the Avatar start again with the destination-tree
    /// </summary>
	[Tooltip("How often shall the Avatar start again with the destination-tree")]
	public int loops;

    /// <summary>
    /// The place, where the avatar will try to go
    /// </summary>
	public Vector3 WorkPlace => getFootOfFirstCollider();

    /// <summary>
    /// The time in seconds, after wich this object is activated (may be sound or firealarm, etc)
    /// </summary>
    [Tooltip("The time in seconds, after wich this object is activated (may be sound or firealarm, etc)")]
    public int activationDelay;

    /// <summary>
    /// The time to wait before starting this (includes start going)
    /// </summary>
    [Tooltip("The time to wait before starting this (includes start going)")]
    public int startDelay;

    /// <summary>
    /// Indicates if this activity is used by multiple avatars at the same time
    /// </summary>
    [Tooltip("Indicates if this activity is used by multiple avatars at the same time")]
    public bool multiUserActivity;

    /// <summary>
    /// The discription text for FUNGUS. Sollte passen zu: "Ich war gerade am..." und "Ich war gerade auf dem weg zum..."
    /// </summary>
    [Tooltip("The discription text for FUNGUS. Sollte passen zu: \"Ich war gerade am...\" und \"Ich war gerade auf dem weg zum...\"")]
    public string discription;
    
    private RegionController myRegion;
    private int internalLoops;

    /// <summary>
    /// Returns the locally rotated movevector, so he wont get broken, when the object rotates
    /// </summary>
    public Vector3 MoveVector => rotateVector(moveVector);

    private ActivityController avatar;

    /// <summary>
    /// Returns if the activity is an avatar
    /// </summary>
    public bool isAvatar => avatar != null;

    private bool withOtherPerson;

    private bool isActivated;

    /// <summary>
    /// Indicates if this object has been activated
    /// </summary>
    public bool IsActivated => isActivated;

    /// <summary>
    /// Indicates if this activity is non-static
    /// </summary>
    public bool isMovable => isAvatar;

    private ActivityController user;
    private Transform parentOfCurrAct;
    private List<ObjectController> participantActivities;

    /// <summary>
    /// Indicates if the Start()-Method has already been started
    /// </summary>
    public bool started { get; set; }

    /// <summary>
    /// Organizes the current user of this activity
    /// Resets the "activated"-bool
    /// </summary>
    public ActivityController CurrentUser
    {
        get
        {
            // When the current user doesn't have this as current activity anymore, then set and return null
            if (user != null && user.CurrentActivity != this && user.NextActivity != this) {

                CurrentUser = null;
            }

            return user;
        }
        set
        {
            // Multi-user acticities do not need any current users
            if(multiUserActivity) return;

            ActivityController oldUser = user;
            user = value;
            bool changed = oldUser != user;

            if (changed) {

                isActivated = false;
                if (oldUser != null) oldUser.log4Me($"I'm not the user of {name} anymore#Detail10Log");
                if (user != null) user.log4Me($"I'm now the user of {name}#Detail10Log");
            }
        }
    }

    /// <summary>
    /// Initializing and Validating
    /// </summary>
    public void Start() {

        if(started) return;
        started = true;

        // Initialisation
		if (time < 1) time = 100;
		internalLoops = loops;

		avatar = gameObject.GetComponentInParent<ActivityController>();

		StartCoroutine(checkIfOutside());

        // Validation
        if (noTurning && turnAngle != 0) {

            Debug.LogWarning($"Achtung: bei {name} ist 'No Turning' aktiviert und eine Rotation eingetragen. Der Avatar wird nicht rotieren.");
        }

        if (LayerMask.LayerToName(gameObject.layer) != "Feuermelder" && tag != "FireFighter Point" && tag != "rallying Point") {

            StartCoroutine(warnDiscription()); 
        }

        if (isGroupActivity && !isAvatar) {

            getParticipantActivities();

            if (participantActivities == null || participantActivities.Count == 0) {

                Debug.LogWarning($"Achtung: Gruppenaktivität {name} hat keine Teilnehmeraktivitäten, stellen Sie sicher, dass {name} unter einem Sammelgameobject hängt, unter dem auch andere Activities sind, die dadurch als Teilnehmeraktivitäten fungieren");
            } 
        }
    }

    /// <summary>
    /// Shows a warning, that there is no activitydiscription
    /// </summary>
    private IEnumerator warnDiscription() {

        yield return new WaitForSeconds(1);
        if (discription == "") {

            Debug.LogWarning($"Warnung: {name} in {myRegion.name} hat keine Tätigkeitsbeschreibung");
        }
    }

    /// <summary>
    /// Checks if the activity is outside
    /// That means: if he is not registered in any region
    /// Then register in the outside area
    /// </summary>
    private IEnumerator checkIfOutside() {

		yield return new WaitForSeconds(0.25f);
		if (myRegion == null) {

            // Register outside
            GameObject.Find("GameLogic").GetComponent<GameLogic>().outside.registerActivity(this);
		}
    }

    /// <summary>
    /// Sets the current region and checks if it's outside
    /// </summary>
    /// <param name="rc">The new region to set. Can be null</param>
    public void setRegion(RegionController rc) {
        
        if (isAvatar) {

            avatar.log4Me($"{name}'s region is now {(rc != null ? rc.name : "null")} (was {(myRegion != null ? myRegion.name : "null")})#Detail10Log");
        }

        myRegion = rc;

        StartCoroutine(checkIfOutside());
    }

    /// <summary>
    /// Get's the lowest point of the first collider
    /// That's what the work place will be
    /// </summary>
    /// <returns>The point in the space</returns>
    public Vector3 getFootOfFirstCollider() {

        if(transform.parent != null && transform.parent.gameObject.tag == "Player" || tag == "Player") {
            
            return transform.position;
        }

		Bounds bounds = GetComponents<Collider>()[0].bounds;
		Vector3 center = new Vector3(bounds.center.x, bounds.center.y - bounds.size.y/2, bounds.center.z);
		return center;
	}

    /// <summary>
    /// Multiplies a vector with the rotation of this object
    /// </summary>
    /// <param name="unturnedVector">The unturned Vector</param>
    /// <returns>The turned vector</returns>
	private Vector3 rotateVector(Vector3 unturnedVector) {

        return transform.rotation * unturnedVector;
    }

    /// <summary>
    /// Returns the current region
    /// </summary>
    /// <returns>the current region</returns>
    public RegionController getRegion() {

        return myRegion;
    }

    /// <summary>
    /// Sets the number of loops back to standard
    /// </summary>
    public void resetLoops() {

		loops = internalLoops;
	}

    /// <summary>
    /// Activates the Object
    /// This can be used by other users than the current user, this is so, to make the rallying point possible, wich is a multi user object
    /// </summary>
    /// <param name="userHere">The user that calls this</param>
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
        // When it's a firefighter point
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
        // When it's a talkdestination, then we only need sound at the groupleader
        if (tag == "talkDestination" && userHere.MyLeader == null) {

            sound.Stop();
        }

        isActivated = true;
        userHere.log4Me($"{name} activated");
    }

    /// <summary>
    /// Returns the activities that are found in getParticipantActivities()
    /// </summary>
    /// <param name="caller">The avatar, who wants to have this</param>
    /// <returns>the activities that are also under the same parent as the given activity</returns>
    public List<ObjectController> getParticipantActivities(ActivityController caller) {

        return participantActivities;
    }

    /// <summary>
    /// Checks the activities that are also under the same parent as the given activity
    /// This should be activities for participants of a groupactivity.
    /// And the activity this should be called on, should be a grouactivity
    /// </summary>
    /// <returns></returns>
    public List<ObjectController> getParticipantActivities() {

        // Get the parent of CurrentActivity (a groupactivity has to be organized under a parent with multiple activities)
        parentOfCurrAct = GetComponentInParent<Transform>().parent;

        // Get the other activities under this parent
        participantActivities = new List<ObjectController>(parentOfCurrAct.GetComponentsInChildren<ObjectController>());
        participantActivities.Remove(this);

        //Debug.Log($"Groupactivity {name} has {participantActivities.Count} groupactivitychild without {name}");

        return participantActivities;
    }

    /// <summary>
    /// Deactivates the object
    /// </summary>
    public void deactivate() {

        isActivated = false;

        AudioSource sound = GetComponent<AudioSource>();
        if (sound != null)
            sound.Stop();
    }
}
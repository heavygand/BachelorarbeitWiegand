#pragma warning disable 1587
/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// This Script manages all activities and deactivates itself in case of alarm
/// </summary>
public class ActivityController : MonoBehaviour {

	public GameObject currentActivity;
	public GameObject nextActivity;

	private GameObject lastFire;
	private Animator animator;
	private NavMeshAgent navComponent;
	private Text alarmText;
	private AvatarController fleeScript;
	private Transform whatBurn;
	private RegionController myRegion;
	private bool toBurn;
	private bool displaced;
	private bool startedDeactivating;
	private ObjectController targetScript;
	private GameObject lastActivity;
	private Coroutine doing;
	private bool activityChangeRequested;
	private bool going;
	private GameObject tool;
	private GameObject leftHand;
	private GameObject rightHand;
	private bool onlySetDontGo;
	private bool iAmPartner;

	/// <summary>
	/// Initialisation
	/// </summary>
	void Start () {

		// Init Components
		animator = GetComponent<Animator>();
        navComponent = GetComponent<NavMeshAgent>();
        fleeScript = (AvatarController)GetComponent(typeof(AvatarController));
        alarmText = GameObject.Find("alarmTimer").GetComponent<Text>();
	}
	

	/// <summary>
	/// In Update, we check if there's a firealarm
	/// </summary>
	void Update () {

        // If the alarm starts, then stop doing activities
        // TODO remove this from update()
        if (alarmText.text == "FIREALARM" && !startedDeactivating) deactivateMe();
    }

	// Set a target
	private void setTarget() {

		activityChangeRequested = false;

		// Proceed with activities, when available
		lastActivity = currentActivity;
		currentActivity = nextActivity;
		nextActivity = null;

		// Get a random Target, if we have no
		int counter = 0;
		while (currentActivity == null && counter < 10) {

            Debug.Log("Picking random target");

            //Get the activities in my region
            List<ObjectController> activities = myRegion.getActivities();

            // Pick a random number from the length of the destinationlist
            int target = Random.Range(0, activities.Count);
			
			// Set this as target
			GameObject foundActivity = activities[target].gameObject;

			ObjectController tempScript = getTargetScript(foundActivity);

			// But not the last one, and not talk to myself (all avatars are sane)
			if (foundActivity != lastActivity && tempScript != null && !(tempScript.isWithOtherPerson && tempScript.getAvatar() == this)) {

                currentActivity = foundActivity; 
            }

			counter++;
		}
		if (counter == 10) {

			Debug.LogError("Es konnten keine Aktivitäten mehr gefunden werden.");
			return;
		}
		// Target Activity found.

		if (onlySetDontGo) {
			
			onlySetDontGo = false;
			stopGoing();
		}
		else{

			startGoing();
		}
	}

	// START GOING
	public void startGoing() {

		if (currentActivity == null) {
			
			setTarget();
			return;
		}

		getTargetScript();

		// When another Avatar is involved, then he must be interrupted, no backtalk ;)
		if (targetScript.isWithOtherPerson) {

			// Give him the Gameobject of my ObjectController and change him
			Debug.Log($"{gameObject.name}: I'm now interrupting {targetScript.getAvatar().name}");
			StartCoroutine(targetScript.getAvatar().requestActivityChange(gameObject.GetComponentInChildren<ObjectController>().gameObject));
		}

		Debug.Log($"{gameObject.name} in {myRegion.gameObject.name}: I'm now going to {currentActivity.name}");

		// Look where to go and set the navmesh destination
		navComponent.SetDestination(targetScript.WorkPlace);

		// Set Animator ready for going
		animator.SetBool("closeEnough", false);
		animator.SetTrigger("walk");
		animator.applyRootMotion = false;

		going = true;
	}

	private ObjectController getTargetScript(GameObject activity) {

		return activity.GetComponent<ObjectController>();
	}

	private void getTargetScript() {

		// Get the Script
		targetScript = currentActivity.GetComponent<ObjectController>();

		// No script
		if (targetScript == null) {

			// Mayby it's in the children
			targetScript = currentActivity.GetComponentInChildren<ObjectController>();

			// If yes, then also set this as current activity
			if (targetScript != null) currentActivity = targetScript.gameObject;
		}
	}

	// Arrived
	private void OnTriggerEnter(Collider other) {

		ObjectController otherScript = other.GetComponent<ObjectController>();

		// Check if we reached an object with objectcontroller
		// Check if it's the first collider (the work place)
		// Check if it's my current activity
		// Check if I'm currently going, because otherwise this could be triggered while doing
		if (otherScript != null &&
			other == otherScript.gameObject.GetComponents<Collider>()[0] &&
			otherScript.gameObject == currentActivity &&
			going) {

			stopGoing();

			StartCoroutine(startDoing());
		}
	}

	private void stopGoing() {

		going = false;

		// rootMotion on, because we're not walking on the navMesh anymore
		animator.applyRootMotion = true;

		// Stop here
		animator.SetBool("closeEnough", true);
		navComponent.isStopped = true;
		animator.speed = 1f;
	}

	private IEnumerator startDoing() {

		getTargetScript();

		// When there's another Avatar involved, start him also
		if (targetScript.isWithOtherPerson) {
			if (iAmPartner) {
				// I'm the partner

				iAmPartner = false;
			} else {
				// I'm the starter

				StartCoroutine(targetScript.getAvatar().startDoing());
			}

			// Look at the target
			Vector3 targetPos = targetScript.gameObject.transform.position;
			transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));
		} else {

			// Rotate as the Activity says
			rotateRelative();
		}

		// Disable Kinematic, so no physics will affect the animation
		GetComponent<Rigidbody>().isKinematic = true;

		// Disable the navcomponent, because he blocks the height of the avatar during an activity
		navComponent.enabled = false;

		// Slide to another place if neccesary
		if (!targetScript.MoveVector.Equals(Vector3.zero)) {

			StartCoroutine(slideToPlace(true));
		}

		// Starts the usage time, after which the activity will stop
		doing = StartCoroutine(startUsageTime());

		yield return 0;
	}

	/// <summary>
	/// Starts the rotation for a given angle
	/// </summary>
	private void rotateRelative() {

		// Get the rotation of the Target
		Quaternion wrongTargetRot = currentActivity.transform.parent.transform.rotation;
		Quaternion targetRot = Quaternion.Euler(
			wrongTargetRot.eulerAngles.x,
			wrongTargetRot.eulerAngles.y + targetScript.turnAngle + currentActivity.transform.localRotation.eulerAngles.y,
			wrongTargetRot.eulerAngles.z);

		// Rotate myself like this
		StartCoroutine(rotate(targetRot));
	}

	/// <summary>
	/// Rotates the avatar to a given rotation
	/// </summary>
	/// <param name="targetRot"></param>
	/// <returns></returns>
	private IEnumerator rotate(Quaternion targetRot) {

		int counter = 0;
		int number = 200;

		// Similar problem as with the moving function Lerp()
		while (counter < number * 2) {

			counter++;
			// First try to slerp
			if (counter <= number) {

				transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.05f);
				Debug.Log($"{gameObject.name}: Rotating myself to {targetRot.eulerAngles.x}, {targetRot.eulerAngles.y}, {targetRot.eulerAngles.z}"); 
			}

			// Then force the rotation
			if (counter > number) {

				transform.rotation = targetRot;
				Debug.Log($"{gameObject.name}: Rotation settet to {targetRot.eulerAngles.x}, {targetRot.eulerAngles.y}, {targetRot.eulerAngles.z}");
			}

			yield return new WaitForSeconds(0);
		}

		yield return 0;
	}

	/// <summary>
	/// This moves the Avatar in a given vector, after a delay of 5 seconds.
	/// <param name="toMoveVector">Says if we must move towards the movevector or back</param>
	/// </summary>
	private IEnumerator slideToPlace(bool toMoveVector) {

        displaced = toMoveVector;

        // Delay
        yield return new WaitForSeconds(2);

        Vector3 moveVector = targetScript.MoveVector;

        Vector3 targetPos = targetScript.WorkPlace + moveVector;

        // toMoveVector determines if we move towards it or away from it
        if (!toMoveVector) {

            targetPos = targetScript.WorkPlace;
        }

        // Move to pos
        while (Vector3.Distance(transform.position, targetPos) >= 0.5f ) {
            
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime / 3);
            yield return new WaitForSeconds(0);
        }

        yield return 0;
	}

	// Continues with the next activity, after the "useage"-time of the activity endet
	private IEnumerator startUsageTime() {
		
		// Starts to do an animation after a second. I do this because the rotation takes some time
		yield return new WaitForSeconds(1);
		
		animator.SetBool(targetScript.activity.ToString(), true);

		// If we need a tool, then spawn it
		tool = getTool();
		if (tool != null) {
			leftHand = getHand("Left");
			rightHand = getHand("Right");
		}

		// Check if my state changed every 0.025ms
		float ms = 0.025f;
		for (int i = 0; i < targetScript.time*(1/ms) && !activityChangeRequested; i++) {

			// If we have a tool, then place it right again
			if (tool != null) {

				adjustTool(tool, leftHand, rightHand);
			}

			yield return new WaitForSeconds(ms); // Every 25ms
		}

		stopDoing();
    }

	private void adjustTool(GameObject tool, GameObject leftHand, GameObject rightHand) {

		Vector3 leftHandPos = leftHand.transform.position;
		Vector3 rightHandPos = rightHand.transform.position;
		Transform broomTransform = tool.transform;

		// POSITION
		// Stick to the left hand
		broomTransform.position = leftHandPos;

		// Move a little bit down along the local pivot,
		// so the end of the broom will later lie exactly in the right hand
		broomTransform.transform.Translate(Vector3.down / 8);

		// ROTATION
		// Let it look at the right hand
		broomTransform.LookAt(rightHandPos);

		// Turn around 90 degrees, so the end of the broom lies exactly in the right hand
		Vector3 rot = broomTransform.rotation.eulerAngles;
		broomTransform.rotation = Quaternion.Euler(new Vector3(rot.x + 90, rot.y, rot.z));
	}

	private GameObject getHand(string side) {
		GameObject found = null;

		Transform[] transformsInChildren = GetComponentsInChildren<Transform>();
		foreach (Transform child in transformsInChildren) {

			if (child.name == "mixamorig:"+side+"HandMiddle1") {

				found = child.gameObject;
			}
		}

		return found;
	}

	private GameObject getTool() {

		if (targetScript.toolToUse == null) return null;

		GameObject tool = Instantiate(targetScript.toolToUse);
		tool.transform.parent = gameObject.transform;
		tool.transform.localScale = targetScript.toolToUse.transform.localScale;
		return tool;
	}

	private void stopDoing() {

		if (doing != null) StopCoroutine(doing);

		Destroy(tool);

		// Stop doing this activity
		if (targetScript != null) animator.SetBool(targetScript.activity.ToString(), false);

        // Re-place when displaced
        if (displaced) StartCoroutine(slideToPlace(false));

		// Wait with the next target until we are ready to walk again (when sitting or laying)
        StartCoroutine(continueWhenDoneStopping());
    }

    private IEnumerator continueWhenDoneStopping() {

        while (!animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("Idle_Neutral_1")) {

            //Debug.Log($"Kann nicht weiter machen, weil {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");
            yield return new WaitForSeconds(0.2f);
        }

        // Reactivate stuff, that maybe was deactivated in stop()
        navComponent.enabled = true;
        navComponent.isStopped = false;
        GetComponent<Rigidbody>().isKinematic = false;

        setTarget();
    }

    /// <summary>
    /// Deactivate this script when we saw a fire
    /// </summary>
    /// <param name="fire"></param>
    public void deactivateMe(Transform fire) {

        // Only accept a fire once
        if (fire.gameObject == lastFire) {

            Debug.Log($"{gameObject.name}: deactivateMe(): I already saw {fire.gameObject.name}.");
            return;
        }

        Debug.Log($"{gameObject.name}: Deactivating Activity from Fire...");
        lastFire = fire.gameObject;

        // This is to call the burn() method later
        toBurn = true;
        whatBurn = fire;

        // Do a stopping movement
        navComponent.isStopped = true;
        animator.applyRootMotion = true;
        animator.SetTrigger("STOP");
        animator.applyRootMotion = false;

        deactivateMe();
    }

	/// <summary>
	/// Starts deactivating all activities, and allows walking again
	/// </summary>
	private void deactivateMe() {

        startedDeactivating = true;

        // Start walking
        animator.applyRootMotion = false;
        StartCoroutine(resumeAfter(2.5f));

        // Deactivate the last state in the animator
        animator.SetBool(targetScript.activity.ToString(), false);

        // Deactivate this script, 3 seconds because some animations have to finish
        StartCoroutine(deactivateAfter(3f));
    }

    /// <summary>
    /// Activates the AvatarController (for fleeing) and if the cause for deactivation was
    /// a fire, then also call burn(). Then deactivate this script.
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    private IEnumerator deactivateAfter(float seconds) {

        yield return new WaitForSeconds(seconds);

        // Activate the fleeing script
        if (!fleeScript.enabled)
            fleeScript.enabled = true;
        fleeScript.Start();

        if (toBurn) {
            
            fleeScript.burn(whatBurn);
            Debug.Log($"{gameObject.name}: Deactivation: Called Burn()");
        }

        // Deactivate this script
        ActivityController thisScript = (ActivityController)GetComponent(typeof(ActivityController));
        thisScript.enabled = false;
        Debug.Log($"{gameObject.name}: Deactivated Activity.");
    }

    /// <summary>
    /// This Method resumes the navMeshAgent after some seconds
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    private IEnumerator resumeAfter(float seconds) {
        
        yield return new WaitForSeconds(seconds);
        navComponent.isStopped = false;
        Debug.Log($"{gameObject.name}: Nav resumed");
    }

    public void setRegion(RegionController rc) {

        myRegion = rc;
    }

	public void requestActivityChange() {
		
		activityChangeRequested = true;

		if(going) setTarget();
	}
	public IEnumerator requestActivityChange(GameObject activity) {

		nextActivity = activity;
		onlySetDontGo = true;
		currentActivity = null;
		iAmPartner = true;

		requestActivityChange();

		yield return 0;
	}
}

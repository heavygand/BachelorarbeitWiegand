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

	public ObjectController currentActivity;

	public ObjectController CurrentActivity
	{
		get
		{
			return currentActivity;
		}
		set
		{
			currentActivity = value;

			if (currentActivity != null) {

				currentActivity.currentUser = this;
			}
		}
	}

	public ObjectController nextActivity;
	public GameObject bubble;

	private ObjectController lastActivity;

	private GameObject lastFire;
	private GameObject tool;
	private GameObject leftHand;
	private GameObject rightHand;
	private GameObject myBubble;

	private bool toBurn;
	private bool displaced;
	private bool startedDeactivating;
	private bool activityChangeRequested;
	private bool going;
	private bool iAmPartner;
	private bool logging;
	private bool detail10Log;
	private bool randomTarget;

	private Animator animator;
	private NavMeshAgent navComponent;
	private Text alarmText;
	private AvatarController fleeScript;
	private Transform whatBurn;
	private RegionController myRegion;
	private Coroutine doing;

	/// <summary>
	/// Initialisation
	/// </summary>
	void Start() {

		// Init Components
		logging = true;//(name == "Testavatar (4)")
		detail10Log = true;
		randomTarget = true;
		animator = GetComponent<Animator>();
		navComponent = GetComponent<NavMeshAgent>();
		fleeScript = (AvatarController)GetComponent(typeof(AvatarController));
		alarmText = GameObject.Find("alarmTimer").GetComponent<Text>();

		if (CurrentActivity != null)
			CurrentActivity.currentUser = this;
		if (nextActivity != null)
			nextActivity.currentUser = this;

		StartCoroutine(checkIfOutside());
	}

	// Set a target
	private void setTarget() {

		activityChangeRequested = false;

		// Take the next activity if there is one, and save the last activity
		tryNextInQueue();

		// Get a random Target, if we have no
		if (CurrentActivity == null)
			CurrentActivity = findTargetFromActivityList();

		// Get an iterative Target, if we still have no activity
		if (CurrentActivity == null) {

			randomTarget = false;
			CurrentActivity = findTargetFromActivityList();
		}

		randomTarget = true;

		if (CurrentActivity == null) {

			Debug.LogError($"{name}: no activity found!");
			return;
		}
		// Target Activity found.

		if (logging && detail10Log)
			Debug.Log($"{name}: Current Activity is {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")}");

		// Decide what to do now
		if (iAmPartner) {

			if (logging)
				Debug.Log($"{name}: calling startDoing(), because iAmPartner = true");

			startDoing();
		}
		else {

			if (logging && detail10Log)
				Debug.Log($"{name}: calling startGoing()");

			// It is possible, that the other avatar is not there anymore, when we arrive, so this bubble will signalise, that we arrived but noone was there.
			// The bubble has to be smaller than the trigger collider of the destination
			if (CurrentActivity.isWithOther) {
				createBubble();
			}
			startGoing();
		}
	}

	// START GOING
	public void startGoing() {

		// First set a target if we have no
		if (CurrentActivity == null) {

			if (logging && detail10Log)
				Debug.Log($"{name}: rufe setTarget auf, weil meine currentactivity war null");

			setTarget();
			return;
		}

		organizeGroupActivity();

		/*
		 * Start going
		*/

		// Look where to go and set the navmesh destination
		if (navComponent.isOnNavMesh) {

			if (logging) Debug.Log($"{name}: in {myRegion.gameObject.name}: I'm now going to {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")}");
			navComponent.SetDestination(CurrentActivity.WorkPlace);
		}
		else {

			Debug.LogError($"{name} wanted to go to {CurrentActivity}, but wasn't on the navmesh");
		}

		// Set Animator ready for going
		animator.SetBool("closeEnough", false);
		animator.SetTrigger("walk");
		animator.applyRootMotion = false;

		going = true;
	}

	// Arrived
	private void OnTriggerEnter(Collider other) {

		// When this is a destination-bubble, then set a new target and destroy this bubble
		if (other.gameObject == myBubble) {

			setTarget();
			destroyBubble("arrived at bubble. Meaning no activity was here!");
			return;
		}

		ObjectController otherScript = other.GetComponent<ObjectController>();

		// Check if we reached an object with objectcontroller
		// Check if it's the first collider (the work place)
		// Check if it's my current activity
		// Check if I'm currently going, because otherwise this could be triggered while doing
		if (otherScript != null &&
			other == otherScript.gameObject.GetComponents<Collider>()[0] &&
			otherScript == CurrentActivity &&
			going) {

			if (logging)
				Debug.Log($"{name}: arrived at {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")}, calling stopGoing() and startDoing()");

			if (myBubble != null) {

				destroyBubble("arrived normally");
			}

			stopGoing();

			startDoing();
		}
	}

	private void stopGoing() {

		going = false;

		// rootMotion on, because we're not walking on the navMesh anymore
		animator.applyRootMotion = true;

		// Stop here
		animator.SetBool("closeEnough", true);
		if (navComponent.isOnNavMesh) {

			navComponent.isStopped = true;
		} else {

			Debug.LogError($"{name}: soll stoppen, aber ist garnicht auf dem NavMesh. (CurrentActivity: {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")})");
		}
		animator.speed = 1f;
	}

	private void startDoing() {

		if (logging)
			Debug.Log($"{gameObject.name}: start doing for {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")}");

		ObjectController[] componentsInChildren = CurrentActivity.GetComponentsInChildren<ObjectController>();

		// Start the partner when I'm the starter, abort if he wasn't interested
		if (CurrentActivity.isWithOther && !startPartnerIfNecessaryAndPossible()) {

			setTarget();
			return;
		}

		organizeLookRotation(componentsInChildren);

		// Disable Kinematic, so no physics will affect the animation
		GetComponent<Rigidbody>().isKinematic = true;

		// Disable the navcomponent, because he blocks the height of the avatar during an activity
		navComponent.enabled = false;

		// Slide to another place if neccesary
		if (!CurrentActivity.MoveVector.Equals(Vector3.zero)) {

			StartCoroutine(slideToPlace(true));
		}

		if (logging)
			Debug.Log($"{name}: calling startUsageTime() for {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")}");

		// Starts the usage time, after which the activity will stop
		doing = StartCoroutine(startUsageTimeAndAnimation());
	}

	// Continues with the next activity, after the "useage"-time of the activity endet
	private IEnumerator startUsageTimeAndAnimation() {

		if (logging)
			Debug.Log($"{name}: starting usage time for {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")}");
		
		// Starts to do an animation after a second. I do this because the rotation takes some time
		yield return new WaitForSeconds(1);

		// Activate animation. Standing does not have to be activated
		if (CurrentActivity.activity.ToString() != "stand") {
			animator.SetBool(CurrentActivity.activity.ToString(), true);
		}

		// If we need a tool, then spawn it
		setToolAndHandsFields();

		// Activity time. Also check if my state changed every 0.025ms
		float ms = 0.025f;
		for (int i = 0; i < CurrentActivity.time * (1 / ms) && !activityChangeRequested; i++) {

			adjustTool();

			yield return new WaitForSeconds(ms);
		}

		activityChangeRequested = false;
		stopDoing();
	}

	private void stopDoing() {

		if (logging)
			Debug.Log($"{gameObject.name}: stops doing {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")}");

		if (doing != null)
			StopCoroutine(doing);

		Destroy(tool);

		// Stop doing this activity (standing does not have to be deactivated)
		if (CurrentActivity != null && CurrentActivity.activity.ToString() != "stand")
			animator.SetBool(CurrentActivity.activity.ToString(), false);

		// Re-place when displaced
		if (displaced)
			StartCoroutine(slideToPlace(false));

		// Wait with the next target until we are ready to walk again (when sitting or laying)
		StartCoroutine(continueWhenDoneStopping());
	}

	private IEnumerator continueWhenDoneStopping() {

		while (!animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("Idle_Neutral_1")) {

			if (logging)
				Debug.Log($"{name}: Kann nicht weiter machen, weil {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");
			yield return new WaitForSeconds(0.2f);
		}

		if (logging)
			Debug.Log($"{name}: reactivating stuff, because done stopping");
		// Reactivate stuff, that maybe was deactivated in stop()
		navComponent.enabled = true;
		navComponent.isStopped = false;
		GetComponent<Rigidbody>().isKinematic = false;

		// Does the activity still have children?
		ObjectController[] componentsInChildren = CurrentActivity.GetComponentsInChildren<ObjectController>();
		if (componentsInChildren.Length >= 2 && componentsInChildren[1] != null) {

			// Then go there first
			nextActivity = componentsInChildren[1];
		}
		// Else check if we still have loops
		else if (CurrentActivity.loops > 0) {

			CurrentActivity.loops--;
			// Then start with the root again
			ObjectController[] componentsInParent = CurrentActivity.gameObject.GetComponentsInParent<ObjectController>();
			nextActivity = componentsInParent[componentsInParent.Length - 1];
		} else
			CurrentActivity.resetLoops(); // Loops was 0

		// The end. Proceed as usual
		setTarget();
	}

	private void putToolInHand(ObjectController.HandUsage handToUse) {

		tool.transform.position = (handToUse == ObjectController.HandUsage.leftHand ?
										leftHand.transform.position :
										rightHand.transform.position);
	}

	private void adjustTool() {

		if (tool == null)
			return;

		// One hand
		if (CurrentActivity.handToUse != ObjectController.HandUsage.bothHands) {

			putToolInHand(CurrentActivity.handToUse);
			return;
		}

		// Both hands

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

			if (child.name == "mixamorig:" + side + "HandMiddle1") {

				found = child.gameObject;
			}
		}

		return found;
	}

	private void setToolAndHandsFields() {

		if (CurrentActivity.toolToUse == null) {

			tool = null;
			return;
		}

		tool = Instantiate(CurrentActivity.toolToUse);
		tool.transform.parent = gameObject.transform;
		tool.transform.localScale = CurrentActivity.toolToUse.transform.localScale;

		leftHand = getHand("Left");
		rightHand = getHand("Right");
	}

	public void requestActivityChange() {

		if (logging && detail10Log)
			Debug.Log($"{name}: I'm interrupting myself");

		activityChangeRequested = true;

		if (going) {

			if (logging && detail10Log)
				Debug.Log($"{name}: was going, therefore setting new target");
			setTarget();
		}
	}
	public IEnumerator requestActivityChange(ObjectController activity) {

		if (logging)
			Debug.Log($"{name}: Was interrupted to do {activity.name}{(activity.isWithOther ? " with " + activity.transform.parent.gameObject.name : "")}");

		nextActivity = activity;

		iAmPartner = activity.isWithOther;

		requestActivityChange();

		yield return 0;
	}

	/// <summary>
	/// Deactivate this script when we saw a fire
	/// </summary>
	/// <param name="fire"></param>
	public void deactivateMe(Transform fire) {

		// Only accept a fire once
		if (fire.gameObject == lastFire) {

			if (logging)
				Debug.Log($"{gameObject.name}: deactivateMe(): I already saw {fire.gameObject.name}.");
			return;
		}

		if (logging)
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
		animator.SetBool(CurrentActivity.activity.ToString(), false);

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
			if (logging)
				Debug.Log($"{gameObject.name}: Deactivation: Called Burn()");
		}

		// Deactivate this script
		ActivityController thisScript = (ActivityController)GetComponent(typeof(ActivityController));
		thisScript.enabled = false;
		if (logging)
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
		if (logging)
			Debug.Log($"{gameObject.name}: Nav resumed");
	}

	public void setRegion(RegionController rc) {

		myRegion = rc;
	}

	private bool startPartnerIfNecessaryAndPossible() {

		// I'm the partner
		if (iAmPartner) {
			if (logging)
				Debug.Log($"{gameObject.name}: I am the partner.");
			iAmPartner = false;
		}
		// I'm the starter
		else {
			// Try interrupting the partner and give him the activity of my ObjectController
			if (getPartnerPriority(CurrentActivity) < CurrentActivity.Priority) {
				if (logging)
					Debug.Log(
						$"{gameObject.name}: I'm now interrupting {CurrentActivity.getAvatar().name}, because his priority was {getPartnerPriority(CurrentActivity)}");

				StartCoroutine(
					CurrentActivity.getAvatar().requestActivityChange(gameObject.GetComponentInChildren<ObjectController>()));
			}
			// When this wasn't important enough for him
			else {
				if (logging)
					Debug.Log($"{gameObject.name}: my partner is not interestet in my activity.");

				return false;
			}
		}

		return true;
	}

	private void organizeLookRotation(ObjectController[] componentsInChildren) {
		// When there's another Avatar involved, look at him.
		if (CurrentActivity.isWithOther) {
			// Look at the target
			Vector3 targetPos = CurrentActivity.gameObject.transform.position;
			transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));
		}
		// When we have more waypoints, then look at them
		else if (CurrentActivity.lookAtNext && componentsInChildren.Length >= 2 && componentsInChildren[1] != null) {
			Vector3 transformPosition = componentsInChildren[1].transform.position;
			transform.LookAt(new Vector3(transformPosition.x, transform.position.y, transformPosition.z));
		}
		// Else, rotate as the activity says
		else {
			rotateRelative();
		}
	}

	/// <summary>
	/// Starts the rotation for a given angle
	/// </summary>
	private void rotateRelative() {

		// Get the rotation of the Target
		Quaternion wrongTargetRot = CurrentActivity.transform.rotation;
		Quaternion targetRot = Quaternion.Euler(
			wrongTargetRot.eulerAngles.x,
			wrongTargetRot.eulerAngles.y + CurrentActivity.turnAngle,
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
			if (counter <= number) {

				transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.05f);
				//if(logging) Debug.Log($"{gameObject.name}: Rotating myself to {targetRot.eulerAngles.x}, {targetRot.eulerAngles.y}, {targetRot.eulerAngles.z}"); 
			}

			yield return new WaitForSeconds(0.01f);
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

		Vector3 moveVector = CurrentActivity.MoveVector;

		Vector3 targetPos = CurrentActivity.WorkPlace + moveVector;

		// toMoveVector determines if we move towards it or away from it
		if (!toMoveVector) {

			targetPos = CurrentActivity.WorkPlace;
		}

		// Move to pos
		while (Vector3.Distance(transform.position, targetPos) >= 0.01f) {

			if (logging && detail10Log)
				Debug.Log($"{name}: Sliding to place {targetPos}");
			transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime / 3);
			yield return new WaitForSeconds(0);
		}

		yield return 0;
	}

	private void destroyBubble(string reason) {

		Destroy(myBubble);
		myBubble = null;

		if (logging)
			Debug.Log($"{name}: destroyed my bubble because {reason}.");
	}

	private void organizeGroupActivity() {
		// When this is a group activity, then it has to be organized (pick and interrupt the others, etc)
		if (CurrentActivity.isGroupActivity) {
			// How many places we got?
			GameObject parent = CurrentActivity.GetComponentInParent<Transform>().parent.gameObject;

			if (logging)
				Debug.Log(
					$"{name}: Parent {parent.name} found for {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")}");

			List<ObjectController> otherActivities = new List<ObjectController>(
				parent.GetComponentsInChildren<ObjectController>());
			otherActivities.Remove(CurrentActivity);

			if (logging)
				Debug.Log($"{name}: Parent {parent.name} had {otherActivities.Count} children without the target");

			// Pick this amount of other avatars in my region
			List<ActivityController> theOthers = myRegion.getTheAvailableOthersFor(this, otherActivities[0]);
			List<ActivityController> participants = theOthers.GetRange(0, otherActivities.Count);

			if (logging)
				Debug.Log($"{name}: {participants.Count} participants picked");

			// And interrupt them with one place in the groupactivity
			int i = 0;
			foreach (ActivityController participant in participants) {
				if (logging)
					Debug.Log($"{name}: trying to interrupt {participant.name} with {otherActivities[i].name}");

				StartCoroutine(participant.requestActivityChange(otherActivities[i]));
				i++;
			}
		}
	}

	private void createBubble() {

		myBubble = Instantiate(bubble);
		myBubble.transform.position = CurrentActivity.WorkPlace;

		if (logging)
			Debug.Log($"{name}: instantiated a bubble at {myBubble.transform.position}");
	}

	private IEnumerator checkIfOutside() {

		yield return new WaitForSeconds(0.25f);
		if (myRegion == null) {

			GameObject.Find("GameActivityController").GetComponentInChildren<RegionController>().registerAvatar(this);
		}
	}

	/// <summary>
	/// In Update, we check if there's a firealarm
	/// </summary>
	void Update() {

		// If the alarm starts, then stop doing activities
		// TODO remove this from update()
		if (alarmText.text == "FIREALARM" && !startedDeactivating)
			deactivateMe();
	}

	private static int getPartnerPriority(ObjectController found) {

		int partnerPriority = -1;

		// When there is a partner
		if (found.isWithOther) {
			ObjectController partnerCurrActivity = found.getAvatar().CurrentActivity;

			// When he has an activity, get it
			if (partnerCurrActivity != null) {

				partnerPriority = partnerCurrActivity.Priority;
			}
		}

		return partnerPriority;
	}

	private bool activityIsOK(ObjectController activity2Check) {

		if (logging)
			Debug.Log(
				$"{name}: trying {activity2Check.name}{(activity2Check.isWithOther ? " with " + activity2Check.getAvatar().name : "")} as foundactivity");

		// Check if the found activity is ok
		Hashtable criteria = new Hashtable {
			{
				"Activity shall not be the last one", activity2Check != lastActivity
			}, {
				"Activity shall not be the the own activity",
				!(activity2Check.isWithOther && activity2Check.getAvatar() == this)
			}, {
				"Activity must not be occupied", activity2Check.currentUser == null
			}, {
				"Activity must be important enough for the partner",
				getPartnerPriority(activity2Check) < activity2Check.Priority
			}
		};

		bool allCriteriaOK = true;
		foreach (string criterium in criteria.Keys) {

			string thisCriteriumIsOK = criteria[criterium].ToString();
			if (thisCriteriumIsOK != "True") {

				if (logging)
					Debug.Log($"{name}: {activity2Check.name} was not ok, because {criterium}");

				allCriteriaOK = false;
				break;
			}
		}

		return allCriteriaOK;
	}

	private void tryNextInQueue() {
		if (logging && detail10Log)
			Debug.Log(
				$"{name}: last activity was {(lastActivity == null ? "null" : lastActivity.name)} is now {(CurrentActivity == null ? "null" : CurrentActivity.name + (CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : ""))}");
		if (logging && detail10Log)
			Debug.Log(
				$"{name}: current activity was {(CurrentActivity == null ? "null" : CurrentActivity.name + (CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : ""))} and is now {(nextActivity == null ? "null" : nextActivity.name)}");

		// Proceed with activities, when available
		lastActivity = CurrentActivity;
		CurrentActivity = nextActivity;
		nextActivity = null;
	}

	private ObjectController findTargetFromActivityList() {

		//Get the activities in my region
		List<ObjectController> activities = myRegion.getActivities();

		ObjectController foundActivity = null;
		int limit = randomTarget ? 10 : activities.Count;

		// Try to find something random 10 times
		for (int i = 0; i <= limit && CurrentActivity == null; i++) {

			if (logging)
				Debug.Log($"{name}: is Picking {(randomTarget ? "a random target" : "the next activity in the list")}");

			// Pick a random number from the length of the destinationlist or i
			int target = randomTarget ? Random.Range(0, activities.Count) : i;

			// Set this as target
			foundActivity = activities[target];

			// Check if found activity is ok
			if (activityIsOK(foundActivity)) {

				// Then tell the activity, that I use it
				foundActivity.currentUser = this;

				// Set this as current
				CurrentActivity = foundActivity;

				if (logging)
					Debug.Log(
						$"{name}: {CurrentActivity.name}{(CurrentActivity.isWithOther ? " with " + CurrentActivity.getAvatar().name : "")} picked {(randomTarget ? "randomly" : "iteratively")}");
			}
		}

		if (foundActivity == null) {

			Debug.LogWarning($"{name}: Es konnten keine random Aktivitäten nach {activities.Count} versuchen gefunden werden.");
		}

		return foundActivity;
	}
}
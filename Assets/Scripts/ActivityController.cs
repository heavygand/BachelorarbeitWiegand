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

                currentActivity.CurrentUser = this;
            }
        }
    }

    // Must stay public
    public ObjectController nextActivity;

    public ObjectController NextActivity
    {
        get
        {
            return nextActivity;
        }
        set
        {
            nextActivity = value;

            if (nextActivity != null) {

                nextActivity.CurrentUser = this;
            }
        }
    }

    [Tooltip("The destination bubble for partner activities")]
    public GameObject bubble;

    private ObjectController lastActivity;
    private ObjectController interruptedFor;
    private ObjectController secondLastActivity;

    private GameObject tool;
    private GameObject leftHand;
    private GameObject rightHand;
    private GameObject myBubble;
    
    private Transform whatBurn;

    private bool panic;
    private bool activityChangeRequested;
    private bool going;
    private bool findOutside;
    private bool iAmParticipant => MyLeader != null;

    private List<ActivityController> myParticipants;
    private Animator animator;
    private NavMeshAgent navComponent;
    private int retries;

    public RegionController myRegion { get; set; }
    private RegionController oldRegion;

    private Coroutine sliding;
    private Coroutine rotateRoutine;

    public Coroutine Doing { get; private set; }

    private ActivityController myLeader;
    private ObjectController myActivity;
    private Transform smartphone;
    private Object lastAlarm;
    private bool arrivedAtRP;
    private bool fireSeen;
    public bool FireSeen => fireSeen;

    public ActivityController MyLeader
    {
        get
        {
            return myLeader;
        }
        set
        {
            myLeader = value;

            Debug.Log($"{name}: {(myLeader != null ? "My leader is now " + myLeader.name : "I have no leader anymore")}");
            Debug.Log($"{(myLeader != null ? myLeader.name + ": I'm now the leader of " + name : "I'm not the leader of "+name+" anymore")}");
        }
    }

    public bool Displaced { get; set; }

    public bool Panic {
        get {
            return panic;
        }
        set {
            panic = value;

            if (panic) {
                // Speed for running
                navComponent.speed = 4;
                animator.SetBool("panicMode", true);
                Debug.Log($"{name}: I'm in panic!");
            }
            else {
                // Speed for going
                navComponent.speed = 2;
                animator.SetBool("panicMode", false);
                Debug.Log($"{name}: Not in panic anymore");
            }
        }
    }

    /// <summary>
    /// Initialisation
    /// </summary>
    void Start() {

        // Init Components
        findOutside = true;

        myActivity = GetComponentInChildren<ObjectController>();
        animator = GetComponent<Animator>();
        navComponent = GetComponent<NavMeshAgent>();

        // When we already have a current activity, then we can start going
        if (CurrentActivity != null) {

            CurrentActivity.CurrentUser = this;
            prepareGoing();
        }
        if (NextActivity != null) {

            NextActivity.CurrentUser = this;
        }

        StartCoroutine(checkIfOutside());
    }

    // Set a target
    private void setTarget() {

        Debug.Log($"{name}: setTarget() called#Detail10Log");

        activityChangeRequested = false;

        if(Panic) nextActivity = myRegion.getRallyingPoint();

        // Take the next activity if there is one, and save the last activity
        tryNextInQueue();

        // Get a random Target, if we have no
        if (CurrentActivity == null)
            findTarget();

        if (CurrentActivity == null) {

            float waitTime = 0.5f;

            Debug.Log($"{name} setting target failed, trying again in {waitTime}s#Detail10Log");

            StartCoroutine(tryAgainAfterTime(waitTime));
            return;
        }
        // Target Activity found.
        Debug.Log($"{name}: Current Activity is {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}#Detail10Log");

        Debug.Log($"{name}: {(iAmParticipant ? "I am a participant of " + MyLeader.name + "'s " + MyLeader.CurrentActivity.name : "I am no participant of anything (-> no leader)")}#Detail10Log");
        Debug.Log($"{name}: {(CurrentActivity.isAvatar ? "My current activity is another avatar -> " + CurrentActivity.getAvatar().name : "I'm gonna do my activity alone")}#Detail10Log");

        // Decide what to do now
        if (iAmParticipant && CurrentActivity.isAvatar) {

            Debug.Log($"{name}: I don't have to start going");

            Doing = StartCoroutine(startDoing());
        }
        else {

            Debug.Log($"{name}: calling startGoing()#Detail10Log");

            // It is possible, that the other avatar is not there anymore, when we arrive, so this bubble will signalise, that we arrived but noone was there.
            // The bubble has to be smaller than the trigger collider of the destination
            if (CurrentActivity.isMovable) { createBubble(); }

            prepareGoing();
        }
    }

    // START GOING
    public void prepareGoing() {

        // First set a target if we have no
        if (CurrentActivity == null) {

            Debug.Log($"{name}: calling setTarget, because my currentactivity was null#Detail10Log");

            setTarget();
            return;
        }

        // Stop sliding, if we still do
        if (sliding != null) {

            Debug.Log($"{name}: stopping to slide, because I want to start going");
            StopCoroutine(sliding);
            sliding = null;
        }

        StartCoroutine(startGoing());
    }

    private IEnumerator startGoing() {

        // Wait for the start delay
        Debug.Log($"{name}: Waiting {CurrentActivity.startDelay} seconds to start going");
        yield return new WaitForSeconds(CurrentActivity.startDelay);

        // Reactivate stuff, that maybe was deactivated
        navComponent.enabled = true;
        navComponent.isStopped = false;

        // Look where to go and set the navmesh destination
        if (navComponent.isOnNavMesh) {

            navComponent.SetDestination(CurrentActivity.WorkPlace);
            Debug.Log($"{name}: I'm now going to {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");
        }
        else {

            Debug.LogError($"{name} wanted to go to {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}, but wasn't on the navmesh");
        }

        if (name != "Cartoon_SportCar_B01") {

            // Set Animator ready for going
            animator.SetBool("closeEnough", false);
            animator.SetTrigger("walk");
            animator.applyRootMotion = false;
        }

        going = true;
    }

    // Arrived
    private void OnTriggerEnter(Collider other) {

        // When this is a destination-bubble, then set a new target and destroy this bubble
        if (other.gameObject == myBubble) {

            Debug.Log($"{name}: arrived at destinationBubble, but no activity was there (probably moved away)");

            destroyBubble();
            setTarget();

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

            Debug.Log($"{name}: arrived at {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");
            
            if (myBubble != null) destroyBubble();

            stopGoing();

            Doing = StartCoroutine(startDoing());
            
            // Arrived at rallying point after firealarm and panic
            if (Panic && myRegion != null && myRegion == myRegion.getMaster().getOutside() && !going) {
                
                // Check if I'm going to call the fire department
                if (Random.Range(0, 10) % 2 == 0) {

                    callFireDepartment(); 
                }
                StartCoroutine(setPanicFalseAfterDelay());
                arrivedAtRP = true;
            }
        }
    }

    private void stopGoing() {

        Debug.Log($"{name}: I stopped going#Detail10Log");

        going = false;

        if (name != "Cartoon_SportCar_B01") {

            animator.applyRootMotion = true;
            animator.SetBool("closeEnough", true);
        }

        // Stop here
        if (navComponent.isOnNavMesh) {

            navComponent.isStopped = true;
        }
        else {

            Debug.LogError($"{name}: soll stoppen, aber ist garnicht auf dem NavMesh. (CurrentActivity: {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")})");
        }

        animator.speed = 1f;
    }

    // Continues with the next activity, after the "useage"-time of the activity endet
    private IEnumerator startDoing() {

        /*
         * PREPARATIONS
        */

        Debug.Log($"{name}: preparing {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");

        // Activate the object
        CurrentActivity.IsActivated = true;

        // Rotate
        organizeLookRotation();

        // Disable the navcomponent, because he blocks the height of the avatar during an activity
        navComponent.enabled = false;

        // I do this because the rotation takes some time
        yield return new WaitForSeconds(1);

        if (activityChangeRequested) {

            stopDoing();
            yield return new WaitForSeconds(2);
            Debug.LogError($"{name}: Called stopDoing(), but Coroutine startDoing() was still not killed after two seconds");
        }

        // Activate animation. Standing does not have to be activated
        if (CurrentActivity.wichAnimation.ToString() != "stand" && !activityChangeRequested) {

            Debug.Log($"{name}: animation {CurrentActivity.wichAnimation} activated.#Detail10Log");

            if (name != "Cartoon_SportCar_B01") {

                animator.SetBool(CurrentActivity.wichAnimation.ToString(), true); 
            }
        }

        // Slide to another place if neccesary
        if (!CurrentActivity.MoveVector.Equals(Vector3.zero)) {

            sliding = StartCoroutine(slideToPlace(true));
        }

        // Organize group activity
        if (CurrentActivity.isGroupActivity && !iAmParticipant) {

            yield return new WaitForSeconds(CurrentActivity.activationDelay);
            organizeGroupActivity();
        }
        else if (iAmParticipant) {

            Debug.Log($"{name}: organizeGroupActivity() not neccesary, because I am a participant#Detail10Log");
        }

        Debug.Log($"{name}: starting {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");

        // If we need a tool, then spawn it
        setToolAndHandsFields();

        // Check if my activity is an avatar
        bool isWithOther = CurrentActivity.isAvatar;
        ActivityController theOther = CurrentActivity.getAvatar();

        /*
         * ACTIVITY TIME LOOP #########
        */

        Debug.Log($"{name}: my usage time for {CurrentActivity.name} runs now#Detail10Log");

        // Activity time. Also check if my state changed every 20ms
        const float ms = 0.02f;
        int elapsedTime = 0;
        bool timeIsOver = false;
        bool partnerIsAway = false;
        while (!timeIsOver && !activityChangeRequested && !partnerIsAway) {

            adjustTool();

            partnerIsAway = isWithOther && theOther.CurrentActivity != myActivity && theOther.NextActivity != myActivity;

            yield return new WaitForSeconds(ms);
            elapsedTime++;

            timeIsOver = elapsedTime >= CurrentActivity.time * (1 / ms);
        }

        /*
         * END ACTIVITY TIME #########
        */

        // Stopped, because activity time is over
        if (timeIsOver) {

            Debug.Log($"{name}: usage time is over for {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");
        }
        // Stopped, because my partner was interrupted or away
        else if (partnerIsAway) {

            Debug.Log($"{name}: stopping {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}, because my partner was interrupted ({CurrentActivity.getAvatar().name})");
            Debug.Log($"{CurrentActivity.getAvatar().name}: My former partner {name} stopped, because I was interrupted");

            if (myParticipants == null) {

                Debug.LogWarning($"{name}: I had to remove {CurrentActivity.getAvatar().name} from my participants, but the myParticipants list was null");
            }
            else {

                myParticipants.Remove(theOther);

                theOther.MyLeader = null;
            }
        }
        // Stopped, because I was interrupted
        else if (activityChangeRequested && Panic) {

            Debug.Log($"{name}: stopping {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}, because panic");
        }
        else if (activityChangeRequested) {

            Debug.Log($"{name}: stopping {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}, because interrupted");
        }
        else {

            Debug.LogError($"{name}: Stopping {CurrentActivity.name} for an unknown reason");
        }
        stopDoing();
    }

    private void stopDoing() {

        Debug.Log($"{gameObject.name}: stops doing {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}#Detail10Log");

        stopCoroutines();

        if (!checkFurtherChildDestinations()) {

            Debug.Log($"{name}: There were no childDestinations, destroying my tool#Detail10Log");

            Destroy(tool);
            tool = null;
        }

        // Stop doing this activity (standing does not have to be deactivated)
        if (name != "Cartoon_SportCar_B01" && CurrentActivity != null && CurrentActivity.wichAnimation.ToString() != "stand") {

            Debug.Log($"{name}: Setting animator bool of {CurrentActivity.wichAnimation} to false#Detail10Log");

            animator.SetBool(CurrentActivity.wichAnimation.ToString(), false);
        }
        // Re-place when displaced
        if (Displaced) {

            Debug.Log($"{name}: I was displaced, so sliding back#Detail10Log");

            sliding = StartCoroutine(slideToPlace(false));
        }

        // Wait with the next target until we are ready to walk again (when sitting or laying)
        StartCoroutine(continueWhenDoneStopping());
    }

    private IEnumerator continueWhenDoneStopping() {

        Debug.Log($"{name}: Started Coroutine continueWhenDoneStopping()#Detail10Log");

        while (name != "Cartoon_SportCar_B01" && animator.GetAnimatorTransitionInfo(0).duration != 0) {

            Debug.Log($"{name}: Cannot proceed, because I'm still {animator.GetAnimatorTransitionInfo(0).duration}s in a transition from {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");

            yield return new WaitForSeconds(0.2f);
        }
        while (name != "Cartoon_SportCar_B01" && !animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("Idle_Neutral_1")) {

            Debug.Log($"{name}: Cannot proceed, because {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name} has exit time");

            yield return new WaitForSeconds(0.2f);
        }

        // When I am the leader of a group activity, then wait until everyone started with my invoked groupactivity
        while (!allParticipantsStartedAndDeorganize() && !Panic) {

            yield return new WaitForSeconds(0.2f);
        }
        if(!iAmParticipant && Panic) deOrganize();

        Debug.Log($"{name}: done stopping {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");

        // The end. Proceed as usual
        setTarget();
    }

    private void stopCoroutines() {

        if (Doing != null) {

            StopCoroutine(Doing);
            Doing = null;
            Debug.Log($"{name}: Coroutine doing stopped");
        }
        if (rotateRoutine != null) {

            StopCoroutine(rotateRoutine);
            rotateRoutine = null;
            Debug.Log($"{name}: Coroutine rotateRoutine stopped");
        }
        if (sliding != null) {

            StopCoroutine(sliding);
            sliding = null;
            Debug.Log($"{name}: Coroutine sliding stopped");
        }
    }

    private bool allParticipantsStartedAndDeorganize() {
        
        if(iAmParticipant || myParticipants == null) return true;

        // Wait untill all participants started the groupactivity
        foreach (ActivityController parti in myParticipants) {

            if (!Panic && !parti.interruptedFor.IsActivated) {

                    Debug.Log($"{name}: Cannot proceed, because {parti.interruptedFor.name} is not activated yet");
                    Debug.Log($"{parti.name}: My Leader {name} cannot proceed, because {parti.interruptedFor.name} is not activated yet");

                return false;
            }
        }

        /*
         * 
         * Group activity is organized
         * 
         */

        deOrganize();

        return true;
    }

    private void deOrganize() {
        
        if(myParticipants == null) return;

        Debug.Log($"{name}: Deorganizing {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");

        // Deorganize the group activity
        foreach (ActivityController parti in myParticipants) {

            if(parti.MyLeader == this) parti.MyLeader = null;

            if (Panic && !parti.Panic) parti.setPanicAndInterrupt();
        }
        myParticipants = null;
    }

    private List<ObjectController> getOtherActivitiesWithoutThis() {

        // Get the parent of CurrentActivity (a groupactivity has to be organized under a parent with multiple activities)
        Transform parentOfCurrAct = CurrentActivity.GetComponentInParent<Transform>().parent;

        // Get the other activities under this parent
        List<ObjectController> otherActivities = new List<ObjectController>(parentOfCurrAct.GetComponentsInChildren<ObjectController>());
        otherActivities.Remove(CurrentActivity);

        // When there are no other activities at the target parent, then wrap the target parent and myself under a new parent, so my own childdestinations will be found
        if (otherActivities.Count == 0) {

            Debug.LogError($"{name}: Parent {parentOfCurrAct.name} has 0 other activities");
        }

        Debug.Log($"{name}: Parent {parentOfCurrAct.name} had {otherActivities.Count} groupactivitychild without {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");

        return otherActivities;
    }

    // When this is a group activity, then it has to be organized (pick and interrupt the others, etc)
    private void organizeGroupActivity() {

        Debug.Log($"{name}: Organizing {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");

        // When my group activity is another avatar
        if (CurrentActivity.isAvatar) {

            // When I could not interrupt my partner
            if (!CurrentActivity.getAvatar().requestActivityChangeFor(myActivity, this)) {
                
                NextActivity = null;
                interrupt();
            }
            // else
            else {

                myParticipants = new List<ActivityController> { CurrentActivity.getAvatar() };
                Debug.Log($"{name}: {CurrentActivity.getAvatar().name} is now my participant and partner#Detail10Log");
            }
            
            return;
        }

        // Else pick the other avatars for this groupactivity
        List<ObjectController> otherActivities = getOtherActivitiesWithoutThis();

        List<ActivityController> theOthers = CurrentActivity.getRegion().getTheAvailableOthersFor(this, otherActivities[0]);

        if (theOthers.Count == 0) {

            Debug.LogWarning($"{name}: No participants found for groupactivity {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}!");

            // When this was a doorbell, then go back to the old region
            if (CurrentActivity == CurrentActivity.getRegion().doorBell) {

                setRegion(oldRegion);
            }

            NextActivity = null;
            interrupt();
            return;
        }

        myParticipants = theOthers.GetRange(0, (theOthers.Count > otherActivities.Count ? otherActivities.Count : theOthers.Count));

        Debug.Log($"{name}: {myParticipants.Count} participants picked{(myParticipants.Count == 1 ? ": just " + myParticipants[0].name : "")}");

        // And try to interrupt them with one place in the groupactivity
        int i = 0;
        foreach (ActivityController participant in myParticipants) {

            if (!participant.requestActivityChangeFor(otherActivities[i], this)) {

                Debug.Log($"{name}: Could not interrupt {participant.name} with {otherActivities[i]}");
            }

            i++;
        }
        
    }

    // Does the activity still have children?
    private bool checkFurtherChildDestinations() {

        // Ignore children and loops, when activityChangeRequested == true

        ObjectController[] componentsInChildren = CurrentActivity.GetComponentsInChildren<ObjectController>();

        // When the activity still has children
        if (!activityChangeRequested && componentsInChildren.Length >= 2 && componentsInChildren[1] != null) {

            Debug.Log($"{name}: Proceeding to {componentsInChildren[1].name} after this");

            // Then go there first
            NextActivity = componentsInChildren[1];

            return true;
        }
        // Else check if we still have loops
        if (!activityChangeRequested && CurrentActivity.loops > 0) {

            Debug.Log($"{name}: No childDestinations anymore, but I still have {CurrentActivity.loops} loops. Will start over again.");

            CurrentActivity.loops--;

            // Then start with the root again
            ObjectController[] componentsInParent = CurrentActivity.gameObject.GetComponentsInParent<ObjectController>();
            NextActivity = componentsInParent[componentsInParent.Length - 1];

            return true;
        }
        // Loops was 0, proceed normally
        CurrentActivity.resetLoops();

        Debug.Log(
                $"{name}: No childDestinations and no loops. Continuing normally after {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}.#Detail10Log");

        return false;
    }

    private void putToolInHand(ObjectController.HandUsage handToUse) {

        // Place right
        tool.transform.position = (handToUse == ObjectController.HandUsage.leftHand ?
                                        leftHand.transform.position :
                                        rightHand.transform.position);
    }

    private void adjustTool() {

        if (tool == null)
            return;

        // No hand
        if (CurrentActivity.handToUse == ObjectController.HandUsage.noHand) {

            Debug.Log($"{name}: Using my tool without hands.");

            tool.transform.position = transform.position;
        }
        // One hand
        else if (CurrentActivity.handToUse == ObjectController.HandUsage.leftHand
            || CurrentActivity.handToUse == ObjectController.HandUsage.rightHand) {

            Debug.Log($"{name}: Using my tool with one hand: {CurrentActivity.handToUse}.");
            putToolInHand(CurrentActivity.handToUse);
        }
        // Both hands
        else {

            Debug.Log($"{name}: Using my tool with both hands.#Detail10Log");

            Vector3 leftHandPos = leftHand.transform.position;
            Vector3 rightHandPos = rightHand.transform.position;
            Transform toolTransform = tool.transform;

            // POSITION
            // Stick to the left hand
            toolTransform.position = leftHandPos;

            // ROTATION
            // Let it look at the right hand
            toolTransform.LookAt(rightHandPos);
        }
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

            Debug.Log($"{name}: There is no tool to use for me#Detail10Log");

            return;
        }

        Debug.Log($"{name}: I have to use a {CurrentActivity.toolToUse.name} for {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")}");

        tool = Instantiate(CurrentActivity.toolToUse);
        tool.transform.parent = gameObject.transform;
        tool.transform.localScale = CurrentActivity.toolToUse.transform.localScale;
        
        // Rotate the tool once
        Quaternion wrongTargetRot = tool.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(
            wrongTargetRot.eulerAngles.x,
            wrongTargetRot.eulerAngles.y + transform.rotation.eulerAngles.y,
            wrongTargetRot.eulerAngles.z);
        tool.transform.rotation = targetRot;

        leftHand = getHand("Left");
        rightHand = getHand("Right");
    }

    private bool requestActivityChangeFor(ObjectController activity, ActivityController requester) {

        Debug.Log($"{name}: Checking if I can be interrupted through {activity.name} from {requester.name}");
        Debug.Log($"{requester.name}: Trying to interrupt the {CurrentActivity.name} of {name} with {activity.name}");

        int myPriority = CurrentActivity == null ? -1 : CurrentActivity.Priority;

        // My priority has to be higher
        if (activity.Priority > myPriority) {

            StartCoroutine(interruptWith(activity, requester));
            return true;
        }

        Debug.Log($"{name}: I refused the interruption through {activity.name} from {requester.name} his prio={activity.Priority}, my prio={myPriority}");
        Debug.Log($"{requester.name}: {name} refused the interruption through {activity.name}, my prio={activity.Priority}, his prio={myPriority} ({CurrentActivity.name})");

        return false;
    }

    public IEnumerator interruptWith(ObjectController activity, ActivityController requester) {

        if(requester == null) Debug.LogError($"{name}: Requester war null in interruptWith()");
        if(activity == null) Debug.LogError($"{name}: activity war null in interruptWith()");

        Debug.Log($"{name}: Interruption {activity.name}{(activity.isAvatar ? " with " + activity.getAvatar().name : "")} accepted");
        Debug.Log($"{requester.name}: Sucessfully interrupted {name} with {activity.name}");

        MyLeader = requester;

        NextActivity = activity;

        interruptedFor = activity;

        interrupt();

        yield return 0;
    }

    private void interrupt() {

        Debug.Log($"{name}: I'm interrupting myself");

        activityChangeRequested = true;

        if (going) {

            Debug.Log($"{name}: was going, therefore stopping and setting new target#Detail10Log");

            stopGoing();
            setTarget();

            if (Panic) {

                doSurprisedStoppingMovement();
            }
        }
    }

    // Did this to see, if there is only one reference to this, since I want to have interruption as private as possible
    public void interruptFromOutside() {

        interrupt();
    }

    private void organizeLookRotation() {

        ObjectController[] componentsInChildren = CurrentActivity.GetComponentsInChildren<ObjectController>();

        // When there's another Avatar involved, look at him.
        if (CurrentActivity.noTurning) {

            // NO ROTATION
        }
        // When there's another Avatar involved, look at him.
        else if (CurrentActivity.lookAtTarget) {

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
        rotateRoutine = StartCoroutine(rotate(targetRot));
    }

    /// <summary>
    /// Rotates the avatar to a given rotation
    /// </summary>
    /// <param name="targetRot"></param>
    /// <returns></returns>
    private IEnumerator rotate(Quaternion targetRot) {

        for (int counter = 0; counter < 150; counter++) {

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.15f);

            Debug.Log($"{gameObject.name}: Rotating...#Detail10Log");

            yield return new WaitForSeconds(0.01f);
        }

        yield return 0;
    }

    /// <summary>
    /// This moves the Avatar in a given vector, after a delay of 5 seconds.
    /// <param name="toMoveVector">Says if we must move towards the movevector or back</param>
    /// </summary>
    private IEnumerator slideToPlace(bool toMoveVector) {

        Displaced = toMoveVector;

        // Delay
        yield return new WaitForSeconds(1);

        Vector3 moveVector = CurrentActivity.MoveVector;

        Vector3 targetPos = CurrentActivity.WorkPlace + moveVector;

        // toMoveVector determines if we move towards it or away from it
        if (!toMoveVector) {

            targetPos = CurrentActivity.WorkPlace;
        }

        // Move to pos
        while (Vector3.Distance(transform.position, targetPos) >= 0.05f) {

            Debug.Log($"{name}: Sliding to place...");
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime / 3);
            yield return new WaitForSeconds(0);
        }

        yield return 0;
    }

    private void createBubble() {

        if (bubble == null) {

            Debug.LogError($"{name}: I HAVE NO BUBBLE ASSIGNED IN THE INSPECTOR!!!");
            return;
        }

        myBubble = Instantiate(bubble);
        myBubble.name = name + "'s destination Bubble to " + CurrentActivity.getAvatar().name;
        myBubble.transform.position = CurrentActivity.WorkPlace;

        Debug.Log($"{name}: instantiated a bubble at {myBubble.transform.position}");
    }

    private void destroyBubble() {

        Debug.Log($"{name}: Destroying my bubble --- ({myBubble.name})");
        Destroy(myBubble);
        myBubble = null;
    }

    private IEnumerator checkIfOutside() {

        yield return new WaitForSeconds(0.25f);
        if (myRegion == null) {

            GameObject.Find("GameLogic").GetComponentInChildren<RegionController>().registerAvatar(this);
        }
    }

    /// <summary>
    /// In Update, we check if there's a firealarm
    /// </summary>
    void Update() {

        // When the alarm starts, then stop doing activities
        // There has to be alarm
        // Not when I'm already at the rallying point
        // Not when I already paniced
        if (myRegion != null && myRegion.HasAlarm && !arrivedAtRP && !Panic) setPanicAndInterrupt();
    }

    private static int getPartnerPriority(ObjectController found) {

        int partnerPriority = -1;

        // When there is a partner
        if (found.isAvatar) {
            ObjectController partnerCurrActivity = found.getAvatar().CurrentActivity;

            // When he has an activity, get it
            if (partnerCurrActivity != null) {

                partnerPriority = partnerCurrActivity.Priority;
            }
        }

        return partnerPriority;
    }

    /// <summary>
    /// Starts calling the fire department
    /// </summary>
    private void callFireDepartment() {

        smartphone = getLeftSmartphone();

        // He must have a smartphone
        if (smartphone != null) {

            Debug.Log($"{name}: I'm calling the Firedepartment");

            // Stop
            navComponent.enabled = true;
            navComponent.isStopped = true;

            // Do calling animation
            animator.SetBool("call", true);
            smartphone.gameObject.SetActive(true);
            smartphone.gameObject.GetComponent<MeshRenderer>().enabled = true;

            // Resume after some secs
            StartCoroutine(resumeFromCalling(10));
        }
    }

    /// <summary>
    /// Get's the left hand smartphone, it's attached to the left hand for realistic movement while calling
    /// </summary>
    /// <returns></returns>
    private Transform getLeftSmartphone() {

        return transform
            .Find("mixamorig:Hips")
            .Find("mixamorig:Spine")
            .Find("mixamorig:Spine1")
            .Find("mixamorig:Spine2")
            .Find("mixamorig:LeftShoulder")
            .Find("mixamorig:LeftArm")
            .Find("mixamorig:LeftForeArm")
            .Find("mixamorig:LeftHand")
            .Find("Smartphone");
    }

    /// <summary>
    /// Resumes on the navMesh after a given amount of seconds, informs the gamelogic the firefighters are called
    /// and hides the smartphone again
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    private IEnumerator resumeFromCalling(int seconds) {

        yield return new WaitForSeconds(seconds);
        animator.SetBool("call", false);
        smartphone.gameObject.SetActive(false);
        myRegion.getMaster().getFireManager().called(this);
        navComponent.enabled = true;
        navComponent.isStopped = false;
    }

    private void tryNextInQueue() {

        Debug.Log(
                $"{name}: last activity was {(lastActivity == null ? "null" : lastActivity.name)} is now {(CurrentActivity == null ? "null" : CurrentActivity.name + (CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : ""))}#Detail10Log");

        if (NextActivity != null) {
            Debug.Log(
                    $"{name}: got {NextActivity.name} from NextActivity. (current was {(CurrentActivity == null ? "null" : CurrentActivity.name + (CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : ""))})");
        }

        // Proceed with activities, when available
        secondLastActivity = lastActivity;
        lastActivity = CurrentActivity;
        CurrentActivity = NextActivity;
        NextActivity = null;

        if (interruptedFor != null && lastActivity != interruptedFor && CurrentActivity != interruptedFor) {

            Debug.Log($"{name}: lastactivity and currentactivity were not {interruptedFor.name}, so iAmPartnerFor = null");

            interruptedFor = null;
        }
    }

    private void findTarget() {

        findTargetIn(myRegion);
    }

    // Finds a destination. Gets the whole list of destinations for this region, and one "outside"-destination, wich means "change the region".
    // When the Avatar gets "outside", he will pick a destination in that new region
    private void findTargetIn(RegionController forRegion) {

        if (forRegion == null) {

            Debug.LogWarning($"{name}: findTargetIn(forRegion) has been called with forRegion == null");
            return;
        }

        Debug.Log($"{name}: Picking a random target in {forRegion.name}");

        //Get the activities in my region
        List<ObjectController> activities = forRegion.getActivities();

        int maxActivities = findOutside ? activities.Count + 1 : activities.Count;// +1 because of the last "outside" destination
        int limit = 20;

        // Try to find something random
        for (int i = 0; i <= limit && CurrentActivity == null; i++) {

            // Pick a random number from the length of the destinationlist
            int target = Random.Range(0, maxActivities);

            ObjectController foundActivity;

            // Set this as target, when inside of this region
            if (target < activities.Count) {

                foundActivity = activities[target];
            }
            // When there were no activities in the region (should not happen)
            else if (activities.Count == 0) {

                Debug.Log($"{name}: could not find any activities in {forRegion.name}.");

                return;
            }
            // Outside of this region
            else if (target == activities.Count) {

                Debug.Log($"{name}: I rather change my region :)");

                findOutside = false;

                findTargetIn(myRegion.getMaster().getRandomRegionWithOut(myRegion));

                findOutside = true;

                return;
            }
            // This should not happen
            else {

                Debug.LogError($"{name} Unknown error in findTarget()");
                return;
            }

            Debug.Log($"{name}: found {foundActivity.name}{(foundActivity.isAvatar ? " with " + foundActivity.getAvatar().name : "")}.#Detail10Log");

            // Check if found activity is ok
            if (activityIsOK(foundActivity)) {

                // When this is a private region, and it's not my current region, then ring the doorbell first
                if (forRegion.isPrivate && forRegion != myRegion) {

                    Debug.Log($"{name}: other region {forRegion.name} is private, so ringing doorbell first.");

                    CurrentActivity = forRegion.doorBell;
                    NextActivity = foundActivity;
                }
                else {

                    CurrentActivity = foundActivity;

                    Debug.Log($"{name}: Currentactivity is now {CurrentActivity.name}{(CurrentActivity.isAvatar ? " with " + CurrentActivity.getAvatar().name : "")} in {forRegion.name}");
                }
            }
        }
    }

    private IEnumerator tryAgainAfterTime(float waitTime) {

        retries++;

        if (retries >= 3) Debug.LogError($"{name} could not find any target after {retries} tries");

        yield return new WaitForSeconds(waitTime);

        setTarget();
    }

    private bool activityIsOK(ObjectController activity2Check) {

        Debug.Log($"{name}: checking {activity2Check.name}{(activity2Check.isAvatar ? " with " + activity2Check.getAvatar().name : "")}#Detail10Log");

        // Check if the found activity is ok, the bool statements, these criteria have to fulfilled
        Hashtable criteria = new Hashtable {
            {
                "Activity shall not be the last one", activity2Check != lastActivity
            }, {
                "Activity shall not be the the own activity",
                !(activity2Check.isAvatar && activity2Check.getAvatar() == this)
            }, {
                "Activity was occupied by "+(activity2Check.CurrentUser!=null?activity2Check.CurrentUser.name:"WTF a GHOST!"), activity2Check.CurrentUser == null
            }, {
                "Activity must be important enough for the partner",
                getPartnerPriority(activity2Check) < activity2Check.Priority
            }, {
                "Activity must not be the doorbell",
                activity2Check != activity2Check.getRegion().doorBell
            }, {
                "Activity must be findable. 'Cannot be Found'-bool was activated here",
                !activity2Check.cannotBeFound
            }, {
                "Activity shall not be the second last one",
                activity2Check != secondLastActivity
            }
        };

        bool allCriteriaOK = true;
        string reason = null;
        foreach (string criterium in criteria.Keys) {

            string thisCriteriumIsOK = criteria[criterium].ToString();
            if (thisCriteriumIsOK != "True") {

                allCriteriaOK = false;
                reason = criterium;
                break;
            }
        }

        Debug.Log($"{name}: {(allCriteriaOK ? "...was OK " : "...was not OK, because " + reason)}#Detail10Log");

        return allCriteriaOK;
    }

    public void setRegion(RegionController rc) {

        oldRegion = myRegion;

        myRegion = rc;

        Debug.Log($"{name}: My region is now {(myRegion != null ? myRegion.name : "null")} (was {(oldRegion != null ? oldRegion.name : "null")})");

        // Unregister in the region if he isn't the caller, myRegion variable will be nulled as a side effekt
        if (oldRegion != null && oldRegion.getAttenders().Contains(this))
            oldRegion.unregisterAvatar(this);

        // When myRegion == null, then register for the outside area
        StartCoroutine(checkIfOutside());
    }

    public RegionController getRegion() {

        return myRegion;
    }

    public void sawFire() {
        
        fireSeen = true;

        Debug.Log($"{name}: I saw a fire in {myRegion.name}");

        setPanicAndInterrupt();
    }

    private void setPanicAndInterrupt() {

        if (Panic) return;
        Panic = true;

        nextActivity = myRegion.getRallyingPoint();

        // This also eventually stops going and sets target
        interrupt();
    }

    // Do a surprised stopping movement
    private void doSurprisedStoppingMovement() {

        Debug.Log($"{name}: Doing Surprised animation");

        navComponent.enabled = true;
        navComponent.isStopped = true;
        animator.applyRootMotion = true;
        animator.SetTrigger("STOP");
        animator.applyRootMotion = false;
    }

    private IEnumerator setPanicFalseAfterDelay() {

        yield return new WaitForSeconds(5);

        Panic = false;
    }
}
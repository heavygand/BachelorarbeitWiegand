#pragma warning disable 1587
/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>
/// This Script manages all activities and deactivates itself in case of alarm
/// </summary>
public class ActivityController : MonoBehaviour {

    #region INSPECTOR VISIBLE MEMBERS
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

    private ObjectController lastActivity;
    public ObjectController LastActivity
    {
        get
        {
            return lastActivity;
        }
        set
        {
            lastActivity = value;
        }
    }

    [Tooltip("The destination bubble for partner activities")]
    public GameObject bubble;
    
    public GameObject exclamationMark;

    [Tooltip("Toggles the display of the debug log")]
    public bool showDebugWindow;

    [Tooltip("Determines if the detail of the log in the scene view")]
    public bool detailLog;

    [Tooltip("Determines if the caller and the line number shall be shown")]
    public bool showPlace;
    public List<string> log;

    #endregion

    #region PUBLIC MEMBERS

    private string lastMessage { get; set; }
    private string secondToLastMessage { get; set; }

    public bool thinking => !Going && !Doing;
    public bool Going => navComponent!=null && navComponent.isOnNavMesh && !navComponent.isStopped;
    public bool Doing => doingRoutine != null;
    public RegionController myRegion { get; set; }
    public ActivityController MyLeader
    {
        get
        {
            return myLeader;
        }
        set
        {
            myLeader = value;

            log4Me($"{(myLeader != null ? "My leader is now " + myLeader.name : "I have no leader anymore")}");

            if (myLeader != null)
                myLeader.log4Me($"{(myLeader != null ? " I'm now the leader of " + name : "I'm not the leader of " + name + " anymore")}");
        }
    }
    public bool FireSeen => fireSeen;

    public Coroutine doingRoutine { get; private set; }

    public bool Displaced { get; set; }
    public bool isPlayer { get; private set; }
    public bool wasWalking { get; private set; }
    public bool activatedAlarm { get; set; }

    private bool panic;
    public bool Panic
    {
        get
        {
            return panic;
        }
        set
        {
            panic = value;

            if (panic && tag != "Vehicle") {

                // Speed for running
                navComponent.speed = 4;

                animator.SetBool("panicMode", true);

                log4Me($"I'm in panic!");
            }
            else {
                // Speed for Going
                navComponent.speed = 2;
                animator.SetBool("panicMode", false);
                log4Me($"Not in panic anymore");
            }
        }
    }
    public ObjectController SecondLastActivity => secondLastActivity;
    private ObjectController secondLastActivity;
    public RegionController fireRegion { get; private set; }
    public ObjectController activityBeforePanic { get; private set; }
    public bool arrivedAtRP { get; set; }
    public bool stopNow { get; set; }

    #endregion
    #region PRIVATE MEMBERS

    private List<ActivityController> myParticipants;
    private Animator animator;
    private NavMeshAgent navComponent;
    private int retries;

    private Transform whatBurn;
    private Transform smartphone;

    private bool iAmParticipant => MyLeader != null;
    private bool activityChangeRequested;
    private bool findOutside;
    private bool fireSeen;
    private bool checkOnTriggerStay;

    private GameObject tool;
    private GameObject leftHand;
    private GameObject rightHand;
    private GameObject myBubble;
    private GameObject exMark;

    private RegionController oldRegion;

    private Coroutine sliding;
    private Coroutine rotateRoutine;

    private ActivityController myLeader;
    private ObjectController myActivity;
    private ObjectController interruptedFor;
    private Coroutine startGoingRoutine;
    #endregion
    /// <summary>
    /// Initialisation
    /// </summary>
    void Start() {

        // Init Components
        myActivity = GetComponentInChildren<ObjectController>();

        isPlayer = tag == "Player";
        if (isPlayer) {

            myActivity = transform.Find("TalkDestination").GetComponent<ObjectController>();
            return;
        }

        findOutside = true;

        animator = GetComponent<Animator>();
        navComponent = GetComponent<NavMeshAgent>();
        navComponent.isStopped = true;

        // When we already have a current activity, then we can start Going
        if (CurrentActivity != null) {

            CurrentActivity.CurrentUser = this;
            prepareGoing();
        }
        if (NextActivity != null) {

            NextActivity.CurrentUser = this;
        }

        StartCoroutine(checkIfOutside());
        StartCoroutine(noCollisionChecker());
    }

    #region NORMAL WORKFLOW

    // Set a target
    private void setTarget() {

        if(startGoingRoutine != null) {

            StopCoroutine(startGoingRoutine);
            startGoingRoutine = null;
        }

        if (stopNow) {

            CurrentActivity = null;
            NextActivity = null;

            return;
        }

        log4Me($"setTarget() called#Detail10Log");

        activityChangeRequested = false;

        // Take the next activity if there is one, and save the last activity
        tryNextInQueue();

        // Get a random Target, if we have no activity yet
        if (CurrentActivity == null)
            findTarget();

        if (CurrentActivity == null) {

            float waitTime = 0.5f;

            log4Me($"{name} setting target failed, trying again in {waitTime}s#Detail10Log");

            StartCoroutine(tryAgainAfterTime(waitTime));
            return;
        }
        // Target Activity found.
        log4Me($"Current Activity is {CurrentActivity.name}#Detail10Log");

        log4Me($"{(iAmParticipant ? "I am a participant of " + MyLeader.name + "'s " + MyLeader.CurrentActivity.name : "I am no participant of anything (-> no leader)")}#Detail10Log");
        log4Me($"{(CurrentActivity.isAvatar ? "My current activity is another avatar -> " + CurrentActivity.getAvatar().name : "I'm gonna do my activity alone")}#Detail10Log");

        // Decide what to do now
        if (iAmParticipant && CurrentActivity.isAvatar) {

            log4Me($"I don't have to start Going");
            stopGoing();
            doingRoutine = StartCoroutine(startDoing());
        }
        else {

            log4Me($"calling startGoing()#Detail10Log");

            prepareGoing();
        }
    }

    // START GOING
    public void prepareGoing() {

        if(isPlayer) return;

        // First set a target if we have no
        if (CurrentActivity == null) {

            log4Me($"calling setTarget, because my currentactivity was null#Detail10Log");

            setTarget();
            return;
        }

        // Stop sliding, if we still do
        if (sliding != null) {

            log4Me($"stopping to slide, because I want to start Going#Detail10Log");
            StopCoroutine(sliding);
            sliding = null;
        }

        startGoingRoutine = StartCoroutine(startGoing());
    }

    private IEnumerator startGoing() {

        // Wait for the start delay
        log4Me($"Waiting {CurrentActivity.startDelay} seconds to start Going#Detail10Log");
        yield return new WaitForSeconds(CurrentActivity.startDelay);
        
        if(activityChangeRequested) {

            setTarget();
        }

        // Reactivate stuff, that maybe was deactivated
        navComponent.enabled = true;
        navComponent.isStopped = false;

        // Look where to go and set the navmesh destination
        if (navComponent.isOnNavMesh) {

            if (CurrentActivity == null) Debug.LogError($"{name}: I have no current activity here");
            
            // It is possible, that the other avatar is not there anymore, when we arrive, so this bubble will signalise, that we arrived but noone was there.
            // The bubble has to be smaller than the trigger collider of the destination
            if (CurrentActivity.isMovable) createBubble();

            navComponent.SetDestination(CurrentActivity.WorkPlace);
            log4Me($"I'm now Going to {CurrentActivity.name}");
        }
        else {

            Debug.LogError($"{name} wanted to go to {CurrentActivity.name}, but wasn't on the navmesh");
        }

        if (tag != "Vehicle") {

            // Set Animator ready for Going
            animator.SetBool("closeEnough", false);
            animator.SetTrigger("walk");
            animator.applyRootMotion = false;
        }
    }

    // Arrived
    private void OnTriggerEnter(Collider other) {

        // When this is a destination-bubble, then set a new target and destroy this bubble
        if (other.gameObject == myBubble) {

            log4Me($"arrived at destinationBubble, but no activity was there (probably moved away)");

            if (arrivedAtRP) {

                NextActivity = LastActivity;
            }

            checkOnTriggerStay = false;
            destroyBubble();
            setTarget();

            return;
        }

        ObjectController otherScript = other.GetComponent<ObjectController>();

        // Check if we reached an object with objectcontroller
        // Check if it's the first collider (the work place)
        // Check if it's my current activity
        // Check if I'm currently Going, because otherwise this could be triggered while doing
        if (otherScript != null &&
            other == otherScript.gameObject.GetComponents<Collider>()[0] &&
            otherScript == CurrentActivity &&
            Going) {

            checkOnTriggerStay = false;

            log4Me($"arrived at {CurrentActivity.name}");
            
            if (myBubble != null) destroyBubble();

            stopGoing();

            doingRoutine = StartCoroutine(startDoing());

        }
    }

    private void stopGoing() {

        log4Me($"I stopped Going#Detail10Log");

        stopCoroutines();

        if (tag != "Vehicle") {
            
            animator.applyRootMotion = true;
            animator.SetBool("closeEnough", true);
        }

        // Stop here
        if (navComponent.isOnNavMesh) {

            navComponent.isStopped = true;
        }
        else {

            //Debug.LogError($"{name}: soll stoppen, aber ist garnicht auf dem NavMesh. (CurrentActivity: {CurrentActivity.name})");
        }

        if (tag != "Vehicle") {
            animator.speed = 1f; 
        }
    }

    // Continues with the next activity, after the "useage"-time of the activity endet
    private IEnumerator startDoing() {

        /*
         * PREPARATIONS FOR THE ACTIVITY
        */

        log4Me($"preparing {CurrentActivity.name}");

        // Activate the object
        StartCoroutine(CurrentActivity.activate(this));

        // Rotate
        organizeLookRotation();

        // Disable the navcomponent, because he blocks the height of the avatar during an activity
        if (CurrentActivity.makeGhost) {
            navComponent.enabled = false; 
        }

        // Wait for rotation
        if (tag != "Vehicle") {

            yield return new WaitForSeconds(1); 
        }

        // Check if interrupted
        if (activityChangeRequested) {

            stopDoing();
            yield return new WaitForSeconds(2);
            Debug.LogError($"{name}: Called stopDoing(), but Coroutine startDoing() was still not killed after two seconds");
        }

        // Activate animation. Standing does not have to be activated
        if (CurrentActivity.wichAnimation.ToString() != "stand" && !activityChangeRequested) {

            log4Me($"animation {CurrentActivity.wichAnimation} activated.#Detail10Log");

            if (tag != "Vehicle") {

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

            log4Me($"organizeGroupActivity() not neccesary, because I am a participant#Detail10Log");
        }

        // If we need a tool, then spawn it
        setToolAndHandsFields();

        // Check if my activity is an avatar
        bool isWithOther = CurrentActivity.isAvatar;
        ActivityController theOther = CurrentActivity.getAvatar();

        /*
         * 
         * ######### ACTIVITY TIME LOOP #########
         * 
         * 
        */
        
        log4Me($"Doing {CurrentActivity.name}");
        
        // Activity time. Also check if my state changed every 20ms
        const float ms = 0.02f;
        int elapsedTime = 0;
        bool timeIsOver = false;
        bool partnerIsAway = false;
        while (!timeIsOver && !activityChangeRequested && !partnerIsAway && tag != "Vehicle") {

            adjustTool();

            if (isWithOther) {

                partnerIsAway = !theOther.isPlayer && theOther.CurrentActivity != myActivity && theOther.NextActivity != myActivity;
                organizeLookRotation();
            }

            yield return new WaitForSeconds(ms);
            elapsedTime++;

            timeIsOver = elapsedTime >= CurrentActivity.time * (1 / ms);
        }

        /*
         * END ACTIVITY TIME #########
        */

        // Stopped, because activity time is over
        if (timeIsOver) {

            log4Me($"usage time is over for {CurrentActivity.name}");
        }
        // Stopped, because my partner was interrupted or away
        else if (partnerIsAway) {

            log4Me($"stopping {CurrentActivity.name}, because my partner was interrupted ({CurrentActivity.getAvatar().name})");
            CurrentActivity.getAvatar().log4Me($"My former partner {name} stopped, because I was interrupted");

            if (myParticipants != null){

                myParticipants.Remove(theOther);

                theOther.MyLeader = null;
            }
        }
        // Stopped, because I was interrupted
        else if (activityChangeRequested && Panic) {

            log4Me($"stopping {CurrentActivity.name}, because panic");
        }
        else if (activityChangeRequested) {

            log4Me($"stopping {CurrentActivity.name}, because interrupted");
        }
        else if (tag == "Vehicle") {

            log4Me($"stopping {CurrentActivity.name}, because vehicle");
        }
        else {

            Debug.LogError($"{name}: Stopping {CurrentActivity.name} for an unknown reason");
        }
        stopDoing();
    }

    private void stopDoing() {

        log4Me($"stops doing {CurrentActivity.name}#Detail10Log");
        
        stopCoroutines();

        if (!checkFurtherChildDestinations()) {
            
            destroyTool();
            log4Me($"There were no childDestinations#Detail10Log");
        }

        // Stop doing this activity (standing does not have to be deactivated)
        if (tag != "Vehicle" && CurrentActivity != null && CurrentActivity.wichAnimation.ToString() != "stand") {

            log4Me($"Setting animator bool of {CurrentActivity.wichAnimation} to false#Detail10Log");

            animator.SetBool(CurrentActivity.wichAnimation.ToString(), false);
        }
        // Re-place when displaced
        if (Displaced) {

            log4Me($"I was displaced, so sliding back#Detail10Log");

            sliding = StartCoroutine(slideToPlace(false));
        }

        // Wait with the next target until we are ready to walk again (when sitting or laying)
        StartCoroutine(continueWhenDoneStopping());
    }

    private IEnumerator continueWhenDoneStopping() {

        log4Me($"Started Coroutine continueWhenDoneStopping()#Detail10Log");

        // Still doing transition
        while (tag != "Vehicle" && animator.GetAnimatorTransitionInfo(0).duration != 0) {

            log4Me($"Cannot proceed, because I'm still {animator.GetAnimatorTransitionInfo(0).duration}s in a transition from {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");

            yield return new WaitForSeconds(0.2f);
        }
        // Still doing activity animation
        while (tag != "Vehicle" && !animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("Idle_Neutral_1")) {

            log4Me($"Cannot proceed, because {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name} has exit time");

            yield return new WaitForSeconds(0.2f);
        }

        // When my activity is still not activated, then wait
        while (!CurrentActivity.IsActivated) {

            log4Me($"Cannot proceed, because {CurrentActivity.name} is not activated yet");
            yield return new WaitForSeconds(0.2f);
        }

        // When I am the leader of a group activity, then wait until everyone started with my invoked groupactivity
        while (!Panic && !allParticipantsStartedAndDeorganize()) {

            yield return new WaitForSeconds(0.2f);
        }
        if(!iAmParticipant && Panic) deOrganize();

        log4Me($"done stopping {CurrentActivity.name}");

        // Proceed as usual
        setTarget();
    }

    #endregion
    #region HELPER METHODS

    private void stopCoroutines() {

        if (doingRoutine != null) {

            StopCoroutine(doingRoutine);
            doingRoutine = null;
            log4Me($"Coroutine doing stopped#Detail10Log");
        }
        if (rotateRoutine != null) {

            StopCoroutine(rotateRoutine);
            rotateRoutine = null;
            log4Me($"Coroutine rotateRoutine stopped#Detail10Log");
        }
        if (sliding != null) {

            StopCoroutine(sliding);
            sliding = null;
            log4Me($"Coroutine sliding stopped#Detail10Log");
        }
    }

    private bool allParticipantsStartedAndDeorganize() {
        
        if(iAmParticipant || myParticipants == null) return true;

        // Wait untill all participants started the groupactivity
        foreach (ActivityController parti in myParticipants) {

            if (!Panic && parti.interruptedFor != null && !parti.interruptedFor.IsActivated) {

                    log4Me($"Cannot proceed, because {parti.name} did not activate {parti.interruptedFor.name} yet");
                    parti.log4Me($"My Leader {name} cannot proceed, because I didn't activate {parti.interruptedFor.name} yet");

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

        log4Me($"Deorganizing {CurrentActivity.name}");

        // Deorganize the group activity
        foreach (ActivityController parti in myParticipants) {

            if(parti.MyLeader == this) parti.MyLeader = null;

            if (Panic && !parti.Panic) {
                
                log4Me($"I have panic, but my paticipant {parti.name} not, so giving him panic also");
                parti.log4Me($"My leader {name} gave me panic, so I'm calling flee()");
                parti.flee();
            }
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

        log4Me($"Parent {parentOfCurrAct.name} had {otherActivities.Count} groupactivitychild without {CurrentActivity.name}");

        return otherActivities;
    }

    // When this is a group activity, then it has to be organized (pick and interrupt the others, etc)
    private void organizeGroupActivity() {

        log4Me($"Organizing {CurrentActivity.name}");

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
                log4Me($"{CurrentActivity.getAvatar().name} is now my participant and partner#Detail10Log");

                // When there's another Avatar involved, look at him.
                if (CurrentActivity.lookAtTarget) {

                    // Look at the target
                    Vector3 targetPos = CurrentActivity.gameObject.transform.position;
                    transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));
                }
            }
            
            return;
        }

        // Else pick the other avatars for this groupactivity
        List<ObjectController> otherActivities = getOtherActivitiesWithoutThis();

        List<ActivityController> theOthers = CurrentActivity.getRegion().getTheAvailableOthersFor(this, otherActivities[0]);

        if (theOthers.Count == 0) {

            log4Me($"No participants found for groupactivity {CurrentActivity.name}!");

            // When this was a doorbell, then go back to the old region
            if (CurrentActivity == CurrentActivity.getRegion().doorBell) {

                setRegion(oldRegion);
            }

            NextActivity = null;
            interrupt();
            return;
        }

        myParticipants = theOthers.GetRange(0, (theOthers.Count > otherActivities.Count ? otherActivities.Count : theOthers.Count));

        log4Me($"{myParticipants.Count} participants picked{(myParticipants.Count == 1 ? ": just " + myParticipants[0].name : "")}");

        // And try to interrupt them with one place in the groupactivity
        int i = 0;
        foreach (ActivityController participant in myParticipants) {

            if (!participant.requestActivityChangeFor(otherActivities[i], this)) {

                log4Me($"Could not interrupt {participant.name} with {otherActivities[i]}");
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

            log4Me($"Proceeding to {componentsInChildren[1].name} after this");

            // Then go there first
            NextActivity = componentsInChildren[1];

            return true;
        }
        // Else check if we still have loops
        if (!activityChangeRequested && CurrentActivity.loops > 0) {

            log4Me($"No childDestinations anymore, but I still have {CurrentActivity.loops} loops. Will start over again.");

            CurrentActivity.loops--;

            // Then start with the root again
            ObjectController[] componentsInParent = CurrentActivity.gameObject.GetComponentsInParent<ObjectController>();
            NextActivity = componentsInParent[componentsInParent.Length - 1];

            return true;
        }
        // Loops was 0, proceed normally
        CurrentActivity.resetLoops();

        log4Me($"No childDestinations and no loops. Continuing normally after {CurrentActivity.name}.#Detail10Log");

        return false;
    }

    private void putToolInHand(ObjectController.HandUsage handToUse) {

        if (tag == "Vehicle")
            return;

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

            log4Me($"Using my tool without hands.#Detail10Log");

            tool.transform.position = transform.position;
        }
        // One hand
        else if (CurrentActivity.handToUse == ObjectController.HandUsage.leftHand
            || CurrentActivity.handToUse == ObjectController.HandUsage.rightHand) {

            log4Me($"Using my tool with one hand: {CurrentActivity.handToUse}.#Detail10Log");
            putToolInHand(CurrentActivity.handToUse);
        }
        // Both hands
        else {

            log4Me($"Using my tool with both hands.#Detail10Log");

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

            log4Me($"There is no tool to use for me#Detail10Log");

            return;
        }

        log4Me($"I have to use a {CurrentActivity.toolToUse.name} for {CurrentActivity.name}#Detail10Log");

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

    // Interruption with resistance
    private bool requestActivityChangeFor(ObjectController activity, ActivityController requester) {

        if(activity == null) Debug.LogError($"{name}: {requester.name} tried to interrupt me with a null activity");
        if(requester == null) Debug.LogError($"{name}: someone who was null, tried to interrupt me with {activity.name}");

        log4Me($"Checking if I can be interrupted through {activity.name}");
        requester.log4Me($"Trying to interrupt {(CurrentActivity != null ? CurrentActivity.name : "no activity")} of {name} with {activity.name}");


        int myPriority = CurrentActivity == null ? -1 : CurrentActivity.Priority;

        // My priority has to be higher
        if (activity.Priority > myPriority) {

            MyLeader = requester;
            interruptWith(activity);

            return true;
        }

        log4Me($"I refused the interruption through {activity.name} from {requester.name} his prio={activity.Priority}, my prio={myPriority}");
        requester.log4Me($"{name} refused the interruption through {activity.name}, my prio={activity.Priority}, his prio={myPriority} ({CurrentActivity.name})");

        return false;
    }
    
    // Interruption without resistance
    public void interruptWith(ObjectController activity) {
        
        if(activity == null) Debug.LogError($"{name}: activity war null in interruptWith()");

        log4Me($"Interruption {activity.name} accepted");

        NextActivity = activity;

        interruptedFor = activity;

        interrupt();
    }

    private void interrupt() {

        log4Me($"I'm interrupting myself");

        activityChangeRequested = true;

        if (Going) {

            log4Me($"was Going, therefore stopping and setting new target#Detail10Log");

            stopGoing();
            setTarget();

            if (Panic) {

                doSurprisedStoppingMovement();
            }
        }
    }

    public void interruptFromOutside() {

        interrupt();
    }

    private void organizeLookRotation() {

        ObjectController[] componentsInChildren = CurrentActivity.GetComponentsInChildren<ObjectController>();
        
        if (CurrentActivity.noTurning) {

            // NO ROTATION
            log4Me($"Don't have to turn, because CurrentActivity.noTurning was active#Detail10Log");
        }
        // When there's another Avatar involved, look at him.
        else if (CurrentActivity.lookAtTarget) {

            // Look at the target
            Vector3 targetPos = CurrentActivity.gameObject.transform.position;
            transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));

            log4Me($"Looking at target, because CurrentActivity.lookAtTarget was active#Detail10Log");
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

            log4Me($"Rotating...#Detail10Log");

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

            log4Me($"Sliding to place...#Detail10Log");
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

        log4Me($"instantiated a bubble at {myBubble.transform.position}");
    }

    private void destroyBubble() {

        log4Me($"Destroying my bubble --- ({myBubble.name})");
        Destroy(myBubble);
        myBubble = null;
    }

    private void destroyTool() {

        if(tool == null) return;

        log4Me($"Destroying my tool --- ({tool.name})");
        Destroy(tool);
        tool = null;
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

            log4Me($"I'm calling the Firedepartment");

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
        myRegion.getMaster().called(this);
    }

    private void tryNextInQueue() {

        log4Me(
                $"last activity was {(lastActivity == null ? "null" : lastActivity.name)} is now {(CurrentActivity == null ? "null" : CurrentActivity.name)}#Detail10Log");

        if (NextActivity != null) {
            log4Me(
                    $"got {(NextActivity == null ? "null" : NextActivity.name)} from NextActivity. (current was {(CurrentActivity == null ? "null" : CurrentActivity.name)})");
        }

        // Proceed with activities, when available
        secondLastActivity = lastActivity;
        lastActivity = CurrentActivity;
        CurrentActivity = NextActivity;
        NextActivity = null;

        if (interruptedFor != null && lastActivity != interruptedFor && CurrentActivity != interruptedFor) {

            log4Me($"lastactivity and currentactivity were not {interruptedFor.name}, so iAmPartnerFor = null#Detail10Log");

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

            log4Me($"findTargetIn(forRegion) has been called with forRegion == null");
            return;
        }

        log4Me($"Picking a random target in {forRegion.name}");

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

                log4Me($"could not find any activities in {forRegion.name}.");

                return;
            }
            // Outside of this region
            else if (target == activities.Count) {

                log4Me($"I rather change my region :)");

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

            log4Me($"found {foundActivity.name}.#Detail10Log");

            // Check if found activity is ok
            if (activityIsOK(foundActivity)) {

                // When this is a private region, and it's not my current region, then ring the doorbell first
                if (forRegion.isPrivate && forRegion != myRegion) {

                    log4Me($"other region {forRegion.name} is private, so ringing doorbell first.");

                    CurrentActivity = forRegion.doorBell;
                    NextActivity = foundActivity;
                }
                else {

                    CurrentActivity = foundActivity;

                    log4Me($"Currentactivity is now {CurrentActivity.name} in {forRegion.name}");
                }
            }
        }
    }

    private IEnumerator tryAgainAfterTime(float waitTime) {

        retries++;

        if (retries >= 5) Debug.LogError($"{name} could not find any target after {retries} tries");

        yield return new WaitForSeconds(waitTime);

        setTarget();
    }

    private bool activityIsOK(ObjectController activity2Check) {

        log4Me($"checking {activity2Check.name}#Detail10Log");

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
                "Activity must be findable. 'Cannot be Found'-bool was activated here",
                !activity2Check.cannotBeFound
            }, {
                "Activity shall not be the second last one",
                activity2Check != SecondLastActivity
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

        log4Me($"{(allCriteriaOK ? "...was OK " : "...was not OK, because " + reason)}#Detail10Log");

        return allCriteriaOK;
    }

    public void setRegion(RegionController rc) {
        
        if(isPlayer) return;

        oldRegion = myRegion;

        myRegion = rc;

        log4Me($"My region is now {(myRegion != null ? myRegion.name : "null")} (was {(oldRegion != null ? oldRegion.name : "null")})#Detail10Log");

        // Unregister in the region if he isn't the caller, myRegion variable will be nulled as a side effekt
        if (oldRegion != null && oldRegion.getAttenders().Contains(this)) oldRegion.unregisterAvatar(this);

        // When myRegion == null, then register for the outside area
        log4Me($"Starting coroutine checkIfOutside()#Detail10Log");
        StartCoroutine(checkIfOutside());
    }

    private IEnumerator checkIfOutside() {

        // This waiting is because of the beginning, this could immediatly register outside, before the normal regionregistration can happen
        yield return new WaitForSeconds(0.25f);

        log4Me($"Coroutine checkIfOutside() started#Detail10Log");

        if (myRegion == null) {

            log4Me($"My region was null, so registering outside#Detail10Log");
            GameObject.Find("GameLogic").GetComponent<GameLogic>().outside.registerAvatar(this);
        }
        else {

            log4Me($"My region was not null, doing nothing in checkIfOutside()#Detail10Log");
        }
    }

    public RegionController getRegion() {

        return myRegion;
    }

    public void sawFire() {
        
        fireSeen = true;
        createExclamationMark();

        log4Me($"I saw a fire in {myRegion.name}");

        myRegion.add2FirePeople(this);

        flee();
    }

    public void flee() {

        if (arrivedAtRP || Panic || tag == "Vehicle") return;

        log4Me($"I am fleeing now");

        fireRegion = myRegion;
        wasWalking = Going;
        activityBeforePanic = CurrentActivity;

        if(myRegion == null) Debug.LogError($"{name} I have no region here");

        NextActivity = myRegion.getRallyingPoint();

        setPanicAndInterrupt();
    }

    public void setPanicAndInterrupt() {
        
        Panic = true;
        log4Me($"Panic settet and interrupting");

        // This also eventually stops Going and sets target
        interruptWith(myRegion.getRallyingPoint());
    }

    // Do a surprised stopping movement
    private void doSurprisedStoppingMovement() {

        log4Me($"Doing Surprised animation#Detail10Log");

        navComponent.enabled = true;
        navComponent.isStopped = true;
        animator.applyRootMotion = true;
        animator.SetTrigger("STOP");
        animator.applyRootMotion = false;
    }

    public void log4Me(string logString, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null) {

        if(log == null) log = new List<string>();

        string toLog = $"{DateTime.Now:HH:mm:ss.fff}*{caller}:{lineNumber}#{(Panic ? "(PANIC) " : "")}{logString}";

        if (logString != secondToLastMessage && logString != lastMessage) log.Add(toLog);

        secondToLastMessage = lastMessage;
        lastMessage = logString;
    }

    private IEnumerator setPanicFalseAfterDelay() {

        yield return new WaitForSeconds(2);

        Panic = false;
    }

    public NavMeshAgent getNavMeshAgent() {

        return navComponent;
    }

    private IEnumerator noCollisionChecker() {

        /*
         *
         * Wait as long as...
         * ...the navAgent is Going
         * ...our distance to the target is high enough
         * 
         */
        while (thinking || Doing || Going) {

            bool hasArrived = Vector3.Distance(transform.position, navComponent.destination) < 0.5f;
            
            if (thinking || Doing || Going && !hasArrived) {

                yield return new WaitForSeconds(1);
            }
            else {

                // When the navAgent is not stopped, but we arrived, then stop and find a new target
                if (Going && hasArrived) {

                    log4Me($"I arrived at the place, but did not call OnTriggerEnter, so checking OnTriggerStay once");
                    checkOnTriggerStay = true;
                }
                else {

                    log4Me($"NoCollisionChecker stopped waiting for another reason");
                }
                yield return new WaitForSeconds(1);
            }
        }
        if(Going && Doing) Debug.LogError($"{name}: I was going and doing at the same time!");
        log4Me($"NoCollisionChecker stopped #####################################");
    }

    private void createExclamationMark() {

        exMark = Instantiate(exclamationMark);
        exMark.transform.parent = transform;
        exMark.transform.localPosition = Vector3.zero;
        exMark.transform.localEulerAngles = Vector3.zero;
    }

    public void removeExclamationMark() {

        Destroy(exMark);
        exMark = null;
    }

    private void OnTriggerStay(Collider other) {

        if(!checkOnTriggerStay) return;

        OnTriggerEnter(other);
    }

    public void doRallyingPointStuff() {

        log4Me($"I'm now doing rallying point stuff");
        
        // Check if I'm Going to call the fire department
        if (Random.Range(0, 10) % 2 == 0) {

            callFireDepartment();
        }

        StartCoroutine(setPanicFalseAfterDelay());
        arrivedAtRP = true;
    }

    #endregion
}
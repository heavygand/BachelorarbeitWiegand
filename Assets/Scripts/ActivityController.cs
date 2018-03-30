using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>
/// This Script manages all behaviours of an avatar or vehicle
/// Interacts with Objectcontrollers
/// Is registered in a Regioncontroller
/// <p>Author: Christian Wiegand</p>
/// <p>Matrikelnummer: 30204300</p>
/// </summary>
public class ActivityController : MonoBehaviour {

    #region INSPECTOR VISIBLE MEMBERS
    /// <summary>
    /// The current activity, this is not only set while doing, but is already set while going
    /// </summary>
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

    /// <summary>
    /// The activity to do after the current activity
    /// </summary>
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

                log4Me($"{nextActivity.name} was set as nextactivity");
                nextActivity.CurrentUser = this;
            }
        }
    }
    /// <summary>
    /// The last activity
    /// </summary>
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
    /// <summary>
    /// The speed of the rotation, when the avatar arrived at his work place. should be lower than 0,2
    /// </summary>
    [Tooltip("The speed of the rotation, when the avatar arrived at his work place. should be lower than 0,2")]
    public float rotationSpeed;

    /// <summary>
    /// This is the rate in seconds, at wich the activity time will refresh it's status. This affects the rotation speed, the positioning of a tool in the hands, the detection that a talkpartner has left and so on. This will have consequences for the performance, but also for the looks
    /// </summary>
    [Tooltip("This is the rate in seconds, at wich the activity time will refresh it's status. This could be the positioning of a tool in the hands, or the detection, that a talkpartner has left. This will have consequences for the performance, but also for the looks")]
    [Range(0.05f, 0.01f)]
    public float activityTimeRefreshRate;

    /// <summary>
    /// The destination bubble for partner activities
    /// </summary>
    [Tooltip("The destination bubble for partner activities")]
    public GameObject bubble;

    /// <summary>
    /// The exclamation mark to show above the head when a fire is seen
    /// </summary>
    [Tooltip("The exclamation mark to show above the head when a fire is seen")]
    public GameObject exclamationMark;

    /// <summary>
    /// Toggles the display of the debug log
    /// </summary>
    [Tooltip("Toggles the display of the debug log")]
    public bool showDebugWindow;

    /// <summary>
    /// Determines if the detail of the log in the scene view
    /// </summary>
    [Tooltip("Determines if the detail of the log in the scene view")]
    public bool detailLog;

    /// <summary>
    /// Determines if the caller and the line number shall be shown
    /// </summary>
    [Tooltip("Determines if the caller and the line number shall be shown")]
    public bool showPlace;


    /// <summary>
    /// A list of logging messages for this avatar
    /// </summary>
    public List<string> log;

    #endregion

    #region PUBLIC MEMBERS

    private string lastMessage { get; set; }
    private string secondToLastMessage { get; set; }

    /// <summary>
    /// Says if the avatar is currently not going and not doing anything
    /// It probably means that he's checking where to go next, or waiting for an animation to end
    /// </summary>
    public bool thinking => !Going && !Doing;

    /// <summary>
    /// Says if the avatar is currently walking/running somewhere
    /// </summary>
    public bool Going => navComponent!=null && navComponent.isOnNavMesh && !navComponent.isStopped;

    /// <summary>
    /// Says if the avatar is currently fulfilling an activity
    /// </summary>
    public bool Doing => doingRoutine != null;

    /// <summary>
    /// My current Region
    /// </summary>
    public RegionController myRegion { get; set; }

    /// <summary>
    /// The AvatarController, that is the leader of my current groupactivity
    /// </summary>
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
                myLeader.log4Me($"{(myLeader != null ? "I'm now the leader of " + name : "I'm not the leader of " + name + " anymore")}");
        }
    }

    /// <summary>
    /// When we saw a fire
    /// </summary>
    public bool FireSeen => fireSeen;

    /// <summary>
    /// The coroutine that represents the doing of something
    /// </summary>
    public Coroutine doingRoutine { get; private set; }

    /// <summary>
    /// Says if the avatar was moved away by slideToPlace()
    /// </summary>
    public bool Displaced { get; set; }

    /// <summary>
    /// Says if this AvatarController is attached to the first person controller
    /// </summary>
    public bool isPlayer { get; private set; }

    /// <summary>
    /// Says if the avatar was currently going somewhere, when his panic started
    /// </summary>
    public bool wasWalking { get; private set; }

    /// <summary>
    /// Says if this avatar activated the firealarm
    /// </summary>
    public bool activatedAlarm { get; set; }

    private bool panic;
    /// <summary>
    /// Not only indicates if the avatar has panic, but also changes the walkspeed and the animation
    /// </summary>
    public bool Panic
    {
        get
        {
            return panic;
        }
        set
        {
            panic = value;

            if (panic && !vehicle) {

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

    /// <summary>
    /// The region where the avatar currently was, when he started fleeing
    /// </summary>
    public RegionController fireRegion { get; private set; }

    /// <summary>
    /// The activity that the avatar currently did, when he started fleeing
    /// </summary>
    public ObjectController activityBeforePanic { get; private set; }

    /// <summary>
    /// Says if we arrived at the rallying point
    /// </summary>
    public bool arrivedAtRP { get; set; }

    /// <summary>
    /// Stops doing everything, when activated
    /// </summary>
    public bool stopNow { get; set; }

    /// <summary>
    /// This is the activity that represents himself. Will probably be his own talkdestination
    /// </summary>
    public ObjectController myActivity { get; private set; }

    /// <summary>
    /// This is the activity that represents the avatars callingDestination
    /// </summary>
    public ObjectController myCalling { get; private set; }

    /// <summary>
    /// Determines, if the Start() method has been started
    /// </summary>
    public bool started { get; set; }

    /// <summary>
    /// Determines if this is a vehicle
    /// </summary>
    public bool vehicle { get; private set; }

    #endregion
    #region PRIVATE MEMBERS

    private List<ActivityController> myParticipants;
    private Animator animator;
    private NavMeshAgent navComponent;
    private int retries;

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
    private GameObject looker;

    private RegionController oldRegion;

    private Coroutine sliding;

    private ActivityController myLeader;
    private ObjectController interruptedFor;
    private Coroutine startGoingRoutine;
    #endregion

    /// <summary>
    /// Initialisation of components
    /// Checking if we are in the outside area
    /// Checking if this is the player, or a vehicle
    /// Looking for attached activities
    /// </summary>
    public void Start() {

        if(started) return;
        started = true;

        vehicle = tag == "Vehicle";
        isPlayer = tag == "Player";

        // Init Components
        if (!vehicle) {

            myActivity = transform.Find("TalkDestination").GetComponent<ObjectController>();

            myActivity.name = $"{myActivity.name} with {name}";

            if(!isPlayer) myCalling = transform.Find("callingDestination").GetComponent<ObjectController>(); 
        }

        if (isPlayer) {

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

        if (myActivity == null && !vehicle) {

            Debug.LogWarning($"Achtung: {name} hat keine Talkdestination, d.h. dieser Avatar kann nicht angesprochen werden.");
        }
        if (myCalling == null && !vehicle) {

            Debug.LogWarning($"Achtung: {name} hat keine Talkdestination, d.h. dieser Avatar kann nicht angesprochen werden.");
        }
        if (!vehicle && bubble == null) {

            Debug.LogError($"Fehler: {name} hat keine Destination Bubble im Inspector bekommen, weise das Prefab unter Prefabs/Attenders im Inspektor zu");
        }
        if (!vehicle && exclamationMark == null) {

            Debug.LogWarning($"Achtung: {name} hat kein Ausrufezeichen im Inspector bekommen, weise das Prefab unter Prefabs/Attenders im Inspektor zu");
        }
    }

    #region NORMAL WORKFLOW

    /// <summary>
    /// Retrieves a new activity
    /// This can be from the next activity assigned in the inspector, or from this region, or from another region
    /// Goes into a waiting state, when no activity could be found
    /// Afterwards decides what to do next, start going or start doing
    /// </summary>
    private void setTarget() {

        if(startGoingRoutine != null) {

            StopCoroutine(startGoingRoutine);
            startGoingRoutine = null;
            log4Me($"startGoingRoutine for {CurrentActivity.name} killed#Detail10Log");
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
        if (iAmParticipant && CurrentActivity.isAvatar || CurrentActivity == myCalling) {

            log4Me($"I don't have to start Going");
            stopGoing();
            doingRoutine = StartCoroutine(startDoing());
        }
        else {

            log4Me($"calling startGoing()#Detail10Log");

            prepareGoing();
        }
    }

    /// <summary>
    /// Checking things, like...
    /// ... if this is the player
    /// ... we still have no target
    /// ... we still slide
    /// Then starts startGoing
    /// </summary>
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

    /// <summary>
    /// Starts the NavAgent and the walk animation
    /// Also checks the current activity for a start delay
    /// </summary>
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

        if (!vehicle) {

            // Set Animator ready for Going
            animator.SetBool("closeEnough", false);
            animator.SetTrigger("walk");
            animator.applyRootMotion = false;
        }
    }

    /// <summary>
    /// Checks where the avatar arrived, destination bubble, or destination activity
    /// Will start stopGoing and startDoing, when it's the destination activity
    /// </summary>
    /// <param name="other">The collider that we touched</param>
    private void OnTriggerEnter(Collider other) {

        // When this is a destination-bubble, then set a new target and destroy this bubble
        if (other.gameObject == myBubble) {

            log4Me($"arrived at destinationBubble, but no activity was there (probably moved away)");

            if (arrivedAtRP) {

                log4Me($"Setting the NextActivity with the last activity {LastActivity.name}");
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

    /// <summary>
    /// Stops going
    /// Stops the walking animation and the NavAgent
    /// </summary>
    private void stopGoing() {

        log4Me($"I stopped Going#Detail10Log");

        stopCoroutines();

        if (!vehicle) {
            
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

        if (!vehicle) {
            animator.speed = 1f; 
        }
    }

    /// <summary>
    /// Starts doing the current activity
    /// Gets the information needed for the correct usage from the ObjectController of the activity and then...
    /// ...activates the object
    /// ...becomes a ghost if necessary
    /// ...activates the animation
    /// ...slides to a place if necessary
    /// ...organizes a groupactivity if necessary
    /// ...spawns a tool if necessary
    /// 
    /// Then starts the activity time loop, wich has a refresh rate to
    /// ...check the remaining activity time and
    /// ...check the current state and
    /// ...fulfilles the rotation to fulfill for this activity
    /// After that, stopDoing is called
    /// </summary>
    private IEnumerator startDoing() {

        /*
         * PREPARATIONS FOR THE ACTIVITY
        */

        log4Me($"preparing {CurrentActivity.name}");

        // Activate the object
        StartCoroutine(CurrentActivity.activate(this));

        // Disable the navcomponent, because he blocks the height of the avatar during an activity
        if (CurrentActivity.makeGhost) {
            navComponent.enabled = false; 
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

            if (!vehicle) {

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
        
        // Activity time. Also check if my state changed every 10ms
        activityTimeRefreshRate = 0.01f;
        int elapsedTime = 0;
        bool timeIsOver = false;
        bool partnerIsAway = false;
        while (!timeIsOver && !activityChangeRequested && !partnerIsAway && !vehicle) {

            adjustTool();

            organizeLookRotation();

            if (isWithOther) partnerIsAway = !theOther.isPlayer && theOther.CurrentActivity != myActivity && theOther.NextActivity != myActivity;

            yield return new WaitForSeconds(activityTimeRefreshRate);
            elapsedTime++;

            timeIsOver = elapsedTime >= CurrentActivity.time * (1 / activityTimeRefreshRate);
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

            log4Me($"stopping {CurrentActivity.name}, because my partner was interrupted ({theOther.name})");
            CurrentActivity.getAvatar().log4Me($"My former partner {name} stopped, because I was interrupted");

            if (myParticipants != null){

                myParticipants.Remove(theOther);

                theOther.MyLeader = null;
            }
        }
        // Stopped, because I was interrupted
        else if (activityChangeRequested && Panic) {

            log4Me($"stopping {CurrentActivity.name}, because activityChangeRequested && panic was true");
        }
        else if (activityChangeRequested) {

            log4Me($"stopping {CurrentActivity.name}, because interrupted");
        }
        else if (vehicle) {

            log4Me($"stopping {CurrentActivity.name}, because vehicle");
        }
        else {

            Debug.LogError($"{name}: Stopping {CurrentActivity.name} for an unknown reason");
        }
        stopDoing();
    }

    /// <summary>
    /// Prepares the termination of the current activity
    /// Calls continueWhenDoneStopping afterwards
    /// </summary>
    private void stopDoing() {

        log4Me($"stops doing {CurrentActivity.name}#Detail10Log");
        
        stopCoroutines();

        if (!checkFurtherChildDestinations()) {
            
            destroyTool();
            log4Me($"There were no childDestinations#Detail10Log");
        }

        // Stop doing this activity (standing does not have to be deactivated)
        if (!vehicle && CurrentActivity != null && CurrentActivity.wichAnimation.ToString() != "stand") {

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

    /// <summary>
    /// Because you can't just immediatly stop everything, we look here if some things have to finish first.
    /// This can be...
    /// ...when we still are in a transition in the animator
    /// ...when we have to wait for the exit time in the animator
    /// ...when the object we activated, still isn't finished with activating yet
    /// ...when we have to wait in the context of a groupactivity, that everyone has started participating
    /// 
    /// After this, setTarget will be called and everything starts over again
    /// </summary>
    private IEnumerator continueWhenDoneStopping() {

        log4Me($"Started Coroutine continueWhenDoneStopping()#Detail10Log");

        // Still doing transition
        while (!vehicle && animator.GetAnimatorTransitionInfo(0).duration != 0) {

            log4Me($"Cannot proceed, because I'm still {animator.GetAnimatorTransitionInfo(0).duration}s in a transition from {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");

            yield return new WaitForSeconds(0.2f);
        }
        // Still doing activity animation
        while (!vehicle && !animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("Idle_Neutral_1")) {

            log4Me($"Cannot proceed, because {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name} has exit time");

            yield return new WaitForSeconds(0.2f);
        }

        // When my activity is still not activated, then wait. (only when single user activity)
        while (!CurrentActivity.multiUserActivity && !CurrentActivity.IsActivated) {

            log4Me($"Cannot proceed, because {CurrentActivity.name} is not activated yet");
            yield return new WaitForSeconds(0.2f);
        }

        // When I am the leader of a group activity, then wait until everyone started with my invoked groupactivity
        while (!Panic && !allParticipantsStartedAndDeorganize()) {

            yield return new WaitForSeconds(0.2f);
        }

        CurrentActivity.deactivate();

        if (!iAmParticipant && Panic) deOrganize();

        log4Me($"done stopping {CurrentActivity.name}");

        // Proceed as usual
        setTarget();
    }

    #endregion
    #region HELPER METHODS

    /// <summary>
    /// Will stop all coroutines that are listet in here
    /// </summary>
    private void stopCoroutines() {

        if (doingRoutine != null) {

            StopCoroutine(doingRoutine);
            doingRoutine = null;
            log4Me($"Coroutine doing stopped#Detail10Log");
        }
        if (sliding != null) {

            StopCoroutine(sliding);
            sliding = null;
            log4Me($"Coroutine sliding stopped#Detail10Log");
        }
    }

    /// <summary>
    /// Goes through all participants we have and checks if they all started
    /// Calls deOrganize() afterwards
    /// </summary>
    /// <returns>false, when someone still hasn't started. Else, true</returns>
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

    /// <summary>
    /// De-organizes a groupactivity, wich means to remove myself as his leader and to forget him as participant
    /// Also passes panic on to them
    /// </summary>
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

    /// <summary>
    /// Organizes a group activity, participants have to be picked and interrupted
    /// Will pick the participants from this region only
    /// </summary>
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
        List<ObjectController> otherActivities = CurrentActivity.getParticipantActivities(this);

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

    /// <summary>
    /// Checks if this activity still has childactivities
    /// When an activity is done and there are still childactivities, then the avatar has to pick them as next activity.
    /// When all childDestinations are done, it will check if there are loops registered in the lowest childDestination, if yes, then it starts with the highest parent activity again
    /// This creats a waypoint system with childactivities. It is used by vehicles and walkDestinations
    /// </summary>
    /// <returns>True, when there are childDestinations or loops. False, when not</returns>
    private bool checkFurtherChildDestinations() {

        // Ignore children and loops, when activityChangeRequested == true

        ObjectController[] componentsInChildren = CurrentActivity.GetComponentsInChildren<ObjectController>();

        // When the activity still has children
        if (!activityChangeRequested && componentsInChildren.Length >= 2 && componentsInChildren[1] != null) {

            log4Me($"Proceeding to {componentsInChildren[1].name} after this");

            // Then go there first
            log4Me($"Setting the NextActivity with componentsInChildren[1] {componentsInChildren[1].name}");
            NextActivity = componentsInChildren[1];

            return true;
        }
        // Else check if we still have loops
        if (!activityChangeRequested && CurrentActivity.loops > 0) {

            log4Me($"No childDestinations anymore, but I still have {CurrentActivity.loops} loops. Will start over again.");

            CurrentActivity.loops--;

            // Then start with the root again
            ObjectController[] componentsInParent = CurrentActivity.gameObject.GetComponentsInParent<ObjectController>();

            log4Me($"Setting the NextActivity with componentsInParent[componentsInParent.Length - 1] {componentsInParent[componentsInParent.Length - 1].name}");
            NextActivity = componentsInParent[componentsInParent.Length - 1];

            return true;
        }
        // Loops was 0, proceed normally
        CurrentActivity.resetLoops();

        log4Me($"No childDestinations and no loops. Continuing normally after {CurrentActivity.name}.#Detail10Log");

        return false;
    }

    /// <summary>
    /// Puts the tool in one hand.
    /// A tool has to be available
    /// </summary>
    /// <param name="handToUse">The hand, where to put our tool in</param>
    private void putToolInHand(ObjectController.HandUsage handToUse) {

        if (vehicle)
            return;

        // Place right
        tool.transform.position = (handToUse == ObjectController.HandUsage.leftHand ?
                                        leftHand.transform.position :
                                        rightHand.transform.position);
    }

    /// <summary>
    /// Adjusts the rotation and position of the tool
    /// Is called from the activity time loop, so a tool will stay placed, so it looks like the avatar holds it
    /// How to adjust the tool, depends on the number of hands to use for this tool
    /// </summary>
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

    /// <summary>
    /// Returns the hand of the given side of the avatar
    /// Attention: The finding depends on the specific name pattern as you can see below, so when you use another type of avatar with other hand names, then you will have to adjust this
    /// </summary>
    /// <param name="side">The side of the body, where the hand shall be searched</param>
    /// <returns>the hand of the given side of the avatar</returns>
    private GameObject getHand(string side) {
        GameObject found = null;

        Transform[] transformsInChildren = GetComponentsInChildren<Transform>();
        foreach (Transform child in transformsInChildren) {

            if (child.name.Contains(side+"HandMiddle1")) {

                found = child.gameObject;
            }
        }

        return found;
    }

    /// <summary>
    /// Initializes the tool and hands for further usage
    /// </summary>
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

    /// <summary>
    /// Requests an activity change with a given activity
    /// This is the correct interface for other avatars to try to interrupt this avatar with
    /// Do not use interruptWith() or interrupt() from other avatars
    /// Will check if the activity of the requester is important enough to interrupt the current activity of this avatar
    /// Gets the priorities from the ObjectControllers and matches them
    /// </summary>
    /// <param name="activity">The activity, the requester wants to do this avatar</param>
    /// <param name="requester">The other avatar, that wants to interrupt this avatar</param>
    /// <returns>If the interruption was succesfull or not</returns>
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

    /// <summary>
    /// This is an interruption with a certain activity with no resistance
    /// Use requestActivityChangeFor() from other avatars, to prevent that avatars get interrupted all the time
    /// Sets the given activity as the next one and calls interrupt()
    /// </summary>
    /// <param name="activity">The activity to do next</param>
    public void interruptWith(ObjectController activity) {
        
        if(activity == null) Debug.LogError($"{name}: activity war null in interruptWith()");

        log4Me($"Interruption {activity.name} accepted, so setting this as nextactivity and interruptedfor");

        NextActivity = activity;

        interruptedFor = activity;

        interrupt();
    }

    /// <summary>
    /// Interrupts the current activity and proceeds with the next one (wich setTarget() will find out)
    /// There are two cases to look at for interruption:
    /// While doing something, set the flag activityChangeRequested, so the activitytime-loop will notice it and stop
    /// While going somewhere, just stop going and find something new in setTarget()
    /// </summary>
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

    /// <summary>
    /// Interruptions from another script
    /// Is only here, because I wanted to control from where interruptions can come
    /// </summary>
    public void interruptFromOutside() {

        interrupt();
    }

    /// <summary>
    /// Retrieves from the ObjectController, where to look at when arrived at the place
    /// Can do no looking
    /// Can look at the target
    /// Can look at the next childactivity
    /// Can do a relative rotation to the targetobject
    /// 
    /// Uses slerp. The slerp is repeadetly called by the activity time loop.
    /// </summary>
    private void organizeLookRotation() {

        if(CurrentActivity.noTurning) return;

        ObjectController[] componentsInChildren = CurrentActivity.GetComponentsInChildren<ObjectController>();
        Quaternion targetRot;

        // When lookAtTarget is active
        if (CurrentActivity.lookAtTarget) {

            targetRot = getLookAtRotation(CurrentActivity.gameObject.transform.position);
        }
        // When we have more waypoints, then look at them
        else if (CurrentActivity.lookAtNext && componentsInChildren.Length >= 2 && componentsInChildren[1] != null) {

            targetRot = getLookAtRotation(componentsInChildren[1].transform.position);
        }
        // Else, rotate as the activity says
        else {

            // Get the rotation of the Target
            Quaternion wrongTargetRot = CurrentActivity.transform.rotation;

            // Add the angle that is in the inspector of the objectcontroller
            targetRot = Quaternion.Euler(
                wrongTargetRot.eulerAngles.x,
                wrongTargetRot.eulerAngles.y + CurrentActivity.turnAngle,
                wrongTargetRot.eulerAngles.z);

            log4Me($"Rotating towards the target");

        }
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed);
    }

    /// <summary>
    /// A helper method for the lookat-rotation
    /// Creates an empty gameobject, lets it look at the given target, als return the correct rotation for a usage in slerp
    /// This trick became necessary, because Transform.LookAt() does the rotation immediatly, but I wanted to have a smooth rotation
    /// </summary>
    /// <param name="targetPos">The target to look at</param>
    /// <returns>The "lookAt"-Quaternionrotation</returns>
    private Quaternion getLookAtRotation(Vector3 targetPos) {

        // Let the looker look at the target
        if (looker == null) {

            looker = new GameObject {
                name = "looker"
            };
            looker.transform.parent = gameObject.transform;
            looker.transform.localPosition = Vector3.zero;
        }

        looker.transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));

        // Return his rotation, for the usage in slerp
        return looker.transform.rotation;
    }

    /// <summary>
    /// After a delay, this moves the Avatar to a given vector relative to the workplace of the current activity
    /// <param name="toMoveVector">Says if we must move towards the movevector or back</param>
    /// </summary>
    private IEnumerator slideToPlace(bool toMoveVector) {

        log4Me($"slideToPlace({toMoveVector}) aufgerufen#Detail10Log");
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
        while (Vector3.Distance(transform.position, targetPos) >= 0.03f) {

            log4Me($"Sliding to place...#Detail10Log");
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime / 3);
            yield return new WaitForSeconds(0);
        }

        yield return 0;
    }

    /// <summary>
    /// Creates a destinationBubble
    /// Such a bubble is created, when the avatar has to walk to a unstatic destination, wich could be gone when arrived
    /// Normally, the avatar would just collide with the trigger collider of the destination, but when nothing is there, then nothing will happen.
    /// So for movable destinations, this bubble is created and will indicate when arrived at the destination without colliding with the target
    /// The destinationBubble should of course be smaller the the target's trigger collider to prevent that the avatar thinks that nothing is there, although the destination is there
    /// </summary>
    private void createBubble() {

        myBubble = Instantiate(bubble);
        myBubble.name = name + "'s destination Bubble to " + CurrentActivity.getAvatar().name;
        myBubble.transform.position = CurrentActivity.WorkPlace;

        log4Me($"instantiated a bubble at {myBubble.transform.position}");
    }

    /// <summary>
    /// Removes the destination bubble
    /// </summary>
    private void destroyBubble() {

        log4Me($"Destroying my bubble --- ({myBubble.name})");
        Destroy(myBubble);
        myBubble = null;
    }

    /// <summary>
    /// Removes the tool that the avatar has in his hands
    /// </summary>
    private void destroyTool() {

        if(tool == null) return;

        log4Me($"Destroying my tool --- ({tool.name})");
        Destroy(tool);
        tool = null;
    }

    /// <summary>
    /// Check if the current activity of the avatar of a found talkdestination is low enough to start this activity
    /// This currently is only for talkdestination, but sould generally for all destination that another avatar represents, like maybe kissing someone or shoving someone. everything where the activity is the avatar itself
    /// </summary>
    /// <param name="found">The own activity of another avatar</param>
    /// <returns>a priority number for comparison</returns>
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

        log4Me($"I'm calling the Firedepartment");

        // Do calling animation
        myCalling.enabled = true;

        ObjectController destination = CurrentActivity;

        log4Me($"Setting the NextActivity with myCallingDestination {myCalling.name}");
        NextActivity = myCalling;

        interrupt();

        // Resume after some secs
        StartCoroutine(resumeFromCalling(myCalling.time, destination));
    }

    /// <summary>
    /// Informs the gamelogic the firefighters are called
    /// </summary>
    /// <param name="seconds">The seconds to wait</param>
    /// <param name="destination">The destination to return to, after the call is finished</param>
    private IEnumerator resumeFromCalling(int seconds, ObjectController destination) {

        yield return new WaitForSeconds(seconds);

        if (!activityChangeRequested) {

            log4Me($"Waited for {seconds}s and setting the NextActivity with the destination in the given parameter of resumeFromCalling {destination.name}");
            NextActivity = destination;

            interrupt(); 
        }

        myCalling.enabled = false;
        myRegion.getMaster().called(this);
    }

    /// <summary>
    /// Gets the next activity and saves the last one
    /// </summary>
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

    /// <summary>
    /// Calls findTarget() in the current region
    /// </summary>
    private void findTarget() {

        findTargetIn(myRegion);
    }

    /// <summary>
    /// Finds a destination.
    /// Gets the whole list of activities for a given region, and one "outside"-destination, wich means "change the region"
    /// When we have to change the region, we will call this method again for that region
    /// After that, we take a random activity from it and call activityIsOK(), wich checks if the activity is ok to start
    /// When the activity is ok, then set the CurrentActivity field
    /// </summary>
    /// <param name="forRegion">The region to search in</param>
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

    /// <summary>
    /// Tries to find a new activity after some time
    /// Is called from setTarget(), when we could not find an activity
    /// </summary>
    /// <param name="waitTime">The time to wait</param>
    private IEnumerator tryAgainAfterTime(float waitTime) {

        retries++;

        if (retries >= 5) Debug.LogWarning($"{name} could not find any target after {retries} tries");

        yield return new WaitForSeconds(waitTime);

        setTarget();
    }

    /// <summary>
    /// Checks if an activity is ok to start with
    /// There is a Hashtable below, in wich criteria can be placed in, wich all will be checked
    /// The hashtable consists of an error string that will be logged, when the activity was not ok, and of a bool statement that has to be fulfilled
    /// </summary>
    /// <param name="activity2Check">The activity to check</param>
    /// <returns>If the checking for this activity was successfull</returns>
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
                "Activity was occupied by "+(activity2Check.CurrentUser!=null?activity2Check.CurrentUser.name:"a GHOST!"),
                (activity2Check.CurrentUser == null || activity2Check.multiUserActivity)
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

    /// <summary>
    /// Sets the regions of this avatar
    /// Also checks bidirectional, to prevent that only one side knows the other
    /// Also check if we are outside, when the region was set to null
    /// </summary>
    /// <param name="rc">The RegionController of the new region, wich can be null.</param>
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

    /// <summary>
    /// Checks if the avatar is outside
    /// That means if he is not registered in any region
    /// Then register in the outside area
    /// </summary>
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

    /// <summary>
    /// Return the current region
    /// </summary>
    /// <returns>the current region</returns>
    public RegionController getRegion() {

        return myRegion;
    }

    /// <summary>
    /// Starts the behaviour, that has to be done, when the avatar saw a fire
    /// This will lead to fleeing and panic
    /// </summary>
    public void sawFire() {
        
        fireSeen = true;
        createExclamationMark();

        log4Me($"I saw a fire in {myRegion.name}");

        myRegion.add2FirePeople(this);

        flee();
    }

    /// <summary>
    /// Starts fleeing to a rallying point
    /// Will lead to panic
    /// The avatar will memorize in wich state he was at this time, wich will be user in the dialogs with the player later on
    /// </summary>
    public void flee() {

        if (arrivedAtRP || Panic || vehicle) return;

        log4Me($"I am fleeing now (will set next activity to rallying point)");

        fireRegion = myRegion;
        wasWalking = Going;
        activityBeforePanic = CurrentActivity;

        if(myRegion == null) Debug.LogError($"{name} I have no region here");

        NextActivity = myRegion.getRallyingPoint();

        setPanicAndInterrupt();
    }

    /// <summary>
    /// Get panic and interrupt the current activity
    /// setTarget() will then find the correct next activity to do
    /// </summary>
    public void setPanicAndInterrupt() {
        
        Panic = true;
        log4Me($"Panic settet and interrupting");

        // This also eventually stops Going and sets target
        interruptWith(myRegion.getRallyingPoint());
    }

    /// <summary>
    /// Does a surprised stopping movement
    /// </summary>
    private void doSurprisedStoppingMovement() {

        log4Me($"Doing Surprised animation#Detail10Log");

        navComponent.enabled = true;
        navComponent.isStopped = true;
        animator.applyRootMotion = true;
        animator.SetTrigger("STOP");
        animator.applyRootMotion = false;
    }

    /// <summary>
    /// This is the personal logging method for this avatar
    /// Useful information is also attached to the logstring
    /// Will be shown in the debug window in the scene view
    /// linenumber and caller are found automaticly and don't have to be provided as parameters
    /// </summary>
    /// <param name="logString">The string to log</param>
    /// <param name="lineNumber">The lineNumber of the caller. Does not have to be provided</param>
    /// <param name="caller">The calling method. Does not have to be provided</param>
    public void log4Me(string logString, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null) {

        if(log == null) log = new List<string>();

        string toLog = $"{DateTime.Now:HH:mm:ss.fff}*{caller}:{lineNumber}#{(Panic ? "(PANIC) " : "")}{logString}";

        if (logString != secondToLastMessage && logString != lastMessage) log.Add(toLog);

        // To prevent the same logline thousand times
        secondToLastMessage = lastMessage;
        lastMessage = logString;
    }

    /// <summary>
    /// Will remove the panic after 2 seconds
    /// </summary>
    private IEnumerator setPanicFalseAfterDelay() {

        yield return new WaitForSeconds(2);

        Panic = false;
    }

    /// <summary>
    /// Return the nav mesh agent
    /// </summary>
    /// <returns>the nav mesh agent</returns>
    public NavMeshAgent getNavMeshAgent() {

        return navComponent;
    }

    /// <summary>
    /// This slow coroutine checks if the avatar arrived at his navmesh destination, but didn't collide with a trigger collider
    /// Unfortunatly, there still are some cases where this can happen, for example when the avatar already stand inside of the collider he wants to go, when he starts going, so OnTriggerEnter() was already called long ago and does not react anymore
    /// In that case, OnTriggerStay will be checked once, to see if we already stand in the target collider.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Spawns the exclamation mark above the head of the avatar
    /// </summary>
    private void createExclamationMark() {

        exMark = Instantiate(exclamationMark);
        exMark.transform.parent = transform;
        exMark.transform.localPosition = Vector3.zero;
        exMark.transform.localEulerAngles = Vector3.zero;
    }

    /// <summary>
    /// Removes the exclamation mark above the head of the avatar
    /// </summary>
    public void removeExclamationMark() {

        Destroy(exMark);
        exMark = null;
    }

    /// <summary>
    /// Check if we are in a collider already
    /// This method will do nothing most of the time, because it's very expensive
    /// Calls OnTriggerEnter()
    /// </summary>
    /// <param name="other">The collider we collide with</param>
    private void OnTriggerStay(Collider other) {

        if(!checkOnTriggerStay) return;

        OnTriggerEnter(other);
    }

    /// <summary>
    /// Do the things that have to be done, when arrived at the rallying point
    /// </summary>
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
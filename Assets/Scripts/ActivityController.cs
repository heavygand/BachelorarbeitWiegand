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
    /// In Update, we check if there's a firealarm, we check if the activity has changed
    /// we check if we're close enough to the destination, we check if we have to do some
    /// special actions like sitting or laying down
    /// </summary>
    void Update () {

        // If the alarm starts, then stop doing activities
        if (alarmText.text == "FIREALARM" && !startedDeactivating) deactivateMe();
    }

    // Set a target. Everything starts here.
    public void setTarget() {

        // Get a random Target, if we have no
        if (currentActivity == null) {

            Debug.Log("Picking random target");

            //Get the activities in my region
            List<ObjectController> activities = myRegion.getActivities();

            // Pick a random number from the length of the destinationlist
            int target = Random.Range(0, activities.Count);

            // Set this as target
            currentActivity = activities[target].gameObject;
        }

        // Get the Script
        targetScript = currentActivity.GetComponent<ObjectController>();

        // START GOING

        // Look where to go and set the navmesh destination
        currTargetPos = currentActivity.transform.position + targetScript.WorkPlace;
        navComponent.SetDestination(currTargetPos);

        // Set Animator ready for going
        animator.SetBool("closeEnough", false);
        animator.SetTrigger("walk");
        animator.applyRootMotion = false;

        Debug.Log($"{gameObject.name} in {myRegion.gameObject.name}: I'm now going to {currentActivity.name}");
    }

    // Called from the targetobject, when arrived
    public void stop() {

        Debug.Log($"{gameObject.name} stopped by {currentActivity.name}");

        // rootMotion on, because we're not walking on the navMesh anymore
        animator.applyRootMotion = true;

        // Stop here
        animator.SetBool("closeEnough", true);
        navComponent.isStopped = true;
        animator.speed = 1f;

        if (targetScript.makeGhost) {

            GetComponent<Rigidbody>().isKinematic = true;
            navComponent.enabled = false;
        }
        rotateRelative();

        if (!targetScript.MoveVector.Equals(Vector3.zero)) {

            StartCoroutine(gotoPos(true)); 
        }

        StartCoroutine(startAction());

        StartCoroutine(continueAfter());
    }
    /// <summary>
    /// This moves the Avatar in a given vector, after a delay of 5 seconds.
    /// <param name="toMoveVector">Says if we must move towards the movevector or back</param>
    /// </summary>
    private IEnumerator gotoPos(bool toMoveVector) {

        displaced = toMoveVector;

        // Delay
        yield return new WaitForSeconds(2);

        Vector3 moveVector = targetScript.MoveVector;

        Vector3 targetPos = targetScript.transform.position + targetScript.WorkPlace + moveVector;

        // toMoveVector determines if we move towards it or away from it
        if (!toMoveVector) {

            targetPos = targetScript.transform.position + targetScript.WorkPlace;
        }

        // Move to pos
        while (Vector3.Distance(transform.position, targetPos) >= 0.5f ) {
            
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime / 3);
            yield return new WaitForSeconds(0);
        }

        yield return 0;
    }

    // Continues with the next activity, after the "useage"-time of the activity endet
    private IEnumerator continueAfter() {

        string thisthing = currentActivity.name;
        
        yield return new WaitForSeconds(targetScript.time);

        Debug.Log($"Aktivitätszeit für {thisthing} vorbei, mache etwas anderes");
        continueToNext();
    }

    public void continueToNext() {

        // When the button in scene view gets pressed
        if (!Application.isPlaying) {
            
            Debug.LogError("Aktivitätswechsel nur zur Laufzeit verfügbar!");
            return;
        }

        // Stop doing this activity
        animator.SetBool(targetScript.activity.ToString(), false);

        // Re-place when displaced
        if (displaced) {

            Debug.Log("Displaced -> re-placing");
            StartCoroutine(gotoPos(false));
        }

        // Proceed with activities, when available
        currentActivity = nextActivity;
        nextActivity = null;

        if(currentActivity != null) Debug.Log($"Proceeding to activity: {currentActivity.name}");

        // Wait with the next target until we are ready to walk again (when sitting or laying)
        StartCoroutine(continueWhenDone());
    }

    private IEnumerator continueWhenDone() {

        while (!animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("Idle_Neutral_1")) {

            Debug.Log($"Kann nicht weiter machen, weil {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");
            yield return new WaitForSeconds(0.2f);
        }
        Debug.Log($"Kann jetzt weiter machen");

        // Reactivate stuff, that maybe was deactivated in stop()
        navComponent.enabled = true;
        navComponent.isStopped = false;
        GetComponent<Rigidbody>().isKinematic = false;

        setTarget();
    }

    /// <summary>
    /// Starts to do an animation after a second. I do this because the rotation before the sitting takes some time
    /// </summary>
    /// <returns></returns>
    private IEnumerator startAction() {

        // Start acting after some time
        yield return new WaitForSeconds(1);
        animator.SetBool(targetScript.activity.ToString(), true);
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
                //Debug.Log($"{gameObject.name} on {myFloor}: Rotating myself to {targetRot.eulerAngles.x}, {targetRot.eulerAngles.y}, {targetRot.eulerAngles.z}"); 
            }

            // Then force the rotation
            if (counter > number) {

                transform.rotation = targetRot;
                //Debug.Log($"{gameObject.name} on {myFloor}: Rotation settet to {targetRot.eulerAngles.x}, {targetRot.eulerAngles.y}, {targetRot.eulerAngles.z}");
            }

            yield return new WaitForSeconds(0);
        }

        yield return 0;
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
    public void deactivateMe() {

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
    
    private GameObject tempState;
    private GameObject[] destinations;
    public GameObject currentActivity;
    public GameObject nextActivity;
    private GameObject lastFire;
    private Vector3 currTargetPos;
    private SortedList<float, GameObject> sortedDests;
    private Animator animator;
    private NavMeshAgent navComponent;
    private Text alarmText;
    private AvatarController fleeScript;
    private Transform whatBurn;
    private RegionController myRegion;
    private bool start;
    private bool toBurn;
    private bool displaced;
    private bool targetsExist;
    private bool startedDeactivating;
    private ObjectController targetScript;
}

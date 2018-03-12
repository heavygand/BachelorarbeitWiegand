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
/// This Script manages the fleeing in case of an alarm or a fire sighting. It also takes care about
/// calling the firefighters and triggering the firealarm and the usage of the smartphone at the end
/// </summary>
public class AvatarController : MonoBehaviour {

    /// <summary>
    /// Initialisation
    /// </summary>
    public void Start() {

        // Don't start if we're currently setting the alarm or we already started
        if (settingAlarm || hasStarted)
            return;
        hasStarted = true;

        // Important for the update() method
        targetSettet = true;
        getMyStuff();

        // Get the rallying Point
        //endTarget = myRegion.getRallyingPoint();
        endTargetPos = endTarget.transform.position;

        // Init Components
        alarmTextField = GameObject.Find("alarmTimer").GetComponent<Text>();
        logic = GameObject.Find("GameLogic").GetComponentInChildren<FireManager>();
        animator = GetComponent<Animator>();
        navComponent = GetComponent<NavMeshAgent>();


        // Init animator
        animator.SetBool("closeEnough", false);
        animator.applyRootMotion = false;
        
        gotoRallyingPoint();
    }

    /// <summary>
    /// Reacts on a given fire
    /// </summary>
    /// <param name="fire"></param>
    public void burn(Transform fire) {

        // Ignore fire in case we are currently setting the alarm
        if (settingAlarm)
            return;

        if (!hasStarted)
            Start();

        // Ignore a fire I have already seen, this is to prevent multiple triggering of one fire shortly after each other
        if (lastFire != null && fire.gameObject == lastFire) {

            Debug.Log($"{gameObject.name}: burn(): I already saw {fire.gameObject.name}.");
            return;
        }

        // Save this
        lastFire = fire.gameObject;

        // Console output
        Debug.Log($"{gameObject.name}: FEUER {fire.gameObject.name} GESEHEN!!");

        gotoRallyingPoint();
    }

    /// <summary>
    /// Chooses and set the target that lies as closest to the avatar as destination
    /// </summary>
    private void gotoRallyingPoint() {

        // Target still not settet
        targetSettet = false;

        // Start setting the destination
        // I am doing navComponent.Resume(); in a coroutine because I had the problem, that the avatar wasn't
        // able to reach the destination when it lies directly behind a corner, this fixed it.
        StartCoroutine(setDestinationAfterDelay(0));
    }

    /// <summary>
    /// Sets the destination of the navMeshAgent to the currentTarget after a given amount of seconds
    /// </summary>
    /// <param name="delay"></param>
    /// <returns></returns>
    private IEnumerator setDestinationAfterDelay(float delay) {

        yield return new WaitForSeconds(delay);
        navComponent.SetDestination(endTargetPos);
        Debug.Log($"{name}: I'm now going to {endTarget.name}");

        if (!targetSettet) targetSettet = true;
    }

    /// <summary>
    /// Update manages calling the fire department, loading of new targets, reaching the destination
    /// and the picture taking with the smartphone
    /// </summary>
    private void Update() {

        // Only update if necessary
        if (settingAlarm || stopped) return;

        // Check if we already ran start(), I had one case where this didn't happen
        if (!hasStarted) Start();

        // Only continue when the target is set
        if (!targetSettet) return;

        // Position and region
        getMyStuff();

        // Check if I'm going to call the fire department
        //if (!settingAlarm && !calling && Random.Range(0, 1000) == 15) callFireDepartment();

        //Check if I'm close enough to the Target, then stop
        if (Vector3.Distance(myPos, currTargetPos) < 1) {

            // If this is the rallying Point, then stay, else move on to the rallying point
            if (Vector3.Distance(myPos, endTargetPos) < 1) {

                animator.SetBool("closeEnough", true);
                navComponent.isStopped = true;
                stopped = true;
                
                animator.applyRootMotion = true;

                // "Gaffen"
                transform.LookAt(GameObject.Find("House").transform);

                // Take a picture with smartphone (not PrefabSportler, he's naked)
                if (Random.Range(0, 2) == 1 && !gameObject.name.Contains("PrefabSportler")) {

                    // Start the "hand up" animation
                    animator.SetTrigger("holdSmartphone");

                    // Spawn the smartphone into the hand
                    StartCoroutine(phoneAfterDelay(0.5f));
                }
            } else {

                // Proceed to the rallying point if we reached a normal destination
                Debug.Log($"{gameObject.name}: {currTarget.name} reached. I'm going to the rallying point!");
                currTarget = endTarget;
                currTargetPos = endTargetPos;
                navComponent.SetDestination(currTargetPos);
            }
        } else if (!stopped) { // Still going

            animator.SetBool("closeEnough", false);
        }

    }

    /// <summary>
    /// Shows the smartphone for the right hand after a given time
    /// </summary>
    /// <param name="delay"></param>
    /// <returns></returns>
    private IEnumerator phoneAfterDelay(float delay) {

        yield return new WaitForSeconds(delay);

        // The smartphone for the right hand is directly under the parentnode
        smartphone = transform.Find("Smartphone");

        // He must have a smartphone
        if (smartphone != null) {

            smartphone.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Finds the current position and floor
    /// </summary>
    private void getMyStuff() {

        myPos = transform.position;
        ActivityController ac = gameObject.GetComponent<ActivityController>();
        myRegion = ac.myRegion;
    }

    /// <summary>
    /// Starts a break from fleeing and triggers a given firealarm
    /// </summary>
    /// <param name="alarmButton"></param>
    public void startAlarm(Transform alarmButton) {

        // If not started yet
        if (!hasStarted) {

            Start();
        }

        // Do nothing if there is already an alarm
        if (alarmTextField.text == "FIREALARM") {

            Debug.Log($"{name}: startAlarm(): Alarm is already on.");
            return;
        }

        // Ignore multiple sightings of the same firealarmbutton
        if (settingAlarm || alarmButton.gameObject == lastAlarm) {

            Debug.Log($"{name}: startAlarm(): I already saw {alarmButton.gameObject.name}.");
            return;
        }

        settingAlarm = true;

        lastAlarm = alarmButton.gameObject;

        // Set this alarmButton as new desination
        navComponent.SetDestination(alarmButton.position);
        Debug.Log($"{name}: Going to Feuermelder {alarmButton.gameObject.name}");

        // Switch to walking
        animator.SetBool("panicMode", false);

        // Do animation after 2 seconds
        animator.SetTrigger("pushButton");
        
        StartCoroutine(proceed(alarmButton));
    }

    /// <summary>
    /// Activates the firealarm and returns to the old destination
    /// </summary>
    /// <param name="alarmButton"></param>
    /// <returns></returns>
    private IEnumerator proceed(Transform alarmButton) {

        // I was forced to do this due to a problem that the avatar sometimes just proceeds to his old
        // destination even though I just settet it to the alarmButton destination
        // And I also want to wait 2 seconds because the avatar needs some time to do the
        // "push" animation.
        yield return new WaitForSeconds(1);
        navComponent.SetDestination(alarmButton.position);
        yield return new WaitForSeconds(1);
        navComponent.SetDestination(alarmButton.position);

        // Then activate the alarm. The alarm is activated when the text contains "FIREALARM"
        // Everytime someone checks if the firealarm is on, he just checks the textField
        alarmTextField.text = "FIREALARM";
        alarmTextField.color = Color.red;
        alarmTextField.fontStyle = FontStyle.Bold;
        logic.turnOnAudio();
        Debug.Log($"{gameObject.name}: Alarm is ON");

        yield return new WaitForSeconds(1);

        // Set the old destination again
        navComponent.SetDestination(currTargetPos);
        Debug.Log($"{gameObject.name}: Running again!");

        // Switch to running
        animator.SetBool("panicMode", true);

        settingAlarm = false;
    }

    private Animator animator;
    private Vector3 currTargetPos;
    private NavMeshAgent navComponent;
    private GameObject[] destinations;
    private bool stopped;
    private SortedList<float, GameObject> sortedDests;
    private Vector3 myPos;
    private GameObject currTarget;
    private GameObject endTarget;
    private Vector3 endTargetPos;
    private bool burned;
    private NavMeshObstacle navMeshObstacle;
    private GameObject lastFire;
    private bool targetSettet;
    private bool hasStarted;
    private GameObject lastAlarm;
    private Text alarmTextField;
    private FireManager logic;
    private bool calling;
    private Transform smartphone;
    private bool settingAlarm;
    private Quaternion oldRotation;
    private Vector3 oldPosition;
    private Text calledTextField;
    private RegionController myRegion;
}
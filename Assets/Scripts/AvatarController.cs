#pragma warning disable 1587
/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        endTarget = GameObject.FindGameObjectWithTag("Rallying Point");
        endTargetPos = endTarget.transform.position;

        // Load targets
        loadTargets4ThisFloor("destFloor");

        // Init Components
        alarmTextField = GameObject.Find("alarmTimer").GetComponent<Text>();
        logic = GameObject.Find("Logic").GetComponent<FireManager>();
        animator = GetComponent<Animator>();
        navComponent = GetComponent<NavMeshAgent>();

        // Speed for running
        navComponent.speed = 4;

        // Init animator
        animator.SetBool("panicMode", true);
        animator.SetBool("closeEnough", false);
        animator.applyRootMotion = false;

        // Choose the closest destination
        chooseFirstClosest();
    }

    /// <summary>
    /// Loads all targets the given tag has to offer on this floor and fills the destinationlist
    /// sorted by distance to the avatar
    /// </summary>
    /// <param name="nameTag"></param>
    private void loadTargets4ThisFloor(string nameTag) {

        // Get a list of destinations on this floor
        destinations = GameObject.FindGameObjectsWithTag(nameTag);

        // Sort by distance
        sortedDests = new SortedList<float, GameObject>();
        foreach (GameObject d in destinations) {

            sortedDests.Add(Vector3.Distance(myPos, d.transform.position), d);
        }
    }

    /// <summary>
    /// Chooses and set the target that lies as closest to the avatar as destination
    /// </summary>
    private void chooseFirstClosest() {

        // Target still not settet
        targetSettet = false;

        // Pick the first. This is the closest because they're sorted by distance
        currTarget = sortedDests.First().Value;
        currTargetPos = currTarget.transform.position;

        // Start setting the destination
        // I am doing navComponent.Resume(); in a coroutine because I had the problem, that the avatar wasn't
        // able to reach the destination when it lies directly behind a corner, this fixed it.
        StartCoroutine(setDestinationAfterSecs(0));
    }

    /// <summary>
    /// Update manages calling the fire department, loading of new targets, reaching the destination
    /// and the picture taking with the smartphone
    /// </summary>
    private void Update() {

        // Only update if necessary
        if (settingAlarm || stopped)
            return;

        // Check if we already ran start(), I had one case where this didn't happen
        if (!hasStarted)
            Start();

        // Only continue when the target is set
        if (!targetSettet)
            return;

        // Position and floor
        getMyStuff();

        // Check if I'm going to call the fire department
        if (!settingAlarm && !calling && Random.Range(0, 1000) == 15)
            callFireDepartment();

        //Check if I'm close enough to the Target, then stop
        if (Vector3.Distance(myPos, currTargetPos) < 1) {

            //If this is the rallying Point or a window, then stay, else move on to the rallying point
            if (Vector3.Distance(myPos, endTargetPos) < 1) {

                animator.SetBool("closeEnough", true);
                navComponent.isStopped = true;
                stopped = true;

                // Wave in case of no way to run
                if (endIsWindow) {

                    animator.SetBool("wave", true);
                } else { // I'm at rallying point

                    animator.applyRootMotion = true;

                    // "Gaffen"
                    transform.LookAt(GameObject.Find("House").transform);

                    // Take a picture with smartphone (not PrefabSportler, he's naked)
                    if (Random.Range(0, 2) == 1 && !gameObject.name.Contains("PrefabSportler")) {

                        // Start the "hand up" animation
                        animator.SetTrigger("holdSmartphone");

                        // Spawn the smartphone into the hand
                        StartCoroutine(phoneAfterSecs(0.5f));
                    }
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
    /// <param name="time"></param>
    /// <returns></returns>
    private IEnumerator phoneAfterSecs(float time) {

        yield return new WaitForSeconds(time);

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

        // If this is no fire that is blocking a staircase then just activate the navMeshObstacle
        // I didn't use a navMeshObstacle for staircase Fires, because other avatars that might have
        // not seen this fire would eventually change their way because of this and this wouldn't be
        // right.
        if (!fire.gameObject.name.Contains("Treppenhaus")) {

            fire.GetComponent<NavMeshObstacle>().enabled = true;
            StartCoroutine(disableObstacle(3, fire));
        }

        // If this is a roomFire, then don't change the staircase
        if (fire.name.StartsWith("Room")) {

            Debug.Log($"{gameObject.name}: {fire.gameObject.name} is a RoomFire");
            return;
        }

        // If we already have the rallying point as destination, then go through the destinations on this floor
        if (currTargetPos == GameObject.FindGameObjectWithTag("Rallying Point").transform.position) {

            // Choose the closest destination
            chooseFirstClosest();

            // If the staircase and the destination have different sides then do nothing, else, switch the staircase
            Debug.Log($"{gameObject.name}: getSide({fire.name}) = {getSide(fire)} && getSide({currTarget.name}) = {getSide(currTarget)}");
            if (getSide(fire) != getSide(currTarget)) {

                return;
            }
        }

        // When we reach this point, we want to have another destination
        // Try to get a successor, if there is no, then go to a window
        if (!setTargetToSuccessor()) {

            gotoWindow(fire);
        }
    }

    /// <summary>
    /// Disables the navMeshObstacle for a given fire after some given seconds
    /// </summary>
    /// <param name="waitSeconds"></param>
    /// <param name="fire"></param>
    /// <returns></returns>
    private IEnumerator disableObstacle(int waitSeconds, Transform fire) {

        yield return new WaitForSeconds(waitSeconds);
        fire.GetComponent<NavMeshObstacle>().enabled = false;
    }

    /// <summary>
    /// Picks the next destination in the sorteddestinationlist, if possible
    /// </summary>
    /// <param></param>
    /// <returns></returns>
    private bool setTargetToSuccessor() {

        // Check how many we have
        int length = sortedDests.Count;
        int index = sortedDests.IndexOfValue(currTarget);
        Debug.Log($"{gameObject.name}: index is {index} and the length is {length}");

        // Return false if there are no successors anymore (length == 1)
        if (length == 1) {

            return false;
        }

        // if there are still some, then next one
        targetSettet = false;
        Debug.Log($"{gameObject.name}: Searching for new Target because {currTarget.name} was not good");

        currTarget = sortedDests.Values[index + 1];

        currTargetPos = currTarget.transform.position;
        StartCoroutine(setDestinationAfterSecs(0.1f));

        // remove old destination, we don't want to pick this anymore
        sortedDests.RemoveAt(index);

        return true;
    }

    /// <summary>
    /// Sets the destination of the navMeshAgent to the currentTarget after a given amount of seconds
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    private IEnumerator setDestinationAfterSecs(float seconds) {

        yield return new WaitForSeconds(seconds);
        navComponent.SetDestination(currTargetPos);
        Debug.Log($"{gameObject.name}: I'm now going to {currTarget.name}");

        if (!targetSettet)
            targetSettet = true;
    }

    /// <summary>
    /// Get the staircase side of a fire
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private string getSide(Transform target) {

        if (target.gameObject.name.Contains("links")) {

            return "links";
        }
        return "rechts";
    }

    /// <summary>
    /// Get the staircase side of a destination
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private string getSide(GameObject target) {

        if (target.name.Contains("Destination1")) {

            return "links";
        }
        return "rechts";
    }

    /// <summary>
    /// Looks for the best window to flee and goes there
    /// </summary>
    /// <param name="fire"></param>
    private void gotoWindow(Transform fire) {

        // Find all windows on this floor	
        loadTargets4ThisFloor("window");

        // choose the closest
        chooseFirstClosest();

        // if the current fire is closer to the window than me, then choose the next window
        foreach (KeyValuePair<float, GameObject> currentWindow in sortedDests) {

            Vector3 windowPos = currentWindow.Value.transform.position;

            // If the fire is further away from the window, then there is nothing to do
            if (Vector3.Distance(fire.position, windowPos) + 2 >= Vector3.Distance(myPos, windowPos)) {

                Debug.Log($"{gameObject.name}: {currTarget.name} is right");
                break;
            }

            // If the fire is closer to the window than I am, then take the next window
            Debug.Log($"{gameObject.name}: {currTarget.name} was not right");
            setTargetToSuccessor();
        }

        endTarget = currTarget;
        endTargetPos = currTargetPos;
        endIsWindow = true;
    }

    /// <summary>
    /// Starts calling the fire department
    /// </summary>
    private void callFireDepartment() {

        smartphone = getLeftSmartphone();

        // He must have a smartphone
        if (smartphone != null) {

            calling = true;

            Debug.Log($"{gameObject.name}: I'm calling th Firedepartment");

            // Stop
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
        logic.called();
        navComponent.isStopped = false;
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

            Debug.Log($"{gameObject.name}: startAlarm(): Alarm is already on.");
            return;
        }

        // Ignore multiple sightings of the same firealarmbutton
        if (settingAlarm || alarmButton.gameObject == lastAlarm) {

            Debug.Log($"{gameObject.name}: startAlarm(): I already saw {alarmButton.gameObject.name}.");
            return;
        }

        lastAlarm = alarmButton.gameObject;

        settingAlarm = true;

        // Set this alarmButton as new desination
        navComponent.SetDestination(alarmButton.position);
        Debug.Log($"{gameObject.name}: Going to Feuermelder {alarmButton.gameObject.name}");
        navComponent.SetDestination(alarmButton.position);

        // Switch to walking
        animator.SetBool("panicMode", false);

        // Do animation after 2 seconds
        animator.SetTrigger("pushButton");

        navComponent.SetDestination(alarmButton.position);
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
    private bool endIsWindow;
    private GameObject lastAlarm;
    private Text alarmTextField;
    private FireManager logic;
    private bool calling;
    private Transform smartphone;
    private bool settingAlarm;
    private Quaternion oldRotation;
    private Vector3 oldPosition;
    private Text calledTextField;
}
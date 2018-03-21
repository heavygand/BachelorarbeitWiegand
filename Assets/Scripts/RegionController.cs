using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// The RegionController organizes everything that happens in this specific region
/// It has a 3D-Text label floating above, to show the current status
/// It registers incoming and outbound avatars and activities with a trigger collider (except the outside region)
/// It hides helper gameobject when the simulation starts
/// It has a firealarm sound and can trigger a panic reaction for all attenders
/// <p>Author: Christian Wiegand</p>
/// <p>Matrikelnummer: 30204300</p>
/// </summary>
public class RegionController : MonoBehaviour {

    private List<ObjectController> activities = new List<ObjectController>();
    private List<ObjectController> disabledActivities = new List<ObjectController>();
    private List<ActivityController> waiters = new List<ActivityController>();
    private List<ActivityController> attenders = new List<ActivityController>();
    private GameLogic master;
    private TextMesh regionText;
    /// <summary>
    /// The list of people, who saw a fire
    /// </summary>
    public List<ActivityController> firePeople { get; } = new List<ActivityController>();

    public TextMesh RegionText {
        get {
            return regionText;
        }
        set {
            regionText = value;
        }
    }

    private bool hasAlarm;
    /// <summary>
    /// Organizes the alarm status. (3D Text and sound and attenderpanic)
    /// </summary>
    public bool HasAlarm {
        get {
            return hasAlarm;
        }
        set {
            hasAlarm = value;

            if (hasAlarm) {

                RegionText.text = "FIREALARM";
                RegionText.color = Color.red;
                RegionText.fontStyle = FontStyle.Bold;
                turnOnAudio();

                foreach (ActivityController attender in attenders) {

                    if (!attender.Panic && !attender.arrivedAtRP) {

                        attender.log4Me($"Calling flee(), because I heared an alarm in {name}.");
                        attender.flee();
                    }
                }
            }
            else {

                RegionText.text = name;
                RegionText.color = Color.white;
                disableAudio();
            }
        }
    }
    /// <summary>
    /// Return the GameLogic
    /// </summary>
    /// <returns>The GameLogic</returns>
    public GameLogic getMaster(){

        return master;
    }
    /// <summary>
    /// Indicates if this region is not publicly available, so when somebody wants to enter, he will have to ring the doorbell before entering
    /// </summary>
    [Tooltip("Indicates if this region is not publicly available, so when somebody wants to enter, he will have to ring the doorbell before entering.")]
    public bool isPrivate;
    /// <summary>
    /// The regions doorbell. Only an essential reference, when this region is private
    /// </summary>
    [Tooltip("Only needed, when this region is private")]
    public ObjectController doorBell;
    /// <summary>
    /// Essential reference: The fire that can break out in this region
    /// </summary>
    [Tooltip("The fire that can break out in this region")]
    public GameObject myFire;
    /// <summary>
    /// Essential reference: The status text to show the current status of this region
    /// </summary>
    [Tooltip("Essential reference: The status text to show the current status of this region")]
    public GameObject statusText;
    /// <summary>
    /// Essential reference: The status text to show the current status of this region
    /// </summary>
    [Tooltip("The status text to show the current status of this region")]
    public List<ObjectController> rallyingPoints;

    /// <summary>
    /// Initializing and checking
    /// </summary>
    void Start () {

        // Set the same name as the parent gameobject, which should be the wrapping gameobject for all region things
        if (name == "Region") {
            name = transform.parent.gameObject.name; 
        }

        // Create statustext for region
        GameObject textGO = Instantiate(statusText);
        textGO.transform.parent = transform;
        textGO.transform.localPosition = new Vector3(0, 14.65f, 0);
        regionText = textGO.GetComponent<TextMesh>();
        regionText.text = name;
        
        // Just to be sure :)
        HasAlarm = false;

        // Get the GameLogic
        master = GameObject.Find("GameLogic").GetComponent<GameLogic>();
        master.register(this);
        
        StartCoroutine(hide(0.5f));
		StartCoroutine(awakeWaiters(0.5f));

        // Find rallying points
        foreach (ObjectController rallyingPoint in transform.parent.gameObject.GetComponentsInChildren<ObjectController>()) {

            if (rallyingPoint.name.StartsWith("Rallying Point")) {

                rallyingPoints.Add(rallyingPoint);
                registerActivity(rallyingPoint);
            }
        }

        // Fire Fighter Point
        registerActivity(GetComponentInChildren<ObjectController>());

        // Checking
        if (isPrivate && doorBell == null) Debug.LogError($"Fehler: {name} ist eine private region, hat aber keine Klingel. Füge ein neues gameobject in die Szene ein, und füge das Klingel und Summer Prefab (normalerweise unter \"Prefabs/Activities/besuchen, klingeln\") darunter ein");
        if (rallyingPoints == null || rallyingPoints.Count == 0) Debug.LogWarning($"Warnung: {name} hat keine Rallying Points. Füge Rallying Points (normalerweise unter Prefabs/Region Stuff) in die Szene ein. Entweder unter dem Sammel-GameObject der Region (wird dann automatisch erkannt), oder weise sie direkt im Inspektor der Region zu.");
        if (myFire == null) Debug.LogWarning($"Warnung: {name} hat kein Feuer zugewiesen bekommen. Füge ein Feuer (normalerweise unter Prefabs/Feuer Stuff) in die Szene ein und weise sie dem Feld im Inspektor zu");
        if (statusText == null) Debug.LogWarning($"Warnung: {name} hat kein Statustext zugewiesen bekommen. Weise das Statustext prefab (normalerweise unter Prefabs/Region Stuff) dem Feld im Inspektor zu");
	}
    /// <summary>
    /// Calls textLookAtCamera() to make sure the statustextlabels are always visible
    /// </summary>
    void Update() {

        textLookAtCamera();
    }
    /// <summary>
    /// Rotates the 3D Text of this region toward the current player camera
    /// </summary>
    private void textLookAtCamera() {

        // Look at the Camera
        Transform textTransform = regionText.gameObject.transform;
        textTransform.LookAt(Camera.main.transform);

        Quaternion wrongTargetRot = textTransform.rotation;
        textTransform.rotation = Quaternion.Euler(
            wrongTargetRot.eulerAngles.x * -1,
            wrongTargetRot.eulerAngles.y + 180,
            wrongTargetRot.eulerAngles.z);
    }
    /// <summary>
    /// After a given time, call awakeWaiters()
    /// </summary>
    /// <param name="waitTime">The time to wait in seconds</param>
    private IEnumerator awakeWaiters(float waitTime) {
		
		yield return new WaitForSeconds(waitTime);
		awakeWaiters();
	}
    /// <summary>
    /// During the initialisation phase, it can happen that some avatars are already registered, but no activities yet. This lead to the case that those avatars still cannot be started. So they have to wait. This method starts them again
    /// </summary>
	private void awakeWaiters() {

		if(waiters.Count == 0) return;

		//Debug.Log($"Regioncontroller: die liste der waiters ist {waiters.Count} lang.");

	    List<ActivityController> waiters2 = new List<ActivityController>(waiters);

	    foreach (ActivityController waiter in waiters2) {

            waiter.log4Me($"I've waited and got startGoing() from the Regioncontroller");
			waiter.prepareGoing();
			waiters.Remove(waiter);
		}
	}
    /// <summary>
    /// Hide helper gamobjects in the scene.
    /// For example the yellow figure representing a sportDestination
    /// </summary>
    /// <param name="seconds">The time to wait in seconds</param>
	private IEnumerator hide(float seconds) {

        yield return new WaitForSeconds(seconds);
        foreach (ObjectController objectController in activities) {

            hide(objectController);
        }
        foreach (ObjectController objectController in disabledActivities) {

            hide(objectController);
        }
    }
    /// <summary>
    /// Hides a single activity from the scene
    /// </summary>
    /// <param name="objectController">The Activity to hide</param>
    private static void hide(ObjectController objectController) {

        MeshRenderer mr = objectController.gameObject.GetComponent<MeshRenderer>();

        if (objectController.isPhysical) return;

        if (mr != null) {
            mr.enabled = false;
        }
        foreach (MeshRenderer chMr in objectController.gameObject.GetComponentsInChildren<MeshRenderer>()) {
            chMr.enabled = false;
        }
	}
    /// <summary>
    /// Registers a new attender in this region
    /// Also is the initial start for avatars when the simulation starts
    /// Also gives panic, when there is an alarm
    /// </summary>
    /// <param name="avatar">The avatar to register here</param>
	public void registerAvatar(ActivityController avatar) {

        if (avatar.getRegion() == this || avatar.vehicle) return;

		attenders.Add(avatar);
        avatar.log4Me($"I am now registered in {name}#Detail10Log");

		avatar.setRegion(this);

        // Check if there is an alarm
        if (hasAlarm && !avatar.Panic && !avatar.arrivedAtRP) {

            avatar.log4Me($"Calling flee(), because I heared an alarm in {name} while registering there.");
            avatar.flee();
        }

        // Start this avatar if we already have an activity and only if he has no activity yet
        if (activities.Count > 0 && avatar.CurrentActivity == null) {

            avatar.log4Me($"I'm starting in {name}#Detail10Log");
			avatar.prepareGoing();
		}
        // Avatar will have to wait, when there still are no activities
        else if(activities.Count == 0) {

            avatar.log4Me($"I'm waiting in {name}, because there still were no activities");
			waiters.Add(avatar);
		}
    }
    /// <summary>
    /// Registers a new activity in this region
    /// </summary>
    /// <param name="activity">The activity to register</param>
    public void registerActivity(ObjectController activity) {
		
		if(activity.isAvatar) activity.getAvatar().log4Me($"My activity {activity.name} is in {name}#Detail10Log");

        activities.Add(activity);
		activity.setRegion(this);
	}
    /// <summary>
    /// Notices when avatars or activities left the region and then unregisters them
    /// </summary>
    /// <param name="other">The collider that left</param>
    private void OnTriggerExit(Collider other) {

		// The Collider has to be the first Collider
		if(other.gameObject.GetComponents<Collider>()[0] != other) return;

        // An attender left
        ActivityController avatar = other.GetComponent<ActivityController>();
        if (avatar != null && avatar.getRegion() == this) unregisterAvatar(avatar);

        // An activity left
        ObjectController activity = other.GetComponent<ObjectController>();
        if (activity != null && activity.getRegion() == this) {

            activity.setRegion(null);
            activities.Remove(activity);
        }
    }
    /// <summary>
    /// Unregisters an attender in this region
    /// </summary>
    /// <param name="avatar">The avatar to unregister here</param>
    public void unregisterAvatar(ActivityController avatar) {

        avatar.log4Me($"I have left region {name}#Detail10Log");

        attenders.Remove(avatar);

        if(avatar.getRegion() == this) avatar.setRegion(null);
    }

    /// <summary>
    /// Notices when avatars or activities enter the region and then registers them
    /// </summary>
    /// <param name="other">The collider that entered</param>
    private void OnTriggerEnter(Collider other) {

		// The Collider has to be the first Collider
		if(other.gameObject.GetComponents<Collider>()[0] != other) return;
        
        // We got an attender
        ActivityController avatar = other.GetComponent<ActivityController>();
        if (avatar != null) {

            avatar.log4Me($"I have entered {name}#Detail10Log");
            registerAvatar(avatar);
        }

        // We got an activity
        ObjectController activity = other.GetComponent<ObjectController>();

		if (activity != null) {

			if (activity.isActiveAndEnabled && !activities.Contains(activity)) {

				registerActivity(activity);
			}
			if (!activity.isActiveAndEnabled)

				disabledActivities.Add(activity); 
		}

        // Only if we already have at least one activity and one attender, we can let the waiters start
        if (activities.Count > 0 && attenders.Count > 0) {

	        awakeWaiters();
        }
    }
    /// <summary>
    /// Returns the list of activities in this region
    /// </summary>
    /// <returns>the list of activities in this region</returns>
    public List<ObjectController> getActivities() {

        return activities;
    }
    /// <summary>
    /// Return a list of regionattenders, that are available for an activity
    /// Is usually used for groupactivities like konferenzDestination
    /// The checking is done by matching the priorities of the current activities of the attenders with the priority of the activity to check.
    /// </summary>
    /// <param name="asker">The avatar that asked, to make sure he is not delivered to himself</param>
    /// <param name="newActivity">The activity to check for</param>
    /// <returns>A list of regionattenders, that are available for an activity</returns>
	public List<ActivityController> getTheAvailableOthersFor(ActivityController asker, ObjectController newActivity) {

        asker.log4Me($"{name} has {attenders.Count} inhabitants");

        // For iteration
        List <ActivityController> theOthers = new List<ActivityController>(attenders);
        theOthers.Remove(asker);

        asker.log4Me($"{name} has {theOthers.Count} participants for {newActivity.name}");

        // For returning
        List<ActivityController> theOthers2 = new List<ActivityController>(theOthers);

		// Only leave a person in the "others", when the requested new activity is more important for the person
		foreach (ActivityController other in theOthers) {

            if (other.CurrentActivity == null) {

                theOthers2.Remove(other);
                Debug.LogError($"{asker.name}: The CurrentActivity of {other.name} was null!");
                continue;
            }

			if (newActivity.Priority <= other.CurrentActivity.Priority) {

                theOthers2.Remove(other);

				asker.log4Me($"{other.name} in {name} could not participate in {newActivity.name} because he is doing something more important: {other.CurrentActivity.name}");
            }
		}

		return theOthers2;
	}
    /// <summary>
    /// Returns the list of attender avatars in this region
    /// </summary>
    /// <returns>the list of attender avatars in this region</returns>
    public List<ActivityController> getAttenders() {

        return attenders;
    }
    /// <summary>
    /// Returns a random rallying point from the list of rallying points in this region
    /// </summary>
    /// <returns>a random rallying point</returns>
    public ObjectController getRallyingPoint() {

        return rallyingPoints[Random.Range(0, rallyingPoints.Count)];
    }

    /// <summary>
    /// Turns the alarmsound on
    /// </summary>
    public void turnOnAudio() {

        GetComponent<AudioSource>().Play();
    }

    /// <summary>
    /// Turns the alarmsound off
    /// </summary>
    public void disableAudio() {

        GetComponent<AudioSource>().Stop();
    }
    /// <summary>
    /// Returns the destination point for the firefighter truck
    /// This will also be the place where the first person controller will spawn afterwards
    /// </summary>
    /// <returns>the destination point for the firefighter truck</returns>
    public ObjectController getFireFighterPoint() {

        return GetComponentInChildren<ObjectController>();
    }
    /// <summary>
    /// Adds an avatar to the list of people, who saw the fire in this region
    /// Is used, for example, for the moment when the first person controller spawns, when the people who saw a fire go to him to talk
    /// </summary>
    /// <param name="fireWitness">The avatar, who saw a fire in this region</param>
    public void add2FirePeople(ActivityController fireWitness) {

        firePeople.Add(fireWitness);
    }
}

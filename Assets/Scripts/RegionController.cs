using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RegionController : MonoBehaviour {

    private List<ObjectController> activities = new List<ObjectController>();
    private List<ObjectController> disabledActivities = new List<ObjectController>();
    private List<ActivityController> waiters = new List<ActivityController>();
    private List<ActivityController> attenders = new List<ActivityController>();
    private GameLogicForActivity master;
    private TextMesh regionText;

    public TextMesh RegionText {
        get {
            return regionText;
        }
        set {
            regionText = value;
        }
    }

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
                getMaster().getFireManager().turnOnAudio();
            }
            else {

                RegionText.text = name;
                RegionText.color = Color.white;
                // the audio disabling is managed from the firemanager
            }
        }
    }

    public GameLogicForActivity getMaster(){

        return master;
    }
    
    [Tooltip("Indicates if this region is not publicly available, so when somebody wants to enter, he will have to ring the doorbell before entering.")]
    public bool isPrivate;

    [Tooltip("Only needed, when this region is private")]
    public ObjectController doorBell;

    [Tooltip("The fire that can break out in this region")]
    public GameObject myFire;

    public GameObject statusText;

    public List<ObjectController> rallyingPoints;

    private bool hasAlarm;

    // Use this for initialization
    void Start () {

        // Create statustext for region
        GameObject textGO = Instantiate(statusText);
        textGO.transform.parent = transform;
        textGO.transform.localPosition = new Vector3(0, 14.65f, 0);
        regionText = textGO.GetComponent<TextMesh>();
        regionText.text = name;
        
        HasAlarm = false;

        master = GameObject.Find("GameLogic").GetComponent<GameLogicForActivity>();
        master.register(this);

        if (isPrivate && doorBell == null) Debug.LogError($"ERROR: {name} is a private region, but has no doorbell!");
        
        StartCoroutine(hide(0.5f));
		StartCoroutine(awakeWaiters(0.5f));
	}

    void Update() {

        textLookAtCamera();
    }

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

    private IEnumerator awakeWaiters(float waitTime) {
		
		yield return new WaitForSeconds(waitTime);
		awakeWaiters();
	}

	private void awakeWaiters() {

		if(waiters.Count == 0) return;

		Debug.Log($"Regioncontroller: die liste der waiters ist {waiters.Count} lang.");

	    List<ActivityController> waiters2 = new List<ActivityController>(waiters);

	    foreach (ActivityController waiter in waiters2) {

            waiter.log4Me($"I've waited and got startGoing() from the Regioncontroller");
			waiter.prepareGoing();
			waiters.Remove(waiter);
		}
	}

	private IEnumerator hide(float seconds) {

        yield return new WaitForSeconds(seconds);
        foreach (ObjectController objectController in activities) {

            hide(objectController);
        }
        foreach (ObjectController objectController in disabledActivities) {

            hide(objectController);
        }
    }

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

	public void registerAvatar(ActivityController avatar) {

        if (avatar.getRegion() == this) return;

        avatar.log4Me($"I am now registered in {name}#Detail10Log");

		attenders.Add(avatar);
		avatar.setRegion(this);

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
        // Do nothing
        else {

        }
    }

    public void registerActivity(ObjectController activity) {
		
		if(activity.isAvatar) activity.getAvatar().log4Me($"My activity {activity.name} is in {name}#Detail10Log");

        activities.Add(activity);
		activity.setRegion(this);
	}

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

    public void unregisterAvatar(ActivityController avatar) {

        avatar.log4Me($"I have left region {name}#Detail10Log");

        attenders.Remove(avatar);

        if(avatar.getRegion() == this) avatar.setRegion(null);
    }


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

        // Only if we already have at least one activity and one attender, we can let them start
        if (activities.Count > 0 && attenders.Count > 0) {

	        awakeWaiters();
        }
    }

    public List<ObjectController> getActivities() {

        return activities;
    }

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

    public List<ActivityController> getAttenders() {

        return attenders;
    }

    public ObjectController getRallyingPoint() {

        return rallyingPoints[Random.Range(0, rallyingPoints.Count)];
    }
}

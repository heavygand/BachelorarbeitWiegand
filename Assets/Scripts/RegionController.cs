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

    private bool logging;

    public GameLogicForActivity getMaster(){

        return master;
    }
    
    [Tooltip("Indicates if this region is not publicly available, so when somebody wants to enter, he will have to ring the doorbell before entering.")]
    public bool isPrivate;

    [Tooltip("Only needed, when this region is private")]
    public ObjectController doorBell;

    // Use this for initialization
    void Start () {

        logging = false;//(name == "the Outside area");

        master = GameObject.Find("GameActivityController").GetComponent<GameLogicForActivity>();
        master.register(this);

        if (isPrivate && doorBell == null) Debug.LogError($"ERROR: {name} is a private region, but has no doorbell!");
        
        StartCoroutine(hide(0.5f));
		StartCoroutine(awakeWaiters(0.5f));
	}

	private IEnumerator awakeWaiters(float waitTime) {
		
		yield return new WaitForSeconds(waitTime);
		awakeWaiters();
	}

	private void awakeWaiters() {

		if(waiters.Count == 0) return;

		if(logging) Debug.Log($"Regioncontroller: die liste der waiters ist {waiters.Count} lang.");
		foreach (ActivityController waiter in waiters) {

			if(logging) Debug.Log($"Regioncontroller: {waiter.gameObject.name} hat gewartet und kriegt startGoing()");
			waiter.startGoing();
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

		if(logging) Debug.Log($"Person {avatar.name} ist in {name}");

		attenders.Add(avatar);
		avatar.setRegion(this);

        // Start this avatar if we already have an activity and only if he has no activity yet
		if (activities.Count > 0 && avatar.CurrentActivity == null) {

			avatar.startGoing();
		}
        else {

			waiters.Add(avatar);
		}
	}

	public void registerActivity(ObjectController activity) {
		
		if(logging) Debug.Log($"Tätigkeit {activity.name} ist in {name}");

        activities.Add(activity);
		activity.setRegion(this);
	}

	private void OnTriggerEnter(Collider other) {

		// The Collider has to be the first Collider
		if(other.gameObject.GetComponents<Collider>()[0] != other) return;
        
        // We got an attender
        ActivityController avatar = other.GetComponent<ActivityController>();
        if (avatar != null) registerAvatar(avatar);

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

		List<ActivityController> theOthers = new List<ActivityController>(attenders);
        List<ActivityController> theOthers2 = new List<ActivityController>(theOthers);
        theOthers.Remove(asker);

		// Only leave a person in the "others", when the requested new activity is more important for the person
		foreach (ActivityController other in theOthers) {

            if (other.CurrentActivity == null) {

                theOthers2.Remove(other);
                Debug.LogError($"The CurrentActivity of {other.name} was null!");
                continue;
            }

			if (newActivity.Priority <= other.CurrentActivity.Priority) {

                theOthers2.Remove(other);

				if (logging) Debug.Log($"{other.name} konnte nicht an {newActivity.name} teilnehmen, weil er etwas wichtigeres macht");
			}
		}

		return theOthers2;
	}
}

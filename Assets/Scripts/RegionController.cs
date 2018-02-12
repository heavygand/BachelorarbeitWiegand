using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RegionController : MonoBehaviour {

    private List<ObjectController> activities = new List<ObjectController>();
    private List<ObjectController> disabledActivities = new List<ObjectController>();
    private List<ActivityController> waiters = new List<ActivityController>();
    private List<ActivityController> attenders = new List<ActivityController>();
	private bool wasAlreadyCalled;

	// Use this for initialization
    void Start () {

        GameLogicForActivity master = GameObject.Find("GameActivityController").GetComponent<GameLogicForActivity>();
        master.register(this);

        StartCoroutine(hide(0.5f));
		StartCoroutine(awakeWaiters(0.5f));
	}

	private IEnumerator awakeWaiters(float waitTime) {
		
		yield return new WaitForSeconds(waitTime);
		awakeWaiters();
	}

	private void awakeWaiters() {

		if(waiters.Count == 0) return;

		Debug.Log($"Regioncontroller: die liste der waiters ist {waiters.Count} lang.");
		foreach (ActivityController waiter in waiters) {

			Debug.Log($"Regioncontroller: {waiter.gameObject.name} hat gewartet und kriegt startGoing()");
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

		Debug.Log($"Person {avatar.name} ist in {name}");

		attenders.Add(avatar);
		avatar.setRegion(this);

		if (activities.Count > 0) {

			avatar.startGoing();
		} else {

			waiters.Add(avatar);
		}
	}

	public void registerActivity(ObjectController activity) {


		Debug.Log($"Tätigkeit {activity.name} ist in {name}");

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
        if (!wasAlreadyCalled && activities.Count > 0 && attenders.Count > 0) {

	        awakeWaiters();
        }

        avatar = null;
        activity = null;
    }

    public List<ObjectController> getActivities() {

        return activities;
    }

    public bool hasActivities() {

        return activities.Count > 0;
    }
}

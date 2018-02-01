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

    private void OnTriggerEnter(Collider other) {
        
        // We got an attender
        ActivityController avatarActivityController = other.GetComponent<ActivityController>();

        if (avatarActivityController != null) {

            attenders.Add(avatarActivityController);
            avatarActivityController.setRegion(this);

            //Debug.Log($"Person {other.name} ist im Hotel");
        }

        // We got an activity
        ObjectController objectActivityController = other.GetComponent<ObjectController>();

        if (objectActivityController != null && objectActivityController.isActiveAndEnabled && !activities.Contains(objectActivityController)) {

            activities.Add(objectActivityController);
            objectActivityController.setRegion(this);

            //Debug.Log($"Tätigkeit {other.name} ist im Hotel bei {other.transform.localPosition}");
        }
        if (objectActivityController != null && !objectActivityController.isActiveAndEnabled) disabledActivities.Add(objectActivityController);

        // Only if we already have at least one activity and one attender, we can let them start
        if (!wasAlreadyCalled && activities.Count > 0 && attenders.Count > 0) {

	        wasAlreadyCalled = true;

			//Debug.Log($"Regioncontroller: wir betreten jetzt die foreach, weil activities.Count > 0 && attenders.Count > 0");

			if (avatarActivityController != null) {

                //Debug.Log($"Regioncontroller: {avatarActivityController.gameObject.name} kriegt startGoing()");
                avatarActivityController.startGoing();
            }
			//Debug.Log($"Regioncontroller: die liste der waiters ist {waiters.Count} lang.");
			foreach (ActivityController waiter in waiters) {

                //Debug.Log($"Regioncontroller: {waiter.gameObject.name} hat gewartet und kriegt startGoing()");
                waiter.startGoing();
			}
        }
        // Else, the attenders will have to wait
        else if(avatarActivityController != null) {

            //Debug.Log($"{avatarActivityController.gameObject.name} musste warten, weil die größe der activities = {activities.Count} war");
            waiters.Add(avatarActivityController);
        }
        avatarActivityController = null;
        objectActivityController = null;
    }

    public List<ObjectController> getActivities() {

        return activities;
    }

    public bool hasActivities() {

        return activities.Count > 0;
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RegionController : MonoBehaviour {

    private List<ObjectController> activities = new List<ObjectController>();
    private List<ObjectController> disabledActivities = new List<ObjectController>();
    private List<ActivityController> waiters = new List<ActivityController>();
    private List<ActivityController> attenders = new List<ActivityController>();

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

    // Update is called once per frame
	void Update () {

    }

    private void OnTriggerEnter(Collider other) {
        
        // We got an attender
        ActivityController activityController = other.GetComponent<ActivityController>();

        if (activityController != null) {

            attenders.Add(activityController);
            activityController.setRegion(this);

            //Debug.Log($"Person {other.name} ist im Hotel");
        }

        // We got an activity
        ObjectController objectController = other.GetComponent<ObjectController>();

        if (objectController != null && objectController.isActiveAndEnabled && !activities.Contains(objectController)) {

            activities.Add(objectController);
            objectController.setRegion(this);

            //Debug.Log($"Tätigkeit {other.name} ist im Hotel bei {other.transform.localPosition}");
        }
        if (objectController != null && !objectController.isActiveAndEnabled) disabledActivities.Add(objectController);

        // Only if we already have at least one activity and one attender, we can let them start
        if (activities.Count > 0 && attenders.Count > 0) {

            if (activityController != null) {

                activityController.changeActivity();
                //Debug.Log($"{activityController.gameObject.name} konnte anfangen");
            }
            foreach (ActivityController waiter in waiters) {

                //Debug.Log($"{waiter.gameObject.name} hat gewartet und konnte anfangen");
                waiter.changeActivity();
			}
        }
        // Else, the attenders will have to wait
        else if(activityController != null) {

            //Debug.Log($"{activityController.gameObject.name} musste warten");
            waiters.Add(activityController);
        }
        activityController = null;
        objectController = null;
    }

    public List<ObjectController> getActivities() {

        return activities;
    }

    public bool hasActivities() {

        return activities.Count > 0;
    }
}

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// ReSharper disable IteratorNeverReturns

/// <summary>
/// Modyfied by Christian Wiegand
/// </summary>
public class FieldOfView : MonoBehaviour {

    public float viewRadius;
    [Range(0,360)]
    public float viewAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [HideInInspector]
    public List<Transform> visibleTargets = new List<Transform>();

    // ReSharper disable once UnusedMember.Local
    void Start() {
        StartCoroutine ("FindTargetsWithDelay", .2f);
    }


    // ReSharper disable once UnusedMember.Local
    IEnumerator FindTargetsWithDelay(float delay) {
        while (true) {
            yield return new WaitForSeconds (delay);
            FindVisibleTargets ();
        }
    }

    void FindVisibleTargets() {
        visibleTargets.Clear ();
        Collider[] targetsInViewRadius = Physics.OverlapSphere (transform.position, viewRadius, targetMask);

        foreach (Collider t in targetsInViewRadius) {
            Transform target = t.transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle (transform.forward, dirToTarget) < viewAngle / 2) {
                float dstToTarget = Vector3.Distance (transform.position, target.position);

                if (!Physics.Raycast (transform.position, dirToTarget, dstToTarget, obstacleMask) && FindDifference(transform.position.y, target.position.y) < (decimal)1.5) {

                    visibleTargets.Add (target);

                    int currentLayer = (int)Math.Log(targetMask.value, 2);
                    ActivityController avatar = (ActivityController)GetComponent(typeof(ActivityController));

                    if (LayerMask.LayerToName(currentLayer) == "Fires" && !avatar.FireSeen) {

                        avatar.sawFire();
                    }

                    // When this is a firealarm, then go there when...
                    // ...there is no alarm
                    // ...I have panic
                    // ...no other avatar is currently activating this
                    ObjectController fireAlarm = target.gameObject.GetComponent<ObjectController>();
                    if (LayerMask.LayerToName(currentLayer) == "Feuermelder") {

                        //avatar.log4Me("I have seen a firealarm");

                        if (avatar.myRegion != null
                            && !avatar.myRegion.HasAlarm
                            && avatar.Panic
                            && fireAlarm.CurrentUser == null) {

                            avatar.log4Me("Starting alarm...");
                            Debug.Log($"{avatar.name} hat den Alarm in {avatar.myRegion.name} gestartet.");
                            avatar.Panic = false;
                            avatar.activatedAlarm = true;

                            fireAlarm.discription = avatar.LastActivity != null ? avatar.LastActivity.discription : avatar.CurrentActivity.discription;
                            avatar.interruptWith(fireAlarm);
                        }/* else {

                            if (avatar.myRegion == null) {

                                avatar.log4Me("Not starting the alarm because my region was null");
                            }
                            if (avatar.myRegion.HasAlarm) {

                                avatar.log4Me("Not starting the alarm because my region already has alarm");
                            }
                            if (!avatar.Panic) {

                                avatar.log4Me("Not starting the alarm because I have no panic");
                            }
                            if (fireAlarm.CurrentUser != null) {

                                avatar.log4Me($"Not starting the alarm because it already has a user: {fireAlarm.CurrentUser.name}");
                            }
                        }*/
                    }
                }
            }
        }
    }

    public decimal FindDifference(decimal nr1, decimal nr2){

        return Math.Abs(nr1 - nr2);
    }

    public decimal FindDifference(float nr1, float nr2) {

        return FindDifference((decimal) nr1, (decimal) nr2);
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal) {
        if (!angleIsGlobal) {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),0,Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}

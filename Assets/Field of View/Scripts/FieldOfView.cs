#pragma warning disable 1587
/// <summary>
/// Modyfied by Christian Wiegand
/// </summary>

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// ReSharper disable IteratorNeverReturns

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
                    ActivityController script = (ActivityController)GetComponent(typeof(ActivityController));

                    if (LayerMask.LayerToName(currentLayer) == "Fires" && !script.FireSeen) {

                        script.sawFire();
                    }

                    // When this is a firealarm, then go there when...
                    // ...there is no alarm
                    // ...I have panic
                    // ...no other avatar is currently activating this
                    ObjectController fireAlarm = target.gameObject.GetComponent<ObjectController>();
                    if (LayerMask.LayerToName(currentLayer) == "Feuermelder" && script.myRegion != null && !script.myRegion.HasAlarm && script.Panic && fireAlarm.CurrentUser == null) {

                        script.log4Me("Firealarm detected, starting alarm...");
                        script.Panic = false;
                        script.interruptWith(fireAlarm);
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

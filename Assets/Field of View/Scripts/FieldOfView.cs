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
                    AvatarController script = (AvatarController)GetComponent(typeof(AvatarController));

                    if (LayerMask.LayerToName(currentLayer) == "Fires") {

                        // Find this avatar and his controller
                        if (!script.enabled) {

                            ActivityController script2 = (ActivityController)GetComponent(typeof(ActivityController));
                            Debug.Log($"{gameObject.name}: Fire detected, script is OFF, activity off, avatar on");
                            script2.deactivateMe(target);
                        }

                        if (script.enabled) {

                            Debug.Log($"{gameObject.name}: Fire detected, script is ON, burning...");
                            script.burn(target); 
                        }
                    }

                    //Debug.Log($"Script.enabled == {script.enabled} && LayerMask.LayerToName(currentLayer) == {LayerMask.LayerToName(currentLayer)}");
                    if (script.enabled && LayerMask.LayerToName(currentLayer) == "Feuermelder") {

                        Debug.Log($"{gameObject.name}: Feuermelder detected, starting alarm...");
                        script.startAlarm(target);
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

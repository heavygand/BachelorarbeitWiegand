#pragma warning disable 1587
/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using UnityEngine;

public class ObjectController : MonoBehaviour {

    private RegionController myRegion;

    public enum Activities {
        sitDown,
        layDown,
        stehen,
        sport
    }

    [Tooltip("The general animation to fulfill here")]
    public Activities activity;

    [Tooltip("The place relative to the Object, where the user has to go")]
    public Vector3 workPlace;

    public Vector3 WorkPlace {
        get {
            return rotateVector(workPlace);
        }
    }

    public Vector3 MoveVector {
        get {
            return rotateVector(moveVector);
        }
    }

    [Tooltip("Rotation relative to the Object")]
    public int turnAngle;

    [Tooltip("The time the user will do this activity")]
    public int time;

    [Tooltip("A direction to slide to, when arrived (seen from the work place)")]
    public Vector3 moveVector;

    [Tooltip("Indicates if this should be visible ingame")]
    public bool isPhysical;

    [Tooltip("Indicates if the user has to be able to \"go through\" things to use this")]
    public bool makeGhost;

    private Vector3 rotateVector(Vector3 unturnedVector) {

        return transform.localRotation * unturnedVector;
    }

    // Use this for initialization
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {

    }

    public void setRegion(RegionController rc) {

        myRegion = rc;
    }

    private void OnTriggerEnter(Collider other) {

        ActivityController user = other.GetComponent<ActivityController>();

        // Check if the intruder is a user, check if he has me as destination, then let him stop
        if (user != null && user.currentActivity == gameObject) user.stop();
    }
}
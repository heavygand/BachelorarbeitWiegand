#pragma warning disable 1587
/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using UnityEngine;

public class ObjectController : MonoBehaviour {

    private RegionController myRegion;

	void Start() {

		if (time < 1) time = 100;
		
		workPlace = getFootOfFirstCollider();
	}

	public Vector3 getFootOfFirstCollider() {
		Bounds bounds = GetComponents<Collider>()[0].bounds;
		Vector3 center = new Vector3(bounds.center.x, bounds.center.y - bounds.size.y/2, bounds.center.z);
		return center;
	}

	public enum Activities {
        sitDown,
        layDown,
        stand,
        sport,
		rummage
	}

    [Tooltip("The general animation to fulfill here")]
    public Activities activity;
	
    private Vector3 workPlace;

    public Vector3 MoveVector {
        get {
            return rotateVector(moveVector);
        }
    }

	public Vector3 WorkPlace {
		get {
			return workPlace;
		}
	}

	[Tooltip("Rotation relative to the Object")]
    public int turnAngle;

    [Tooltip("The time in seconds to use this activity")]
    public int time;

    [Tooltip("A direction to slide to, when arrived (seen from the center of the stopper-collider)")]
    public Vector3 moveVector;

    [Tooltip("Indicates if this should be visible ingame")]
    public bool isPhysical;

    [Tooltip("Indicates if the user has to be able to \"go through\" things to use this")]
    public bool makeGhost;

    private Vector3 rotateVector(Vector3 unturnedVector) {

        return transform.localRotation * unturnedVector;
    }

    public void setRegion(RegionController rc) {

        myRegion = rc;
    }
}
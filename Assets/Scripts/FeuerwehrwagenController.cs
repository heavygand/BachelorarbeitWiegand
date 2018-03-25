using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Organizes the lights and the siren of the Firefighter Truck
/// 
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>
public class FeuerwehrwagenController : MonoBehaviour {

    public GameObject licht1;
    public GameObject licht2;
    public GameObject spawnPoint;

    public int drehgeschwindigkeit;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
        if(licht1 != null) {

            rotate(licht1);
        }
        if (licht2 != null) {

            rotate(licht2);
        }
    }

    /// <summary>
    /// Rotates the blue lights
    /// </summary>
    /// <param name="licht">Wich light?</param>
    private void rotate(GameObject licht) {

        Quaternion wrongTargetRot = licht.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(
            wrongTargetRot.eulerAngles.x,
            wrongTargetRot.eulerAngles.y + drehgeschwindigkeit,
            wrongTargetRot.eulerAngles.z);

        licht.transform.rotation = Quaternion.Slerp(licht.transform.rotation, targetRot, 0.15f);
    }

    /// <summary>
    /// Mutes the Siren
    /// </summary>
    public void muteSiren() {

        GetComponent<AudioSource>().Stop();
    }
}

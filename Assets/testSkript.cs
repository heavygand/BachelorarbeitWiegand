﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testSkript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter(Collider other) {

        Debug.Log("Etwas kam rein");
    }
    private void OnTriggerStay(Collider other) {

        Debug.Log("Etwas ist drin");
    }
    private void OnTriggerExit(Collider other) {

        Debug.Log("Etwas ging raus");
    }
}
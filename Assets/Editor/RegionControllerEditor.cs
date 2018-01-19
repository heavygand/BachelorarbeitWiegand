using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (RegionController))]
public class RegionControllerEditor : Editor {

	void OnSceneGUI() {

        RegionController region = (RegionController)target;

	    // ToDo: Wenn hier nichts mehr kommt, dann die Klasse löschen
    }
}

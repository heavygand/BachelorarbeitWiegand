using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (ActivityController))]
public class ActivityControllerEditor : Editor {

	void OnSceneGUI() {

        ActivityController user = (ActivityController)target;

	    string buttonText = $"change Activity for {user.name}";
        Handles.BeginGUI();
        if (GUILayout.Button(buttonText, GUILayout.Width(buttonText.Length*7), GUILayout.Height(30))) {
            Debug.Log("Activitätswechselbutton gedrückt");
            user.requestActivityChange();
        }
        Handles.EndGUI();
		
		Handles.color = Color.red;
		Vector3 leftHandPos = new Vector3();
		Vector3 rightHandPos = new Vector3();
		Transform[] transformsInChildren = user.GetComponentsInChildren<Transform>();
		foreach (Transform transform in transformsInChildren) {

			if (transform.name == "mixamorig:RightHandMiddle1") {

				rightHandPos = transform.position;
			}
			if (transform.name == "mixamorig:LeftHandMiddle1") {

				leftHandPos = transform.position;
			}
		}
		Vector3 fromRightToLeft = leftHandPos - rightHandPos;

		Handles.DrawLine(leftHandPos, rightHandPos);
	}
}

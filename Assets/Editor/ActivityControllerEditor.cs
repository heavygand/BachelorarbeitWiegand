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
            user.interruptFromOutside();
        }
        Handles.EndGUI();

        if (user.Displaced) {

            Handles.DrawLine(user.transform.position, user.CurrentActivity.WorkPlace + user.CurrentActivity.MoveVector);
        }
	}
}

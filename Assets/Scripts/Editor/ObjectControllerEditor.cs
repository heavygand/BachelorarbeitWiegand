using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

/// <summary>
/// Organizes the GUI of the ObjectController in the Sceneview
/// 
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>
[CustomEditor (typeof (ObjectController))]
[CanEditMultipleObjects]
public class ObjectControllerEditor : Editor {

    private ObjectController oc;

    /// <summary>
    /// Draw the work- and moveplace
    /// </summary>
    void OnSceneGUI() {

        oc = (ObjectController)target;

        if (oc.noTurning) {

            oc.turnAngle = 0;
            oc.lookAtNext = false;
        }

        Handles.color = Color.white;
		
	    Vector3 center = oc.WorkPlace;
		
        // Draw the workplace
		drawPlace(center, "Work Place");

        // Draw the move place if needed
        if (oc.moveVector != Vector3.zero) drawPlace(center + oc.MoveVector, "Move Place");
		
	}

    /// <summary>
    /// Draws a general place
    /// </summary>
    /// <param name="place">The point in space</param>
    /// <param name="text">The text to show</param>
    private void drawPlace(Vector3 place, string text) {

        Handles.DrawSolidDisc(place, Vector3.up, 0.025f);
        Handles.DrawLine(place, place + Vector3.up);

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        Handles.Label(place + Vector3.up, " " + text, style);
    }
}

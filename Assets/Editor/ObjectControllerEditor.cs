﻿using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (ObjectController))]
public class ObjectControllerEditor : Editor {

    private ObjectController oc;


    void OnSceneGUI() {

        oc = (ObjectController)target;
        
		Handles.color = Color.white;

        drawPlace(oc.transform.position + oc.WorkPlace, "Work Place");

        if (oc.moveVector != Vector3.zero) drawPlace(oc.transform.position + oc.WorkPlace + oc.MoveVector, "Move Place");
    }

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

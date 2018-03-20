﻿using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (RegionController))]
public class RegionControllerEditor : Editor {

    private RegionController region;

    void OnSceneGUI() {

        region = (RegionController)target;

        if (region.isPrivate && region.doorBell == null)
            Debug.LogWarning($"Warning: {region.name} is a private region, but has no doorbell, this will lead to an error on play");

        Handles.BeginGUI();
        string buttonText = "create fire";
	    if (GUILayout.Button(buttonText, GUILayout.Width(buttonText.Length * 7), GUILayout.Height(30))) {

            region.getMaster().activateFire(region);
        }
        buttonText = "create alarm";
        if (GUILayout.Button(buttonText, GUILayout.Width(buttonText.Length * 7), GUILayout.Height(30))) {

            region.HasAlarm = true;
        }
        
        GUI.Box(new Rect(0, 90, 400, 700), $"{region.name} Debug Window\n{getData()}", ActivityControllerEditor.GetDebugStyle());

        Handles.EndGUI();
    }

    private string getData() {

        string output = $"{region.getAttenders().Count} Inhabitants";

        return output;
    }
}

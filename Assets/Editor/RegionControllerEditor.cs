using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (RegionController))]
public class RegionControllerEditor : Editor {

	void OnSceneGUI() {

        RegionController region = (RegionController)target;

        if (region.isPrivate && region.doorBell == null)
            Debug.LogWarning($"Warning: {region.name} is a private region, but has no doorbell, this will lead to an error on play");
    }
}

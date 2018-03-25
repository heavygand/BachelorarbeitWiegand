using System;
using UnityEngine;
using System.Collections;
using Fungus;
using UnityEditor;

[CustomEditor (typeof (GameLogic))]
public class GameLogicEditor : Editor {

	void OnSceneGUI() {

        GameLogic game = (GameLogic)target;
    }
}

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor (typeof (ActivityController))]
public class ActivityControllerEditor : Editor {

    ActivityController user;
    
    string log;

    void OnSceneGUI() {

        user = (ActivityController)target;

        string buttonText = $"change Activity for {user.name}";
        Handles.BeginGUI();
        if (GUILayout.Button(buttonText, GUILayout.Width(buttonText.Length * 7), GUILayout.Height(30))) {
            Debug.Log("Activitätswechselbutton gedrückt");
            user.interruptFromOutside();
        }

        if (user.showDebugWindow) {

            GUI.Box(new Rect(0, 60, 800, 1400), $"{user.name} Debug Window\n{readLog()}", GetGuiStyle()); 
        }

        Handles.EndGUI();
    }

    private GUIStyle GetGuiStyle() {

        GUIStyle style = new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.UpperLeft,
            normal = {
                background = MakeTex(2, 2, Color.black),
                textColor = Color.green
            }
        };
        return style;
    }

    private string readLog() {

        string output = null;
        List<string> userLog = user.log;

        if (userLog == null) {

            output = "No entries";
        }
        else {

            foreach (string line in userLog) {

                string time = substringBefore(line, "*");
                string place = substringBefore(substringAfter(line, "*"), "#");
                string message = substringAfter(line, "#");
                
                bool detail10LogIsOk = !line.EndsWith("#Detail10Log") || user.detailLog;

                // Check if line is ok to show
                if (detail10LogIsOk) {

                    output += $"\n{substringBefore(time, ".")}{(user.showPlace?" "+place:"")} {substringBefore(message, "#Detail10Log")}";
                }
            }
        }
        return output;
    }

    private Texture2D MakeTex(int width, int height, Color col) {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

	private static string substringBefore(string line, string beforeWhat) {

        if (!line.Contains(beforeWhat)) return line;

        return line.Substring(0, line.IndexOf(beforeWhat));
    }

    private static string substringAfter(string line, string afterWhat) {

        return line.Substring(line.IndexOf(afterWhat) + 1);
    }
}

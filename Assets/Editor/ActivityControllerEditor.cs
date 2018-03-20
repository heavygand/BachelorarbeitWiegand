using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.AI;

[CustomEditor (typeof (ActivityController))]
public class ActivityControllerEditor : Editor {

    ActivityController user;
    
    string log;
    private string lastMessage;
    private string secondToLastMessage;
    private Vector3 pos;
    private string region;
    private string lastNotDetailMessage;

    void OnSceneGUI() {

        user = (ActivityController)target;

        string buttonText = $"Set panic and interrupt {user.name}";
        Handles.BeginGUI();

        /*
         * 
         * ACTIVITY CHANGE BUTTON
         * 
         */
        if (GUILayout.Button(buttonText, GUILayout.Width(buttonText.Length * 7), GUILayout.Height(30))) {
            
            user.flee();
        }

        /*
         * 
         * DEBUG WINDOW
         * 
         */
        if (user.showDebugWindow) {

            GUI.Box(new Rect(0, 60, 800, 1400), $"{user.name} Debug Window\n{readLog()}", GetDebugStyle()); 
        }

        Handles.EndGUI();

        /*
         * 
         * LABEL ABOVE HEAD
         * 
         */
        organizeLabelAboveHead();
    }

    private void organizeLabelAboveHead() {

        pos = user.transform.position;

        region = "null";
        if (user.getRegion() != null) region = $"{user.getRegion().name}";

        if (user.log != null && (user.thinking || user.Doing || user.Going)) {

            string distance = "";
            if (user.Going) {

                Handles.color = Color.red;
                NavMeshAgent navAgent = user.getNavMeshAgent();
                Vector3 dest = navAgent.destination;
                Handles.DrawLine(pos, dest);
                distance += $", distance: {truncate(Vector3.Distance(pos, dest), 1)}";
            }

            for (int i = user.log.Count - 1; i >= 0 ; i--) {

                string currentLog = user.log[i];

                if (!isDetail10Log(currentLog)) {

                    drawLabel(substringAfter(currentLog, "#")+distance);
                    break;
                }
            }
        }
        else if (user.isPlayer) {

            drawLabel($"PLAYER is playing");
        }
        else {

            drawLabel($"UNKNOWN STATUS");
        }
    }

    private void drawLabel(string content) {

        Handles.Label(pos + Vector3.up * 2, $"{(user.Panic?"PANIC! ":"")}In {region}: {content}", GetDebugStyle());
    }

    public static float truncate(float value, int digits) {

        double mult = Math.Pow(10.0, digits);
        double result = Math.Truncate(mult * value) / mult;
        return (float)result;
    }

    public static GUIStyle GetDebugStyle() {

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

        for (int i = userLog.Count-1; i >= 0; i--) {

            string line = userLog[i];

            string time = substringBefore(line, "*");
            string place = substringBefore(substringAfter(line, "*"), "#");
            string message = substringAfter(line, "#");
                
            bool detail10LogIsOk = !isDetail10Log(line) || user.detailLog;

            // Check if line is ok to show
            if (message != secondToLastMessage && message != lastMessage && detail10LogIsOk) {

                output += $"\n{substringBefore(time, ".")}{(user.showPlace?" "+place:"")} {substringBefore(message, "#Detail10Log")}";
            }

            secondToLastMessage = lastMessage;
            lastMessage = message;
        }

        return output;
    }

    private static bool isDetail10Log(string line) {

        return line.EndsWith("#Detail10Log");
    }

    private static Texture2D MakeTex(int width, int height, Color col) {
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

    public static string substringAfter(string line, string afterWhat) {

        return line.Substring(line.IndexOf(afterWhat) + afterWhat.Length);
    }
}

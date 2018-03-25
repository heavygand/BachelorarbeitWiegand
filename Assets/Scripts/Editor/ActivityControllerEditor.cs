using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.AI;

/// <summary>
/// Organizes the GUI of the ActivityController in the Sceneview
/// 
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>
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

            user.log4Me($"Calling flee(), because the button was pressed");
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

    /// <summary>
    /// Organizes the floating status label above of the head of the avatar
    /// </summary>
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

    /// <summary>
    /// Draws the label for the floating status label
    /// </summary>
    /// <param name="content">The main part of the floating label to be shown</param>
    private void drawLabel(string content) {

        Handles.Label(pos + Vector3.up * 2, $"{(user.Panic?"PANIC! ":"")}In {region}: {content}", GetDebugStyle());
    }

    /// <summary>
    /// Cuts off unnecessary digits of the distance to the target
    /// </summary>
    /// <param name="value">The value to truncate</param>
    /// <param name="digits">The amount of digits to leave</param>
    /// <returns>The truncated value</returns>
    public static float truncate(float value, int digits) {

        double mult = Math.Pow(10.0, digits);
        double result = Math.Truncate(mult * value) / mult;
        return (float)result;
    }

    /// <summary>
    /// This is the special debug-console style I developed
    /// </summary>
    /// <returns>Christian Wiegands Debug-Console-Style</returns>
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

    /// <summary>
    /// Gets and processes the avatar log for showing in the avatar console
    /// </summary>
    /// <returns>The whole log as one string. Line breaks included</returns>
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

    /// <summary>
    /// Checks if the line is a Detail10Log
    /// The name is Detail10Log, because I originally intended to use more debug-loglevels, but this one was enough...
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private static bool isDetail10Log(string line) {

        return line.EndsWith("#Detail10Log");
    }

    /// <summary>
    /// Creates a new texture
    /// </summary>
    /// <param name="width">The width</param>
    /// <param name="height">The height</param>
    /// <param name="col">The color</param>
    /// <returns>The new texture</returns>
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

    /// <summary>
    /// Delivers the substring before a certain string
    /// </summary>
    /// <param name="line">The stringline to search in</param>
    /// <param name="beforeWhat">The string before wich the text is desired</param>
    /// <returns>The substring before the given string</returns>
    private static string substringBefore(string line, string beforeWhat) {

        if (!line.Contains(beforeWhat)) return line;

        return line.Substring(0, line.IndexOf(beforeWhat));
    }

    /// <summary>
    /// Delivers the substring after a certain string
    /// </summary>
    /// <param name="line">The stringline to search in</param>
    /// <param name="afterWhat">The string after wich the text is desired</param>
    /// <returns>The substring after the given string</returns>
    public static string substringAfter(string line, string afterWhat) {

        return line.Substring(line.IndexOf(afterWhat) + afterWhat.Length);
    }
}

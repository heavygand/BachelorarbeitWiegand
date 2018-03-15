using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.AI;

[CustomEditor (typeof (ActivityController))]
public class ActivityControllerEditor : Editor {

    ActivityController user;
    
    string log;

    void OnSceneGUI() {

        user = (ActivityController)target;

        string buttonText = $"Set panic and interrupt {user.name}";
        Handles.BeginGUI();
        if (GUILayout.Button(buttonText, GUILayout.Width(buttonText.Length * 7), GUILayout.Height(30))) {
            
            user.setPanicAndInterrupt();
        }

        if (user.showDebugWindow) {

            GUI.Box(new Rect(0, 60, 800, 1400), $"{user.name} Debug Window\n{readLog()}", GetDebugStyle()); 
        }

        Handles.EndGUI();

        Vector3 pos = user.transform.position;
        if (user.CurrentActivity!=null && (user.Going || user.getNavMeshAgent() != null && !user.getNavMeshAgent().isStopped)) {

            Handles.color = Color.red;

            NavMeshAgent navAgent = user.getNavMeshAgent();
            Vector3 dest = navAgent.destination;

            Handles.DrawLine(pos, dest);
            
            Handles.Label(pos + Vector3.up * 2, $"MOVING to {user.CurrentActivity.name}{(user.CurrentActivity.isAvatar ? " with " + user.CurrentActivity.getAvatar().name : "")}, distance: {Vector3.Distance(pos, dest)}", GetDebugStyle());
        }
        else {

            Handles.Label(pos + Vector3.up * 2, $"STOPPED", GetDebugStyle());
        }
    }

    private GUIStyle GetDebugStyle() {

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
                
            bool detail10LogIsOk = !line.EndsWith("#Detail10Log") || user.detailLog;

            // Check if line is ok to show
            if (detail10LogIsOk) {

                output += $"\n{substringBefore(time, ".")}{(user.showPlace?" "+place:"")} {substringBefore(message, "#Detail10Log")}";
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

/// <summary>
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Extends the FireManager script
/// </summary>
public static class FireManagerHelper {

    /// <summary>
    /// Actualizes the Textfield that shows the alarm and starts the alarm at the end
    /// </summary>
    /// <param name="fireManager"></param>
    /// <returns></returns>
    internal static IEnumerator startAlarmIn30( this FireManager fireManager) {

        fireManager.alarmStarted = true;
        bool timerStopped = false;

        // Start counting down on the textField
        fireManager.timer.text = "Alarm in: 30";
        for (int i = 29; i >= 0; i--) {

            yield return new WaitForSeconds(1);

            // This happens when someone sets the alarm from a fireAlarm
            if (fireManager.timer.text == "FIREALARM") {

                timerStopped = true;
                break;
            }
            fireManager.timer.text = "Alarm in: " + i.ToString();
        }

        // Set the alarmtext and sound
        if (!timerStopped) {

            fireManager.timer.text = "FIREALARM";
            fireManager.timer.color = Color.red;
            fireManager.timer.fontStyle = FontStyle.Bold;

            fireManager.turnOnAudio();
        }
    }

    /// <summary>
    /// Turns the alarmsound on
    /// </summary>
    /// <param name="fireManager"></param>
    public static void turnOnAudio(this FireManager fireManager) {

        fireManager.GetComponent<AudioSource>().Play();
    }

    /// <summary>
    /// Changes firefighters textField if they were called
    /// </summary>
    /// <param name="fireManager"></param>
    public static void called(this FireManager fireManager) {

        if (fireManager.fireDepartmentCalled)
            return;

        fireManager.fireDepartmentCalled = true;
        Text calledTextField = GameObject.Find("feuerwehrText").GetComponent<Text>();
        calledTextField.color = Color.red;
        calledTextField.fontStyle = FontStyle.Bold;

        // Starts the timer for the firefighters to arrive
        fireManager.StartCoroutine(fireManager.fireFightersIn120(calledTextField));
    }

    /// <summary>
    /// Actualizes the firefightertextfield
    /// </summary>
    /// <param name="fireManager"></param>
    /// <param name="calledTextField"></param>
    /// <returns></returns>
    internal static IEnumerator fireFightersIn120(this FireManager fireManager, Text calledTextField) {

        calledTextField.text = "Firefighters arrive in: 120s";
        for (int i = 120; i >= 0; i--) {

            yield return new WaitForSeconds(1);

            calledTextField.text = "Firefighters arrive in: " + i.ToString() + "s";
        }

        fireManager.drehleiter.SetActive(true);
        calledTextField.text = "Firefighters arrived";
    }
}
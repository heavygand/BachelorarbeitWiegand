/// <summary>
/// Modyfied by Christian Wiegand
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FireManager : MonoBehaviour {

    List<RegionController> fireRegions;
    public GameObject[] floors;
    public Text timer;
    internal static bool alarmStarted;
    internal bool fireDepartmentCalled;
    internal GameObject drehleiter;
    private bool activatable;
    private GameLogicForActivity master;

    // Use this for initialization
    void Start() {
        
        activatable = true;
        fireRegions = new List<RegionController>();
        master = GetComponentInParent<GameLogicForActivity>();
        master.setFireManager(this);
    }

    private void AddFireInHouse() {

        activateFire(GameObject.Find("House").GetComponent<RegionController>());
    }

    void AddRandomFire() {

        RegionController randomRegion = master.getRandomRegionWithOut(fireRegions);

        activateFire(randomRegion);
        
        if (!alarmStarted) {
            StartCoroutine(startAlarmIn30());
        }
    }

    void ClearAllFires() {

        List<RegionController> tempFireRegions = new List<RegionController>(fireRegions);

        foreach (RegionController fireRegion in tempFireRegions) {

            deactivateFire(fireRegion);
        }
    }

    private void deactivateFire(RegionController fireRegion) {

        fireRegion.myFire.SetActive(false);
        fireRegions.Remove(fireRegion);
    }

    void OnGUI() {

        if (GUI.Button(new Rect(10, 10, 200, 50), "Add random fire")) {
            AddRandomFire();
        }
        if (GUI.Button(new Rect(10, 70, 200, 50), "Clear all fires")) {
            ClearAllFires();
        }
        if (GUI.Button(new Rect(10, 130, 200, 50), "Add fire in House")) {
            AddFireInHouse();
        }
        if (GUI.Button(new Rect(10, 400, 200, 50), "Toggle Mouse Look (F)")) {
            toggleMouseLook();
        }
    }

    private void toggleMouseLook() {

        if(!activatable) return;

        activatable = false;

        StartCoroutine(reactivateAfterDelay(0.5f));

        MouseLook mouseLook = GameObject.Find("Spectator Camera").GetComponent<MouseLook>();

        mouseLook.enabled = !mouseLook.isActiveAndEnabled;
    }

    private IEnumerator reactivateAfterDelay(float f) {

        yield return new WaitForSeconds(f);
        activatable = true;
    }

    private void Update() {

        if (Input.GetKey(KeyCode.F)) {

            toggleMouseLook();
        }
    }

    private void activateFire(RegionController randomRegion) {

        randomRegion.myFire.SetActive(true);
        fireRegions.Add(randomRegion);

        Debug.LogWarning($"Simulation: Fire Activated in {randomRegion.name}");
    }
    /// <summary>
    /// Actualizes the Textfield that shows the alarm and starts the alarm at the end
    /// </summary>
    /// <returns></returns>
    private IEnumerator startAlarmIn30() {

        alarmStarted = true;
        bool timerStopped = false;

        // Start counting down on the textField
        timer.text = "Alarm in: 30";
        for (int i = 29; i >= 0; i--) {

            yield return new WaitForSeconds(1);

            // This happens when someone sets the alarm from a fireAlarm
            if (timer.text == "FIREALARM") {

                timerStopped = true;
                break;
            }
            timer.text = "Alarm in: " + i.ToString();
        }

        // Set the alarmtext and sound
        if (!timerStopped) {

            timer.text = "FIREALARM";
            timer.color = Color.red;
            timer.fontStyle = FontStyle.Bold;

            turnOnAudio();
        }
    }

    /// <summary>
    /// Turns the alarmsound on
    /// </summary>
    /// <param name="fireManager"></param>
    public void turnOnAudio() {

        GetComponent<AudioSource>().Play();
    }

    /// <summary>
    /// Changes firefighters textField if they were called
    /// </summary>
    /// <param name="fireManager"></param>
    public void called() {

        if (fireDepartmentCalled) return;

        fireDepartmentCalled = true;
        Text calledTextField = GameObject.Find("feuerwehrText").GetComponent<Text>();
        calledTextField.color = Color.red;
        calledTextField.fontStyle = FontStyle.Bold;
    }
}

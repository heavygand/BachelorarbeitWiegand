using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireManager : MonoBehaviour {

    List<RegionController> fireRegions;
    internal bool fireDepartmentCalled;
    private bool activatable;
    private GameLogicForActivity master;
    private Coroutine alarmstarter;

    // Use this for initialization
    void Start() {
        
        activatable = true;
        fireRegions = new List<RegionController>();
        master = GetComponentInParent<GameLogicForActivity>();
        master.setFireManager(this);
    }

    void AddRandomFire() {

        RegionController randomRegion = master.getRandomRegionWithOut(fireRegions);

        activateFire(randomRegion);
    }

    void ClearAllFires() {

        StopCoroutine(alarmstarter);

        List<RegionController> tempFireRegions = new List<RegionController>(fireRegions);

        foreach (RegionController fireRegion in tempFireRegions) {

            deactivateFire(fireRegion);
        }

        disableAudio();
    }

    private void deactivateFire(RegionController fireRegion) {

        fireRegion.myFire.SetActive(false);
        fireRegion.HasAlarm = false;
        fireRegions.Remove(fireRegion);
    }

    void OnGUI() {

        if (GUI.Button(new Rect(10, 10, 200, 50), "Add random fire (E)")) {
            AddRandomFire();
        }
        if (GUI.Button(new Rect(10, 70, 200, 50), "Clear all fires and alarms (C)")) {
            ClearAllFires();
        }
        if (GUI.Button(new Rect(10, 130, 200, 50), "Toggle Pause (T)")) {
            setPause();
        }
        if (GUI.Button(new Rect(10, 190, 200, 50), "Toggle Mouse Look (F)")) {
            toggleMouseLook();
        }
    }

    private static void setPause() {
        Debug.Break();
        Cursor.visible = true;
    }

    private void toggleMouseLook() {

        if(!activatable) return;

        activatable = false;

        StartCoroutine(reactivateAfterDelay(0.5f));

        MouseLook mouseLook = GameObject.Find("Spectator Camera").GetComponent<MouseLook>();

        mouseLook.enabled = !mouseLook.isActiveAndEnabled;
        Cursor.visible = !mouseLook.isActiveAndEnabled;
    }

    private IEnumerator reactivateAfterDelay(float f) {

        yield return new WaitForSeconds(f);
        activatable = true;
    }

    private void Update() {

        if (Input.GetKey(KeyCode.F)) {

            toggleMouseLook();
        }
        if (Input.GetKey(KeyCode.E)) {

            AddRandomFire();
        }
        if (Input.GetKey(KeyCode.C)) {

            ClearAllFires();
        }
        if (Input.GetKey(KeyCode.T)) {

            setPause();
        }
    }

    private void activateFire(RegionController randomRegion) {

        randomRegion.myFire.SetActive(true);
        fireRegions.Add(randomRegion);

        Debug.Log($"Simulation: Fire Activated in {randomRegion.name}");

        if (!randomRegion.HasAlarm) {

            alarmstarter = StartCoroutine(startAlarmIn30(randomRegion));
        }
    }

    /// <summary>
    /// Actualizes the Textfield that shows the alarm and starts the alarm at the end
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    private IEnumerator startAlarmIn30(RegionController region) {
        
        bool timerStopped = false;

        // Start counting down on the textField
        region.RegionText.text = "Alarm in: 30";
        for (int i = 29; i >= 0; i--) {

            yield return new WaitForSeconds(1);

            // This happens when someone sets the alarm from a fireAlarm
            if (region.HasAlarm) {

                timerStopped = true;
                break;
            }
            region.RegionText.text = "Alarm in: " + i;
        }

        // Set the alarmtext and sound
        if (!timerStopped) {

            region.HasAlarm = true;

            turnOnAudio();
        }
    }

    /// <summary>
    /// Turns the alarmsound on
    /// </summary>
    public void turnOnAudio() {

        GetComponent<AudioSource>().Play();
    }

    /// <summary>
    /// Turns the alarmsound on
    /// </summary>
    public void disableAudio() {

        GetComponent<AudioSource>().Stop();
    }

    /// <summary>
    /// Changes firefighters textField if they were called
    /// </summary>
    public void called(ActivityController caller) {

        if (fireDepartmentCalled) return;

        fireDepartmentCalled = true;
        Debug.LogWarning($"Fire department called, thanks to {caller.name}.");
        //TODO: An dieser Stelle kann code eingebaut werden, der die Feuerwehr ruft
        /*
        Text calledTextField = GameObject.Find("feuerwehrText").GetComponent<Text>();
        calledTextField.color = Color.red;
        calledTextField.fontStyle = FontStyle.Bold;
        */
    }
}

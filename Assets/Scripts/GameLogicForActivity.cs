using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogicForActivity : MonoBehaviour {

    private List<RegionController> regions = new List<RegionController>();

    List<RegionController> fireRegions;
    internal bool fireDepartmentCalled;
    private Coroutine alarmstarter;

    // Update is called once per frame
    void Update () {

        if (Input.GetKeyDown(KeyCode.F)) {

            toggleMouseLook();
        }
        if (Input.GetKeyDown(KeyCode.E)) {

            AddRandomFire();
        }
        if (Input.GetKeyDown(KeyCode.C)) {

            ClearAllFires();
        }
        if (Input.GetKeyDown(KeyCode.T)) {

            setPause();
        }
    }

    public RegionController getRandomRegionWithOut(List<RegionController> withoutRegions) {

        List<RegionController> localRegions = new List<RegionController>(regions);

        foreach (RegionController region in withoutRegions) {

            localRegions.Remove(region); 
        }

        return getRandomRegion(localRegions);
    }

    public RegionController getRandomRegionWithOut(RegionController region) {

        List<RegionController> localRegions = new List<RegionController>(regions);
        localRegions.Remove(region);

        return getRandomRegion(localRegions);
    }

    public RegionController getRandomRegion() {

        return getRandomRegion(regions);
    }

    public RegionController getRandomRegion(List<RegionController> searchRegions) {

        if (searchRegions.Count == 0) {

            //Debug.Log($"{name}: Cannot find a random region in an empty list: returning {regions[0].name}");
            return regions[0];
        }

        return searchRegions[Random.Range(0, searchRegions.Count)];
    }

    public void register(RegionController rc) {
        
        regions.Add(rc);
    }

    public List<RegionController> getRegions() {

        return regions;
    }

    public RegionController getOutside() {

        return GetComponentInChildren<RegionController>();
    }

    // Use this for initialization
    void Start() {

        fireRegions = new List<RegionController>();
    }

    void AddRandomFire() {

        RegionController randomRegion = getRandomRegionWithOut(fireRegions);

        activateFire(randomRegion);
    }

    void ClearAllFires() {

        StopCoroutine(alarmstarter);

        List<RegionController> tempFireRegions = new List<RegionController>(fireRegions);

        foreach (RegionController fireRegion in tempFireRegions) {

            deactivateFire(fireRegion);
        }
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

        MouseLook mouseLook = GameObject.Find("Spectator Camera").GetComponent<MouseLook>();

        mouseLook.enabled = !mouseLook.isActiveAndEnabled;
        Cursor.visible = !mouseLook.isActiveAndEnabled;
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
        }
    }

    /// <summary>
    /// Changes firefighters textField if they were called
    /// </summary>
    public void called(ActivityController caller) {

        if (fireDepartmentCalled)
            return;

        fireDepartmentCalled = true;
        Debug.Log($"Fire department called, thanks to {caller.name}.");


    }
}
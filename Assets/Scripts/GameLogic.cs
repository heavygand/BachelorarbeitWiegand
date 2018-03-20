using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour {

    private List<RegionController> regions = new List<RegionController>();

    List<RegionController> fireRegions;
    internal bool fireDepartmentCalled;
    private Coroutine alarmstarter;

    public RegionController outside;
    public GameObject fireFighters;
    public GameObject spectator;
    public GameObject firstPersonController;

    // Update is called once per frame
    void Update () {

        if (Input.GetKeyDown(KeyCode.F)) {

            toggleMouseLook();
        }
        if (Input.GetKeyDown(KeyCode.U)) {

            AddRandomFire();
        }
        if (Input.GetKeyDown(KeyCode.C)) {

            ClearAllFires();
        }
        if (Input.GetKeyDown(KeyCode.T)) {

            clearAlarms();
        }
        if (Input.GetKeyDown(KeyCode.G)) {

            activateFireFighters(fireRegions.Count<=0?getRandomRegion(): fireRegions[0]);
        }
    }

    void OnGUI() {

        if (GUI.Button(new Rect(10, 10, 200, 50), "Add random fire (U)")) {
            AddRandomFire();
        }
        if (GUI.Button(new Rect(10, 70, 200, 50), "Clear fires (C)")) {
            ClearAllFires();
        }
        if (GUI.Button(new Rect(10, 130, 200, 50), "Clear alarms (T)")) {
            clearAlarms();
        }
        if (GUI.Button(new Rect(10, 190, 200, 50), "Toggle Mouse Look (F)")) {
            toggleMouseLook();
        }
        if (GUI.Button(new Rect(10, 250, 200, 50), "Call Firefighters (G)")) {

            activateFireFighters(fireRegions.Count==0 ? getRandomRegion() : fireRegions[0]);
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

        RegionController randomRegion = searchRegions[Random.Range(0, searchRegions.Count)];

        //Debug.Log($"{randomRegion.name} found randomly");
        return randomRegion;
    }

    public void register(RegionController rc) {
        
        regions.Add(rc);
    }

    public List<RegionController> getRegions() {

        return regions;
    }

    public RegionController getOutside() {

        return outside;
    }

    // Use this for initialization
    void Start() {

        fireRegions = new List<RegionController>();
    }

    void AddRandomFire() {

        activateFire(getRandomRegionWithOut(fireRegions));
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

    private void clearAlarms() {
        
        StopCoroutine(alarmstarter);

        foreach (RegionController region in regions) {
            
            region.HasAlarm = false;
        }
    }

    private void toggleMouseLook() {

        if(spectator == null) return;

        MouseLook mouseLook = spectator.GetComponent<MouseLook>();

        mouseLook.enabled = !mouseLook.isActiveAndEnabled;
        Cursor.visible = !mouseLook.isActiveAndEnabled;
    }

    public void activateFire(RegionController randomRegion) {

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
    /// Firefighters called
    /// </summary>
    public void called(ActivityController caller) {

        if (fireDepartmentCalled) return;

        fireDepartmentCalled = true;
        Debug.Log($"Fire department called, thanks to {caller.name}.");
        
        activateFireFighters(caller.fireRegion);
    }

    private void activateFireFighters(RegionController region) {

        spectator.SetActive(false);
        fireFighters.SetActive(true);
        ActivityController ac = fireFighters.GetComponent<ActivityController>();
        ac.NextActivity = region.getFireFighterPoint();
        ac.prepareGoing();
    }

    public IEnumerator activateFirstPerson() {

        // Disable Firefighter Truck
        FeuerwehrwagenController fireFighterScript = fireFighters.GetComponent<FeuerwehrwagenController>();
        fireFighterScript.muteSiren();

        yield return new WaitForSeconds(1);
        fireFighters.GetComponent<AudioListener>().enabled = false;

        // Activate the fire fighter
        firstPersonController.SetActive(true);
        fireFighters.transform.Find("Camera").GetComponent<Camera>().enabled = false;
        firstPersonController.transform.position = fireFighterScript.spawnPoint.transform.position;
    }
}
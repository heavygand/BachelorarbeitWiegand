using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Core of the Simulation flow.
/// Knows all regions.
/// Organizes the menucontrol.
/// Adds fires, alarms and the current player manifestation in the Simulation.
/// <p>Author: Christian Wiegand</p>
/// <p>Matrikelnummer: 30204300</p>
/// </summary>
public class GameLogic : MonoBehaviour {

    private List<RegionController> regions = new List<RegionController>();

    List<RegionController> fireRegions;
    internal bool fireDepartmentCalled;
    private Coroutine alarmstarter;

    /// <summary>
    /// Essential reference: The outside region
    /// </summary>
    [Tooltip("Essential reference: The outside region")]
    public RegionController outside;

    /// <summary>
    /// Essential reference: The spectator gameobject
    /// </summary>
    [Tooltip("Essential reference: The spectator gameobject")]
    public GameObject spectator;

    /// <summary>
    /// Essential reference: The firefighters truck the player will be driven with, towards the fireregion
    /// </summary>
    [Tooltip("The firefighters truck the player will be driven with, towards the fireregion")]
    public GameObject fireFighters;

    /// <summary>
    /// Essential reference: The first person controller to use when arrived at the fire region
    /// </summary>
    [Tooltip("The first person controller to use when arrived at the fire region")]
    public GameObject firstPersonController;

    /// <summary>
    /// The state of the players manifestation in the simulation
    /// </summary>
    private enum gamestate {
        spectator,
        firefightertruck,
        firstpersoncontroller
    }

    private gamestate GameState;

    /// <summary>
    /// Initializing and checking
    /// </summary>
    void Start() {

        // Initializing
        fireRegions = new List<RegionController>();
        GameState = gamestate.spectator;

        // Checking
        if (outside == null) Debug.LogError($"Fehler: Die Simulation hat keine outside Region, füge eine Region in die Szene ein und weise sie der GameLogic im Inspektor zu");
        if (spectator == null) Debug.LogError($"Fehler: Die Simulation hat keinen spectator, dieser ist normalerweise im gameLogic prefab enthalten");
        if (fireFighters == null) Debug.LogWarning($"Warnung: Die Simulation hat keinen fireFighter truck zugewiesen bekommen, dieser ist normalerweise im gameLogic prefab enthalten");
        if (firstPersonController == null) Debug.LogWarning($"Warnung: Die Simulation hat keinen firstPersonController zugewiesen bekommen, dieser ist normalerweise im gameLogic prefab enthalten");
        if (outside.GetComponent<Collider>() != null && outside.GetComponent<Collider>().isTrigger) Debug.LogWarning($"Warnung: Die \"Outside\" Region sollte keinen Triggercollider haben, da sie quasi überall ist");
        if (name != "GameLogic") Debug.LogError($"Fehler: Die GameLogic muss \"GameLogic\" heißen, weil andere Komponenten in der Szene nach diesem Namen suchen");
    }
    
    /// <summary>
    /// Register Keyboard inputs
    /// </summary>
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
        if (Input.GetKeyDown(KeyCode.V)) {

            clearAlarms();
        }
        if (Input.GetKeyDown(KeyCode.G)) {

            activateFireFighters(fireRegions.Count<=0?getRandomRegion(): fireRegions[0]);
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {

            Debug.Break();
            Application.Quit();
        }
    }

    /// <summary>
    /// Draws the simulation-UI
    /// </summary>
    void OnGUI() {

        if (GUI.Button(new Rect(10, 10, 200, 50), "Add random fire (U)")) {
            AddRandomFire();
        }
        if (GUI.Button(new Rect(10, 70, 200, 50), "Clear fires (C)")) {
            ClearAllFires();
        }
        if (GUI.Button(new Rect(10, 130, 200, 50), "Clear alarms (V)")) {
            clearAlarms();
        }
        if (GUI.Button(new Rect(10, 190, 200, 50), "Toggle Mouse Look (F)")) {
            toggleMouseLook();
        }
        if (GUI.Button(new Rect(10, 250, 200, 50), "Call Firefighters (G)")) {

            activateFireFighters(fireRegions.Count==0 ? getRandomRegion() : fireRegions[0]);
        }
        if (GUI.Button(new Rect(10, 310, 200, 50), "Quit (ESC)")) {

            Debug.Break();
            Application.Quit();
        }


        if (GameState != gamestate.firefightertruck) {

            // Show Nagivation (implemented in "CameraWASD.cs" on the Spectator Camera GameObject)
            int height = 30;
            int space = 10;
            int navY = 370;
            int navX = 10;
            int width = 30;
            int down = height + space;
            int right = width + space;
            GUI.Box(new Rect(navX, navY, width * 3 + space * 2, height), "Navigation");
            int i, j = 1;

            if (GameState == gamestate.spectator) {
                i = 0;
                GUI.Box(new Rect(navX + right * i, navY + down * j, width, height), "Q");
            }

            i = 1;
            GUI.Box(new Rect(navX + right * i, navY + down * j, width, height), "W");

            i = 2;
            GUI.Box(new Rect(navX + right * i, navY + down * j, width, height), "E");

            j = 2;
            i = 0;
            GUI.Box(new Rect(navX + right * i, navY + down * j, width, height), "A");

            i = 1;
            GUI.Box(new Rect(navX + right * i, navY + down * j, width, height), "S");

            i = 2;
            GUI.Box(new Rect(navX + right * i, navY + down * j, width, height), "D"); 
        }
    }

    /// <summary>
    /// Returns a random region, without the given list of regions
    /// </summary>
    /// <param name="withoutRegions">The regions to leave out</param>
    /// <returns>a random Region</returns>
    public RegionController getRandomRegionWithOut(List<RegionController> withoutRegions) {

        List<RegionController> localRegions = new List<RegionController>(regions);

        foreach (RegionController region in withoutRegions) {

            localRegions.Remove(region); 
        }

        return getRandomRegion(localRegions);
    }
    /// <summary>
    /// Returns a random region, without the parameter region
    /// </summary>
    /// <param name="region">The region to leave out</param>
    /// <returns>a random Region</returns>
    public RegionController getRandomRegionWithOut(RegionController region) {

        List<RegionController> localRegions = new List<RegionController>(regions);
        localRegions.Remove(region);

        return getRandomRegion(localRegions);
    }
    /// <summary>
    /// Returns a random region from the internal list of regions, does not deliver a region with fire
    /// </summary>
    /// <returns>a random Region</returns>
    public RegionController getRandomRegion() {

        return getRandomRegion(regions);
    }
    /// <summary>
    /// Returns a random region from the parameter list of regions, does not deliver a region with fire
    /// </summary>
    /// <param name="searchRegions">The list of regions to search in</param>
    /// <returns>a random Region</returns>
    public RegionController getRandomRegion(List<RegionController> searchRegions) {

        // Do not deliver a fireregion
        List<RegionController> tempRegions = new List<RegionController>(searchRegions);
        foreach (RegionController region in tempRegions) {

            if (fireRegions.Contains(region)) {

                searchRegions.Remove(region);
            }
        }

        if (searchRegions.Count == 0) {

            //Debug.Log($"{name}: Cannot find a random region in an empty list: returning {regions[0].name}");
            return regions[0];
        }

        RegionController randomRegion = searchRegions[Random.Range(0, searchRegions.Count)];

        //Debug.Log($"{randomRegion.name} found randomly");
        return randomRegion;
    }
    /// <summary>
    /// Registers a region in the game
    /// </summary>
    /// <param name="rc">The RegionController to register</param>
    public void register(RegionController rc) {
        
        regions.Add(rc);
    }
    /// <summary>
    /// Returns the regionlist
    /// </summary>
    /// <returns>The list of regions</returns>
    public List<RegionController> getRegions() {

        return regions;
    }
    /// <summary>
    /// Returns the outside region
    /// </summary>
    /// <returns>The outside region</returns>
    public RegionController getOutside() {

        return outside;
    }
    /// <summary>
    /// Activates a fire in a random region
    /// </summary>
    void AddRandomFire() {

        activateFire(getRandomRegionWithOut(fireRegions));
    }
    /// <summary>
    /// Clears all fires, that are known in the list of fireregions
    /// </summary>
    void ClearAllFires() {

        List<RegionController> tempFireRegions = new List<RegionController>(fireRegions);

        foreach (RegionController fireRegion in tempFireRegions) {

            deactivateFire(fireRegion);
        }
    }
    /// <summary>
    /// Deactivates a fire for a region and removes it from the fireregions list
    /// </summary>
    /// <param name="fireRegion">The region where to deactivate the fire</param>
    private void deactivateFire(RegionController fireRegion) {

        fireRegion.myFire.SetActive(false);
        fireRegions.Remove(fireRegion);
    }
    /// <summary>
    /// Deactivates all alarms
    /// </summary>
    private void clearAlarms() {
        
        StopCoroutine(alarmstarter);

        foreach (RegionController region in regions) {
            
            region.HasAlarm = false;
        }
    }
    /// <summary>
    /// There are two states for the first person controller. This toggles between them.
    /// First:  Free lookaround with deactivated mouse (for moving and conversation starting).
    /// Second: Frozen lookaround with activated mouse (for talking).
    /// This ensures, that the player can click on the dialogmenu buttons.
    /// </summary>
    private void toggleMouseLook() {

        if(spectator == null) return;

        MouseLook mouseLook = spectator.GetComponent<MouseLook>();

        mouseLook.enabled = !mouseLook.isActiveAndEnabled;
        Cursor.visible = !mouseLook.isActiveAndEnabled;
    }
    /// <summary>
    /// Activates a fire in a given region and adds the region to the list of fireregions
    /// Also starts the "smokealarm", wich is a 30 seconds timer coroutine
    /// </summary>
    /// <param name="region">The region where to activate the fire</param>
    public void activateFire(RegionController region) {

        region.myFire.SetActive(true);
        fireRegions.Add(region);

        Debug.Log($"Simulation: Fire Activated in {region.name}");

        if (!region.HasAlarm) {

            alarmstarter = StartCoroutine(startAlarmIn30(region));
        }
    }

    /// <summary>
    /// Actualizes the regions floating statustext that shows the alarm and starts the alarm at the end
    /// </summary>
    /// <param name="region">The region where to start the alarmcounter</param>
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
    /// Informs that the firefighters are called and invokes the firefighteractivation
    /// The caller is needed to get the region where the fire is
    /// </summary>
    /// <param name="caller">The avatar that has called</param>
    public void called(ActivityController caller) {

        if (fireDepartmentCalled) return;

        fireDepartmentCalled = true;
        Debug.Log($"Fire department called, thanks to {caller.name}.");
        
        activateFireFighters(caller.fireRegion);
    }
    /// <summary>
    /// Activates the firefighters.
    /// Switches the players manifestation from spectator to the firefighter truck
    /// This will spawn a firefighter truck in the simulation, and sets the firefighterpoint as drive-destination
    /// </summary>
    /// <param name="region">The region where the fire is, and to get the destination point from for the truck</param>
    private void activateFireFighters(RegionController region) {

        GameState = gamestate.firefightertruck;
        spectator.SetActive(false);
        fireFighters.SetActive(true);
        ActivityController ac = fireFighters.GetComponent<ActivityController>();
        ac.NextActivity = region.getFireFighterPoint();
        ac.prepareGoing();
    }
    /// <summary>
    /// <p>Switches the players manifestation from firefighter truck to first person controller</p>
    /// The parameter region is needed to get the list of people, that saw the fire
    /// Firstly, the truck will be disabled, but not deactivated, wich means that he will stay visible, but can't move anymore
    /// After that, the player is spawned as a first person controller
    /// Then, the people wich saw a fire are starting to move towards the player to tell him where the fire is
    /// </summary>
    /// <param name="region">The region to get the list of persons who saw fire from</param>
    public IEnumerator activateFirstPerson(RegionController region) {

        GameState = gamestate.firstpersoncontroller;

        // Disable Firefighter Truck, but let him be visible
        FeuerwehrwagenController fireFighterScript = fireFighters.GetComponent<FeuerwehrwagenController>();
        fireFighterScript.muteSiren();
        fireFighters.GetComponent<NavMeshAgent>().enabled = false;
        fireFighters.GetComponent<NavMeshObstacle>().enabled = true;

        yield return new WaitForSeconds(1);
        fireFighters.GetComponent<AudioListener>().enabled = false;

        // Activate the fire fighter (FPC)
        firstPersonController.SetActive(true);
        fireFighters.transform.Find("Camera").GetComponent<Camera>().enabled = false;
        firstPersonController.transform.position = fireFighterScript.spawnPoint.transform.position;

        // Wait for everything to start
        ActivityController activityController = firstPersonController.GetComponent<ActivityController>();
        if (!activityController.started) activityController.Start();

        ObjectController talkDestination = activityController.myActivity;
        if (!talkDestination.started) talkDestination.Start();
        
        // LET THE PEOPLE COME
        foreach (ActivityController fireWitness in region.firePeople) {

            fireWitness.log4Me($"The Gamelogic gave me panic");
            fireWitness.Panic = true;
            fireWitness.log4Me($"The Gamelogic is trying to interrupt me with {talkDestination.name}");
            fireWitness.interruptWith(talkDestination);
        }
    }
}
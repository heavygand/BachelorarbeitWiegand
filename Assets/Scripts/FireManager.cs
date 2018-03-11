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
    internal bool alarmStarted;
    internal bool fireDepartmentCalled;
    internal GameObject drehleiter;
    private bool activatable;

    // Use this for initialization
    void Start() {
        
        activatable = true;
        fireRegions = new List<RegionController>();
    }

    void AddRandomFire() {

        GameLogicForActivity master = GetComponentInParent<GameLogicForActivity>();
        RegionController randomRegion = master.getRandomRegionWithOut(fireRegions);

        randomRegion.myFire.SetActive(true);
        fireRegions.Add(randomRegion);

        Debug.LogError($"Simulation: Fire Activated in {randomRegion.name}");
        /*
        if (!alarmStarted) {
            StartCoroutine(this.startAlarmIn30());
        }*/
    }

    void ClearAllFires() {

        List<RegionController> tempFireRegions = new List<RegionController>(fireRegions);

        foreach (RegionController fireRegion in tempFireRegions) {

            fireRegion.myFire.SetActive(false);
            fireRegions.Remove(fireRegion);
        }
    }

    void OnGUI() {

        if (GUI.Button(new Rect(10, 10, 200, 50), "Add random fire")) {
            AddRandomFire();
        }
        if (GUI.Button(new Rect(10, 70, 200, 50), "Clear all fires")) {
            ClearAllFires();
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
}

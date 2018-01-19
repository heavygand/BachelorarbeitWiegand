/// <summary>
/// Modyfied by Christian Wiegand
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FireManager : MonoBehaviour {

    Feuer[] fires;
    public GameObject[] floors;
    public Text timer;
    internal bool alarmStarted;
    internal bool fireDepartmentCalled;
    internal GameObject drehleiter;

    // Use this for initialization
    void Start() {

        ClearAllFires();
        drehleiter = GameObject.Find("drehleiter_feuerwehr");
        drehleiter.SetActive(false);
    }

    void EnableMeshRendererForGroup(bool enable, GameObject group) {
        foreach (MeshRenderer renderer in group.transform.GetComponentsInChildren<MeshRenderer>()) {
            renderer.enabled = enable;
        }
    }

    void ShowBuildingLevel(int level) {
        for (int l = 0; l < floors.Length; ++l) {
            EnableMeshRendererForGroup(l <= level, floors[l]);
        }
    }

    void AddRandomFire() {

        fires[Random.Range(0, fires.Length - 1)].gameObject.SetActive(true);

        if (!alarmStarted) {
            StartCoroutine(this.startAlarmIn30());
        }
    }

    void ClearAllFires() {
        fires = FindObjectsOfType(typeof(Feuer)) as Feuer[];
        foreach (Feuer fire in fires) {

            if (fire.tag == "dontGoOut")
                continue;
            fire.gameObject.SetActive(false);
        }
    }


    void OnGUI() {
        if (GUI.Button(new Rect(10, 10, 200, 50), "Add random fire")) {
            AddRandomFire();
        }

        if (GUI.Button(new Rect(10, 70, 200, 50), "Clear all fires")) {
            ClearAllFires();
        }

        if (GUI.Button(new Rect(10, 400, 40, 40), "0")) {
            ShowBuildingLevel(0);
        }
        if (GUI.Button(new Rect(60, 400, 40, 40), "1")) {
            ShowBuildingLevel(1);
        }
        if (GUI.Button(new Rect(110, 400, 40, 40), "2")) {
            ShowBuildingLevel(2);
        }
        if (GUI.Button(new Rect(160, 400, 40, 40), "3")) {
            ShowBuildingLevel(3);
        }
        if (GUI.Button(new Rect(210, 400, 40, 40), "4")) {
            ShowBuildingLevel(4);
        }
        if (GUI.Button(new Rect(260, 400, 40, 40), "R")) {
            ShowBuildingLevel(5);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogicForActivity : MonoBehaviour {

	// Use this for initialization
	void Awake () {

        Debug.Log($"Simulation startet {System.DateTime.UtcNow.ToString("HH:mm dd MMMM, yyyy")} ######################################################################");

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void register(RegionController rc) {
        
        regions.Add(rc);
    }

    public List<RegionController> getRegions() {

        return regions;
    }

    private List<RegionController> regions = new List<RegionController>();
}
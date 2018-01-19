using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogicForActivity : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void register(RegionController rc) {
        
        regions.Add(rc);
//        Debug.Log(rc.name + " bei GameLogicForActivity registriert");
    }

    public List<RegionController> getRegions(RegionController rc) {

        return regions;
    }

    private List<RegionController> regions = new List<RegionController>();
}
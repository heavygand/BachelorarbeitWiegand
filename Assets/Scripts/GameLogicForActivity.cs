using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogicForActivity : MonoBehaviour {

    private List<RegionController> regions = new List<RegionController>();
    private FireManager fireManager;

    // Use this for initialization
    void Awake () {

        Debug.Log($"Simulation startet {System.DateTime.UtcNow.ToString("HH:mm dd MMMM, yyyy")} ######################################################################");

    }
	
	// Update is called once per frame
	void Update () {

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

            Debug.LogError($"{name}: Cannot find a random region in an empty list!");
            return null;
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

    public void setFireManager(FireManager fm) {

        fireManager = fm;
    }
    public FireManager getFireManager() {

        return fireManager;
    }
}
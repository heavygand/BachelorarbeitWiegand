using System.Collections;
using System.Collections.Generic;
using System.IO;
using Fungus;
using UnityEngine;

public class dialogController : MonoBehaviour {

    public GameObject dialogMenu;
    public GameObject button;
    public Flowchart flowchart;

    // Use this for initialization
    void Start () {

        string path = @"C:\Users\erikd\OneDrive\Bachelorarbeit\BachelorarbeitWiegand\Assets\Texts\Dialogs";

        if (File.Exists(path)) {

            // This path is a file
            ProcessFile(path);
        }
        else if (Directory.Exists(path)) {

            // This path is a directory
            ProcessDirectory(path);
        }
        else {

            Debug.Log($"{path} is not a valid file or directory.");
        }

        
        //flowchart.CreateBlock(new Vector2(100, 100)).BlockName = "LoL";
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    // Process all files in the directory passed in, recurse on any directories 
    // that are found, and process the files they contain.
    public static void ProcessDirectory(string targetDirectory) {

        // Process the list of files found in the directory.
        string[] fileEntries = Directory.GetFiles(targetDirectory);
        foreach (string fileName in fileEntries)
            ProcessFile(fileName);

        // Recurse into subdirectories of this directory.
        string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
        foreach (string subdirectory in subdirectoryEntries)
            ProcessDirectory(subdirectory);
    }

    // Insert logic for processing found files here.
    public static void ProcessFile(string path) {

        if (path.EndsWith(".txt")) {

            Debug.Log($"Contents of {path}."); 
        }

        // Read each line of the file into a string array. Each element of the array is one line of the file.
        string[] lines = File.ReadAllLines(path);

        // Display the file contents by using a foreach loop.
        foreach (string line in lines) {

            Debug.Log(line);
        }
    }
}

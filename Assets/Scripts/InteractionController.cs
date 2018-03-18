using UnityEngine;
using System.IO;
using Fungus;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class InteractionController : MonoBehaviour {

    public float distanceToSee;
    public string ObjectName = "nothing";
    public Flowchart flowchart;
    public string dialogPath;

    private Material originalMaterial, tempMaterial;
    private Renderer rend;
    private ActivityController selectedAvatar;
    private ActivityController me;
    private ObjectController myTalkDestination;
    private FirstPersonController fpc;
    private UnityStandardAssets.Characters.FirstPerson.MouseLook mouseLook;

    private GameObject lastRend;
    private static string[] dialogFiles;

    // Init components
    void Start() {
        
        me = gameObject.GetComponent<ActivityController>();
        myTalkDestination = gameObject.GetComponentInChildren<ObjectController>();

        fpc = GetComponent<FirstPersonController>();
        mouseLook = fpc.mouseLookScript;
    }
    #region AVATAR SELECTION
    /*
     * 
     * ####################################
     * PART 1: AVATAR SELECTION
     * ####################################
     * 
     */

    private void setControl(bool control) {

        mouseLook.SetCursorLock(control);
        fpc.enabled = control;
    }

    // Update is called once per frame
    void Update() {

        if (Input.GetKeyDown(KeyCode.F)) {

            toggleMouseAndController();
        }

        RaycastHit hitInfo;

        //Draws ray in scene view during playmode; the multiplication in the second parameter controls how long the line will be
        Debug.DrawRay(transform.position, transform.forward * distanceToSee, Color.magenta);

        //A raycast returns a true or false value
        //we  initiate raycast through the Physics class
        //out parameter is saying take collider information of the object we hit, and push it out and 
        //store is in the what I hit variable. The variable is empty by default, but once the raycast hits
        //any collider, it's going to take the information, and store it in whatIHit variable. So then,
        //if I wanted to access something, I could access it through the whatIHit variable. 

        if (Physics.Raycast(transform.position, transform.forward, out hitInfo, distanceToSee)) {

            GameObject currRend = hitInfo.collider.gameObject;

            if (currRend == lastRend || currRend == myTalkDestination.gameObject || currRend == gameObject) {

                lastRend = null;
                return;
            }
            lastRend = currRend;

            ObjectController objectController = currRend.GetComponent<ObjectController>();

            // When this is no avatar
            if (objectController == null || !objectController.isAvatar) {

                deselectAvatarAndFlowchart();
                return;
            }
            // This is an avatar:

            // When this is the same avatar, do nothing

            //When this is another avatar
            if(selectedAvatar != currRend.transform.parent.gameObject.GetComponent<ActivityController>()) {

                if(selectedAvatar != null) selectedAvatar.deselect();
                
                // Because the huge collider of the talkdestination
                selectedAvatar = currRend.transform.parent.gameObject.GetComponent<ActivityController>();
                ObjectName = selectedAvatar.name;
                selectedAvatar.select();
                setFlowchart();
            }
        }
        // Nothing is hit
        else {

            deselectAvatarAndFlowchart();
        }

        flowchart.SetStringVariable("name", ObjectName);
    }

    private void toggleMouseAndController() {

        mouseLook.SetCursorLock(!mouseLook.lockCursor);
        fpc.enabled = !fpc.enabled;
    }

    // Set Fungus variables
    private void setFlowchart() {

        flowchart.SetBooleanVariable("avatarSelected", true);
        flowchart.SetGameObjectVariable("currentAvatar", selectedAvatar.gameObject);
    }

    private void deselectAvatarAndFlowchart() {

        ObjectName = "nobody";

        if (selectedAvatar != null) {
            selectedAvatar.deselect();
            selectedAvatar = null;
        }

        flowchart.SetBooleanVariable("avatarSelected", false);
        flowchart.SetGameObjectVariable("currentAvatar", null);
    }
    #endregion
    #region EVENT METHODS CALLED FROM FUNGUS
    /*
     * 
     * ############################################
     * PART 2: EVENT METHODS CALLED FROM FUNGUS
     * ############################################
     * 
     */

    public void interruptSelected() {

        me.log4Me("interruptSelected called");
        if (selectedAvatar.CurrentActivity != myTalkDestination && selectedAvatar.NextActivity != myTalkDestination) {

            me.log4Me($"Trying to interrupt {selectedAvatar.name}");
            selectedAvatar.MyLeader = me;
            selectedAvatar.interruptWith(myTalkDestination);

            setControl(false);
        } else {

            me.log4Me("So the selected avatar has my talkdestination");
        }
    }

    public void sendAway() {

        selectedAvatar.interruptFromOutside();
        setControl(true);
    }

    public void returnToActivity() {

        selectedAvatar.MyLeader = me;
        selectedAvatar.interruptWith(selectedAvatar.LastActivity);
        setControl(true);
    }

    public void wasErlebt() {

        readDialogPath();
    }
    #endregion
    #region FUNGUS MENU BUILDING
    /*
     * 
     * ####################################
     * PART 3: TEXTFILE PROCESSING
     * ####################################
     * 
     */

    private void readDialogPath() {

        if (Directory.Exists(dialogPath)) {
            
            dialogFiles = Directory.GetFiles(dialogPath);
        }
        else {

            Debug.LogError($"{dialogPath} is not a valid directory.");
        }
    }

    public void showDialogMenu() {

        foreach (string fullFileName in dialogFiles) {

            ProcessFile(fullFileName);
        }
    }

    public void ProcessFile(string path) {

        if (!path.EndsWith(".txt")) return;

        // Read each line of the file into a string array.
        string[] lines = File.ReadAllLines(path);
        
        foreach (string line in lines) {

            // Create the Button
            string buttontext2Search = "BUTTONTEXT:";
            if (line.StartsWith(buttontext2Search)) {

                string readButtonText = substringAfter(line, buttontext2Search);
            }

            // Set the response Text
            string readResponseText2Search = "RESPONSETEXT:";
            if (line.StartsWith(readResponseText2Search)) {

                string readResponseText = substringAfter(line, readResponseText2Search);
            }
        }
    }

    public static string substringAfter(string line, string afterWhat) {

        return line.Substring(line.IndexOf(afterWhat) + afterWhat.Length);
    }
    #endregion
}
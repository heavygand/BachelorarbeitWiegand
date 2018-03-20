using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Fungus;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class InteractionController : MonoBehaviour {

    public float distanceToSee;
    public string ObjectName = "nothing";
    public Flowchart flowchart;
    public string dialogDirectory;
    public GameObject sprechenText;

    private Material originalMaterial, tempMaterial;
    private Renderer rend;
    private ActivityController selectedAvatar;
    private ActivityController me;
    private ObjectController myTalkDestination;
    private FirstPersonController fpc;
    private UnityStandardAssets.Characters.FirstPerson.MouseLook mouseLook;
    private GameObject lastRend;
    private static string[] dialogFiles;
    private string fullPath;
    private string searchStringDoingStartText = "DOING:";
    private string searchStringGoingStartText = "GOING:";
    private string searchStringEndText = "END:";
    private string searchStringWhereText = "WHERE:";
    private string searchStringAlarmActivatedText = "ALARMACTIVATED:";
    private string doingStartText = "";
    private string goingStartText = "";
    private string endText = "";
    private string alarmActivatedText = "";
    private string whereText = "";
    private GameObject textGO;

    // Init components
    void Start() {
        
        me = gameObject.GetComponent<ActivityController>();
        myTalkDestination = gameObject.GetComponentInChildren<ObjectController>();

        fpc = GetComponent<FirstPersonController>();
        mouseLook = fpc.mouseLookScript;

        if (!findDirectoryIn(Directory.GetCurrentDirectory())) {

            Debug.LogError($"Could not find directory \"{dialogDirectory}\" under {Directory.GetCurrentDirectory()}.");
        }
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

                if (selectedAvatar != null) {

                    removeSelectText();
                }
                
                // Because the huge collider of the talkdestination
                selectedAvatar = currRend.transform.parent.gameObject.GetComponent<ActivityController>();
                ObjectName = selectedAvatar.name;
                showSelectText();
                setFlowchart();
            }
        }
        // Nothing is hit
        else {

            deselectAvatarAndFlowchart();
        }

        if (textGO != null) {

            textLookAtCamera(); 
        }
    }

    private void textLookAtCamera() {

        // Look at the Camera
        Transform textTransform = textGO.transform;
        textTransform.LookAt(Camera.main.transform);

        Quaternion wrongTargetRot = textTransform.rotation;
        textTransform.rotation = Quaternion.Euler(
            wrongTargetRot.eulerAngles.x * -1,
            wrongTargetRot.eulerAngles.y + 180,
            wrongTargetRot.eulerAngles.z);
    }

    private void showSelectText() {

        // Create statustext for region
        textGO = Instantiate(sprechenText);
        textGO.transform.parent = selectedAvatar.transform;
        textGO.transform.localPosition = new Vector3(0, 1.5f, 0);
        //TextMesh regionText = textGO.GetComponent<TextMesh>();
        //regionText.text = name;
    }

    private void toggleMouseAndController() {

        mouseLook.SetCursorLock(!mouseLook.lockCursor);
        fpc.enabled = !fpc.enabled;
    }

    // Set Fungus variables
    private void setFlowchart() {

        flowchart.SetBooleanVariable("avatarSelected", true);
        flowchart.SetGameObjectVariable("currentAvatar", selectedAvatar.gameObject);
        flowchart.SetStringVariable("name", ObjectName);
    }

    private void deselectAvatarAndFlowchart() {

        ObjectName = "nobody";

        if (selectedAvatar != null) {
            removeSelectText();
            selectedAvatar = null;
        }

        flowchart.SetBooleanVariable("avatarSelected", false);
        flowchart.SetGameObjectVariable("currentAvatar", null);
        flowchart.SetStringVariable("name", ObjectName);
    }

    private void removeSelectText() {
        Destroy(textGO);
        textGO = null;
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

        removeSelectText();

        if (selectedAvatar.CurrentActivity != myTalkDestination && selectedAvatar.NextActivity != myTalkDestination) {

            me.log4Me($"Trying to interrupt {selectedAvatar.name}");
            selectedAvatar.MyLeader = me;
            selectedAvatar.interruptWith(myTalkDestination);

            setControl(false);
        }
        else {

            me.log4Me("So the selected avatar has my talkdestination");
        }
    }

    public void sendAway() {

        selectedAvatar.interruptFromOutside();
        showSelectText();
        selectedAvatar.removeExclamationMark();
        setControl(true);
    }

    public void returnToActivity() {

        selectedAvatar.MyLeader = me;
        selectedAvatar.interruptWith(selectedAvatar.LastActivity);
        showSelectText();
        selectedAvatar.removeExclamationMark();
        setControl(true);
    }

    public string wasErlebt() {

        getTextValues(getFile("Standard.txt"));
        if (selectedAvatar.FireSeen) {

            getTextValues(getFile($"{selectedAvatar.fireRegion.name}.txt"));
        }

        ObjectController lastActivity = selectedAvatar.activityBeforePanic;

        string discription = lastActivity != null ? " "+lastActivity.discription+(lastActivity.isAvatar?" mit "+ lastActivity.getAvatar().name:"") : " [no activity found]";
        return (selectedAvatar.wasWalking?goingStartText:doingStartText) + discription + endText + (selectedAvatar.activatedAlarm ? alarmActivatedText : "");
    }
    #endregion
    #region TEXTFILE PROCESSING
    /*
     * 
     * ####################################
     * PART 3: TEXTFILE PROCESSING
     * ####################################
     * 
     */
    private bool findDirectoryIn(string path) {//dialogDirectory

        string[] directories = Directory.GetDirectories(path);

        foreach (string directory in directories) {

            if(substringAfterLast(directory, "\\") == dialogDirectory) {

                fullPath = directory;
                dialogFiles = Directory.GetFiles(fullPath);
                return true;
            }
            if (findDirectoryIn(directory)) {

                return true;
            }
        }
        return false;
    }

    public string[] getFile(string file) {

        foreach (string fullFileName in dialogFiles) {

            if (fullFileName.EndsWith(file)) {

                //Debug.Log($"{file} found in {fullFileName}");
                return File.ReadAllLines(fullFileName);
            }
        }
        Debug.LogError($"{file} was not found in {fullPath}.");
        return null;
    }

    public void getTextValues(string[] lines) {
        
        foreach (string line in lines) {
            
            if (line.StartsWith(searchStringDoingStartText)) {
                
                doingStartText = substringAfter(line, searchStringDoingStartText);
            }
            else if (line.StartsWith(searchStringGoingStartText)) {

                goingStartText = substringAfter(line, searchStringGoingStartText);
            }
            else if (line.StartsWith(searchStringEndText)) {

                endText =  substringAfter(line, searchStringEndText);
            }
            else if (line.StartsWith(searchStringAlarmActivatedText)) {

                alarmActivatedText = substringAfter(line, searchStringAlarmActivatedText);
            }
            else if (line.StartsWith(searchStringWhereText)) {

                whereText = substringAfter(line, searchStringWhereText);
            }
        }
    }

    public static string substringAfter(string line, string afterWhat) {

        return line.Substring(line.IndexOf(afterWhat) + afterWhat.Length);
    }

    public static string substringAfterLast(string line, string afterWhat) {

        return line.Substring(line.LastIndexOf(afterWhat) + afterWhat.Length);
    }
    #endregion
}
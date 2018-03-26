using UnityEngine;
using System.IO;
using Fungus;
using UnityStandardAssets.Characters.FirstPerson;

/// <summary>
/// Organizes all interaction of the player with avatars
/// 
/// Author: Christian Wiegand
/// Matrikelnummer: 30204300
/// </summary>
public class InteractionController : MonoBehaviour {

    /// <summary>
    /// This is the length of the raycast, that finds avatars
    /// </summary>
    public float distanceToSee;

    /// <summary>
    /// The name of the selected avatar will show up in the inspector
    /// </summary>
    public string ObjectName = "nothing";

    /// <summary>
    /// Essential: The Fungus Flowchart
    /// </summary>
    public Flowchart flowchart;

    /// <summary>
    /// The local directory where the dialog text files are
    /// </summary>
    public string dialogDirectory;

    /// <summary>
    /// A gameobject that indicates wich avatar is currently selected
    /// </summary>
    public GameObject sprechenText;

    private Material originalMaterial, tempMaterial;
    private Renderer rend;

    private ActivityController selAv;
    /// <summary>
    /// The currently selected avatar
    /// This also organizes the showing of the selectiontext
    /// </summary>
    public ActivityController selectedAvatar
    {
        get
        {
            return selAv;
        }
        set
        {
            selAv = value;

            if (selAv != null && !talking) {

                showSelectText();
            }
            else {

                removeSelectText();
            }
        }
    }
    private ActivityController me;
    private ObjectController myTalkDestination;
    private FirstPersonController fpc;
    private UnityStandardAssets.Characters.FirstPerson.MouseLook mouseLook;
    private GameObject lastRend;
    private GameObject textGO;
    private static string[] dialogFiles;
    private bool talking;
    private bool isLocated;

    /*
     * 
     * ###################
     * Dialog text finding
     * ###################
     * 
     */
    private string fullPath;
    private string DoingStart = "DOING:";
    private string GoingStart = "GOING:";
    private string End = "END:";
    private string Where = "WHERE:";
    private string StandardGreeting = "STANDARDGREETING:";
    private string FireSeenGreeting = "FIRESEENGREETING:";
    private string AlarmActivated = "ALARMACTIVATED:";
    private string Nothing = "NOTHING:";
    private string doingStartText = "";
    private string goingStartText = "";
    private string endText = "";
    private string alarmActivatedText = "";
    private string whereText = "";
    private string StandardGreetingText = "";
    private string FireSeenGreetingText = "";
    private string nothingText = "";
    private bool wasWalking;
    private ObjectController lastActivity;

    /// <summary>
    /// Initialisation and validation
    /// </summary>
    void Start() {
        
        me = gameObject.GetComponent<ActivityController>();
        myTalkDestination = gameObject.GetComponentInChildren<ObjectController>();

        fpc = GetComponent<FirstPersonController>();
        mouseLook = fpc.mouseLookScript;

        if (!findDirectoryIn(Directory.GetCurrentDirectory())) {

            Debug.LogError($"Could not find directory \"{dialogDirectory}\" under {Directory.GetCurrentDirectory()}.");
        }
        if (flowchart == null) Debug.LogError($"Fehler: First Person Controller hat keinen Flowchart zugewiesen bekommen im Inspektor. Bitte einen in die Szene hinzufügen und zuweisen");
        if (sprechenText == null) Debug.LogWarning($"Warnung: First Person Controller hat kein GameObject, das markierte Avatare anzeigt zugewiesen bekommen im Inspektor. Bitte ein Prefab zuweisen");
    }

    #region AVATAR SELECTION
    /*
     * 
     * ####################################
     * PART 1: AVATAR SELECTION
     * ####################################
     * 
     */

    /// <summary>
    /// Update takes care of the raycast selection, key input, and that the text of the selected avatar looks at the camera
    /// </summary>
    void Update() {

        if (Input.GetKeyDown(KeyCode.F)) {

            toggleMouseAndController();
        }

        if (textGO != null) {

            textLookAtCamera();
        }

        if(talking) return;

        RaycastHit hitInfo;

        // Draws ray in scene view during playmode; the multiplication in the second parameter controls how long the line will be
        Debug.DrawRay(transform.position, transform.forward * distanceToSee, Color.magenta);

        // A raycast returns a true or false value
        // we  initiate raycast through the Physics class
        // out parameter is saying take collider information of the object we hit, and push it out and 
        // store it in the hitInfo variable. The variable is empty by default, but once the raycast hits
        // any collider, it's going to take the information, and store it in hitInfo variable.

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
                
                // Because the huge collider of the talkdestination
                selectedAvatar = currRend.transform.parent.gameObject.GetComponent<ActivityController>();
                ObjectName = selectedAvatar.name;
                setFlowchart();
            }
        }
        // Nothing is hit
        else {

            deselectAvatarAndFlowchart();
        }
    }

    /// <summary>
    /// Rotates the selectiontext towards the player
    /// </summary>
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

    /// <summary>
    /// Resets the selected avatar internally and in the flowchart variables
    /// </summary>
    private void deselectAvatarAndFlowchart() {

        ObjectName = "nobody";

        selectedAvatar = null;

        flowchart.SetBooleanVariable("avatarSelected", false);
        flowchart.SetGameObjectVariable("currentAvatar", null);
        flowchart.SetStringVariable("name", ObjectName);
    }


    /// <summary>
    /// Spawns the selectiontext correctly
    /// </summary>
    private void showSelectText() {

        // When there is still a text somewhere
        if(textGO != null) {

            removeSelectText();
        }
        
        textGO = Instantiate(sprechenText);
        textGO.transform.parent = selectedAvatar.transform;
        textGO.transform.localPosition = new Vector3(0, 1.5f, 0);
    }


    /// <summary>
    /// Removes the selectiontext
    /// </summary>
    private void removeSelectText() {

        if(textGO == null) return;
        
        Destroy(textGO);
        textGO = null;
    }

    /// <summary>
    /// Switches between a locked camera with mouse and an unlocked camera with no mouse
    /// This is for switching between the normal "first person mode" and the "dialog mode"
    /// </summary>
    private void toggleMouseAndController() {

        me.log4Me($"toggleMouseAndController() called, so calling setControl({!fpc.enabled})");
        setControl(!fpc.enabled);
    }

    /// <summary>
    /// Sets a locked camera with mouse (false), or sets an unlocked camera with no mouse (true)
    /// This is for switching between the normal "first person mode" and the "dialog mode"
    /// </summary>
    /// <param name="value">If an unlocked camera with no mouse shall be enabled</param>
    private void setControl(bool value) {

        mouseLook.SetCursorLock(value);
        me.log4Me($"mouseLook.SetCursorLock({value}) called");

        fpc.enabled = value;
        me.log4Me($"fpc.enabled = {value} settet");
    }

    /// <summary>
    /// Sets the Fungus Flowchart variables with the data of the currently selected avatar
    /// </summary>
    private void setFlowchart() {

        flowchart.SetBooleanVariable("avatarSelected", true);
        flowchart.SetGameObjectVariable("currentAvatar", selectedAvatar.gameObject);
        flowchart.SetStringVariable("name", ObjectName);
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

    /// <summary>
    /// Interrupts the currently selected avatar and reads the dialogtext-files
    /// Also removes the selectiontext while in a dialog
    /// </summary>
    public void interruptSelected() {

        me.log4Me("interruptSelected called");

        // First get the standard text
        getTextValues(getFile("Standard.txt"));

        // Then get the specific text for a fire in the current region
        if (selectedAvatar.FireSeen) {

            getTextValues(getFile($"{selectedAvatar.fireRegion.name}.txt"));
        }

        talking = true;
        removeSelectText();
        wasWalking = selectedAvatar.Going;
        lastActivity = selectedAvatar.CurrentActivity;

        if (selectedAvatar.CurrentActivity != myTalkDestination && selectedAvatar.NextActivity != myTalkDestination) {

            me.log4Me($"Trying to interrupt {selectedAvatar.name}, who is currently {(wasWalking?"going":"doing")}");
            selectedAvatar.MyLeader = me;
            selectedAvatar.interruptWith(myTalkDestination);
        }
        else {

            me.log4Me($"{selectedAvatar.name} already has my talkdestination");
        }

        me.log4Me("setting control to false");
        setControl(false);
    }

    /// <summary>
    /// Return the name of the currently selected avatar
    /// </summary>
    /// <returns>the name of the currently selected avatar</returns>
    public string getName() {

        return selectedAvatar.name;
    }

    /// <summary>
    /// Returns the greeting text for the current case
    /// </summary>
    /// <returns>the greeting text for the current case</returns>
    public string getGreetingText() {

        if (selectedAvatar.FireSeen) {

            return FireSeenGreetingText;
        }

        return StandardGreetingText;
    }

    /// <summary>
    /// Lets the avatar stop the talkdestination with the player and start something else
    /// </summary>
    public void sendAway() {

        selectedAvatar.MyLeader = null;
        selectedAvatar.log4Me($"I was send away by the player");
        selectedAvatar.interruptFromOutside();
        showSelectText();
        selectedAvatar.removeExclamationMark();

        talking = false;
        setControl(true);
    }

    /// <summary>
    /// Returns to the activity, that the avatar did, before he was interrupted
    /// </summary>
    /// <param name="person">The avatar that shall return</param>
    public void returnToActivity(ActivityController person) {

        person.MyLeader = null;
        person.log4Me($"I am returning to {person.LastActivity.name}");
        person.interruptWith(person.LastActivity);
        person.removeExclamationMark();
    }

    /// <summary>
    /// Returns to the activity, that the selected avatar did, before he was interrupted by the player
    /// </summary>
    public void returnToActivity() {

        returnToActivity(selectedAvatar);

        showSelectText();
        talking = false;
        setControl(true);
    }

    /// <summary>
    /// Builds and returns the story of the avatar
    /// </summary>
    /// <returns>the story of the avatar</returns>
    public string wasErlebt() {

        // When he hasn't expierienced anything
        if (selectedAvatar.activityBeforePanic == null) {

            return nothingText + " " + (wasWalking ? goingStartText : doingStartText) + " "+lastActivity.discription;
        }

        // When he expierienced something
        lastActivity = selectedAvatar.activityBeforePanic;
        string discription = lastActivity != null ? " " + lastActivity.discription + (lastActivity.isAvatar ? " mit " + lastActivity.getAvatar().name : "") : " [no activity found]";
        return (selectedAvatar.wasWalking?goingStartText:doingStartText) + discription + endText + (selectedAvatar.activatedAlarm ? alarmActivatedText : "");
    }

    /// <summary>
    /// Return the text of the selected avatar, that contains fire information
    /// </summary>
    /// <returns></returns>
    public string whereFire() {

        fireIsLocated();
        return whereText;
    }

    /// <summary>
    /// Indicates to the attendant crowd, that the fireinformation has been given to the player
    /// </summary>
    public void fireIsLocated() {

        if(isLocated) return;
        isLocated = true;

        foreach (ActivityController fireWitness in selectedAvatar.fireRegion.firePeople) {

            if (selectedAvatar != fireWitness) {

                fireWitness.log4Me($"I am no firewitness anymore");
                returnToActivity(fireWitness);
            }
        }
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

    /// <summary>
    /// Looks for the given local dialog directory
    /// Warning: no other path is allowed to contain a directory with the same name as in the variable dialogDirectory
    /// </summary>
    /// <param name="path">The path to look in recursively</param>
    /// <returns>If the local dialog directory has been found</returns>
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

    /// <summary>
    /// Gets the content of a certain file. Looks in the already existing list of dialogfiles
    /// </summary>
    /// <param name="file">The filename to search for</param>
    /// <returns>A stringarray of all line in the found file</returns>
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

    /// <summary>
    /// Reads text dialog values out of a set of string lines
    /// </summary>
    /// <param name="lines">The lines of a file as stringarray</param>
    public void getTextValues(string[] lines) {
        
        foreach (string line in lines) {
            
            if (line.StartsWith(DoingStart)) {
                
                doingStartText = substringAfter(line, DoingStart);
            }
            else if (line.StartsWith(GoingStart)) {

                goingStartText = substringAfter(line, GoingStart);
            }
            else if (line.StartsWith(End)) {

                endText =  substringAfter(line, End);
            }
            else if (line.StartsWith(AlarmActivated)) {

                alarmActivatedText = substringAfter(line, AlarmActivated);
            }
            else if (line.StartsWith(Where)) {

                whereText = substringAfter(line, Where);
            }
            else if (line.StartsWith(StandardGreeting)) {

                StandardGreetingText = substringAfter(line, StandardGreeting);
            }
            else if (line.StartsWith(FireSeenGreeting)) {

                FireSeenGreetingText = substringAfter(line, FireSeenGreeting);
            }
            else if (line.StartsWith(Nothing)) {

                nothingText = substringAfter(line, Nothing);
            }
        }
    }

    /// <summary>
    /// Delivers the substring after a certain string
    /// </summary>
    /// <param name="line">The stringline to search in</param>
    /// <param name="afterWhat">The string after wich the text is desired</param>
    /// <returns>The substring after the given string</returns>
    public static string substringAfter(string line, string afterWhat) {

        return line.Substring(line.IndexOf(afterWhat) + afterWhat.Length);
    }

    /// <summary>
    /// Delivers the substring after the last appearance of a certain string
    /// </summary>
    /// <param name="line">The stringline to search in</param>
    /// <param name="afterWhat">The string after wich the text is desired</param>
    /// <returns>The substring after the last appearance of the given string</returns>
    public static string substringAfterLast(string line, string afterWhat) {

        return line.Substring(line.LastIndexOf(afterWhat) + afterWhat.Length);
    }
    #endregion
}
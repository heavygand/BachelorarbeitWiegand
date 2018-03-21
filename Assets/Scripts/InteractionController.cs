using UnityEngine;
using System.IO;
using Fungus;
using UnityStandardAssets.Characters.FirstPerson;

public class InteractionController : MonoBehaviour {

    public float distanceToSee;
    public string ObjectName = "nothing";
    public Flowchart flowchart;
    public string dialogDirectory;
    public GameObject sprechenText;

    private Material originalMaterial, tempMaterial;
    private Renderer rend;

    private ActivityController selAv;
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
    private static string[] dialogFiles;
    
    private string fullPath;
    private string DoingStart = "DOING:";
    private string GoingStart = "GOING:";
    private string End = "END:";
    private string Where = "WHERE:";
    private string StandardGreeting = "STANDARDGREETING:";
    private string FireSeenGreeting = "FIRESEENGREETING:";
    private string AlarmActivated = "ALARMACTIVATED:";
    private string doingStartText = "";
    private string goingStartText = "";
    private string endText = "";
    private string alarmActivatedText = "";
    private string whereText = "";
    private string StandardGreetingText = "";
    private string FireSeenGreetingText = "";

    private GameObject textGO;
    private bool talking;
    private bool isLocated;

    // Init components
    void Start() {
        
        me = gameObject.GetComponent<ActivityController>();
        myTalkDestination = gameObject.GetComponentInChildren<ObjectController>();

        fpc = GetComponent<FirstPersonController>();
        mouseLook = fpc.mouseLookScript;

        if (!findDirectoryIn(Directory.GetCurrentDirectory())) {

            Debug.LogError($"Could not find directory \"{dialogDirectory}\" under {Directory.GetCurrentDirectory()}.");
        }
        if (flowchart == null) Debug.LogError($"Fehler: First Person Controller hat keinen Flowchart zugewiesen bekommen im Inspektor. Bitte einen in die Szene hinzufügen und zuweisen");
    }

    #region AVATAR SELECTION
    /*
     * 
     * ####################################
     * PART 1: AVATAR SELECTION
     * ####################################
     * 
     */

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

    private void deselectAvatarAndFlowchart() {

        ObjectName = "nobody";

        selectedAvatar = null;

        flowchart.SetBooleanVariable("avatarSelected", false);
        flowchart.SetGameObjectVariable("currentAvatar", null);
        flowchart.SetStringVariable("name", ObjectName);
    }

    private void showSelectText() {

        // When there is still a text somewhere
        if(textGO != null) {

            removeSelectText();
        }
        
        textGO = Instantiate(sprechenText);
        textGO.transform.parent = selectedAvatar.transform;
        textGO.transform.localPosition = new Vector3(0, 1.5f, 0);
    }

    private void removeSelectText() {

        if(textGO == null) return;
        
        Destroy(textGO);
        textGO = null;
    }

    private void toggleMouseAndController() {

        me.log4Me($"toggleMouseAndController() called, so calling setControl({!fpc.enabled})");
        setControl(!fpc.enabled);
    }

    private void setControl(bool value) {

        mouseLook.SetCursorLock(value);
        me.log4Me($"mouseLook.SetCursorLock({value}) called");

        fpc.enabled = value;
        me.log4Me($"fpc.enabled = {value} settet");
    }

    // Set Fungus variables
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

        if (selectedAvatar.CurrentActivity != myTalkDestination && selectedAvatar.NextActivity != myTalkDestination) {

            me.log4Me($"Trying to interrupt {selectedAvatar.name}");
            selectedAvatar.MyLeader = me;
            selectedAvatar.interruptWith(myTalkDestination);
        }
        else {

            me.log4Me($"So {selectedAvatar.name} has my talkdestination");
        }

        me.log4Me("setting control to false");
        setControl(false);
    }

    public string getGreetingText() {

        if (selectedAvatar.FireSeen) {

            return FireSeenGreetingText;
        }

        return StandardGreetingText;
    }

    public void sendAway() {

        selectedAvatar.MyLeader = null;
        selectedAvatar.log4Me($"I was send away by the player");
        selectedAvatar.interruptFromOutside();
        showSelectText();
        selectedAvatar.removeExclamationMark();

        talking = false;
        setControl(true);
    }

    public void returnToActivity(ActivityController person) {

        person.MyLeader = null;
        person.log4Me($"I am returning to {person.LastActivity.name}");
        person.interruptWith(person.LastActivity);
        person.removeExclamationMark();
    }

    public void returnToActivity() {

        returnToActivity(selectedAvatar);

        showSelectText();
        talking = false;
        setControl(true);
    }

    public string wasErlebt() {

        ObjectController lastActivity;
        string discription;

        if (selectedAvatar.activityBeforePanic == null) {

            lastActivity = selectedAvatar.LastActivity;
            discription = lastActivity != null ? " "+lastActivity.discription+(lastActivity.isAvatar?" mit "+ lastActivity.getAvatar().name:"") : " [no activity found]";
            return "Ich habe nichts erlebt. "+ (selectedAvatar.Going ? goingStartText : doingStartText) + discription;
        }

        lastActivity = selectedAvatar.activityBeforePanic;
        discription = lastActivity != null ? " " + lastActivity.discription + (lastActivity.isAvatar ? " mit " + lastActivity.getAvatar().name : "") : " [no activity found]";
        return (selectedAvatar.wasWalking?goingStartText:doingStartText) + discription + endText + (selectedAvatar.activatedAlarm ? alarmActivatedText : "");
    }

    public string whereFire() {

        fireIsLocated();
        return whereText;
    }

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
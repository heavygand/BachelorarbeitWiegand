using UnityEngine;
using System.Collections;
public class PlayerRayCasting : MonoBehaviour {
    public float distanceToSee;
    public string ObjectName = "nothing";
    private Color highlightColor;
    Material originalMaterial, tempMaterial;
    Renderer rend;

    void Start() {

        highlightColor = Color.green;
    }

    // Update is called once per frame
    void Update() {
        RaycastHit hitInfo;
        GameObject currRend;
        GameObject avatar = null;

        //Draws ray in scene view during playmode; the multiplication in the second parameter controls how long the line will be
        Debug.DrawRay(transform.position, transform.forward * distanceToSee, Color.magenta);

        //A raycast returns a true or false value
        //we  initiate raycast through the Physics class
        //out parameter is saying take collider information of the object we hit, and push it out and 
        //store is in the what I hit variable. The variable is empty by default, but once the raycast hits
        //any collider, it's going to take the information, and store it in whatIHit variable. So then,
        //if I wanted to access something, I could access it through the whatIHit variable. 

        if (Physics.Raycast(transform.position, transform.forward, out hitInfo, distanceToSee)) {

            currRend = hitInfo.collider.gameObject;

            ObjectController objectController = currRend.GetComponent<ObjectController>();

            if (objectController == null || !objectController.isAvatar) {

                ObjectName = "nothing";
                return;
            }

            avatar = currRend.transform.parent.gameObject; 
            ObjectName = avatar.name;


        }
    }
}
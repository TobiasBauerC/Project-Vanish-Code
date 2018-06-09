using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AIFieldOfView))]
public class FieldOfViewEditor : Editor
{
    void OnSceneGUI()
    {
        //get enemy object
        AIFieldOfView fov = (AIFieldOfView)target;
        //set wire color to white
        Handles.color = Color.green;
        //draw the circle based on the radius in fov script
        Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360.0f, fov.standingViewRadius);
        // make two lines that show the area of view
        Vector3 viewAngleA = fov.DirFromAngle(-fov.viewAngle / 2, false);
        Vector3 viewAngleB = fov.DirFromAngle(fov.viewAngle / 2, false);

        // draw the field of view lines
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleA * fov.standingViewRadius);
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleB * fov.standingViewRadius);
        Handles.DrawLine(fov.transform.position + Vector3.down * fov.viewHeight, fov.transform.position + Vector3.up * fov.viewHeight);

        //change color to red for line of sight line
        Handles.color = Color.red;
        //set each line to red 
        foreach (Transform visibleTarget in fov._visibleTargets)
        {
            Handles.DrawLine(fov.transform.position, visibleTarget.position);
        }
    }

}

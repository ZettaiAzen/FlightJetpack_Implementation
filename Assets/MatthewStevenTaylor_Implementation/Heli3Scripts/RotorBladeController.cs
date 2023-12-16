using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script is identical to BladeController.cs
//For the purposes of seperating the implementations, I chose to remake this script
//I did not want references across multiple folders from different implementations
public class RotorBladeController : MonoBehaviour
{
    //Created enums to hold the main 3 axis
    //This allows us to manually set the rotation of each rotor blade individually from the same script
    public enum Axis
    {
        x, y, z
    }
    public Axis rotationAxis;

    //This is just to set the blade speed and to invert the rotations
    private float bladeSpeed;
    public bool inverseRotation = false;

    private Vector3 rotation;
    float rotateDegree;

    //This creates the bladespeed rotation and sets a maximum rotation so it doesnt break itself apart (an actual bug I had :c)
    public float BladeSpeed
    {
        get { return bladeSpeed; }
        set { bladeSpeed = Mathf.Clamp(value, 0, 3000); }
    }

    //Sets the current transform angles to the vector3 rotation variable
    private void Start()
    {
        rotation = transform.localEulerAngles;
    }


    private void Update()
    {
        //Checks if the rotation is supposed to be inverse or not and updates the rotateDegree
        if (inverseRotation)
        {
            rotateDegree -= bladeSpeed * Time.deltaTime;
        }
        else
        {
            rotateDegree += bladeSpeed * Time.deltaTime;
        }
        rotateDegree = rotateDegree % 360;
        //Created a switch case for the different types of rotations based on the axis selected
        switch (rotationAxis)
        {
            case Axis.y:
                transform.localRotation = Quaternion.Euler(rotation.x, rotateDegree, rotation.z);
                break;
            case Axis.z:
                transform.localRotation = Quaternion.Euler(rotation.x, rotation.y, rotateDegree);
                break;
            default:
                transform.localRotation = Quaternion.Euler(rotateDegree, rotation.y, rotation.z);
                break;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateObject : MonoBehaviour
{

    [SerializeField] private Transform jetpack;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
         if (Input.GetKey("a"))
         {
             jetpack.Rotate(0f, -0.1f, 0f); // rotates the object every frame, the reason the rotation is so small is because it calls the rotation every frame
            // yes, i know theres a better way to do this but this is mainly to just rotate the camera as it follows the object TwT
         }

         if (Input.GetKey("d"))
         {
            jetpack.Rotate(0f, 0.1f, 0f);
         } 
    }
}

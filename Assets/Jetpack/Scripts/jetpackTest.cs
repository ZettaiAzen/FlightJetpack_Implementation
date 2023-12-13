using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jetpackTest : MonoBehaviour
{
    private float horizontalInput;
    private float verticalInput;
    private float thrustPower = 45f;
    private Vector3 movement;
    private Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    private Vector3 lift = new Vector3(0f, 250f, 0f);
    private Quaternion rotationAngle;

    [SerializeField] private ParticleSystem ps1; // particles for the left and right thruster respectively
    [SerializeField] private ParticleSystem ps2;

    [SerializeField] private Rigidbody rb;
    // Update is called once per frame
    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right; // gets the camera Vectors

        Vector3 relativeForwardMovement = verticalInput * camForward;
        Vector3 relativeHorizontalMovement = horizontalInput * camRight; // we multiply the camera vector by our movement direction to get the direction based on the camera

        movement = (relativeForwardMovement + relativeHorizontalMovement);  // we then combine it to make our movement relative to camera positioning

        float currentYAxis = transform.rotation.eulerAngles.y; // saves the y axis rotation so it doesnt get reset

       if (movement != Vector3.zero)
        {
            Vector3 movementDirection = new Vector3(horizontalInput, 0f, verticalInput); // same as above, we get direction based on movement input
            rotationAngle = Quaternion.Euler(-movementDirection.x * 25, currentYAxis, -movementDirection.z * 15); // we then translate this into a rotation value
            ps1.enableEmission = true;
            ps2.enableEmission = true;
        }
        else
        {
            rotationAngle = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0); // if no longer moving, reset the rotation
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, rotationAngle, 1f); // lerp is used for a smoother rotation ( also to make it look more like physics affects it )

    }

    void FixedUpdate() // all our forces are in fixedupdate as it better represents unity's physics calculations
    {
        //rb.MovePosition(rb.position + movement);

        rb.AddForce(movement.normalized * thrustPower, ForceMode.Impulse); // adds a force to indicate movement ( thrust )

        //rb.velocity = movement;

        rb.AddForce(gravity * rb.mass, ForceMode.Acceleration); // we also use a force to simulate our gravity ( acceleration is used as gravity is a constant acceleration )

        if (Input.GetKey("space"))
        {
            rb.AddForce(lift * rb.mass, ForceMode.Force); // and lift
            ps1.enableEmission = true;
            ps2.enableEmission = true;
        }
        else
        {
            ps1.enableEmission = false;
            ps2.enableEmission = false;
        }
    }
}

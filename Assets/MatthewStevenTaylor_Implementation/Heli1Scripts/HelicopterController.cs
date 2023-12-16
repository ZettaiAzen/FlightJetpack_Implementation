using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This was the first implementation I created for the helicopter
//This implementation does not have any safety measures such as the hovering
//Instead, this directly combined the helicopter lift and tilt into a single movement method
//While this is more accurate to real life physics, it is also much more difficult to control hence why I have 2 implementations
public class HelicopterController : MonoBehaviour
{
    private Rigidbody rigidbody;

    [SerializeField] private float responsiveness = 200f;
    [SerializeField] private float throttleAmount = 25f;
    private float throttle;

    private float roll;
    private float pitch;
    private float yaw;

    [SerializeField] private float rotorSpeedModifer = 10f;
    [SerializeField] private Transform rotorsTransform;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleInputs();

        rotorsTransform.Rotate(Vector3.up * throttle * rotorSpeedModifer);
    }

    //This needs to be in FixedUpdate() because FixedUpdate can handle physics better than Update()
    //This is because FixedUpdate() has the same frequency as the physics system in Unity while Update can have variable intervals between updates
    //So this way more consistent than Update(), it was designed that way
    private void FixedUpdate()
    {
        rigidbody.AddForce(transform.up * throttle, ForceMode.Impulse);

        rigidbody.AddTorque(transform.right * pitch * responsiveness);
        rigidbody.AddTorque(-transform.forward * roll * responsiveness);
        rigidbody.AddTorque(transform.up * yaw * responsiveness);
    }

    private void HandleInputs()
    {
        //Gets user inputs
        roll = Input.GetAxis("Roll");
        pitch = Input.GetAxis("Pitch");
        yaw = Input.GetAxis("Yaw");

        //Throttle inputs to ascend and descend
        if (Input.GetKey(KeyCode.Space))
        {
            throttle += Time.deltaTime * throttleAmount;
        } else if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle -= Time.deltaTime * throttleAmount;
        }

        //Because throttle is a percentage value, it is clamped between 0 & 100%
        throttle = Mathf.Clamp(throttle, 0f, 100f);
    }
}

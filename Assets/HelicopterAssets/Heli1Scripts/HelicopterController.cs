using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This was the first implementation I created for the helicopter
//This implementation does not have any safety measures such as the hovering
//Instead, this directly combined the helicopter lift and tilt into a single movement method
//While this is more accurate to real life physics, it is also much more difficult to control hence why I have 2 implementations
public class HelicopterController : MonoBehaviour
{
    private Rigidbody helicopterRB;
    [SerializeField]private float effectiveHeight = 30f;

    [SerializeField] private float responsiveness = 200f;
    [SerializeField] private float throttleAmount = 0.1f;
    [SerializeField] private float maxThrust = 5f;
    private float throttle;

    private float roll;
    private float pitch;
    private float yaw;

    [SerializeField] private float rotorSpeedModifer = 10f;
    [SerializeField] private Transform rotorsTransform;

    private void Awake()
    {
        helicopterRB = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleInputs();

        rotorsTransform.Rotate(Vector3.up * (maxThrust * throttle) * rotorSpeedModifer);
    }

    //This needs to be in FixedUpdate() because FixedUpdate can handle physics better than Update()
    //This is because FixedUpdate() has the same frequency as the physics system in Unity while Update can have variable intervals between updates
    //So this way more consistent than Update(), it was designed that way
    private void FixedUpdate()
    {
        helicopterRB.AddForce(transform.up * throttle, ForceMode.Impulse);

        helicopterRB.AddTorque(transform.right * pitch * responsiveness);
        helicopterRB.AddTorque(-transform.forward * roll * responsiveness);
        helicopterRB.AddTorque(transform.up * yaw * responsiveness);

    }

    private void HandleInputs()
    {
        roll = Input.GetAxisRaw("Roll");
        pitch = Input.GetAxisRaw("Pitch");
        yaw = Input.GetAxisRaw("Yaw");

        if (Input.GetKey(KeyCode.Space))
        {
            throttle += Time.deltaTime * throttleAmount;
        } else if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle -= Time.deltaTime * throttleAmount;
        }

        throttle = Mathf.Clamp(throttle, 0f, 100f);
    }
}

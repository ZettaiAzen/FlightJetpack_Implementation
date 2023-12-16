using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

//This is a combination of my HelicopterController.cs and HelicopterMainEngine.cs scripts
public class HelicopterPhysicsController : MonoBehaviour
{
    //Calling the rigidbody of the helicopter to apply forces
    [SerializeField] Rigidbody rb;
    //Calling the RotorBladeController script to get the main rotor and tail rotor
    [SerializeField] private RotorBladeController mainBlade;
    [SerializeField] private RotorBladeController subBlade;

    //Setting engine power for lift
    private float EnginePower;
    public float enginePower
    {
        get { return EnginePower; }
        //Setting the rotors spinning to correlate with the engine power
        set { mainBlade.BladeSpeed = value * 250; subBlade.BladeSpeed = value * 500; EnginePower = value; }
    }

    [SerializeField] private float effectiveHeight;

    //Variable to increment the engine power
    //It has to be kept small as it would stack too fast if a larger number was used
    [SerializeField] private float engineLift = 0.0075f;
    //Force used to make the controls more responsive
    [SerializeField] private float adjustmentForce = 50f;

    //Variables used for stabilization so the helicopter returns to a neutral position to hover
    private Quaternion stabilization;
    [SerializeField] private float stabilizationForce = 2f;

    //Variables to store the roll, pitch and yaw angles
    private float roll;
    private float pitch;
    private float yaw;

    //Variables to check if the player is grounded in HandleGroundCheck() function
    public LayerMask groundLayer;
    private float distanceToGround;
    public bool isOnGround = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        HandleGroundCheck();
        HandleInputs();
    }

    protected void FixedUpdate()
    {
        //This ensures that the helicopter cannot move on the ground when grounded
        if (!isOnGround)
        {
            HelicopterHover();
            HelicopterMovements();
        }
    }

    //Checks input on the player
    private void HandleInputs()
    {
        //Allows the helicopter to descend and land smoothly
        if (Input.GetKey(KeyCode.LeftShift))
        {
            enginePower -= engineLift * adjustmentForce;

            //enginePower can never be negative
            if (enginePower < 0) enginePower = 0;
        }

        //Adds the lifting force to the helicopter
        //This is the only force used in this implementation
        if (Input.GetAxis("Throttle") > 0)
        {
            enginePower += engineLift;
            rb.AddForce(transform.up * enginePower, ForceMode.Impulse);
        }
        //Ensures that the changes in pitch or roll of the helicopter will not decrease its height by a noticeable amount
        else if (Input.GetAxis("Pitch") > 0 || Input.GetAxis("Pitch") < 0 && !isOnGround)
        {
            enginePower = Mathf.Lerp(enginePower, 17.5f, 0.003f);
        }
        else if (Input.GetAxis("Roll") > 0 || Input.GetAxis("Roll") < 0 && !isOnGround)
        {
            enginePower = Mathf.Lerp(enginePower, 17.5f, 0.003f);
        }
        //If no inputs are being pressed, the helicopter maintains enough engine power to hover
        else if (Input.GetAxis("Throttle") < 0.5 && !isOnGround)
        {
            enginePower = Mathf.Lerp(enginePower, 11.5f, 0.05f);
        }
    }

    //This function casts a raycast downwards to check if the helicopter is touching the ground
    //I chose to use this instead of using regular collisions because I found that it would look much smoother with my hover effect on my helicopter
    private void HandleGroundCheck()
    {
        RaycastHit hit;
        Vector3 direction = transform.TransformDirection(Vector3.down);
        Ray ray = new Ray(transform.position, direction);

        if (Physics.Raycast(ray, out hit, 3000, groundLayer))
        {
            distanceToGround = hit.distance;
            if (distanceToGround < 0.05)
            {
                isOnGround = true;
            }
            else
            {
                isOnGround = false;
            }
        }
    }

    //This is the helicopter hover effect
    //This creates a constant lift of the helicopter in order to keep it hovering in the air when not throttling or descending
    private void HelicopterHover()
    {
        float upForce = 1 - Mathf.Clamp(rb.transform.position.y / effectiveHeight, 0, 1);
        upForce = Mathf.Lerp(0, enginePower, upForce) * rb.mass;
        rb.AddRelativeForce(Vector3.up * upForce);
    }

    //This controls the helicopter's movements
    //The helicopter will adjust its rotations in order to pitch, roll or yaw
    //This in combination with the lift force being applied to the transform.up means the lift force is tilted
    //In the direction the helicopter wants to go
    private void HelicopterMovements()
    {
        Vector3 angles = rb.rotation.eulerAngles;
        roll = (angles.z + 180f) % 360f - 180f;
        pitch = (angles.x + 180f) % 360f - 180f;
        yaw = (angles.y + 180f) % 360f - 180f;
        //Forward movement -> AddTorque on transform.right, rotate around right-axis (x-axis)
        if (Input.GetAxis("Pitch") > 0)
        {
            //Sets a maximum limit for how far the helicopter can pitch forwards or backwards
            //To prevent the helicopter from pitching too far and flipping
            if (pitch < 5.0f)
            {
                rb.AddTorque(transform.right * Input.GetAxis("Pitch") * adjustmentForce, ForceMode.Impulse);
            }
        }
        else if (Input.GetAxis("Pitch") < 0)
        {
            //Sets a maximum limit for how far the helicopter can pitch forwards or backwards
            //To prevent the helicopter from pitching too far and flipping
            if (pitch > -5.0f)
            {
                rb.AddTorque(transform.right * Input.GetAxis("Pitch") * adjustmentForce, ForceMode.Impulse);
            }
        }

        //Side to side movement (banking/rolling) -> AddTorque on transform.forward, rotate around forward-axis (z-axis)
        else if (Input.GetAxis("Roll") > 0)
        {
            //Sets a maximum limit for how far the helicopter can roll to either side
            //To prevent the helicopter from rolling too far and flipping
            if (roll > -5.0f)
            {
                rb.AddTorque(-transform.forward * Input.GetAxis("Roll") * adjustmentForce, ForceMode.Impulse);
            }
        }
        else if (Input.GetAxis("Roll") < 0)
        {
            //Sets a maximum limit for how far the helicopter can roll to either side
            //To prevent the helicopter from rolling too far and flipping
            if (roll < 5.0f)
            {
                rb.AddTorque(-transform.forward * Input.GetAxis("Roll") * adjustmentForce, ForceMode.Impulse);
            }
        }

        //Rotation (yawing) -> AddTorque on transform.up, rotate around up-axis (y-axis)
        //No limit is needed for yawing because helicopters are designed to be able to turn on the spot
        //Helicopters also should be able to spin the full 360 degrees unimpeded
        else if (Input.GetAxis("Yaw") > 0)
        {
            rb.AddTorque(transform.up * Input.GetAxis("Yaw") * adjustmentForce, ForceMode.Impulse);
        }
        else if (Input.GetAxis("Yaw") < 0)
        {
            rb.AddTorque(transform.up * Input.GetAxis("Yaw") * adjustmentForce, ForceMode.Impulse);
        }

        //If no input, reset helicopter to stable Roll and Pitch, maintain Yaw
        if (Input.GetAxis("Roll") == 0 && Input.GetAxis("Pitch") == 0 && Input.GetAxis("Yaw") == 0)
        {
            stabilization = Quaternion.Euler(0f, yaw, 0f);
            rb.MoveRotation(Quaternion.SlerpUnclamped(rb.rotation, stabilization, stabilizationForce * Time.deltaTime));
        }
    }
}
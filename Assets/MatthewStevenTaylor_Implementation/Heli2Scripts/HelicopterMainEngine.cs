using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

//This was the second implementation I created for the helicopter
//This implementation is designed to be much more controlled than the other one which has no safety
//This also simulates the physics as how they would theoretically look with proper controls
//I created a final implementation that attempts to combine the physics of the first implementation with the safety features and polish of this implementation
public class HelicopterMainEngine : MonoBehaviour
{
    //Calling the rigidbody component of the helicopter to apply forces
    //Calling the blades as well to make them spin
    Rigidbody helicopterRb;
    [SerializeField] private BladesController mainBlade;
    [SerializeField] private BladesController subBlade;

    //Sets the engine power of the helicopter
    //This is both for the propulsion/lift of the helicopter
    //Also matches the rotor blade spinning to the engine power
    private float EnginePower;
    public float enginePower
    {
        get { return EnginePower; }
        set { mainBlade.BladeSpeed = value * 250; subBlade.BladeSpeed = value * 500; EnginePower = value; }
    }

    //This sets the maximum height that the helicopter will be allowed to reach
    //Acts as a set barrier so the helicopter cannot ascend forever
    [SerializeField] private float effectiveHeight;

    //This creates a set amount of engine power that will be added as the throttle button is held down
    //Allows for a smooth increase of engine power
    [SerializeField] private float engineLift = 0.0075f;

    //Forces used to enable the helicopter from flying forwards, backwards and turning to left and right
    [SerializeField] private float forwardForce;
    [SerializeField] private float backwardForce;
    [SerializeField] private float turnForce;
    private float turnForceHelper = 1.5f;
    private float turning;

    //Tilt forces used to set the tilt of the aircraft when moving forwards/backwards and turning from side to side
    [SerializeField] private float forwardTiltForce;
    [SerializeField] private float turnTiltForce;

    //Variables to check if the player is grounded in HandleGroundCheck() function
    public LayerMask groundLayer;
    private float distanceToGround;
    public bool isOnGround = true;

    //Vector3 variables used for moving around as well as tilting
    private Vector3 movement = Vector3.zero;
    private Vector3 tilting = Vector3.zero;

    void Start()
    {
        helicopterRb = GetComponent<Rigidbody>();
    }

    //Update can just be used to check whether the helicopter is on the floor or if I am holding down inputs
    void Update()
    {
        HandleGroundCheck();
        HandleInputs();
    }

    //All of the physics functions are called in FixedUpdate() instead of Update() as FixedUpdate works better with physics functions
    //This is because FixedUpdate() has the same frequency as the physics system in Unity while Update can have variable intervals between updates
    //So this is way more consistent than Update()
    protected void FixedUpdate()
    {
        HelicopterHover();
        HelicopterMovements();
        HelicopterTilting();
    }

    //Checks the inputs from the player
    void HandleInputs()
    {
        //The helicopter will be unable to move in any direction until the helicopter is considered above ground
        //This is done to simulate how it works in real life as well, whereby helicopters must ensure they have enough clearance off the ground before they can manuevre safely
        if (!isOnGround)
        {
            movement.x = Input.GetAxis("Horizontal");
            movement.y = Input.GetAxis("Vertical");

            //This is to have the helicopter descend when the player presses C
            //Helicopter loses lift by decreasing engine power using the engineLift variable to reduce it steadily
            if (Input.GetKey(KeyCode.C))
            {
                enginePower -= engineLift;

                if (enginePower < 0)
                {
                    enginePower = 0;
                }
            }
        }
        //Applying lift when throttling by increasing engine power using the engineLift variable to smoothly increment it
        if (Input.GetAxis("Throttle") > 0)
        {
            enginePower += engineLift;
        }
        //Maintaining altitude when moving forwards or backwards by smooothly 
        else if (Input.GetAxis("Vertical") > 0 || Input.GetAxis("Vertical") < 0 && !isOnGround)
        {
            //I used lerp to smoothly shift the engine power from its current power to 17f
            //This means that regardless of whether it is lower or higher than 17, it will still linearly increment or reduce respectively by 0.003 until it reaches 17f
            enginePower = Mathf.Lerp(enginePower, 17.5f, 0.003f);
        }
        //Default engine power
        //This creates a "stabilisation" and helps with the hover-like effect of the helicopter when it is not grounded and the throttle button is not being held
        //This was necessary to add because if not, the helicopter would maintain its last set engine power and continue ascending upwards until it hit the effective height
        //This simultaneously fixes that issue as well as creating a smooth stabilisation effect for when the player is simply hovering and not making any movements
        else if (Input.GetAxis("Throttle") < 0.5f && !isOnGround)
        {
            enginePower = Mathf.Lerp(enginePower, 11f, 0.003f);
        }
    }

    //This function casts a raycast downwards to check if the helicopter is touching the ground
    //I chose to use this instead of using regular collisions because I found that it would look much smoother with my hover effect on my helicopter
    void HandleGroundCheck()
    {
        RaycastHit hit;
        Vector3 direction = transform.TransformDirection(Vector3.down);
        Ray ray = new Ray(transform.position, direction);

        if (Physics.Raycast(ray, out hit, 3000, groundLayer))
        {
            distanceToGround = hit.distance;
            if (distanceToGround < 1)
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
    void HelicopterHover()
    {
        float upForce = 1 - Mathf.Clamp(helicopterRb.transform.position.y / effectiveHeight, 0, 1);
        upForce = Mathf.Lerp(0, enginePower, upForce) * helicopterRb.mass;
        helicopterRb.AddRelativeForce(Vector3.up * upForce);
    }

    //This is how the helicopter moves
    void HelicopterMovements()
    {
        //If the helicopter is moving forwards
        if(Input.GetAxis("Vertical") > 0)
        {
            helicopterRb.AddRelativeForce(Vector3.forward * Mathf.Max(0f, movement.y * forwardForce * helicopterRb.mass));
        } 
        //If the helicopter is moving backwards
        else if(Input.GetAxis("Vertical") < 0)
        {
            helicopterRb.AddRelativeForce(Vector3.back * Mathf.Max(0f, -movement.y * backwardForce * helicopterRb.mass));
        }

        //If the helicopter is turning left or right
        float turn = turnForce * Mathf.Lerp(movement.x, movement.x * (turnForceHelper - Mathf.Abs(movement.y)), Mathf.Max(0f, movement.y));
        turning = Mathf.Lerp(turning, turn, Time.fixedDeltaTime * turnForce);
        helicopterRb.AddRelativeTorque(0f, turning * helicopterRb.mass, 0f);
    }

    //Helicopter tilting
    //Helicopters only have 1 force acting on it, lift, which is generated from the main rotor blades
    //In order to move in any direction, the helicopter has to "tilt", this causes the upwards force to be angled in that direction and thus move forward
    //This function is used to "replicate" it and also puts a cap on how much tilt it can have, this ensures that the helicopter will never flip completely and crash
    //I have created a 2nd implementation which does not have this cap and can allow full control (you will crash)
    void HelicopterTilting()
    {
        tilting.y = Mathf.Lerp(tilting.y, movement.y * forwardTiltForce, Time.deltaTime);
        tilting.x = Mathf.Lerp(tilting.x, movement.x * turnTiltForce, Time.deltaTime);
        helicopterRb.transform.localRotation = Quaternion.Euler(tilting.y, helicopterRb.transform.localEulerAngles.y, -tilting.x);
    }
}

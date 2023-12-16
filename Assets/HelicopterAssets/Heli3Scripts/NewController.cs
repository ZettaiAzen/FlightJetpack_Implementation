using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class NewController : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] private RotorBladeController mainBlade;
    [SerializeField] private RotorBladeController subBlade;

    private float EnginePower;
    public float enginePower
    {
        get { return EnginePower; }
        set { mainBlade.BladeSpeed = value * 250; subBlade.BladeSpeed = value * 500; EnginePower = value; }
    }

    [SerializeField] private float effectiveHeight;

    [SerializeField] private float engineLift = 0.0075f;
    [SerializeField] private float adjustmentForce = 50f;

    [SerializeField] private float rotorSpeedModifer = 10f;

    private Quaternion stabilization;
    [SerializeField] private float stabilizationForce = 2f;

    private float roll;
    private float pitch;
    private float yaw;

    public LayerMask groundLayer;
    private float distanceToGround;
    public bool isOnGround = true;

    private Vector3 movement = Vector3.zero;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.rotation = Quaternion.Euler(Vector3.zero);
    }

    void Update()
    {
        HandleGroundCheck();
        HandleInputs();
    }

    protected void FixedUpdate()
    {
        if (!isOnGround)
        {
            HelicopterHover();
            HelicopterMovements();
            //rotorsTransform.Rotate(Vector3.up * enginePower * rotorSpeedModifer);
            //tailRotorsTransform.Rotate(Vector3.right * enginePower * rotorSpeedModifer);
        }
    }

    private void HandleInputs()
    {
        if(!isOnGround)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                enginePower -= engineLift;

                if (enginePower < 0) enginePower = 0;
            }
        }

        if (Input.GetAxis("Throttle") > 0)
        {
            enginePower += engineLift;
            rb.AddForce(transform.up * enginePower, ForceMode.Impulse);
        }
        else if (Input.GetAxis("Pitch") > 0 || Input.GetAxis("Pitch") < 0 && !isOnGround)
        {
            enginePower = Mathf.Lerp(enginePower, 17.5f, 0.003f);
        }
        else if (Input.GetAxis("Roll") > 0 || Input.GetAxis("Roll") < 0 && !isOnGround)
        {
            enginePower = Mathf.Lerp(enginePower, 17.5f, 0.003f);
        }
        else if(Input.GetAxis("Throttle") < 0.5 && !isOnGround)
        {
            enginePower = Mathf.Lerp(enginePower, 12f, 0.05f);
        }
    }

    private void HandleGroundCheck()
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

    private void HelicopterHover()
    {
        float upForce = 1 - Mathf.Clamp(rb.transform.position.y / effectiveHeight, 0, 1);
        upForce = Mathf.Lerp(0, enginePower, upForce) * rb.mass;
        rb.AddRelativeForce(Vector3.up * upForce);
    }

    private void HelicopterMovements()
    {
        Vector3 angles = rb.rotation.eulerAngles;
        roll = (angles.z + 180f) % 360f - 180f;
        pitch = (angles.x + 180f) % 360f - 180f;
        yaw = (angles.y + 180f) % 360f - 180f;
        Debug.Log("Roll: " + roll + "Pitch: " + pitch + "Yaw: " + yaw);
        //Forward movement -> AddTorque on transform.right, rotate around right-axis (x-axis)
        if (Input.GetAxis("Pitch") > 0)
        {
            if (pitch < 5.0f)
            {
                rb.AddTorque(transform.right * Input.GetAxis("Pitch") * adjustmentForce, ForceMode.Impulse);
            }
        }
        else if (Input.GetAxis("Pitch") < 0)
        {
            if (pitch > -5.0f) 
            {
                rb.AddTorque(transform.right * Input.GetAxis("Pitch") * adjustmentForce, ForceMode.Impulse);
            }
        }

        //Side to side movement (banking/rolling) -> AddTorque on transform.forward, rotate around forward-axis (z-axis)
        else if (Input.GetAxis("Roll") > 0)
        {
            if (roll > -5.0f)
            {
                rb.AddTorque(-transform.forward * Input.GetAxis("Roll") * adjustmentForce, ForceMode.Impulse);
            }
        }
        else if (Input.GetAxis("Roll") < 0)
        {
            if (roll < 5.0f)
            {
                rb.AddTorque(-transform.forward * Input.GetAxis("Roll") * adjustmentForce, ForceMode.Impulse);
            }
        }

        //Rotation (yawing) -> AddTorque on transform.up, rotate around up-axis (y-axis)
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

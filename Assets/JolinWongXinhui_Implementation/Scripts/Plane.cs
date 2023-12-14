using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Plane : MonoBehaviour
{
    // code adapted from: https://youtu.be/7vAHo2B1zLc?si=LJOAjzEaOgHnRvJW

    Rigidbody plane_rb;

    Vector3 controlInput;

    Vector3 velocity;
    Vector3 lastVelocity;
    Vector3 angularVelocity;

    [SerializeField]
    float initialSpeed;

    Vector3 localVelocity;
    Vector3 localAngularVelocity;
    Vector3 localGForce;

    float angleOfAttack;
    float angleOfAttackYaw;
    
    float throttle; // percentage of max thrust
    float throttleIncrement = 0.1f; // how much the throttle can increase at a time
    [SerializeField]
    float maxThrust = 4000f; // max thrust that can be applied to plane
    bool breaksEnabled;

    [SerializeField]
    float liftPower;
    [SerializeField]
    AnimationCurve liftAOACurve;

    [SerializeField]
    float rudderPower;
    [SerializeField]
    AnimationCurve rudderAOACurve;

    [SerializeField]
    Vector3 turnSpeed;
    [SerializeField]
    Vector3 turnAcceleration;
    [SerializeField]
    AnimationCurve steeringCurve;

    //variables that will affect drag coefficients
    [SerializeField]
    AnimationCurve dragForward;
    [SerializeField]
    AnimationCurve dragBack;
    [SerializeField]
    AnimationCurve dragLeft;
    [SerializeField]
    AnimationCurve dragRight;
    [SerializeField]
    AnimationCurve dragTop;
    [SerializeField]
    AnimationCurve dragBottom;
    [SerializeField]
    float breaksDrag;
    [SerializeField]
    float inducedDrag;

    [SerializeField]
    float gLimit;
    [SerializeField]
    float gLimitPitch;

    // Start is called before the first frame update
    void Start()
    {
        // getting plane's rigidbody component
        plane_rb = GetComponent<Rigidbody>();

        // setting plane to have initial speed
        plane_rb.velocity = plane_rb.rotation * new Vector3(0, 0, initialSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        HandleInputs();
    }

    void HandleInputs()
    {
        breaksEnabled = false;

        // for throttle
        if (Input.GetKey(KeyCode.Space))
        {
            throttle += throttleIncrement;
        }
        
        if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle -= throttleIncrement;

            // if player tries to lower throttle more than min, breaks will activate
            if(throttle == 0f)
            {
                Debug.Log("breaks");
                breaksEnabled = true;
            }
        }

        // must clamp throttle so that it does not exceed 0-100
        throttle = Mathf.Clamp(throttle, 0f, 100f);

        // for controlInput, handles rotations

        // for pitch
        if (Input.GetKey(KeyCode.W))
        {
            // point nose down
            controlInput = new Vector3(-1, controlInput.y, controlInput.z);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            // point nose up
            controlInput = new Vector3(1, controlInput.y, controlInput.z);
        }
        else
        {
            controlInput = new Vector3(0, controlInput.y, controlInput.z);
        }

        // for roll
        if (Input.GetKey(KeyCode.E))
        {
            // rolls plane clockwise
            controlInput = new Vector3(controlInput.x, controlInput.y, -1);
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            // rolls plane anticlockwise
            controlInput = new Vector3(controlInput.x, controlInput.y, 1);
        }
        else
        {
            controlInput = new Vector3(controlInput.x, controlInput.y, 0);
        }

        // for yaw
        if (Input.GetKey(KeyCode.A))
        {
            // turns plane left
            controlInput = new Vector3(controlInput.x, -1, controlInput.z);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            // turns plane right
            controlInput = new Vector3(controlInput.x, 1, controlInput.z);
        }
        else
        {
            controlInput = new Vector3(controlInput.x, 0, controlInput.z);
        }
    }

    // similar to Unity's Vector3.Scale but this allows there to be independant scaling for the different directions of movement for drag calculation
    public static Vector3 Scale6(Vector3 value, float posX, float negX, float posY, float negY, float posZ, float negZ)
    {
        Vector3 result = value;

        if (result.x > 0)
        {
            result.x *= posX;
        }
        else if (result.x < 0)
        {
            result.x *= negX;
        }

        if (result.y > 0)
        {
            result.y *= posY;
        }
        else if (result.y < 0)
        {
            result.y *= negY;
        }

        if (result.z > 0)
        {
            result.z *= posZ;
        }
        else if (result.z < 0)
        {
            result.z *= negZ;
        }

        return result;
    }

    void CalculateState(float dt)
    {
        // will be used to convert global plane velocity to local 
        var inverseRotation = Quaternion.Inverse(plane_rb.rotation);

        velocity = plane_rb.velocity;
        angularVelocity = plane_rb.angularVelocity;
        localVelocity = inverseRotation * (velocity); // transforms plane's world velocity into local
        localAngularVelocity = inverseRotation * (angularVelocity); // transforms plane's world angular velocity into local

    }

    void CalculateAngleOfAttack()
    {
        angleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z);
        angleOfAttackYaw = Mathf.Atan2(localVelocity.x, localVelocity.z);
    }

    void CalculateGForce(float dt)
    {
        var invRotation = Quaternion.Inverse(plane_rb.rotation); // to convert global plane velocity to local
        var acceleration = (velocity - lastVelocity) / dt;
        localGForce = invRotation * acceleration;
        lastVelocity = velocity;

    }

    Vector3 CalculateGForce(Vector3 angularVelocity, Vector3 velocity)
    {
        // derived from tangential velocity of circular motion and G-force of circular motion
        // tangential velocity = angularVelocity * radius || G-Force = (tangential velocity)^2 / radius
        // G-Force = (tangential velocity * angularVelocity * radius) / radius
        // G-Force = tangential velocity * angularVelocity
        // we'll cross product these two
        return Vector3.Cross(angularVelocity, velocity);

    }

    // calculates the Gforce limit given the input the player presses in
    Vector3 CalculateGForceLimit(Vector3 input)
    {
        return Scale6(input,
            gLimit, gLimitPitch, //pitching down and up. difference is because humans deal with negative and positive gforce differently
            gLimit, gLimit, // yaw
            gLimit, gLimit // roll
            ) * 9.81f; //multiplied by gravity
    }

    float CalculateGForceLimiter(Vector3 controlInput, Vector3 maxAngularVelocity)
    {
        var limit = CalculateGForceLimit(controlInput);
        // using scale to calculate the amount of angularvelocity the plane would have if no limiter
        var maxGForce = CalculateGForce(Vector3.Scale(controlInput, maxAngularVelocity), localVelocity);

        // if the gforce without limiter is over the limit,
        if (maxGForce.magnitude > limit.magnitude)
        {
            // return a scaling factor to adjust the control input so that the produced gforce will only hit the limit and not exceed
            return limit.magnitude / maxGForce.magnitude;
        }

        // else the scaling factor will be 1, the control input is unaffected
        return 1;
    }

  
    Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float liftPower, AnimationCurve aoaCurve)
    {
        var liftVelocity = Vector3.ProjectOnPlane(localVelocity, rightAxis); // simulating the wind travelling over the wings of the plane
        var velocitySquared = liftVelocity.sqrMagnitude;

        // lift is calculated by velocity^2 * liftCoefficient * liftPower
        var liftCoefficient = aoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        var liftForce = velocitySquared * liftCoefficient * liftPower;

        // lift is perpendicular to velocity of plane
        var liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
        var lift = liftDirection * liftForce;

        // calculating induced drag which is liftCoefficient^2 * inducedDrag 
        // inducedDrag is a hand tuned variable
        var dragForce = (liftCoefficient * liftCoefficient) * inducedDrag;
        var dragDirection = -liftVelocity.normalized;
        var inducedDragForce = dragDirection * velocitySquared * dragForce;

        return lift + inducedDragForce;
    }

    float CalculateSteering(float dt, float angularVelocity, float targetVelocity, float acceleration)
    {
        var difference = targetVelocity - angularVelocity;
        var accelerationForce = acceleration * dt;

        // limits the change of rotation to the acceleration
        return Mathf.Clamp(difference, -accelerationForce, accelerationForce);
    }

    // updating drag
    void UpdateDrag()
    {
        float localVelocitySquared = localVelocity.sqrMagnitude;

        float breaksDragApplied;

        if (!breaksEnabled)
        {
            breaksDragApplied = 0f;
        }
        else
        {
            breaksDragApplied = breaksDrag;
        }

        // using Unity's animation curve, we can adjust the drag coefficient for each orientation in relation to speed of plane
        var dragCoefficient = Scale6(localVelocity.normalized,
            dragRight.Evaluate(Mathf.Abs(localVelocity.x)), dragLeft.Evaluate(Mathf.Abs(localVelocity.x)),
            dragTop.Evaluate(Mathf.Abs(localVelocity.y)), dragBottom.Evaluate(Mathf.Abs(localVelocity.y)),
            dragForward.Evaluate(Mathf.Abs(localVelocity.z)) + breaksDragApplied, // adding the drag for breaks if needed for the forward coefficient
            dragBack.Evaluate(Mathf.Abs(localVelocity.z)));

        // simplified drag formula = 1/2 * velocity^2 * dragCoefficient
        // 1/2 can be ignored since dragCoefficient is our own hand tuned variable
        var drag = dragCoefficient.magnitude * localVelocitySquared * -localVelocity.normalized; // drag is in the opposite direction of velocity
        plane_rb.AddRelativeForce(drag);

    }

    void UpdateLift()
    {
        // to move up or down
        var liftForce = CalculateLift(angleOfAttack, Vector3.right, liftPower, liftAOACurve);

        // to move left or right
        var yawForce = CalculateLift(angleOfAttackYaw, Vector3.up, rudderPower, rudderAOACurve);

        // adding forces to the rigidbody
        plane_rb.AddRelativeForce(liftForce);
        plane_rb.AddRelativeForce(yawForce);
    }

    void UpdateSteering(float dt)
    {
        var speed = localVelocity.z; // getting speed that the plane is travelling at
        var steeringPower = steeringCurve.Evaluate(speed); // getting the steering power for the respective speed, higher speed more steering and vice versa

        // the second argument is max angular velocity
        var gForceScaling = CalculateGForceLimiter(controlInput, turnSpeed * Mathf.Deg2Rad * steeringPower);
        
        var targetAngularVelocity = Vector3.Scale(controlInput, turnSpeed * steeringPower * gForceScaling); //changing the target velocity using the scaling of the gForceLimiter
        var angularVelocity = localAngularVelocity * Mathf.Rad2Deg;

        // correction of rotation, acceleration is controlled by the steeringPower
        var correction = new Vector3(
            CalculateSteering(dt, angularVelocity.x, targetAngularVelocity.x, turnAcceleration.x * steeringPower),
            CalculateSteering(dt, angularVelocity.y, targetAngularVelocity.y, turnAcceleration.y * steeringPower),
            CalculateSteering(dt, angularVelocity.z, targetAngularVelocity.z, turnAcceleration.z * steeringPower)
            );

        plane_rb.AddRelativeTorque(correction * Mathf.Deg2Rad, ForceMode.VelocityChange);
    }


    // to update the plane's physics
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        CalculateState(dt);
        CalculateAngleOfAttack();
        CalculateGForce(dt);

        // adding thrust
        plane_rb.AddRelativeForce(throttle/100f * maxThrust * Vector3.forward);

        // adding drag
        UpdateDrag();

        // adding lift
        UpdateLift();

        // adding torque
        UpdateSteering(dt);
    }

    void OnCollisionEnter(Collision collision)
    {
        // if collides with anything, die
        Debug.Log("UWA IM DEAD");
        this.gameObject.SetActive(false);

    }


    // UI

    [SerializeField]
    TextMeshProUGUI velocityUI;
    [SerializeField]
    TextMeshProUGUI GForceUI;
    [SerializeField]
    Slider throttleUI;

    void LateUpdate()
    {
        velocityUI.text = "Velocity: " + Mathf.Round(localVelocity.z);
        GForceUI.text = "G-Force: " + Mathf.Round(localGForce.y / 9.81f);
        throttleUI.value = throttle;
    }


}

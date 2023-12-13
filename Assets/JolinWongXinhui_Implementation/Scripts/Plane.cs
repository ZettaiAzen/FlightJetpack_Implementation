using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plane : MonoBehaviour
{
    Rigidbody plane_rb;

    Vector3 velocity;
    Vector3 lastVelocity;
    Vector3 angularVelocity;

    Vector3 localVelocity;
    Vector3 localAngularVelocity;
    Vector3 localGForce;

    float angleOfAttack;
    float angleOfAttackYaw;
    [SerializeField]
    AnimationCurve aoaCurve;

    float throttle; // percentage of max thrust
    float throttleIncrement = 0.1f; // how much the throttle can increase at a time
    [SerializeField]
    float maxThrust = 4000f; // max thrust that can be applie to plane
    bool breaksEnabled;

    [SerializeField]
    float liftPower;

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

    // Start is called before the first frame update
    void Start()
    {
        // getting plane's rigidbody component
        plane_rb = GetComponent<Rigidbody>();
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
        
        if (Input.GetKey(KeyCode.LeftControl))
        {
            throttle -= throttleIncrement;

            // if player tries to lower throttle more than min, breaks will activate
            if(throttle == 0)
            {
                breaksEnabled = true;
            }
        }

        // must clamp throttle so that it does not exceed 0-100
        throttle = Mathf.Clamp(throttle, 0f, 100f);
    }

    void CalculateState(float dt)
    {
        // will be used to convert global plane velocity to local since the plane's directions are flipped
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
        var inverseRotation = Quaternion.Inverse(plane_rb.rotation);
        var acceleration = (velocity - lastVelocity) / dt;
        localGForce = inverseRotation * (acceleration);
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

    Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float liftPower, AnimationCurve aoaCurve)
    {
        var liftVelocity = Vector3.ProjectOnPlane(localVelocity, rightAxis); // simulating the wind travelling over the wings of the plane
        var velocitySquared = liftVelocity.sqrMagnitude;

        // lift is calculated by velocity^2 * liftCoefficient * liftPower
        var liftCoefficient = aoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        var liftForce = velocitySquared * liftCoefficient * liftPower;

        // lift is perpendicular to velocity of plane
        var liftDirection = new Vector3(-localVelocity.y, localVelocity.x);
        var lift = liftDirection * liftForce;

        // calculating induced drag which is liftCoefficient^2 * inducedDrag 
        // inducedDrag is a hand tuned variable
        var dragForce = (liftCoefficient * liftCoefficient) * inducedDrag;
        var dragDirection = -liftVelocity.normalized;
        var inducedDragForce = dragDirection * velocitySquared * dragForce;

        return lift + inducedDragForce;
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
        var drag = dragCoefficient.magnitude * localVelocitySquared * -localVelocity; // drag is in the opposite direction of velocity

        plane_rb.AddRelativeForce(drag);

    }

    void UpdateLift()
    {
        var liftForce = CalculateLift(angleOfAttack, Vector3.right, liftPower, aoaCurve);

        plane_rb.AddRelativeForce(liftForce);
    }


    // to update the plane's physics
    void FixedUpdate()
    {
        Debug.Log("Velocity: " + localVelocity);
        float dt = Time.fixedDeltaTime;

        CalculateState(dt);
        CalculateAngleOfAttack();
        CalculateGForce(dt);

        // adding thrust
        plane_rb.AddRelativeForce(throttle * maxThrust * Vector3.forward);

        // adding drag
        UpdateDrag();

        // adding lift
        UpdateLift();
    }

    
}

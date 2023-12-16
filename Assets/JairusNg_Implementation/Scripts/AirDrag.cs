using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirDrag : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 localVelocity;
    [SerializeField] public float forwardDrag;
    [SerializeField] public float backwardsDrag;
    [SerializeField] public float rightDrag;
    [SerializeField] public float leftDrag;
    [SerializeField] public float topDrag;
    [SerializeField] public float bottomDrag;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate() // fixed update is used as it better represents unity's physics calculations
    {
        localVelocity = transform.InverseTransformDirection(rb.velocity);

        UpdateDrag();

    }

    public static Vector3 movementScale(Vector3 value, float posX, float negX, float posY, float negY, float posZ, float negZ) // gets the directional values of the object
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

    void UpdateDrag()
    {
        var lv = localVelocity; // local velocity is used as we want to apply drag based on the objects local direction
        var lvSquared = lv.sqrMagnitude; // formula of drag is 1/2 * v2 * drag coefficient

        var dragCoefficient = movementScale(lv.normalized, rightDrag, leftDrag, topDrag, bottomDrag, forwardDrag, backwardsDrag); // we translate our movement into the drag coefficient

        var drag = dragCoefficient.magnitude * lvSquared * -lv.normalized; // drag opposes movement 

        rb.AddRelativeForce(drag * Time.deltaTime);

    }
}

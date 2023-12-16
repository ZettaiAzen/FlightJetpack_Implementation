using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform pov;
    [SerializeField] Transform lookAt;
    [SerializeField] float speed;

    private Vector3 target;

    private void Update()
    {
        target = pov.position;
    }

    private void FixedUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * speed);
        transform.LookAt(lookAt);
    }
}

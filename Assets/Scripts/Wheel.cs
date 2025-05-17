using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    public Rigidbody vehicleBody;

    //Hit Detection
    RaycastHit hit;
    public LayerMask layerMask;
    public bool isGrounded;

    //Suspension
    public float restLength;
    public float wheelRadius;
    public float currentLength;
    float lastLength;
    public float springStiffness;
    public float damperStiffness;
    public Vector3 fZ;

    //Wheel Motion
    Vector3 linearVelocityLocal;
    [HideInInspector] public Vector3 angularVelocityLocal;
    Vector3 longitudinalDir;
    Vector3 lateralDir;

    //Friction
    public float throttle;
    public float uLong;
    public float uLat;
    public Vector3 simpleTireForce;

    void Start()
    {

    }

    void FixedUpdate()
    {
        if (Physics.Raycast(transform.position, -transform.up, out hit, restLength + wheelRadius, layerMask)) //Fire a raycast to get the distance between the toplink and the ground
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if(isGrounded) //If we hit something, calculate and apply the suspension and friction forces
        {
            currentLength = hit.distance - wheelRadius;
            CalculateSuspensionForce();
            ApplySuspensionForce();

            GetWheelMotionOnGround();
            GetSimpleTireForce();
            ApplySimpleTireForce();
        }
        else //If we don't, return the suspension to its resting length (we do not deal with overextended springs)
        {
            ResetValues();
        }
    }

    void CalculateSuspensionForce()
    {
        //Hooke's Law
        float springDisplacement = restLength - currentLength;
        float springForce = springDisplacement * springStiffness;

        //Damping Equation
        float springVelocity = (lastLength - currentLength) / Time.fixedDeltaTime;
        float damperForce = springVelocity * damperStiffness;

        float suspensionForce = springForce + damperForce;
        fZ = hit.normal.normalized * suspensionForce; //Suspension force acts perpendicular to the contact patch

        lastLength = currentLength; //Set the lastLength for the next frame
    }
    void ApplySuspensionForce()
    {
        vehicleBody.AddForceAtPosition(fZ, transform.position); //Apply the suspension force to the vehicle at the toplink position
    }

    void GetWheelMotionOnGround()
    {
        //Get the velocity of the wheel relative to the ground
        linearVelocityLocal = transform.InverseTransformDirection(vehicleBody.GetPointVelocity(hit.point));
        angularVelocityLocal = linearVelocityLocal / wheelRadius; // omega = v / r

        // lateral and longitudinal directions of motion of the wheel
        longitudinalDir = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
        lateralDir = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
    }

    void GetSimpleTireForce()
    {
        throttle = -Input.GetAxisRaw("Vertical"); //Make sure your vertical axis is defined in the input manager!

        Vector3 longitudinalTireForce = (throttle * uLong) * Mathf.Max(0.0f, fZ.y) * -longitudinalDir; // F_long = u * N * -longDir
        Vector3 lateralTireForce = (Mathf.Clamp(linearVelocityLocal.x, -1.0f, 1.0f) * uLat) * Mathf.Max(0.0f, fZ.y) * -lateralDir; //F_lat = u * N * -latDir
        simpleTireForce = longitudinalTireForce + lateralTireForce;
    }

    void ApplySimpleTireForce()
    {
        vehicleBody.AddForceAtPosition(simpleTireForce, hit.point); //apply the friction force at the wheel's contact patch
    }

    void ResetValues() //when in the air,
    {
        lastLength = currentLength = restLength; //fully extend suspension
        fZ = simpleTireForce = Vector3.zero; //set forces to zero
    }
}

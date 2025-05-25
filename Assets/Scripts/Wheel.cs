using System;
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
    public float motorTorque;
    float totalTorque;
    public float wheelInertia;
    public float wheelAngularVelocity;
    public Vector3 linearVelocityLocal;
    Vector3 angularVelocityLocal;
    Vector3 longitudinalDir;
    Vector3 lateralDir;

    //Lateral Friction
    float slipAngle;
    float muX;
    public Vector3 fX;

    //Longitudinal Friction
    float slipSpeed;
    float muY;
    public Vector3 fY;

    //Inputs
    public float throttleInput; //Temp; Until Drivetrain

    // //Deprecated
    // public float uLong;
    // public float uLat;
    // public Vector3 simpleTireForce;

    void Start()
    {

    }

    void FixedUpdate()
    {
        throttleInput = Input.GetAxisRaw("Vertical"); //Temp; Make sure your vertical axis is defined in the input manager!

        if (Physics.Raycast(transform.position, -transform.up, out hit, restLength + wheelRadius, layerMask)) //Fire a raycast to get the distance between the toplink and the ground
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded) //If we hit something, calculate and apply the suspension and friction forces
        {
            currentLength = hit.distance - wheelRadius;
            CalculateSuspensionForce();
            ApplySuspensionForce();

            GetWheelMotionOnGround();
            CalculateLateralFriction();
            CalculateLongitudinalFriction();
            ApplyFrictionForce();

            // GetSimpleTireForce();
            // ApplySimpleTireForce();
        }
        else //If we don't, reset the values that need resetting
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
        linearVelocityLocal = transform.InverseTransformDirection(vehicleBody.GetPointVelocity(hit.point)); //RB.GetPointVelocity Does Not Update w/ Substeps, If There's A Way To Get This Value Without The Use Of RB Functions, We Can Substep The Whole VP Implementation And Keep The Timestep @ 0.02
        angularVelocityLocal = linearVelocityLocal / wheelRadius; // omega = v / r

        // lateral and longitudinal directions of motion of the wheel
        longitudinalDir = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
        lateralDir = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
    }

    void CalculateLateralFriction()
    {
        //Calculate wheel slip (lateral)
        float slipAnglePeak = 8.0f; //Pre-Pacejka: Hard-Coded Peak Slip Angle, Should Be Equal To The Peak In Pacejka Curve

        slipAngle = 0.0f;
        if (linearVelocityLocal.z != 0.0f) //Prevent division by zero
        {
            slipAngle = Mathf.Atan(-linearVelocityLocal.x / Mathf.Abs(linearVelocityLocal.z)) * Mathf.Rad2Deg; //alpha = Arctan(V_lat / V_long)
        }

        //Map wheel slip to friction curve
        muX = MapRangeClamped(Mathf.Abs(slipAngle), 0.0f, slipAnglePeak, 0.0f, 1.0f) * Mathf.Sign(slipAngle);
    }

    void CalculateLongitudinalFriction()
    {
        //Integrate wheel angular velocity
        float driveTorque = throttleInput * motorTorque; //Explicitly define the drive torque; this will come from the drivetrain later
        float frictionTorque = (muY * Mathf.Max(fZ.y, 0.0f)) * wheelRadius; //T_f = F_f * R
        totalTorque = driveTorque - frictionTorque;

        float wheelAngularAcceleration = totalTorque / wheelInertia; //
        wheelAngularVelocity += wheelAngularAcceleration * Time.fixedDeltaTime;

        //Calculate wheel slip (longitudinal)
        float slipSpeedPeak = 4.0f; //Pre-Pacejka: Hard-Coded Peak Slip Speed, Should Be Equal To The Peak In Pacejka Curve
        slipSpeed = (wheelAngularVelocity * wheelRadius) - linearVelocityLocal.z; //slip = (omega * R_e) - V

        muY = MapRangeClamped(Mathf.Abs(slipSpeed), 0.0f, slipSpeedPeak, 0.0f, 1.0f) * Mathf.Sign(slipSpeed); //Pre-Pacejka
    }

    void ApplyFrictionForce()
    {
        fX = lateralDir * muX * Mathf.Max(fZ.y, 0.0f); //F_lat = u * N * -latDir
        fY = longitudinalDir * muY * Mathf.Max(fZ.y, 0.0f); // F_long = u * N * -longDir
        vehicleBody.AddForceAtPosition(fX + fY, hit.point); //apply the friction force at the wheel's contact patch
    }

    // void GetSimpleTireForce()
    // {
    //     throttle = -Input.GetAxisRaw("Vertical"); //Make sure your vertical axis is defined in the input manager!

    //     Vector3 longitudinalTireForce = (throttle * uLong) * Mathf.Max(0.0f, fZ.y) * -longitudinalDir; // F_long = u * N * -longDir
    //     Vector3 lateralTireForce = (Mathf.Clamp(linearVelocityLocal.x, -1.0f, 1.0f) * uLat) * Mathf.Max(0.0f, fZ.y) * -lateralDir; //F_lat = u * N * -latDir
    //     simpleTireForce = longitudinalTireForce + lateralTireForce;
    // }

    // void ApplySimpleTireForce()
    // {
    //     vehicleBody.AddForceAtPosition(simpleTireForce, hit.point); //apply the friction force at the wheel's contact patch
    // }

    void ResetValues() //when in the air,
    {
        lastLength = currentLength = restLength; //fully extend suspension

        slipAngle = slipSpeed = 0.0f; //set wheel slip to zero
        muX = muY = 0.0f; //set friction coefficients to zero
        fX = fY = fZ = Vector3.zero; //set forces to zero

        // fZ = simpleTireForce = Vector3.zero; //set forces to zero
    }
    
    float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB) //Maps a value from one range to another
    {
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }
}

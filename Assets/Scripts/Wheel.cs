using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    float deltaTime;
    public Rigidbody vehicleBody;

    [Header("Hit Detection - Inputs")]
    public LayerMask layerMask;
    [Header("Hit Detection - Outputs")]
    public bool isGrounded;
    RaycastHit hit;

    [Header("Suspension - Inputs")]
    public float restLength;
    public float springStiffness;
    public float damperStiffness;
    [Header("Suspension - Outputs")]
    public Vector3 fZ;
    public float currentLength;
    float lastLength;

    [Header("Wheel Motion - Inputs")]
    float driveTorque;
    public float wheelRadius;
    public float wheelInertia;
    [Header("Wheel Motion - Outputs")]
    public float wheelAngularVelocity;
    public Vector3 linearVelocityLocal;
    float totalTorque;
    Vector3 angularVelocityLocal;
    Vector3 longitudinalDir;
    Vector3 lateralDir;

    [Header("Friction - Outputs")]
    public Vector3 fX;
    public Vector3 fY;
    float slipAngle;
    float muX;
    float slipSpeed;
    float muY;

    //Inputs
    //float throttleInput; //Temp; Until Drivetrain

    // //Deprecated
    // public float uLong;
    // public float uLat;
    // public Vector3 simpleTireForce;

    public void UpdatePhysicsPre(float argDeltaTime)
    {
        // throttleInput = Input.GetAxisRaw("Vertical"); //Temp; Make sure your vertical axis is defined in the input manager!
        deltaTime = argDeltaTime;

        if (Physics.Raycast(transform.position, -transform.up, out hit, restLength + wheelRadius, layerMask)) //Fire a raycast to get the distance between the toplink and the ground
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded) //If we hit something,
        {
            //Calculate and apply the suspension force (Fz)
            currentLength = hit.distance - wheelRadius;
            CalculateSuspensionForce();
            ApplySuspensionForce();

            //Calculate the wheel's velocity and direction vectors
            GetWheelMotionOnGround();


            // GetSimpleTireForce();
            // ApplySimpleTireForce();
        }
        else //If we don't,
        {
            //Reset values that need resetting
            ResetValues();
        }
    }

    public void UpdatePhysicsDrivetrain(float argDeltaTime, float argDriveTorque)
    {
        deltaTime = argDeltaTime;
        driveTorque = argDriveTorque;

        if (isGrounded)
        {
            //Calculate the friction force (Fx, Fy)
            CalculateLateralFriction();
            CalculateLongitudinalFriction();
        }
        else
        {
            //Keep the wheel's ability to spin
            GetWheelMotionInAir();
        }
    }

    public void UpdatePhysicsPost()
    { 
        if (isGrounded)
        {
            //Apply the friction force (Fx, Fy)
            ApplyFrictionForce();
        }
    }

    void CalculateSuspensionForce()
    {
        //Hooke's Law
        float springDisplacement = restLength - currentLength;
        float springForce = springDisplacement * springStiffness;

        //Damping Equation
        float springVelocity = (lastLength - currentLength) / deltaTime;
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

        //Lateral and longitudinal directions of motion of the wheel
        longitudinalDir = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
        lateralDir = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
    }

    void CalculateLateralFriction()
    {
        //Calculate Wheel Slip (Lateral)
        float slipAnglePeak = 8.0f; //Pre-Pacejka: Hard-Coded Peak Slip Angle, Should Be Equal To The Peak In Pacejka Curve
        float lowSpeedSlipAngle = slipAnglePeak * Mathf.Sign(-linearVelocityLocal.x) * Mathf.Clamp01(Mathf.Abs(linearVelocityLocal.x)); //Ramp function that mimics slip angle formula at low speeds
        float highSpeedSlipAngle = 0.0f;
        if (linearVelocityLocal.z != 0.0f)
        {
            highSpeedSlipAngle = Mathf.Atan(-linearVelocityLocal.x / Mathf.Abs(linearVelocityLocal.z)) * Mathf.Rad2Deg;
        }
        slipAngle = Mathf.Lerp(lowSpeedSlipAngle, highSpeedSlipAngle, MapRangeClamped(linearVelocityLocal.magnitude, 3.0f, 6.0f, 0.0f, 1.0f)); //Transition Between Low And High Speed Friction Models Based Off Of Wheel Speed

        //Map Wheel Slip To Friction Curve
        muX = MapRangeClamped(Mathf.Abs(slipAngle), 0.0f, slipAnglePeak, 0.0f, 1.0f) * Mathf.Sign(slipAngle); //Pre-Pacejka
    }

    void CalculateLongitudinalFriction()
    {
        // int substeps = 5;
        // float subDT = deltaTime / (float)substeps;
        // for (int i = 0; i < substeps; i++) //Substep Friction And Wheel Accel For Stability (Total Steps = (Physics Rate * Iterations); @ 250 = Stable
        // {
        //     //Calculate Torque Acting On Wheel
        //     float driveTorque = throttleInput * motorTorque; //Temp, will come from drivetrain later
        //     float frictionTorque = muY * Mathf.Max(fZ.y, 0.0f) * wheelRadius;
        //     totalTorque = driveTorque - frictionTorque;

        //     //Integrate Angular Velocity
        //     float wheelAngularAcceleration = totalTorque / wheelInertia;
        //     wheelAngularVelocity += wheelAngularAcceleration * subDT;

        //     //Calculate Wheel Slip (Longitduinal)
        //     float slipSpeedPeak = 4.0f; //Pre-Pacejka: Hard-Coded Peak Slip Speed, Should Be Equal To The Peak In Pacejka Curve
        //     slipSpeed = wheelAngularVelocity - angularVelocityLocal.z;

        //     //Map Wheel Slip To Friction Curve
        //     muY = MapRangeClamped(Mathf.Abs(slipSpeed), 0.0f, slipSpeedPeak, 0.0f, 1.0f) * Mathf.Sign(slipSpeed); //Pre-Pacejka
        // }

        //Calculate Torque Acting On Wheel
        // float driveTorque = throttleInput * motorTorque; //Temp, will come from drivetrain later
        float frictionTorque = muY * Mathf.Max(fZ.y, 0.0f) * wheelRadius;
        totalTorque = driveTorque - frictionTorque;

        //Integrate Angular Velocity
        float wheelAngularAcceleration = totalTorque / wheelInertia;
        wheelAngularVelocity += wheelAngularAcceleration * deltaTime;

        //Calculate Wheel Slip (Longitduinal)
        float slipSpeedPeak = 4.0f; //Pre-Pacejka: Hard-Coded Peak Slip Speed, Should Be Equal To The Peak In Pacejka Curve
        slipSpeed = wheelAngularVelocity - angularVelocityLocal.z;

        //Map Wheel Slip To Friction Curve
        muY = MapRangeClamped(Mathf.Abs(slipSpeed), 0.0f, slipSpeedPeak, 0.0f, 1.0f) * Mathf.Sign(slipSpeed); //Pre-Pacejka
    }

    void ApplyFrictionForce()
    {
        fX = lateralDir * muX * Mathf.Max(fZ.y, 0.0f); //F_lat = u * N * -latDir
        fY = longitudinalDir * muY * Mathf.Max(fZ.y, 0.0f); // F_long = u * N * -longDir
        vehicleBody.AddForceAtPosition(fX + fY, hit.point); //Apply the friction force at the wheel's contact patch
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

    void ResetValues()
    {
        lastLength = currentLength = restLength; //Fully extend suspension

        slipAngle = slipSpeed = 0.0f; //Set wheel slip to zero
        muX = muY = 0.0f; //Set friction coefficients to zero
        fX = fY = fZ = Vector3.zero; //Set forces to zero

        // fZ = simpleTireForce = Vector3.zero; //Set forces to zero
    }

    void GetWheelMotionInAir()
    {
        // int substeps = 5;
        // float subDT = deltaTime / (float)substeps;
        // for (int i = 0; i < substeps; i++)
        // {
        //     float driveTorque = throttleInput * motorTorque; //Temp, will come from drivetrain later
        //     float totalTorque = driveTorque;

        //     float wheelAngularAcceleration = totalTorque / wheelInertia;
        //     wheelAngularVelocity += wheelAngularAcceleration * subDT;
        // }

        // float driveTorque = throttleInput * motorTorque; //Temp, will come from drivetrain later
        float totalTorque = driveTorque;

        float wheelAngularAcceleration = totalTorque / wheelInertia;
        wheelAngularVelocity += wheelAngularAcceleration * deltaTime;
    }

    float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB) //Maps a value from one range to another
    {
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }
}

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
    public float brakeTorque;
    public float handbrakeTorque;
    float brakeInput;
    float handbrakeInput;
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

    [Header("Friction - Inputs")]
    public float lateralRelaxationLength;
    public float longitudinalRelaxationLength;
    public float rollingResistanceCoeff;

    [Header("Friction - Outputs")]
    public Vector3 fX;
    public Vector3 fY;
    float slipAngle;
    float slipAngleDyn;
    float muX;
    float slipSpeed;
    float slipSpeedDyn;
    float muY;

    // //Deprecated
    // public float uLong;
    // public float uLat;
    // public Vector3 simpleTireForce;

    public void UpdatePhysicsPre(float argDeltaTime)
    {
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

    public void UpdatePhysicsDrivetrain(float argDeltaTime, float argDriveTorque, float argBrakeInput, float argHandbrakeInput)
    {
        deltaTime = argDeltaTime;
        driveTorque = argDriveTorque;
        brakeInput = argBrakeInput;
        handbrakeInput = argHandbrakeInput;

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
        linearVelocityLocal = transform.InverseTransformDirection(vehicleBody.GetPointVelocity(hit.point));
        angularVelocityLocal = linearVelocityLocal / wheelRadius; // omega = v / r

        //Lateral and longitudinal directions of motion of the wheel
        longitudinalDir = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
        lateralDir = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
    }

    void CalculateLateralFriction()
    {
        //Calculate Wheel Slip (Lateral)
        float slipAnglePeak = 8.0f; //Pre-Pacejka: Hard-Coded Peak Slip Angle, Should Be Equal To The Peak In Pacejka Curve
        float lowSpeedSlipAngle = slipAnglePeak * Mathf.Sign(-linearVelocityLocal.x) * Mathf.Clamp01(Mathf.Abs(linearVelocityLocal.x * 4.0f)); //Ramp function that mimics slip angle formula at low speeds
        float highSpeedSlipAngle = 0.0f;
        if (linearVelocityLocal.z != 0.0f) //Prevent division by zero
        {
            highSpeedSlipAngle = Mathf.Atan(-linearVelocityLocal.x / Mathf.Abs(linearVelocityLocal.z)) * Mathf.Rad2Deg;
        }
        slipAngle = Mathf.Lerp(lowSpeedSlipAngle, highSpeedSlipAngle, MapRangeClamped(linearVelocityLocal.magnitude, 3.0f, 6.0f, 0.0f, 1.0f)); //Transition Between Low And High Speed Friction Models Based Off Of Wheel Speed

        //Transient Behavior For Friction Model (Lateral)
        float transientX = (Mathf.Abs(angularVelocityLocal.x) / lateralRelaxationLength) * deltaTime;
        transientX = Mathf.Clamp(transientX, -1.0f, 1.0f); //Important, prevents absurd values
        slipAngleDyn += (slipAngle - slipAngleDyn) * transientX;

        //Map Wheel Slip To Friction Curve
        muX = MapRangeClamped(Mathf.Abs(slipAngleDyn), 0.0f, slipAnglePeak, 0.0f, 1.0f) * Mathf.Sign(slipAngleDyn); //Pre-Pacejka
    }

    void CalculateLongitudinalFriction()
    {
        //Calculate Torque Acting On Wheel
        float frictionTorque = muY * Mathf.Max(fZ.y, 0.0f) * wheelRadius;
        float brakingTorque = (brakeTorque * brakeInput) + (handbrakeTorque * handbrakeInput);
        float rollingResistanceTorque = rollingResistanceCoeff * Mathf.Max(fZ.y, 0.0f) * wheelRadius;
        float resistiveTorques = Mathf.Min(Mathf.Abs(brakingTorque + rollingResistanceTorque), Mathf.Abs((wheelAngularVelocity / deltaTime) * wheelInertia)) * Mathf.Sign(wheelAngularVelocity); //Fixes brakes overshooting and spinning the wheel in the opposite direction
        totalTorque = driveTorque - frictionTorque - resistiveTorques;

        //Integrate Angular Velocity
        float wheelAngularAcceleration = totalTorque / wheelInertia;
        wheelAngularVelocity += wheelAngularAcceleration * deltaTime;

        //Calculate Wheel Slip (Longitduinal)
        float slipSpeedPeak = 4.0f; //Pre-Pacejka: Hard-Coded Peak Slip Speed, Should Be Equal To The Peak In Pacejka Curve
        float slipSpeedLow = Mathf.Sign(wheelAngularVelocity - angularVelocityLocal.z) * slipSpeedPeak * Mathf.Clamp01(Mathf.Abs(linearVelocityLocal.z * 4.0f)); //Ramp function that mimics slip speed formula at low speeds
        float slipSpeedHigh = wheelAngularVelocity - angularVelocityLocal.z;
        slipSpeed = Mathf.Lerp(slipSpeedLow, slipSpeedHigh, MapRangeClamped(Mathf.Abs(wheelAngularVelocity), 3.0f, 6.0f, 0.0f, 1.0f));
        
        //Transient Behavior For Friction Model (Longitduinal)
        float transientY = (Mathf.Abs(slipSpeed) / longitudinalRelaxationLength) * deltaTime;
        transientY = Mathf.Clamp(transientY, -1.0f, 1.0f); //Important, prevents absurd values
        slipSpeedDyn += (slipSpeed - slipSpeedDyn) * transientY;

        //Map Wheel Slip To Friction Curve
        muY = MapRangeClamped(Mathf.Abs(slipSpeedDyn), 0.0f, slipSpeedPeak, 0.0f, 1.0f) * Mathf.Sign(slipSpeedDyn); //Pre-Pacejka
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
        //Calculate Torque Acting On Wheel
        float brakingTorque = (brakeTorque * brakeInput) + (handbrakeTorque * handbrakeInput);
        float resistiveTorques = Mathf.Min(Mathf.Abs(brakingTorque), Mathf.Abs((wheelAngularVelocity / deltaTime) * wheelInertia)) * Mathf.Sign(wheelAngularVelocity);
        totalTorque = driveTorque - resistiveTorques;

        //Integrate Angular Velocity
        float wheelAngularAcceleration = totalTorque / wheelInertia;
        wheelAngularVelocity += wheelAngularAcceleration * deltaTime;
    }

    float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB) //Maps a value from one range to another
    {
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }
}

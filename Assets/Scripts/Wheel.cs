using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    float dt;
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
    // public float brakeTorque;
    // public float handbrakeTorque;
    float totalTorque;
    public float wheelInertia;
    public float wheelAngularVelocity;
    public Vector3 linearVelocityLocal;
    Vector3 angularVelocityLocal;
    Vector3 longitudinalDir;
    Vector3 lateralDir;

    //Lateral Friction
    float slipAngle;
    // float slipAngleDyn;
    // public float lateralRelaxationLength;
    float muX;
    public Vector3 fX;

    //Longitudinal Friction
    float slipSpeed;
    // float slipSpeedDyn;
    // public float longitudinalRelaxationLength;
    float muY;
    public Vector3 fY;

    //Inputs
    public float throttleInput; //Temp; Until Drivetrain
    // public float brakeInput;
    // public float handbrakeInput;

    // //Deprecated
    // public float uLong;
    // public float uLat;
    // public Vector3 simpleTireForce;

    void Start()
    {

    }

    void FixedUpdate()
    {
        dt = Time.fixedDeltaTime;

        throttleInput = Input.GetAxisRaw("Vertical"); //Temp; Make sure your vertical axis is defined in the input manager!
        // throttleInput = Mathf.Max(Input.GetAxisRaw("Vertical"), 0.0f); 
        // brakeInput = Mathf.Min(Input.GetAxisRaw("Vertical"), 0.0f);
        // handbrakeInput = System.Convert.ToSingle(Input.GetKey(KeyCode.Space));

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
        else //If we don't, return the suspension to its resting length (we do not deal with overextended springs)
        {
            GetWheelMotionInAir();
            ResetValues();
        }
    }

    void CalculateSuspensionForce()
    {
        //Hooke's Law
        float springDisplacement = restLength - currentLength;
        float springForce = springDisplacement * springStiffness;

        //Damping Equation
        float springVelocity = (lastLength - currentLength) / dt;
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
        //Calculate Wheel Slip (Lateral)
        float slipAnglePeak = 8.0f; //Pre-Pacejka: Hard-Coded Peak Slip Angle, Should Be Equal To The Peak In Pacejka Curve
        float lowSpeedSlipAngle = slipAnglePeak * Mathf.Sign(linearVelocityLocal.x) * Mathf.Clamp01(Mathf.Abs(linearVelocityLocal.x)); // Multiply By Clamped01 lateral velocity for low physics rates
        float highSpeedSlipAngle = 0.0f;
        if (linearVelocityLocal.z != 0.0f)
        {
            highSpeedSlipAngle = Mathf.Atan(linearVelocityLocal.x / Mathf.Abs(linearVelocityLocal.z)) * Mathf.Rad2Deg;
        }
        slipAngle = Mathf.Lerp(lowSpeedSlipAngle, highSpeedSlipAngle, MapRangeClamped(linearVelocityLocal.magnitude, 3.0f, 6.0f, 0.0f, 1.0f)); //Transition Between Low And High Speed Friction Models Based Off Of Wheel Speed

        // //Transient Behavior For Friction Model (Requires Higher Physics Rate And Removal Of Mult. By Clamped01 At lowSpeedSlipAngle)
        // float transientX = (Mathf.Abs(angularVelocityLocal.x) / lateralRelaxationLength) * dt;
        // transientX = Mathf.Clamp(transientX, -1.0f, 1.0f); //Important
        // slipAngleDyn += (slipAngle - slipAngleDyn) * transientX;

        //Map Wheel Slip To Friction Curve
        muX = MapRangeClamped(Mathf.Abs(slipAngle), 0.0f, slipAnglePeak, 0.0f, 1.0f) * Mathf.Sign(slipAngle); //Pre-Pacejka
        // muX = EvaluatePacejka(slipAngleDyn * Mathf.Deg2Rad, B, C, D, E); //Pacejka
    }

    void CalculateLongitudinalFriction()
    {
        int substeps = 50;
        float subDT = dt / (float)substeps;
        for (int i = 0; i < substeps; i++) //Substep Friction And Wheel Accel For Stability (Total Steps = (Physics Rate * Iterations); @ 150 = Stable Friction; Increase Total Steps Further To Decrease The Remaining Wheel Angular Velocity The Wheels Have When They Lock Up)
        {
            //Calculate Torque Acting On Wheel
            float driveTorque = throttleInput * motorTorque; //Temp, will come from drivetrain later
            float frictionTorque = muY * Mathf.Max(fZ.y, 0.0f) * wheelRadius;
            // float brakingTorque = (brakeTorque * brakeInput) + (handbrakeTorque * handbrakeInput);
            // float rollingResistanceCoeff = 0.1f; //Increase Rolling Resistance Coeff At Very Very Low Angular Velocities To Mask Long Friction Oscillations At Very Low Speeds
            // float rollingResistanceTorque = rollingResistanceCoeff * Mathf.Max(fZ.y, 0.0f) * wheelRadius;
            // float resistiveTorques = Mathf.Min(Mathf.Abs(brakingTorque + rollingResistanceTorque), Mathf.Abs((wheelAngularVelocity / subDT) * wheelInertia)) * Mathf.Sign(wheelAngularVelocity);
            // totalTorque = driveTorque - frictionTorque - resistiveTorques;
            totalTorque = driveTorque - frictionTorque;

            //Integrate Angular Velocity
            float wheelAngularAcceleration = totalTorque / wheelInertia;
            wheelAngularVelocity += wheelAngularAcceleration * subDT;

            //Calculate Wheel Slip (Longitduinal)
            float slipSpeedPeak = 4.0f; //Pre-Pacejka: Hard-Coded Peak Slip Speed, Should Be Equal To The Peak In Pacejka Curve
            slipSpeed = wheelAngularVelocity - angularVelocityLocal.z;
            // float slipSpeedLow = Mathf.Sign(wheelAngularVelocity - angularVelocityLocal.z) * slipSpeedPeak;
            // float slipSpeedHigh = wheelAngularVelocity - angularVelocityLocal.z;
            // slipSpeed = Mathf.Lerp(slipSpeedLow, slipSpeedHigh, MapRangeClamped(Mathf.Abs(angularVelocityLocal.magnitude), 3.0f, 6.0f, 0.0f, 1.0f));

            // //Transient Behavior For Friction Model (Requires Higher Physics Rate & Low-High Speed Slip)
            // float transientY = (Mathf.Abs(slipSpeed) / longitudinalRelaxationLength) * subDT;
            // transientY = Mathf.Clamp(transientY, -1.0f, 1.0f); //Important
            // slipSpeedDyn += (slipSpeed - slipSpeedDyn) * transientY;

            //Map Wheel Slip To Friction Curve
            muY = MapRangeClamped(Mathf.Abs(slipSpeed), 0.0f, slipSpeedPeak, 0.0f, 1.0f) * Mathf.Sign(slipSpeed); //Pre-Pacejka
            // muY = EvaluatePacejka(slipRatioDyn, B, C, D, E); //Pacejka
        }
    }

    void ApplyFrictionForce()
    {
        fX = -lateralDir * muX * Mathf.Max(fZ.y, 0.0f); // F_long = u * N * -longDir
        fY = longitudinalDir * muY * Mathf.Max(fZ.y, 0.0f); //F_lat = u * N * -latDir
        vehicleBody.AddForceAtPosition(fX + fY, hit.point); //apply the friction force at the wheel's contact patch
    }

    void GetWheelMotionInAir()
    {
        int substeps = 50;
        float subDT = dt / (float)substeps;
        for (int i = 0; i < substeps; i++)
        {
            float driveTorque = throttleInput * motorTorque; //Temp, will come from drivetrain later
            // float brakingTorque = Mathf.Min(Mathf.Abs((brakeTorque * brakeInput) + (handBrakeTorque * handbrakeInput)), Mathf.Abs((wheelAngularVelocity / subDT) * wheelInertia)) * Mathf.Sign(wheelAngularVelocity);
            // float totalTorque = driveTorque - brakingTorque;
            float totalTorque = driveTorque;

            float wheelAngularAcceleration = totalTorque / wheelInertia;
            wheelAngularVelocity += wheelAngularAcceleration * subDT;   
        }
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
    
    float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB)
    {
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }
}

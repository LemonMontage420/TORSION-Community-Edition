using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
    [Header("Inputs")]
    // public float starterInput;
    // float throttleInput;
    public float throttle;

    [Header("Idler Circuit")]
    [Range(0.0f, 1.0f)]
    public float idleThrottle;
    [Range(0.0f, 1.0f)]
    public float throttleStability;
    public float idleRPM;
    float idler;

    [Header("Rev Limiter")]
    public float redlineRPM;
    public float throttleCutoffDuration;
    bool throttleCut;
    float instability;
    float timer;

    [Header("Engine Parameters")]
    public AnimationCurve torqueCurve;
    public float starterTorque;
    public float initialFrictionTorque;
    public float frictionLossCoefficient;
    public float inertia;

    [Header("Outputs")]
    public float netTorque;
    [HideInInspector] public float angularVelocity;
    public float engineRPM;

    public void Initialize()
    {
        //Set engine to idle
        angularVelocity = RPM2Rads(idleRPM);
        engineRPM = Rads2RPM(angularVelocity);
    }

    public void UpdatePhysics(float argDeltaTime, float throttleInput, float starterInput, float argLoadTorque)
    {
        // float throttleSensitivity = 10.0f; //Change how responsive the throttle is

        //Player input
        // throttleInput = Mathf.MoveTowards(throttleInput, Mathf.Max(Input.GetAxisRaw("Vertical"), 0.0f), Time.fixedDeltaTime * throttleSensitivity); //Make sure your vertical axis is defined in the input manager!
        // starterInput = System.Convert.ToSingle(Input.GetKey(KeyCode.K));

        //Idler Circuit
        idler = MapRangeClamped(engineRPM, idleRPM - 200.0f, idleRPM + 200.0f, idleThrottle, 0.0f);
        timer += argDeltaTime;
        if (timer >= 0.1f) //Update instability every tenth of a second
        {
            timer = 0.0f;
            instability = Random.Range(throttleStability, 1.0f);
        }
        idler *= instability; //Add throttle instability

        //Rev Limiter
        if (engineRPM >= redlineRPM && !throttleCut)
        {
            StartCoroutine(ThrottleCutoff());
        }

        //Combine player input, idler circuit, and rev limiter into final throttle value
        if (!throttleCut)
        {
            throttle = Mathf.MoveTowards(throttle, Mathf.Max(throttleInput, idler), argDeltaTime * 10.0f);
        }
        else
        {
            throttle = Mathf.MoveTowards(throttle, 0.0f, argDeltaTime * 10.0f);
        }

        //Calculate engine torque
        float startingTorque = starterInput * starterTorque;
        float grossTorque = torqueCurve.Evaluate(engineRPM) * throttle; //Evaluate the engine's current gross torque output based off the current RPM and throttle input
        float frictionLosses = Mathf.Min(Mathf.Abs(initialFrictionTorque + (engineRPM * frictionLossCoefficient)), Mathf.Abs((angularVelocity / argDeltaTime) * inertia)) * Mathf.Sign(angularVelocity); //loss = constant + (linear * RPM)
        netTorque = (grossTorque + startingTorque) - frictionLosses - argLoadTorque;

        //Integrate engine speed
        float angularAcceleration = netTorque / inertia; //Newton's 2nd law of motion
        angularVelocity += angularAcceleration * argDeltaTime; //Newton's 1st equation of motion
        engineRPM = Rads2RPM(angularVelocity);
    }

    IEnumerator ThrottleCutoff()
    {
        throttleCut = true;
        yield return new WaitForSeconds(throttleCutoffDuration);
        throttleCut = false;
    }

    float Rads2RPM(float rads) //Rad/s -> RPM
    {
        float rpm = rads * (60.0f / (Mathf.PI * 2.0f));
        return rpm;
    }
    float RPM2Rads(float rpm) //RPM -> Rad/s
    {
        float rads = rpm * ((Mathf.PI * 2.0f) / 60.0f);
        return rads;
    }
    float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB) //Maps a value from one range to another
    {
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }
}
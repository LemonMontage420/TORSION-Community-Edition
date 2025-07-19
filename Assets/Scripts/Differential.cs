using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Differential : MonoBehaviour
{
    public float finalDriveRatio;

    public Vector2 GetDownstreamTorque(float argTorqueIn) //Open Differential; Uncomment once drivetrain is complete
    {
        return new Vector2(argTorqueIn * finalDriveRatio * 0.5f, argTorqueIn * finalDriveRatio * 0.5f);
    }

    public float GetUpstreamAngularVelocity(Vector2 argAngularVelocityIn) //Uncomment once drivetrain is complete
    {
        return (argAngularVelocityIn.x + argAngularVelocityIn.y) * finalDriveRatio * 0.5f;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour
{
    Wheel wheel;
    Steering steering;
    float wheelRot;

    public void Initialize(Wheel argWheel, Steering argSteering)
    {
        wheel = argWheel;
        steering = argSteering;
    }

    public void UpdateVisuals(float argDeltaTime)
    {
        //Lateral = Wheel Spacers (X); Vertical = Suspension Motion (Y); Longitudinal = Unused (Z)
        transform.localPosition = new Vector3(transform.localPosition.x, wheel.transform.localPosition.y - wheel.currentLength, wheel.transform.localPosition.z);

        //Integrate Wheel Rotation
        wheelRot += wheel.wheelAngularVelocity * Mathf.Rad2Deg * argDeltaTime;
        if (Mathf.Abs(wheelRot) > 360.0f) //Prevent from reaching absurd values
        {
            wheelRot -= 360.0f * Mathf.Sign(wheelRot);
        }

        //Roll = Tire Roll (X); Yaw = Steering (Y); Pitch = Camber (Z) NOTE: If you want to have non-zero camber, place an empty at the wheel's origin, parent the wheel to it, and rotate the empty on the Z
        transform.localRotation = Quaternion.Euler(new Vector3(wheelRot, steering.steerAngle, 0.0f)); //IMPORTANT: 0.0 fixes gimbal lock
    }
}

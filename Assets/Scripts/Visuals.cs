using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour
{
    public Wheel wheel;
    public Steering steering;
    float wheelRot;

    void Start()
    {
        
    }

    void LateUpdate()
    {
        //Lateral = Wheel Spacers (X); Vertical = Suspension Motion (Y); Longitudinal = Unused (Z)
        transform.localPosition = new Vector3(transform.localPosition.x, wheel.transform.localPosition.y - wheel.currentLength, wheel.transform.localPosition.z);

        wheelRot += wheel.angularVelocityLocal.z * Mathf.Rad2Deg * Time.deltaTime;
        if(Mathf.Abs(wheelRot) > 360.0f) //Prevent the value from reaching absurd values
        {
            wheelRot -= 360.0f * Mathf.Sign(wheelRot);
        }
        //Roll = Tire Roll (X); Yaw = Steering (Y); Pitch = Camber (Z) NOTE: If you want to have non-zero camber, parent an empty to the wheel located at the wheel's origin and rotate that
        transform.localRotation = Quaternion.Euler(new Vector3(wheelRot, steering.steerAngle, 0.0f)); //0.0 IMPORTANT!!! fixes gimbal lock
    }
}

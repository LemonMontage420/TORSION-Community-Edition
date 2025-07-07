using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clutch : MonoBehaviour
{
    public float clutchTorqueCapacity;
    public float clutchStiffness;
    [Range(0.0f, 1.0f)]
    public float clutchDamping;
    
    bool inGear;
    float clutchInput;
    public float clutchEngagement;
    float velocityEngine;
    float velocityTransmission;
    public float slip;
    public float clutchTorque;

    void FixedUpdate()
    {
        //Calculate engagement
        float clutchSensitivity = 5.0f;
        clutchInput = Mathf.MoveTowards(clutchInput, System.Convert.ToSingle(Input.GetKey(KeyCode.X)), Time.fixedDeltaTime * clutchSensitivity);
        clutchEngagement = 1.0f - clutchInput;

        //Calculate slip
        if (inGear)
        {
            slip = velocityEngine - velocityTransmission;
        }
        else
        {
            slip = 0.0f;
        }

        //Calculate torque
        float torque = clutchEngagement * slip * clutchStiffness; //tau = omega * k
        clutchTorque += (torque - clutchTorque) * clutchDamping; //Damping
        clutchTorque = Mathf.Clamp(clutchTorque, -clutchTorqueCapacity, clutchTorqueCapacity); //Make sure torque capacity isn't exceeded
    }
}
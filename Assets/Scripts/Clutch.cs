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
    public float clutchEngagement;
    float velocityEngine;
    float velocityTransmission;
    public float slip;
    public float clutchTorque;

    public void UpdatePhysics(float clutchInput, bool argInGear, float argVelocityIn, float argVelocityOut)
    {
        inGear = argInGear;
        clutchEngagement = 1.0f - clutchInput;
        velocityEngine = argVelocityIn;
        velocityTransmission = argVelocityOut;

        // //Calculate engagement
        // float clutchSensitivity = 5.0f;
        // clutchInput = Mathf.MoveTowards(clutchInput, System.Convert.ToSingle(Input.GetKey(KeyCode.X)), Time.fixedDeltaTime * clutchSensitivity);

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
        clutchTorque = Mathf.Clamp(clutchTorque, -clutchTorqueCapacity, clutchTorqueCapacity); //Make sure it doesn't exceed the torque capacity of the clutch
    }
}
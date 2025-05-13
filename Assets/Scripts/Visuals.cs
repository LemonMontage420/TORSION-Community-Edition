using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour
{
    public Wheel wheel;
    // public Steering steering;
    float wheelRot;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, wheel.transform.localPosition.y - wheel.currentLength, wheel.transform.localPosition.z);
        // transform.localRotation = Quaternion.Euler(new Vector3(transform.localEulerAngles.x, steering.steerAngle, transform.localEulerAngles.z));
        
        // wheelRot += (wheel.linearVelocityLocal.z / wheel.wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;
        // if(Mathf.Abs(wheelRot) > 360.0f)
        // {
        //     wheelRot -= 360.0f * Mathf.Sign(wheelRot);
        // }
        // transform.localRotation = Quaternion.Euler(new Vector3(wheelRot, steering.steerAngle, 0.0f)); //0.0 IMPORTANT!!! fixes gimbal lock
    }
}

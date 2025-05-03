using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Steering : MonoBehaviour
{
    public enum SteeringBehavior {Left, Right, Disabled};
    public SteeringBehavior steeringBehavior;

    public float steeringInput;
    public float wheelbase;
    public float rearTrackLength;
    public float turningRadius;
    public float steerAngle;
    void FixedUpdate()
    {
        float input = Input.GetAxisRaw("Horizontal");
        steeringInput = Mathf.MoveTowards(steeringInput, input, Time.fixedDeltaTime * 10.0f);

        float inner = Mathf.Atan(wheelbase / (turningRadius + (rearTrackLength / 2.0f))) * Mathf.Rad2Deg * steeringInput;
        float outer = Mathf.Atan(wheelbase / (turningRadius - (rearTrackLength / 2.0f))) * Mathf.Rad2Deg * steeringInput;

        steerAngle = 0.0f;
        if(steeringBehavior != SteeringBehavior.Disabled)
        {
            if(steeringInput > 0.0f) //Turning Right
            {
                if(steeringBehavior == SteeringBehavior.Left)
                {
                    steerAngle = inner;
                }
                if(steeringBehavior == SteeringBehavior.Right)
                {
                    steerAngle = outer;
                }
            }
            if(steeringInput < 0.0f) //Turning Left
            {
                if(steeringBehavior == SteeringBehavior.Left)
                {
                    steerAngle = outer;
                }
                if(steeringBehavior == SteeringBehavior.Right)
                {
                    steerAngle = inner;
                }
            }
        }

        transform.localRotation = Quaternion.Euler(new Vector3(transform.localEulerAngles.x, steerAngle, transform.localEulerAngles.z));
    }
}

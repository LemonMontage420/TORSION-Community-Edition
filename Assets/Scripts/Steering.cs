using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Steering : MonoBehaviour
{
    public enum SteeringBehavior { Left, Right, Disabled };

    [Header("Inputs")]
    public SteeringBehavior steeringBehavior;
    public float steeringInput;
    public float wheelbase; //Distance between front and rear wheels
    public float rearTrackLength; //Distance between the left and right rear wheels
    public float turningRadius; //Search up online or set to your preference to control max steering angle

    [Header("Outputs")]
    public float steerAngle;

    void FixedUpdate()
    {
        //Calulate Steering Input
        float input = Input.GetAxisRaw("Horizontal"); //Make sure your horizontal axis is defined in the input manager!
        float steeringSpeed = 10.0f;
        steeringInput = Mathf.MoveTowards(steeringInput, input, Time.fixedDeltaTime * steeringSpeed); //Smooth out the raw input data

        //Ackermann Equations
        float inner = Mathf.Atan(wheelbase / (turningRadius + (rearTrackLength / 2.0f))) * Mathf.Rad2Deg * steeringInput;
        float outer = Mathf.Atan(wheelbase / (turningRadius - (rearTrackLength / 2.0f))) * Mathf.Rad2Deg * steeringInput;

        steerAngle = 0.0f;
        if(steeringBehavior != SteeringBehavior.Disabled)
        {
            if(steeringInput > 0.0f) //Turning Right
            {
                if(steeringBehavior == SteeringBehavior.Left) //Left wheel
                {
                    steerAngle = inner;
                }
                if(steeringBehavior == SteeringBehavior.Right) //Right wheel
                {
                    steerAngle = outer;
                }
            }
            if(steeringInput < 0.0f) //Turning Left
            {
                if(steeringBehavior == SteeringBehavior.Left) //Left wheel
                {
                    steerAngle = outer;
                }
                if(steeringBehavior == SteeringBehavior.Right) //Right wheel
                {
                    steerAngle = inner;
                }
            }
        }

        // IMPORTANT FOR LATERAL FRICTION!! Set the toplink's rotation accordingly; Allows there to be a lateral velocity at the contact patch
        transform.localRotation = Quaternion.Euler(new Vector3(transform.localEulerAngles.x, steerAngle, transform.localEulerAngles.z));
    }
}

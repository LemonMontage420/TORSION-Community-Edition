using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Steering : MonoBehaviour
{
    public enum SteeringBehavior { Left, Right, Disabled };

    [Header("Inputs")]
    public SteeringBehavior steeringBehavior;
    float wheelbase; //Distance between front and rear wheels
    float rearTrackLength; //Distance between the left and right rear wheels
    float turningRadius; //Search up online or set to your preference to control max steering angle

    [Header("Outputs")]
    public float steerAngle;

    public void Initialize(float argWheelBase, float argRearTrackLength, float argTurningRadius)
    {
        wheelbase = argWheelBase;
        rearTrackLength = argRearTrackLength;
        turningRadius = argTurningRadius;
    }

    public void UpdatePhysics(float steeringInput)
    {
        //Calulate Steering Input
        // steeringInput = Mathf.MoveTowards(steeringInput, input, Time.fixedDeltaTime * steeringSpeed); //Smooth out the raw input data

        //Ackermann Equations
        float inner = Mathf.Atan(wheelbase / (turningRadius + (rearTrackLength / 2.0f))) * Mathf.Rad2Deg * steeringInput;
        float outer = Mathf.Atan(wheelbase / (turningRadius - (rearTrackLength / 2.0f))) * Mathf.Rad2Deg * steeringInput;

        steerAngle = 0.0f;
        if (steeringBehavior != SteeringBehavior.Disabled)
        {
            if (steeringInput > 0.0f) //Turning Right
            {
                if (steeringBehavior == SteeringBehavior.Left) //Left wheel
                {
                    steerAngle = inner;
                }
                if (steeringBehavior == SteeringBehavior.Right) //Right wheel
                {
                    steerAngle = outer;
                }
            }
            if (steeringInput < 0.0f) //Turning Left
            {
                if (steeringBehavior == SteeringBehavior.Left) //Left wheel
                {
                    steerAngle = outer;
                }
                if (steeringBehavior == SteeringBehavior.Right) //Right wheel
                {
                    steerAngle = inner;
                }
            }
        }

        // IMPORTANT FOR LATERAL FRICTION!! Set the toplink's rotation accordingly; Allows there to be a lateral velocity at the contact patch
        transform.localRotation = Quaternion.Euler(new Vector3(transform.localEulerAngles.x, steerAngle, transform.localEulerAngles.z));
    }
}

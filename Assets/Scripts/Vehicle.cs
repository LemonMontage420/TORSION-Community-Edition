using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public Wheel[] wheels;

    void Start()
    {
        Debug.Log("I'm A Vehicle! " + transform.name);

        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].Initialize();
        }
    }

    void Update()
    {
        
    }
}

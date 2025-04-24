using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visuals : MonoBehaviour
{
    public Wheel wheel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, wheel.transform.localPosition.y - wheel.currentLength, wheel.transform.localPosition.z);
    }
}

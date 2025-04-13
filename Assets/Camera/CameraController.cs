using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    bool newZoomInput;
    bool newOrbitInput;
    [System.Serializable]
    public struct CameraPositions
    {
        public Transform camPos;

        public bool canZoom;
        public Vector2 zoomRange;
        public float defaultZoomDistance;
        public bool canOrbit;
        public bool isGlobal;
        [Range(-90, 0)]
        public float verticalOrbitRangeMin;
        [Range(0, 90)]
        public float verticalOrbitRangeMax;
    }
    public CameraPositions[] cameraPositions;
    int currentTarget;
    public Transform orbitTarget;
    public float orbitSpeed;
    public float zoomSpeed;
    private Vector2 currentMousePos;
    private Vector2 lastMousePos;
    private Vector3 orbitOriginRotation;

    // public Transform globalOrbitTarget;
    
    // public bool globalOrbit;
    
    // public float maxZoomDistance;
    // [Range(-90, 0)]
    // public float verticalOrbitRangeMin;
    // [Range(0, 90)]
    // public float verticalOrbitRangeMax;
    

    
    // Start is called before the first frame update
    void Awake()
    {
        orbitOriginRotation = Vector3.zero;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(Input.GetKeyDown(KeyCode.V))
        {
            currentTarget++;
            newZoomInput = false;
            newOrbitInput = false;
        }
        if(currentTarget >= cameraPositions.Length)
        {
            currentTarget = 0;
        }

        //Orbiting
        lastMousePos = currentMousePos;
        currentMousePos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        float mouseVelX = (currentMousePos.x - lastMousePos.x) / Time.deltaTime;
        float mouseVelY = (currentMousePos.y - lastMousePos.y) / Time.deltaTime;

        if(cameraPositions[currentTarget].canOrbit)
        {
            Vector3 futureOrbitOriginRotation = orbitOriginRotation;
            if (Input.GetMouseButton(0))
            {
                futureOrbitOriginRotation.y += mouseVelX * orbitSpeed * Time.deltaTime;
                futureOrbitOriginRotation.x -= mouseVelY * orbitSpeed * Time.deltaTime;
                futureOrbitOriginRotation.x = Mathf.Clamp(futureOrbitOriginRotation.x, cameraPositions[currentTarget].verticalOrbitRangeMin, cameraPositions[currentTarget].verticalOrbitRangeMax);
                newOrbitInput = true;
            }
            orbitOriginRotation = futureOrbitOriginRotation;
        }
        if (cameraPositions[currentTarget].isGlobal)
        {
            cameraPositions[currentTarget].camPos.rotation = Quaternion.identity;
        }

        //Zooming
        float zoomPos = transform.localPosition.z;
        if(cameraPositions[currentTarget].canZoom)
        {
            //Zoom Out
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                zoomPos -= zoomSpeed * Time.deltaTime;
                if(zoomPos <= cameraPositions[currentTarget].zoomRange.x)
                {
                    zoomPos = cameraPositions[currentTarget].zoomRange.x;
                }
                newZoomInput = true;
            }
            //Zoom In
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                zoomPos += zoomSpeed * Time.deltaTime;
                if(zoomPos >= cameraPositions[currentTarget].zoomRange.y)
                {
                    zoomPos = cameraPositions[currentTarget].zoomRange.y;
                }
                newZoomInput = true;
            }
        }

        orbitTarget.parent = cameraPositions[currentTarget].camPos;
        orbitTarget.position = Vector3.Lerp(orbitTarget.position, cameraPositions[currentTarget].camPos.position, Time.deltaTime * 20.0f);
        if(!newOrbitInput | !cameraPositions[currentTarget].canOrbit)
        {
            orbitTarget.rotation = Quaternion.Slerp(orbitTarget.rotation, cameraPositions[currentTarget].camPos.rotation, Time.deltaTime * 20.0f);
        }
        else
        {
            orbitTarget.localRotation = Quaternion.Slerp(orbitTarget.localRotation, Quaternion.Euler(orbitOriginRotation), Time.deltaTime * 20.0f);
        }
        if(!newZoomInput | !cameraPositions[currentTarget].canZoom)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(0.0f, 0.0f, cameraPositions[currentTarget].defaultZoomDistance), Time.deltaTime * 20.0f);
        }
        else
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, zoomPos);
        }
        


        // //Orbiting
        // lastMousePos = currentMousePos;
        // currentMousePos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        // float mouseVelX = (currentMousePos.x - lastMousePos.x) / Time.deltaTime;
        // float mouseVelY = (currentMousePos.y - lastMousePos.y) / Time.deltaTime;

        // Vector3 futureOrbitOriginRotation = orbitOriginRotation;
        // if (Input.GetMouseButton(0))
        // {
        //     futureOrbitOriginRotation.y += mouseVelX * orbitSpeed * Time.deltaTime;
        //     futureOrbitOriginRotation.x -= mouseVelY * orbitSpeed * Time.deltaTime;
        //     futureOrbitOriginRotation.x = Mathf.Clamp(futureOrbitOriginRotation.x, verticalOrbitRangeMin, verticalOrbitRangeMax);
        // }
        // orbitOriginRotation = futureOrbitOriginRotation;
        // orbitTarget.localRotation = Quaternion.Euler(orbitOriginRotation);

        // if (globalOrbit)
        // {
        //     globalOrbitTarget.rotation = Quaternion.identity;
        // }


        // //Zooming
        // Vector3 camDir = (orbitTarget.position - transform.position).normalized;
        // Vector3 futurePosition = transform.position;

        // //Zoom Out
        // if (Input.GetAxis("Mouse ScrollWheel") < 0)
        // {
        //     futurePosition -= camDir * zoomSpeed * Time.deltaTime;
        // }
        // //Zoom In
        // if (Input.GetAxis("Mouse ScrollWheel") > 0)
        // {
        //     futurePosition += camDir * zoomSpeed * Time.deltaTime;
        // }

        // //Make Sure The Camera Would Still Be Within Legal Values Before Applying The New Position
        // if (Vector3.Dot((orbitTarget.position - futurePosition).normalized, transform.forward) > 0.5f)
        // {
        //     if (Vector3.Distance(futurePosition, orbitTarget.position) < maxZoomDistance)
        //     {
        //         transform.position = futurePosition;
        //     }
        // }


        // transform.LookAt(orbitTarget); //Make Sure The Camera Is Always Facing The Origin
        // transform.localRotation = Quaternion.identity; //Prevent The Camera From Gimbal Locking
    }
}

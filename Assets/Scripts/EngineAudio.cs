using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EngineClip
{
    public float refRPM; //RPM at which the clip is at 100% volume (NOTE: Spacing between audio clips MUST be the same (i.e if the first clip has refRPM of 1000 and the second is 1500, the third must be 2000, and so on in 500 RPM increments))

    [HideInInspector]
    public AudioSource sourceOff;
    [HideInInspector]
    public AudioSource sourceOn;

    public AudioClip clipOff;
    public AudioClip clipOn;
    [HideInInspector]
    public bool noFallingEdge; //Tick to prevent audio clip's volume from falling off

    public EngineClip(AudioSource sourceOff, AudioSource sourceOn, AudioClip clipOff, AudioClip clipOn, float refRPM, bool noFallingEdge)
    {
        //Setters
        this.clipOff = clipOff;
        this.clipOn = clipOn;
        this.refRPM = refRPM;
        this.noFallingEdge = noFallingEdge;
        this.sourceOff = sourceOff;
        this.sourceOn = sourceOn;

        //Set sources' clips to the audio clips
        this.sourceOff.clip = this.clipOff;
        this.sourceOn.clip = this.clipOn;

        //Set the sources to loop
        this.sourceOff.loop = true;
        this.sourceOn.loop = true;

        //Set sources to play on startup
        this.sourceOff.Play();
        this.sourceOn.Play();

        //Set volume to 0 on startup
        this.sourceOff.volume = 0.0f;
        this.sourceOn.volume = 0.0f;

        //-----Add more init. params. for 3D audio settings here-----//

    }

    public float GetVolume(float inRPM, float range)
    {
        float min = refRPM - range;
        float max = refRPM + range;

        if ((inRPM < min || inRPM > max) && !noFallingEdge) //Set to zero if outside range
        {
            return 0.0f;
        }
        if (inRPM <= refRPM) //Rising Edge
        {
            return Mathf.Max((inRPM - min) / (refRPM - min), 0.0f);
        }
        else //Falling Edge
        {
            if (!noFallingEdge)
            {
                return Mathf.Max((max - inRPM) / (max - refRPM), 0.0f);
            }
            else
            { 
                return 1.0f;
            }
        }
    }
}

public class EngineAudio : MonoBehaviour
{
    public Engine engine;
    public EngineClip[] engineClips; //Input in ascending order (ex: 1koff, 1kon -> 2koff, 2kon -> ....)
    float RPMInterval; //must be the same between all clips!!!

    void Start()
    {
        bool noFall = false;
        for (int i = 0; i < engineClips.Length; i++)
        {
            AudioSource newSourceOff = gameObject.AddComponent<AudioSource>();
            AudioSource newSourceOn = gameObject.AddComponent<AudioSource>();

            if (i == (engineClips.Length - 1)) //Make sure the final clip doesn't have a falloff in volume
            {
                noFall = true;
            }
            engineClips[i] = new EngineClip(newSourceOff, newSourceOn, engineClips[i].clipOff, engineClips[i].clipOn, engineClips[i].refRPM, noFall);
        }

        RPMInterval = engineClips[1].refRPM - engineClips[0].refRPM; //NOTE: RPM spacing between all clips must be the same!!!
    }

    void LateUpdate() //Use LateUpdate for processes that come after input and game logic (i.e audio, visuals, particles, etc.)
    {
        for (int i = 0; i < engineClips.Length; i++)
        {
            //Reset source volume
            engineClips[i].sourceOff.volume = 0.0f;
            engineClips[i].sourceOn.volume = 0.0f;

            //Set audio pitch based off of its own reference
            engineClips[i].sourceOff.pitch = engine.engineRPM / engineClips[i].refRPM;
            engineClips[i].sourceOn.pitch = engine.engineRPM / engineClips[i].refRPM;

            //Set the volume of a clip based off the current engine speed and throttle input
            engineClips[i].sourceOff.volume = engineClips[i].GetVolume(engine.engineRPM, RPMInterval) * (1.0f - engine.throttle);
            engineClips[i].sourceOn.volume = engineClips[i].GetVolume(engine.engineRPM, RPMInterval) * engine.throttle;

            //Keep all sources (even silent ones) playing due to audio clipping issues when trying to pause or stop sources and replay them
            if (!engineClips[i].sourceOff.isPlaying)
            {
                engineClips[i].sourceOff.Play();
            }
            if (!engineClips[i].sourceOn.isPlaying)
            {
                engineClips[i].sourceOn.Play();
            }
        }
    }
}

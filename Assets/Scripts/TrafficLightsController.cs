using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightsController : MonoBehaviour
{
    public MeshRenderer redCover;
    public MeshRenderer amberCover;
    public MeshRenderer greenCover;
    public Collider actionSurface;
    public Collider stoppingSurface;
    public bool startWithRed;

    private float startTime;
    private float timer;
    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
        timer = startTime;
        stoppingSurface.gameObject.tag = "MustStop";
        // Set initial covers
        // If starting with red, display red
        if (startWithRed)
        {
            redCover.enabled = false;
            amberCover.enabled = true;
            greenCover.enabled = true;
            actionSurface.gameObject.tag = "TrafficRed";
        }
        // If starting with green, display red&amber at first
        else
        {
            redCover.enabled = false;
            amberCover.enabled = false;
            greenCover.enabled = true;
            actionSurface.gameObject.tag = "TrafficRedAndAmber";
        }
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        // If it's time to stop, wait 15 seconds and change state
        if (startWithRed)
        {
            // 15 seconds passed, it's time to go. Change the lights for red&amber
            if (timer >= 15.0f)
            {
                startWithRed = false;
                amberCover.enabled = false;
                actionSurface.gameObject.tag = "TrafficRedAndAmber";
                timer -= 15.0f;
            }
        }
        // If it's time to go, perform sequence of changes (red&amber - green - amber)
        // and then change state
        else
        {
            // 2 seconds passed, now you can really go. Change the lights for green
            if (timer >= 2.0f && amberCover.enabled == false)
            {
                redCover.enabled = true;
                amberCover.enabled = true;
                greenCover.enabled = false;
                actionSurface.gameObject.tag = "TrafficGreen";
                stoppingSurface.gameObject.tag = "CanGo";
            }
            // 12 seconds passed, now you should prepare to stop. Change the lights for amber
            if (timer >= 12.0f && greenCover.enabled == false)
            {
                amberCover.enabled = false;
                greenCover.enabled = true;
                actionSurface.gameObject.tag = "TrafficAmber";
            }
            // 15 seconds passed, it's time to wait. Change the lights for red
            if (timer >= 15.0f)
            {
                startWithRed = true;
                redCover.enabled = false;
                amberCover.enabled = true;
                actionSurface.gameObject.tag = "TrafficRed";
                stoppingSurface.gameObject.tag = "MustStop";
                timer -= 10.0f;
            }
        }
    }
}

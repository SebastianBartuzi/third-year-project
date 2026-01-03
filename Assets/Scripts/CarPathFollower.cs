using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPathFollower : MonoBehaviour
{
    public Rigidbody car;
    public Transform carRotation;
    public List<Collider> path;

    private List<Vector3> vectors;
    private int currentNode;
    private int goalNode;
    private Vector3 move;
    private float speed = 7f;
    private float rotDelta;
    private bool pedestrianOrRedInFront = false;
    private bool carInFront = false;


    // Start is called before the first frame update
    private void Start()
    {
        vectors = new List<Vector3>();
        int x = 0;
        int z = 0;
        move = new Vector3(1, 0, 0);
        // The first vector
        if (path[0].transform.position.x - path[path.Count - 1].transform.position.x < 0)
        {
            x = -1;
        }
        else if (path[0].transform.position.x - path[path.Count - 1].transform.position.x > 0)
        {
            x = 1;
        }
        if (path[0].transform.position.z - path[path.Count - 1].transform.position.z < 0)
        {
            z = -1;
        }
        else if (path[0].transform.position.z - path[path.Count - 1].transform.position.z > 0)
        {
            z = 1;
        }
        vectors.Add(new Vector3(x, 0, z));
        // Vectors TO ith node
        for (int i = 1; i < path.Count; i++)
        {
            x = 0;
            z = 0;
            if (path[i].transform.position.x - path[i - 1].transform.position.x < 0)
            {
                x = -1;
            }
            else if (path[i].transform.position.x - path[i - 1].transform.position.x > 0)
            {
                x = 1;
            }
            if (path[i].transform.position.z - path[i - 1].transform.position.z < 0)
            {
                z = -1;
            }
            else if (path[i].transform.position.z - path[i - 1].transform.position.z > 0)
            {
                z = 1;
            }
            vectors.Add(new Vector3(x, 0, z));
        }
        // Set initial nodes that will change to 0 and 1 respectively at the start
        currentNode = 0;
        goalNode = 1;
    }

    // Update is called once per frame
    private void Update()
    {
        // Vibrate so the car will recognise when it hits object with CanGo tag
        if (pedestrianOrRedInFront || carInFront)
        {
            move = new Vector3(0, 0, 0);
        }
        else
        {
            move = new Vector3(1, 0, 0);
        }
        // Move car ahead
        car.transform.Translate(move * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Car is about to turn
        if (other == path[goalNode])
        {
            // Update the current and goal node
            if (goalNode == path.Count - 1)
            {
                currentNode = goalNode;
                goalNode = 0;
            }
            else
            {
                goalNode += 1;
                currentNode = goalNode - 1;
            }
            // Rotate pedestrian
            rotDelta = Vector3.SignedAngle(vectors[goalNode], vectors[currentNode], Vector3.up);
            carRotation.transform.Rotate(new Vector3(0, -rotDelta, 0));
        }
        // Stop the car when other car is in front
        if (other.gameObject.tag == "Car")
        {
            carInFront = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Stop the car upon entry to stop zone
        if (other.gameObject.tag == "MustStop")
        {
            float rotDelta = Math.Abs(other.gameObject.transform.rotation.eulerAngles.y - car.transform.rotation.eulerAngles.y);
            if (rotDelta == 90)
            {
                pedestrianOrRedInFront = true;
            }
        }
        // Start when a traffic light changes or pedestrian is back on pavement
        if (other.gameObject.tag == "CanGo")
        {
            pedestrianOrRedInFront = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Start when other car is moving away 
        if (other.gameObject.tag == "Car")
        {
            carInFront = false;
        }
    }
}

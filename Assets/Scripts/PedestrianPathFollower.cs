using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianPathFollower : MonoBehaviour
{
    public Rigidbody pedestrian;
    public Transform pedestrianRotation;
    public Collider actionSurfaceLeft;
    public Collider actionSurfaceRight;
    public Collider stoppingSurface;
    public List<Collider> path;
    public Rigidbody car;

    private List<Vector3> vectors;
    private int currentNode;
    private int goalNode;
    private Vector3 move;
    private float speed = 1.6f;
    private float rotDelta;
    private List<string> collisions = new List<string>();

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
        // Move pedestrian ahead
        pedestrian.transform.Translate(move * speed * Time.deltaTime);
        // If pedestrian is waiting for a car to pass, check if it's safe to walk now
        if (move.x == 0)
        {
            if (!collisions.Contains("Car") || car.velocity.magnitude <= 2)
            {
                move = new Vector3(1, 0, 0);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Give a proper tag to actionSurface
        if (other.gameObject.tag == "Street")
        {
            actionSurfaceLeft.gameObject.tag = "PedestrianOnStreet";
            actionSurfaceRight.gameObject.tag = "PedestrianOnStreet";
            stoppingSurface.gameObject.tag = "MustStop";
        }
        if (other.gameObject.tag == "Sideway")
        {
            actionSurfaceLeft.gameObject.tag = "PedestrianOnPavement";
            actionSurfaceRight.gameObject.tag = "PedestrianOnPavement";
            stoppingSurface.gameObject.tag = "CanGo";
        }
        // Pedestrian is about to turn
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
            pedestrianRotation.transform.Rotate(new Vector3(0, -rotDelta, 0));
        }
        // Make list of collisions (later we check if pedestrian is about to cross the street and if there is a car nearby)
        if (!collisions.Contains(other.gameObject.tag) && (other.gameObject.tag == "AboutToCross" || other.gameObject.tag == "Car"))
        {
            collisions.Add(other.gameObject.tag);
        }
        // If pedestrian wants to cross the street, but a car is too close, stop
        if (collisions.Contains("AboutToCross") && collisions.Contains("Car") && car.velocity.magnitude > 2)
        {
            move = new Vector3(0, 0, 0);
        }
    }

    // Remove collisions from list
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "AboutToCross" || other.gameObject.tag == "Car")
        {
            collisions.Remove(other.gameObject.tag);
        }
    }
}

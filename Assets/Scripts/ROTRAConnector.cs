using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading;

namespace MultithreadingApplication
{
    public class ROTRAConnector : MonoBehaviour
    {
        public Rigidbody car;
        public Collider frontCollider;
        public Collider backCollider;
        public Camera cam;
        public List<GameObject> canvasRecommendation;
        public List<RawImage> canvasBox;
        public List<Text> canvasText;
        public List<GameObject> canvasAnimation;

        private string command = "";
        private bool cameraToFront = true;
        private List<string> currentRecommendations = new List<string>();
        private List<string> currentBeliefs = new List<string>();
        private List<string> currentIntentions = new List<string>();
        private List<string> beliefs = new List<string>();
        private List<string> intentions = new List<string>();
        private HashSet<string> constantBeliefs = new HashSet<string>();
        private HashSet<string> constantIntentions = new HashSet<string>();
        private Queue<int> directionOfMovement = new Queue<int>();
        private int previousVelocity;
        private int sidewayCountdown = 100;
        private int cyclepathCountdown = 100;
        private int keepDistanceCountdown = 100;
        private Queue<string> recommendationsInQueue = new Queue<string>();
        private float timeDisplayed;
        private List<int> boxPosition = new List<int>() { 0, 1, 2, 3, 4 };
        private Color red = new Color(0.75f, 0.06f, 0.02f);
        private Color blue = new Color(0.09f, 0.05f, 0.66f);

        // Dictionary of objects' tags with assigned such beliefs and intentions
        // that cause them to be called
        private IDictionary<string, List<List<string>>> tags = new Dictionary<string, List<List<string>>>()
        {
            // R109 - TRAFFIC SIGNS
            {"Sign", new List<List<string>>(){ new List<string>() {"seenSign", "signNoConflictsWithAuthorisedPersons"}, new List<string>() { } } },
            // R126 - STOPPING DISTANCES
            {"Car", new List<List<string>>(){ new List<string>() {"cantStopBeforeCarInFrontStops"}, new List<string>() { } } },
            // R140 - CYCLE LANES
            {"CyclingPath", new List<List<string>>(){ new List<string>() {"cycleLaneAvoidable"}, new List<string>() {"beInCycleLane"} } },
            // R145 - DRIVING ON PAVEMENTS
            {"Sideway", new List<List<string>>(){ new List<string>() {"pavement", "notAccessProperty"}, new List<string>() { } } },
            // R170 - EXTRA CARE AT JUNCTIONS, ALSO GIVE WAY TO PEDESTRIANS
            {"PedestrianOnStreet", new List<List<string>>(){ new List<string>() { "pedestriansInRoad" }, new List<string>() { } } },
            // R171 - STOP AT STOP SIGN
            {"SignStop", new List<List<string>>(){ new List<string>() {"stopSign", "whiteLineAcrossRoad"}, new List<string>() { } } },
            // R172 - GIVE WAY AT GIVE WAY SIGN
            {"SignGiveWay", new List<List<string>>(){ new List<string>() {"giveWaySign", "dottedWhiteLineAcrossRoad"}, new List<string>() { } } },
            // R175 - TRAFFIC LIGHTS
            {"Traffic", new List<List<string>>(){ new List<string>() { }, new List<string>() {"approachingTrafficLight"} } },
            {"TrafficRed", new List<List<string>>(){ new List<string>() {"lightRed"}, new List<string>() { } } },
            {"TrafficAmber", new List<List<string>>(){ new List<string>() {"lightAmber", "ableToStopByWhiteLine" }, new List<string>() { } } }
        };

        // Dictionary of rules written in humand approachable language
        private IDictionary<string, string> rules = new Dictionary<string, string>()
        {
            // Initial rules
            {"must-consideration_others", "You must take others into consideration."}, // R144
            {"must-drive_care_attention", "You must pay attention."}, // R144
            {"must-not_drive_dangerously", "You must not drive dangerously."}, // R144
            {"should-give_way_other_roads", "While turning, you should give way to other roads users."}, // R183
            {"should-keep_left", "While turning, you should keep left."}, // R183
            // Rest of rules
            {"must-follow_sign", "You must follow the sign."}, // R109
            {"should-brake_early_lightly", "You should break early and lightly."}, // R117
            {"must-reduce_speed", "You must reduce speed."}, // R124
            {"should-increase_distance_to_car_infront", "You should increase distance to car infront."}, // R126
            {"must-use_road", "You must use road."}, // R140
            {"must-avoid_parking", "You must avoid parking here."}, // R140
            {"must-road_surfaces-avoid_non", "You must avoid non-road surfaces."}, // R145
            {"should-give_way_to_pedestrians", "You should give way to pedestrians."}, // R170
            {"must-stop_at_white_line", "You must stop at white line."}, // R171&175
            {"should-wait_for_gap_before_moving_off", "You should wait for gap before moving off."}, // R171
            {"must-give_way_at_dotted_white_line", "You must give way at dotted white line."} // R172
        };


        // CONTROLS FOR R117
        // Timer controls
        private float startTimeS = 0f;
        private float timerS = 0f;
        private float holdTimeS = 0.5f;
        private float previousVelocityS = 0f;
        private bool heldS = false;
        private string keyS = "s";

        // Start is called before the first frame update
        private void Start()
        {
            // Hide UI elements
            foreach (var e in canvasRecommendation)
            {
                e.SetActive(false);
            }
            // Get output from runrotr.pl
            command = "swipl Assets/ROTRA/runrotr.pl";
            ThreadStart threadStart = new ThreadStart(callROTRA);
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        // Update is called once per frame
        private void Update()
        {
            // Check if it's moving forward or back
            int movementDirection = getDirectionOfMovement(car.velocity.x, car.velocity.z, car.transform.rotation.eulerAngles.y);
            // Update length of collision zone
            int velDelta = (int)car.velocity.magnitude - previousVelocity;
            frontCollider.transform.Translate(new Vector3((float)(velDelta * 0.5), 0, 0));
            frontCollider.transform.localScale += new Vector3((float)(velDelta * 0.1), 0, 0);
            backCollider.transform.Translate(new Vector3((float)(-velDelta * 0.5), 0, 0));
            backCollider.transform.localScale += new Vector3((float)(velDelta * 0.1), 0, 0);
            previousVelocity = (int)car.velocity.magnitude;
            // Update clocks for R126, R140, R145
            sidewayCountdown += 1;
            cyclepathCountdown += 1;
            keepDistanceCountdown += 1;
            // Enable right colliders and rotate camera
            if (movementDirection == 0 || movementDirection == 1)
            {
                frontCollider.enabled = true;
                backCollider.enabled = false;
                if (!cameraToFront && car.velocity.magnitude > 0.2 && car.velocity.magnitude < 6)
                {
                    rotateCamera();
                }
            }
            else
            {
                frontCollider.enabled = false;
                backCollider.enabled = true;
                if (cameraToFront && car.velocity.magnitude > 0.2 && car.velocity.magnitude < 6)
                {
                    rotateCamera();
                }
            }
            // Read car's beliefs and intentions
            updateBeliefsAndIntentions();
            beliefs = beliefs.Concat(constantBeliefs).ToList();
            beliefs.Sort();
            intentions = intentions.Concat(constantIntentions).ToList();
            intentions.Sort();
            // Check if the current state of beliefs and intentions has changed
            if (!(beliefs.SequenceEqual(currentBeliefs) && intentions.SequenceEqual(currentIntentions)))
            {
                // Replace current beliefs and intentions
                currentBeliefs = beliefs;
                currentIntentions = intentions;
                // Construct the command
                command = "swipl Assets/ROTRA/readrules.pl ";
                foreach (var belief in beliefs)
                {
                    command = command + belief + " ";
                }
                command = command + "break ";
                foreach (var intention in intentions)
                {
                    command = command + intention + " ";
                }
                // Get output from readrules.pl
                ThreadStart threadStart = new ThreadStart(callROTRA);
                Thread thread = new Thread(threadStart);
                thread.Start();
            }
            // Reset beliefs and intentions
            beliefs = new List<string>();
            intentions = new List<string>();
            removeRecommendationFromDisplay();
            displayRecommendation();
            moveRecommendationAnimations();
        }

        private void callROTRA()
        {
            string output = "";
            // Make command call to ROTRA
            // Windows
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                command = "/c " + command;
                var processInfo = new ProcessStartInfo("cmd.exe", command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };
                StringBuilder sb = new StringBuilder();
                Process p = Process.Start(processInfo);
                p.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
                p.BeginOutputReadLine();
                p.WaitForExit();
                output = sb.ToString();
            }
            // Display recommendations and update the current ones
            if (output.Length > 4)
            {
                string recommendationsString = output.Substring(0, output.Length - 4);
                List<String> recommendations = recommendationsString.Substring(1, recommendationsString.Length - 2).Split(',').ToList();
                for (int i = 0; i < recommendations.Count; i++)
                {
                    if (!currentRecommendations.Contains(recommendations[i]))
                    {
                        recommendationsInQueue.Enqueue(recommendations[i]);
                    }
                }
                currentRecommendations = recommendations;
            }
        }

        // Rotate the camera when movement direction has changed
        private void rotateCamera()
        {
            cam.transform.Rotate(new Vector3(0, 180, 0));
            if (cameraToFront)
            {
                cam.transform.Translate(new Vector3(0, 0, -8));
                cameraToFront = false;
            }
            else
            {
                cam.transform.Translate(new Vector3(0, 0, -8));
                cameraToFront = true;
            }
        }

        // Update beliefs and intentions that require touch with other objects
        // These states will be there for a constant amount of time
        private void OnTriggerStay(Collider other)
        {
            // R109 - TRAFFIC SIGNS
            if (other.gameObject.tag.StartsWith("Sign"))
            {
                if (towardsRightDirection(other.gameObject))
                {
                    addConnectedBeliefsAndIntentions(other.gameObject.tag);
                }
            }
            // R126 - STOPPING DISTANCES
            if (other.gameObject.tag == "Car")
            {
                if (keepDistanceCountdown >= 200)
                {
                    addConnectedBeliefsAndIntentions(other.gameObject.tag);
                    keepDistanceCountdown = 0;
                }
            }
            // R140 - CYCLE LANES
            if (other.gameObject.tag == "CyclingPath")
            {
                if (cyclepathCountdown >= 200)
                {
                    addConnectedBeliefsAndIntentions(other.gameObject.tag);
                    cyclepathCountdown = 0;
                }
            }
            // R145 - DRIVING ON PAVEMENTS
            if (other.gameObject.tag == "Sideway")
            {
                if (sidewayCountdown >= 200)
                {
                    addConnectedBeliefsAndIntentions(other.gameObject.tag);
                    sidewayCountdown = 0;
                }
            }
            // R170 - EXTRA CARE AT JUNCTIONS, ALSO GIVE WAY TO PEDESTRIANS
            if (other.gameObject.tag == "PedestrianOnStreet")
            {
                if (car.velocity.magnitude > 2 && towardsRightDirection(other.gameObject))
                {
                    addConnectedBeliefsAndIntentions(other.gameObject.tag);
                }
            }
            if (other.gameObject.tag == "PedestrianOnPavement")
            {
                removeConnectedBeliefsAndIntentions("PedestrianOnStreet");
            }
            // R171 - STOP AT STOP SIGN
            if (other.gameObject.tag == "SignStop")
            {
                if (towardsRightDirection(other.gameObject))
                {
                    addConnectedBeliefsAndIntentions(other.gameObject.tag);
                }
            }
            // R172 - GIVE WAY AT GIVE WAY SIGN
            if (other.gameObject.tag == "SignGiveWay")
            {
                if (towardsRightDirection(other.gameObject))
                {
                    addConnectedBeliefsAndIntentions(other.gameObject.tag);
                }
            }
            // R175-178 - TRAFFIC LIGHTS
            if (other.gameObject.tag.StartsWith("Traffic"))
            {
                addConnectedBeliefsAndIntentions("Traffic");
                // R175a - TRAFFIC LIGHTS (STOP AT RED)
                if (other.gameObject.tag == "TrafficRed")
                {
                    if (towardsRightDirection(other.gameObject))
                    {
                        addConnectedBeliefsAndIntentions(other.gameObject.tag);
                    }
                }
                // R175b - TRAFFIC LIGHTS (STOP AT RED)
                if (other.gameObject.tag == "TrafficAmber" && car.velocity.magnitude < 6)
                {
                    if (towardsRightDirection(other.gameObject))
                    {
                        addConnectedBeliefsAndIntentions(other.gameObject.tag);
                    }
                }
                // In case light changes for red&amber or green, remove beliefs
                // and intentions for red and amber
                if (other.gameObject.tag == "TrafficRedAndAmber" || other.gameObject.tag == "TrafficGreen")
                {
                    removeConnectedBeliefsAndIntentions("TrafficRed");
                    removeConnectedBeliefsAndIntentions("TrafficAmber");
                }
            }
        }

        // Remove these constant beliefs and intentions after the car stops touching objects
        private void OnTriggerExit(Collider other)
        {
            removeConnectedBeliefsAndIntentions(other.gameObject.tag);
        }

        // Update beliefs and intentions that don't require any touch with other objects
        private void updateBeliefsAndIntentions()
        {
            // R117 - Standard Break
            // Starts the timer when the key is pressed
            if (Input.GetKeyDown(keyS))
            {
                startTimeS = Time.time;
                timerS = startTimeS;
                previousVelocityS = car.velocity.magnitude;
            }
            // Adds time to the timer as long as the key is pressed
            if (Input.GetKey(keyS) && heldS == false)
            {
                timerS += Time.deltaTime;
                // Will add an intention after 0.5 seconds, if car has slown down (will not work for
                // car moving backwards) and if car moves fast enough
                if (timerS > (startTimeS + holdTimeS) && previousVelocityS - car.velocity.magnitude > 0 && previousVelocityS > 8)
                {
                    heldS = true;
                    intentions.Add("brake");
                }
            }
            if (Input.GetKeyUp(keyS))
            {
                heldS = false;
            }

            // R124 - MAXIMUM SPEED EXCEEDED
            // 16 is around machine's maximum speed so let's suppose
            // this is around 30mph
            if (car.velocity.magnitude > 16)
            {
                beliefs.Add("exceedingSpeedLimit");
            }
        }

        // Add beliefs and intentions to constant ones based on tags dictionary
        private void addConnectedBeliefsAndIntentions(string tag)
        {
            foreach (var key in tags.Keys)
            {
                if (tag == key)
                {
                    foreach (string belief in tags[key][0])
                    {
                        if (!constantBeliefs.Contains(belief))
                        {
                            constantBeliefs.Add(belief);
                        }
                    }
                    foreach (string intention in tags[key][1])
                    {
                        if (!constantIntentions.Contains(intention))
                        {
                            constantIntentions.Add(intention);
                        }
                    }
                }
            }
        }

        // Remove beliefs and intentions from constant ones based on tags dictionary
        private void removeConnectedBeliefsAndIntentions(string tag)
        {
            foreach (var key in tags.Keys)
            {
                if (tag.StartsWith(key))
                {
                    foreach (string belief in tags[key][0])
                    {
                        constantBeliefs.Remove(belief);
                    }
                    foreach (string intention in tags[key][1])
                    {
                        constantIntentions.Remove(intention);
                    }
                }
            }
        }

        // Get direction of movement (in degrees) of the car
        private bool towardsRightDirection(GameObject obj)
        {
            return (Math.Abs(obj.transform.rotation.eulerAngles.y - cam.transform.eulerAngles.y) < 90 
                 || Math.Abs(obj.transform.rotation.eulerAngles.y - cam.transform.eulerAngles.y) > 270);
        }

        // Get direction of movement (-1 if backwards, 0 if not moving, 1 if ahead) of the car
        private int getDirectionOfMovement(float deltaX, float deltaZ, float rot)
        {
            // Populate queue if it's needed. The queue is needed beacause functions below can
            // sometimes return bad values, so we take the most frequent direction of movement 
            if (directionOfMovement.Count <= 25)
            {
                for (int i = directionOfMovement.Count; i < 25; i++)
                {
                    if (i == 0)
                    {
                        directionOfMovement.Enqueue(0);
                    }
                    else
                    {
                        directionOfMovement.Enqueue(directionOfMovement.Peek());
                    }
                }
            }
            int direction;
            // Car not moving
            if (Math.Abs(deltaX) <= 0.1 && Math.Abs(deltaZ) <= 0.1)
            {
                direction = 0;
            }
            // 1st quarter
            else if (deltaX >= 0 && deltaZ >= 0)
            {
                if (rot >= 0 && rot <= 90)
                {
                    direction = 1;
                }
                else
                {
                    direction = -1;
                }
            }
            // 2nd quarter
            else if (deltaX >= 0 && deltaZ <= 0)
            {
                if (rot >= 90 && rot <= 180)
                {
                    direction = 1;
                }
                else
                {
                    direction = -1;
                }
            }
            // 3rd quarter
            else if (deltaX <= 0 && deltaZ <= 0)
            {
                if (rot >= 180 && rot <= 270)
                {
                    direction = 1;
                }
                else
                {
                    direction = -1;
                }
            }
            // 4th quarter
            else
            {
                if (rot >= 270 && (rot <= 360 || rot == 0))
                {
                    direction = 1;
                }
                else
                {
                    direction = -1;
                }
            }
            directionOfMovement.Dequeue();
            directionOfMovement.Enqueue(direction);
            return directionOfMovement.GroupBy(x => x).OrderByDescending(g => g.Count()).Select(g => g.Key).First();
        }

        // Display a recommendation on screen. Function is called on each updated
        // but progresses through if statement if there is someting waiting to
        // be displayed
        private void displayRecommendation()
        {
            if (recommendationsInQueue.Count > 0)
            {
                // Only 5 recommendations can be displayed at one moment
                foreach (var i in boxPosition)
                {
                    // Display information
                    if (canvasRecommendation[i].activeSelf == false)
                    {
                        string rec = recommendationsInQueue.Dequeue();
                        canvasRecommendation[i].SetActive(true);
                        if (rec.StartsWith("must"))
                        {
                            canvasBox[i].color = red;
                        }
                        else if (rec.StartsWith("should"))
                        {
                            canvasBox[i].color = blue;
                        }
                        canvasText[i].text = rules[rec];
                        // Start the clock if it's top recommendation
                        if (i == boxPosition[0])
                        {
                            timeDisplayed = 0;
                        }
                        break;
                    }
                }
            }
        }

        // Function to remove the top recommendation from the screen. It moves the top
        // information box to bottom, and the rest - one position up
        private void removeRecommendationFromDisplay()
        {
            timeDisplayed += 1;
            // If the top recommendation has been there for long enough
            if (timeDisplayed == 50)
            {
                // Hide it and move to the bottom
                canvasRecommendation[boxPosition[0]].SetActive(false);
                canvasText[boxPosition[0]].text = "";
                canvasRecommendation[boxPosition[0]].transform.Translate(new Vector3(0, -160, 0));
                canvasAnimation[boxPosition[0]].transform.Translate(new Vector3(canvasRecommendation[boxPosition[0]].transform.position.x
                                                                                - canvasAnimation[boxPosition[0]].transform.position.x, 0, 0));
                // Move the rest one position up
                for (int i = 1; i < 5; i++)
                {
                    canvasRecommendation[boxPosition[i]].transform.Translate(new Vector3(0, 40, 0));
                }
                // Reset clock
                timeDisplayed = 0;
                // Correct the list of box ids from top to bottom
                int tempId = boxPosition[0];
                for (int i = 0; i < 4; i++)
                {
                    boxPosition[i] = boxPosition[i + 1];
                }
                boxPosition[4] = tempId;
            }
        }

        // It moves a white box outside screen so it looks like a nice animation
        // when a new recommendation is to be displayed
        private void moveRecommendationAnimations()
        {
            foreach (var i in boxPosition)
            {
                if (canvasRecommendation[i].activeSelf && canvasAnimation[i].transform.position.x < 9000)
                {
                    canvasAnimation[i].transform.Translate(new Vector3(5, 0, 0) * 100f * Time.deltaTime);
                }
            }
        }
    }
}

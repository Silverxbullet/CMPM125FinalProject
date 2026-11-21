
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class lb_BirdController : MonoBehaviour
{
    public int idealNumberOfBirds;
    public int maximumNumberOfBirds;
    public Camera currentCamera;
    public float unspawnDistance = 10.0f;
    public bool highQuality = true;
    public bool collideWithObjects = true;
    public LayerMask groundLayer;
    public float birdScale = 1.0f;

    public bool robin = true;
    public bool blueJay = true;
    public bool cardinal = true;
    public bool chickadee = true;
    public bool sparrow = true;
    public bool goldFinch = true;
    public bool crow = true;

    bool pause = false;

    GameObject[] myBirds;
    List<string> myBirdTypes = new List<string>();
    List<GameObject> birdGroundTargets = new List<GameObject>();
    List<GameObject> birdPerchTargets = new List<GameObject>();

    int activeBirds = 0;
    int birdIndex = 0;

    GameObject[] featherEmitters = new GameObject[3];

    public void AllFlee()
    {
        if (!pause)
        {
            for (int i = 0; i < myBirds.Length; i++)
            {
                if (myBirds[i].activeSelf)
                {
                    myBirds[i].SendMessage("Flee");
                }
            }
        }
    }

    public void Pause()
    {
        if (pause)
        {
            AllUnPause();
        }
        else
        {
            AllPause();
        }
    }

    public void AllPause()
    {
        pause = true;

        for (int i = 0; i < myBirds.Length; i++)
        {
            if (myBirds[i].activeSelf)
            {
                myBirds[i].SendMessage("PauseBird");
            }
        }
    }

    public void AllUnPause()
    {
        pause = false;

        for (int i = 0; i < myBirds.Length; i++)
        {
            if (myBirds[i].activeSelf)
            {
                myBirds[i].SendMessage("UnPauseBird");
            }
        }
    }

    public void SpawnAmount(int amt)
    {
        for (int i = 0; i <= amt; i++)
        {
            SpawnBird();
        }
    }

    public void ChangeCamera(Camera cam)
    {
        currentCamera = cam;
    }

    void Start()
    {
        // Find the camera
        if (currentCamera == null)
        {
            GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");

            if (cameraObject != null)
            {
                currentCamera = cameraObject.GetComponent<Camera>();
            }
        }

        if (currentCamera == null)
        {
            Debug.LogError("lb_BirdController: No camera found. Assign Current Camera in the Inspector or tag your camera MainCamera.");
            enabled = false;
            return;
        }

        if (idealNumberOfBirds >= maximumNumberOfBirds)
        {
            idealNumberOfBirds = maximumNumberOfBirds - 1;
        }

        // Set up the bird types to use
        if (robin)
        {
            myBirdTypes.Add("lb_robin");
        }

        if (blueJay)
        {
            myBirdTypes.Add("lb_blueJay");
        }

        if (cardinal)
        {
            myBirdTypes.Add("lb_cardinal");
        }

        if (chickadee)
        {
            myBirdTypes.Add("lb_chickadee");
        }

        if (sparrow)
        {
            myBirdTypes.Add("lb_sparrow");
        }

        if (goldFinch)
        {
            myBirdTypes.Add("lb_goldFinch");
        }

        if (crow)
        {
            myBirdTypes.Add("lb_crow");
        }

        if (myBirdTypes.Count == 0)
        {
            Debug.LogError("lb_BirdController: No bird types are enabled in the Inspector.");
            enabled = false;
            return;
        }

        // Instantiate birds based on amounts and bird types
        myBirds = new GameObject[maximumNumberOfBirds];

        GameObject bird;

        for (int i = 0; i < myBirds.Length; i++)
        {
            if (highQuality)
            {
                bird = Resources.Load(
                    myBirdTypes[Random.Range(0, myBirdTypes.Count)] + "HQ",
                    typeof(GameObject)
                ) as GameObject;
            }
            else
            {
                bird = Resources.Load(
                    myBirdTypes[Random.Range(0, myBirdTypes.Count)],
                    typeof(GameObject)
                ) as GameObject;
            }

            if (bird == null)
            {
                Debug.LogError("lb_BirdController: Could not load a bird prefab from the Resources folder.");
                enabled = false;
                return;
            }

            myBirds[i] = Instantiate(bird, Vector3.zero, Quaternion.identity);

            myBirds[i].transform.localScale =
                myBirds[i].transform.localScale * birdScale;

            myBirds[i].transform.parent = transform;

            myBirds[i].SendMessage("SetController", this);

            myBirds[i].SetActive(false);
        }

        // Find all the targets
        GameObject[] groundTargets =
            GameObject.FindGameObjectsWithTag("lb_groundTarget");

        GameObject[] perchTargets =
            GameObject.FindGameObjectsWithTag("lb_perchTarget");

        for (int i = 0; i < groundTargets.Length; i++)
        {
            if (Vector3.Distance(
                groundTargets[i].transform.position,
                currentCamera.transform.position
            ) < unspawnDistance)
            {
                birdGroundTargets.Add(groundTargets[i]);
            }
        }

        for (int i = 0; i < perchTargets.Length; i++)
        {
            if (Vector3.Distance(
                perchTargets[i].transform.position,
                currentCamera.transform.position
            ) < unspawnDistance)
            {
                birdPerchTargets.Add(perchTargets[i]);
            }
        }

        // Instantiate 3 feather emitters for killing the birds
        GameObject fEmitter =
            Resources.Load("featherEmitter", typeof(GameObject)) as GameObject;

        if (fEmitter == null)
        {
            Debug.LogError("lb_BirdController: Could not load featherEmitter from Resources.");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            featherEmitters[i] =
                Instantiate(fEmitter, Vector3.zero, Quaternion.identity);

            featherEmitters[i].transform.parent = transform;

            featherEmitters[i].SetActive(false);
        }
    }

    void OnEnable()
    {
        InvokeRepeating("UpdateBirds", 1, 1);
        StartCoroutine("UpdateTargets");
    }

    Vector3 FindPointInGroundTarget(GameObject target)
    {
        // Find a random point within a ground target's collider
        Collider targetCollider = target.GetComponent<Collider>();

        if (targetCollider == null)
        {
            Debug.LogWarning("Ground target is missing a Collider component.");
            return target.transform.position;
        }

        Vector3 point;

        point.x = Random.Range(
            targetCollider.bounds.max.x,
            targetCollider.bounds.min.x
        );

        point.y = targetCollider.bounds.max.y;

        point.z = Random.Range(
            targetCollider.bounds.max.z,
            targetCollider.bounds.min.z
        );

        // Raycast down until it hits the ground
        RaycastHit hit;

        if (Physics.Raycast(
            point,
            -Vector3.up,
            out hit,
            targetCollider.bounds.size.y,
            groundLayer
        ))
        {
            return hit.point;
        }

        return point;
    }

    void UpdateBirds()
    {
        // This function is called once a second
        if (activeBirds < idealNumberOfBirds && AreThereActiveTargets())
        {
            SpawnBird();
        }
        else if (
            activeBirds < maximumNumberOfBirds &&
            Random.value < .05 &&
            AreThereActiveTargets()
        )
        {
            SpawnBird();
        }

        // Check one bird every second to see if it should be unspawned
        if (
            myBirds[birdIndex].activeSelf &&
            BirdOffCamera(myBirds[birdIndex].transform.position) &&
            Vector3.Distance(
                myBirds[birdIndex].transform.position,
                currentCamera.transform.position
            ) > unspawnDistance
        )
        {
            Unspawn(myBirds[birdIndex]);
        }

        birdIndex = birdIndex == myBirds.Length - 1 ? 0 : birdIndex + 1;
    }

    IEnumerator UpdateTargets()
    {
        List<GameObject> gtRemove = new List<GameObject>();
        List<GameObject> ptRemove = new List<GameObject>();

        while (true)
        {
            gtRemove.Clear();
            ptRemove.Clear();

            // Check targets to see if they are out of range
            for (int i = 0; i < birdGroundTargets.Count; i++)
            {
                if (Vector3.Distance(
                    birdGroundTargets[i].transform.position,
                    currentCamera.transform.position
                ) > unspawnDistance)
                {
                    gtRemove.Add(birdGroundTargets[i]);
                }

                yield return 0;
            }

            for (int i = 0; i < birdPerchTargets.Count; i++)
            {
                if (Vector3.Distance(
                    birdPerchTargets[i].transform.position,
                    currentCamera.transform.position
                ) > unspawnDistance)
                {
                    ptRemove.Add(birdPerchTargets[i]);
                }

                yield return 0;
            }

            // Remove targets found out of range
            foreach (GameObject entry in gtRemove)
            {
                birdGroundTargets.Remove(entry);
            }

            foreach (GameObject entry in ptRemove)
            {
                birdPerchTargets.Remove(entry);
            }

            yield return 0;

            // Check for any new targets
            Collider[] hits = Physics.OverlapSphere(
                currentCamera.transform.position,
                unspawnDistance
            );

            foreach (Collider hit in hits)
            {
                if (
                    hit.CompareTag("lb_groundTarget") &&
                    !birdGroundTargets.Contains(hit.gameObject)
                )
                {
                    birdGroundTargets.Add(hit.gameObject);
                }

                if (
                    hit.CompareTag("lb_perchTarget") &&
                    !birdPerchTargets.Contains(hit.gameObject)
                )
                {
                    birdPerchTargets.Add(hit.gameObject);
                }
            }

            yield return 0;
        }
    }

    bool BirdOffCamera(Vector3 birdPos)
    {
        Vector3 screenPos =
            currentCamera.WorldToViewportPoint(birdPos);

        if (
            screenPos.x < 0 ||
            screenPos.x > 1 ||
            screenPos.y < 0 ||
            screenPos.y > 1
        )
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Unspawn(GameObject bird)
    {
        bird.transform.position = Vector3.zero;
        bird.SetActive(false);
        activeBirds--;
    }

    void SpawnBird()
    {
        if (!pause)
        {
            GameObject bird = null;

            int randomBirdIndex =
                Mathf.FloorToInt(Random.Range(0, myBirds.Length));

            int loopCheck = 0;

            // Find a random bird that is not active
            while (bird == null)
            {
                if (myBirds[randomBirdIndex].activeSelf == false)
                {
                    bird = myBirds[randomBirdIndex];
                }

                randomBirdIndex =
                    randomBirdIndex + 1 >= myBirds.Length
                    ? 0
                    : randomBirdIndex + 1;

                loopCheck++;

                if (loopCheck >= myBirds.Length)
                {
                    // All birds are active
                    return;
                }
            }

            // Find a point off camera to position the bird
            bird.transform.position = FindPositionOffCamera();

            if (bird.transform.position == Vector3.zero)
            {
                // Could not find a suitable spawn point
                return;
            }
            else
            {
                bird.SetActive(true);
                activeBirds++;

                BirdFindTarget(bird);
            }
        }
    }

    bool AreThereActiveTargets()
    {
        if (birdGroundTargets.Count > 0 || birdPerchTargets.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    Vector3 FindPositionOffCamera()
    {
        RaycastHit hit;

        float dist = Random.Range(2, 10);

        Vector3 ray = -currentCamera.transform.forward;

        int loopCheck = 0;

        // Find a random ray pointing away from the camera's field of view
        ray += new Vector3(
            Random.Range(-.5f, .5f),
            Random.Range(-.5f, .5f),
            Random.Range(-.5f, .5f)
        );

        // Cycle through random rays until we find one that doesn't hit anything
        while (Physics.Raycast(
            currentCamera.transform.position,
            ray,
            out hit,
            dist
        ))
        {
            dist = Random.Range(2, 10);

            loopCheck++;

            if (loopCheck > 35)
            {
                // Can't find any good spawn points
                return Vector3.zero;
            }
        }

        return currentCamera.transform.position + (ray * dist);
    }

    void BirdFindTarget(GameObject bird)
    {
        GameObject target;

        if (birdGroundTargets.Count > 0 || birdPerchTargets.Count > 0)
        {
            // Pick a random target based on the available target areas
            float gtArea = 0.0f;
            float ptArea = birdPerchTargets.Count * 0.3f;

            for (int i = 0; i < birdGroundTargets.Count; i++)
            {
                Collider targetCollider =
                    birdGroundTargets[i].GetComponent<Collider>();

                if (targetCollider != null)
                {
                    gtArea +=
                        targetCollider.bounds.size.x *
                        targetCollider.bounds.size.z;
                }
            }

            if (
                ptArea == 0.0f ||
                Random.value < gtArea / (gtArea + ptArea)
            )
            {
                if (birdGroundTargets.Count > 0)
                {
                    target = birdGroundTargets[
                        Mathf.FloorToInt(
                            Random.Range(0, birdGroundTargets.Count)
                        )
                    ];

                    bird.SendMessage(
                        "FlyToTarget",
                        FindPointInGroundTarget(target)
                    );
                }
            }
            else
            {
                target = birdPerchTargets[
                    Mathf.FloorToInt(
                        Random.Range(0, birdPerchTargets.Count)
                    )
                ];

                bird.SendMessage(
                    "FlyToTarget",
                    target.transform.position
                );
            }
        }
        else
        {
            bird.SendMessage(
                "FlyToTarget",
                currentCamera.transform.position +
                new Vector3(
                    Random.Range(-100, 100),
                    Random.Range(5, 10),
                    Random.Range(-100, 100)
                )
            );
        }
    }

    void FeatherEmit(Vector3 pos)
    {
        foreach (GameObject fEmit in featherEmitters)
        {
            if (!fEmit.activeSelf)
            {
                fEmit.transform.position = pos;
                fEmit.SetActive(true);

                StartCoroutine("DeactivateFeathers", fEmit);

                break;
            }
        }
    }

    IEnumerator DeactivateFeathers(GameObject featherEmit)
    {
        yield return new WaitForSeconds(4.5f);

        featherEmit.SetActive(false);
    }
}

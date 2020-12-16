using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using Pathfinding;

[System.Serializable]
public struct BehaviorStats
{
    public float movementSpeed;
    public float soundSensitivityLowerAlertness;
    public float soundSensitivityHigherAlertness;
    public float timeToSeePlayer;
    public float timeToHearPlayer;
    public float seeTimeCap;
    public float hearTimeCap;
}

public enum BehaviorTypes
{
    IDLE,
    WANDER_TOWARDS,
    CHASE_PLAYER,
    RUN_TO_SOUND,
    FREAKOUT
}

public class InfectedBrain : MonoBehaviour
{
    //Changable Data
    [SerializeField]
    public BehaviorTypes reactionToLosingPlayer = BehaviorTypes.FREAKOUT;
    public List<BehaviorStats> stats;
    public Transform targetAStar;
    public BehaviorTypes currentBehavior;
    public int ratioHearingLessens;
    public int ratioSeeingLessens;
    public float idleRotationSpeed = 45.0f;
    public float idlePauseTime = 45.0f;
    public float freakOutRadius = 15;
    public float freakOutTime = 5.0f;
    public float freakOutPauses = 0.2f;
    public Vector3 rotBase;
    public Tilemap tilemapSeen;
    public Tilemap tilemapGuesses;
    public Tile occludedTile;
    public LayerMask wallLayer;
    public Image angerIndicator;

    //Neat Data (not to change, fun to look at)
    public float idleTargetRotation;
    public float timeBeforeBehaviorChange;
    public float timeLeftPausing;
    public float timeSeeingPlayer;
    public float timeHearingPlayer;
    public bool hearingPlayer = false;
    public bool awareOfPlayer = false;

    int idealRotateAngle = 1;
    SightDetector sight;
    GameObject player;
    AIPath AIPath;
    float minDistanceToProvoke;


    public void hearNoise(float volume, Vector3 pos, GameObject obj = null)
    {
        if(volume >= stats[(int)currentBehavior].soundSensitivityLowerAlertness)
        {
            if (obj == player) hearingPlayer = true;

            if (volume >= stats[(int)currentBehavior].soundSensitivityHigherAlertness)
            {
                if (obj == player) { awareOfPlayer = true; angerIndicator.fillAmount = 1; }
            }
            else if (currentBehavior == BehaviorTypes.IDLE)
            {
                idleTargetRotation = Vector3.SignedAngle(rotBase, pos - transform.position, Vector3.forward);
                if (idleTargetRotation < 0) idleTargetRotation += 360;
                timeLeftPausing = idlePauseTime * 2;
                idealRotateAngle = Vector3.SignedAngle(transform.up, pos - transform.position, Vector3.forward) > 0 ? 2 : -2;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        AIPath = GetComponent<AIPath>();
        minDistanceToProvoke = AIPath.endReachedDistance;
        sight = GetComponent<SightDetector>();
        BeginBehavior(currentBehavior);
    }

    public void BeginBehavior(BehaviorTypes type)
    {
        currentBehavior = type;
        AIPath.maxSpeed = stats[(int)type].movementSpeed;

        tilemapSeen.GetComponent<TilemapRenderer>().enabled = false;
        switch (type)
        {
            case BehaviorTypes.IDLE:
                {
                    timeLeftPausing = idlePauseTime;
                    break;
                }
            case BehaviorTypes.WANDER_TOWARDS:
                {
                    break;
                }
            case BehaviorTypes.RUN_TO_SOUND:
                {
                    break;
                }
            case BehaviorTypes.FREAKOUT:
                {
                    targetAStar.position = transform.position;
                    timeLeftPausing = freakOutPauses * 3;
                    timeBeforeBehaviorChange = freakOutTime;
                    SetUpFreakoutGrid();
                    break;
                }
            case BehaviorTypes.CHASE_PLAYER:
                {
                    break;
                }
        }
    }

    // Update is called once per frame
    void Update()
    {
        CheckForSenseTimingTriggers();

        switch(currentBehavior)
        {
            case BehaviorTypes.IDLE:
            {
                    if (awareOfPlayer)
                    {
                        BeginBehavior(BehaviorTypes.CHASE_PLAYER);
                        break;
                    }

                    if (AIPath.reachedEndOfPath)
                    {
                        if (Vector3.Angle(transform.up, new Vector3(Mathf.Cos(idleTargetRotation * Mathf.Deg2Rad), Mathf.Sin(idleTargetRotation * Mathf.Deg2Rad))) >= idleRotationSpeed * Time.deltaTime)
                        {
                            transform.Rotate(new Vector3(0, 0, idleRotationSpeed * Time.deltaTime * idealRotateAngle));
                            if (transform.rotation.eulerAngles.z < 0)
                                transform.rotation = Quaternion.Euler(new Vector3(0, 0, 360 + transform.rotation.eulerAngles.z));
                        }
                        else
                        {
                            //transform.rotation = Quaternion.Euler(new Vector3(0, 0, idleTargetRotation-90));
                            timeLeftPausing -= Time.deltaTime;
                            if (timeLeftPausing <= 0)
                            {
                                idealRotateAngle = Random.Range(0, 2) == 0 ? 1 : -1;
                                timeLeftPausing = idlePauseTime;
                                idleTargetRotation = Random.Range(0, 360);
                            }
                        }
                    }
                    break;
            }
            case BehaviorTypes.WANDER_TOWARDS:
            {
                    break;
            }
            case BehaviorTypes.RUN_TO_SOUND:
            {
                    if (awareOfPlayer) BeginBehavior(BehaviorTypes.CHASE_PLAYER);
                    else if (Vector3.Distance(targetAStar.position, transform.position) < 1.5f) BeginBehavior(BehaviorTypes.FREAKOUT);
                    break;
            }
            case BehaviorTypes.FREAKOUT:
            {
                    if (awareOfPlayer)
                    {
                        BeginBehavior(BehaviorTypes.CHASE_PLAYER);
                        break;
                    }

                    if (Vector2.Distance(transform.position,targetAStar.position) <= minDistanceToProvoke)
                    {
                        timeLeftPausing -= Time.deltaTime;
                        if(timeLeftPausing <= 0)
                        {
                            SetFreakOutSeens();
                            NewFreakOutAnim();
                            timeLeftPausing = freakOutPauses;
                        }
                    }

                    timeBeforeBehaviorChange -= Time.deltaTime;
                    if(timeBeforeBehaviorChange <= 0)
                    {
                        targetAStar.position = transform.position;
                        BeginBehavior(BehaviorTypes.IDLE);
                    }
                    break;
            }
            case BehaviorTypes.CHASE_PLAYER:
            {
                    if (awareOfPlayer) targetAStar.position = player.transform.position;
                    else if (Vector3.Distance(targetAStar.position, transform.position) < 1.5f) BeginBehavior(reactionToLosingPlayer);
                    break;
            }
        }
    }

    void CheckForSenseTimingTriggers()
    {

        if (hearingPlayer)
        {
            timeHearingPlayer = Mathf.Min(timeHearingPlayer += Time.deltaTime, stats[(int)currentBehavior].hearTimeCap);
            hearingPlayer = false;
            if (timeHearingPlayer >= stats[(int)currentBehavior].timeToHearPlayer) awareOfPlayer = true;
        }
        else if (timeHearingPlayer > 0)
        {
            timeHearingPlayer = Mathf.Max(0, timeHearingPlayer - Time.deltaTime/ratioHearingLessens);
            if(timeSeeingPlayer <= 0 && timeHearingPlayer <= 0)
            {
                awareOfPlayer = false;
            }
        }

        if (sight.canSeePlayer)
        {
            timeSeeingPlayer = Mathf.Min(timeSeeingPlayer += Time.deltaTime, stats[(int)currentBehavior].seeTimeCap);
            if (timeSeeingPlayer >= stats[(int)currentBehavior].timeToSeePlayer) awareOfPlayer = true;

            if(currentBehavior == BehaviorTypes.IDLE)
            {
                idleTargetRotation = Vector3.SignedAngle(rotBase, player.transform.position - transform.position, Vector3.forward);
                if (idleTargetRotation < 0) idleTargetRotation += 360;
                timeLeftPausing = idlePauseTime * 2;
                idealRotateAngle = Vector3.SignedAngle(transform.up, player.transform.position - transform.position, Vector3.forward) > 0 ? 2 : -2;
            }
        }
        else if (timeSeeingPlayer > 0)
        {
            timeSeeingPlayer = Mathf.Max(0, timeSeeingPlayer - Time.deltaTime/ratioSeeingLessens);
            if (timeSeeingPlayer <= 0)
            {
                awareOfPlayer = false;
            }
        }

        angerIndicator.fillAmount = Mathf.Max(timeSeeingPlayer / stats[(int)currentBehavior].timeToSeePlayer, timeHearingPlayer / stats[(int)currentBehavior].timeToHearPlayer);
        angerIndicator.rectTransform.rotation = Quaternion.Euler(0,0,0);
    }

    void SetUpFreakoutGrid()
    {
        tilemapSeen.GetComponent<TilemapRenderer>().enabled = true;
        tilemapSeen.ClearAllTiles();
        tilemapSeen.transform.position = transform.position;
        int boxReach = (int) (freakOutRadius / tilemapSeen.layoutGrid.cellSize.x);
        Vector3Int tempTile;
        for (int i = -boxReach; i <= boxReach; i++)
            for (int j = -boxReach; j <= boxReach; j++)
            {
                bool clear = true;
                tempTile = new Vector3Int(i, j, 0);

                if (Vector3.Distance(transform.position, tilemapSeen.GetCellCenterWorld(tempTile)) > freakOutRadius)
                { tilemapSeen.SetTile(tempTile, occludedTile); clear = false; }

                Vector2 dir = tilemapSeen.GetCellCenterWorld(tempTile) - transform.position;
                if (clear && Physics2D.Raycast(transform.position, dir, dir.magnitude,wallLayer).transform != null)
                { tilemapSeen.SetTile(tempTile, occludedTile); clear = false; }
            }
    }

    void NewFreakOutAnim()
    {
        Vector3 newTarget;
        int loops = 0;
        bool newLocale;
        bool inRange;
        do
        {
            newTarget = Random.insideUnitCircle.normalized * Random.Range(minDistanceToProvoke + 0.1f, freakOutRadius);
            targetAStar.position = newTarget + transform.position;
            Vector3 dir = targetAStar.position - tilemapSeen.transform.position;
            newLocale = tilemapSeen.GetTile(Vector3Int.FloorToInt(dir / tilemapSeen.layoutGrid.cellSize.x)) == occludedTile;
            inRange = dir.magnitude < freakOutRadius;
            /*if(!newLocale)*/ loops++;
        } while ((loops <= 5 && !newLocale) || !inRange || Physics2D.Raycast(transform.position, newTarget, newTarget.magnitude, wallLayer).transform != null);
    }

    void SetFreakOutSeens()
    {
        int boxReach = (int)(freakOutRadius / tilemapSeen.layoutGrid.cellSize.x);
        Vector3Int tempTile;
        for (int i = -boxReach; i <= boxReach; i++)
            for (int j = -boxReach; j <= boxReach; j++)
            {
                tempTile = new Vector3Int(i, j, 0);
                if (tilemapSeen.GetTile(tempTile) == occludedTile)
                {
                    continue;
                }

                Vector3 dir = tilemapSeen.GetCellCenterWorld(tempTile) - transform.position;
                float angle = Vector2.Angle(transform.up, dir);
                if (dir.magnitude <= sight.viewConeDistance
                    && (Mathf.Abs(angle) <= sight.viewConeAngle))
                {
                    tilemapSeen.SetTile(tempTile, occludedTile);
                }
            }
    }
}
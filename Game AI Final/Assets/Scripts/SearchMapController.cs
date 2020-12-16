using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Pathfinding;

public struct SearchMapCellData
{
    public Vector3 pos;
    public int distanceFromLastSeenPlayerPos;
    public bool isExposed;
    public bool isSearchable;
}

public class SearchMapController : MonoBehaviour
{
    public LayerMask wallLayer;
    public Vector2 rectSizeOfMap;
    public Vector2 sizeOfCells;
    public Vector2Int numOfCells;
    public float exposureMapRefreshRate;
    Dictionary<Vector2Int,SearchMapCellData> cells;
    List<Vector2Int> searchCellsLeft;
    List<Vector2Int> searchCells;
    public Vector3 lastSeenPlayerPos;

    GameObject hunterAI;
    float timeTillRefresh;

    // Start is called before the first frame update
    void Start()
    {
        InitGrid();
        hunterAI = GameObject.FindObjectOfType<InfectedBrain>().gameObject;
        timeTillRefresh = exposureMapRefreshRate;

        //FindCellAtPos(Vector3.zero);
        //FindCellAtPos(Vector3.one);
        //FindCellAtPos(-Vector3.one);
        //FindCellAtPos(-Vector3.one * .4f);
    }


    private void Update()
    {
        if(hunterAI.GetComponent<InfectedBrain>().awareOfPlayer)
        {
            searchCellsLeft.Clear();
            searchCells.Clear();

            Vector2Int cellKey = FindCellAtPos(hunterAI.GetComponent<SightDetector>().player.transform.position);
            searchCells.Add(cellKey);
            searchCellsLeft.Add(cellKey);
            SearchMapCellData data = cells[cellKey];
            data.distanceFromLastSeenPlayerPos = 0;
            cells[cellKey] = data;
        }
        else if(hunterAI.GetComponent<InfectedBrain>().currentBehavior == BehaviorTypes.WANDER_TOWARDS)
        {
            int count = searchCells.Count; 
            for(int i = 0; i < count; i++)
            {
                Vector2Int key = searchCells[i];
                SearchKeyAttemptAdd(key, new Vector2Int(key.x + 1, key.y));
                SearchKeyAttemptAdd(key, new Vector2Int(key.x - 1, key.y));
                SearchKeyAttemptAdd(key, new Vector2Int(key.x, key.y + 1));
                SearchKeyAttemptAdd(key, new Vector2Int(key.x, key.y - 1));
            }

            if(Vector2.Distance(hunterAI.GetComponent<InfectedBrain>().targetAStar.position,hunterAI.transform.position) <= hunterAI.GetComponent<AIPath>().endReachedDistance * 1)
            {
                GoToNextPoint();
            }
        }
    }

    public void GoToNextPoint()
    {
        if(searchCellsLeft.Count == 0)
        {
            hunterAI.GetComponent<InfectedBrain>().BeginBehavior(BehaviorTypes.IDLE);
            return;
        }

        searchCellsLeft.Remove(FindCellAtPos(hunterAI.GetComponent<InfectedBrain>().targetAStar.position));

        if (searchCellsLeft.Count == 0)
        {
            hunterAI.GetComponent<InfectedBrain>().BeginBehavior(BehaviorTypes.IDLE);
            return;
        }

        Vector2Int target = searchCellsLeft[0];
        SearchMapCellData smcdTarget = cells[target];
        float priorityTarget = smcdTarget.distanceFromLastSeenPlayerPos * sizeOfCells.x + Vector2.Distance(smcdTarget.pos, hunterAI.transform.position);
        foreach (Vector2Int tmp in searchCellsLeft)
        {
            SearchMapCellData smcdTmp = cells[tmp];
            float priorityTmp = smcdTmp.distanceFromLastSeenPlayerPos + Vector2.Distance(smcdTmp.pos, hunterAI.transform.position);
            if(priorityTarget > priorityTmp)
            {
                priorityTarget = priorityTmp;
                target = tmp;
            }
        }
        hunterAI.GetComponent<InfectedBrain>().targetAStar.transform.position = cells[target].pos;
    }

    void SearchKeyAttemptAdd(Vector2Int key, Vector2Int temp)
    {
        if (!searchCells.Contains(temp) && cells.ContainsKey(temp))
        {
            searchCells.Add(temp);
            searchCellsLeft.Add(temp);

            SearchMapCellData data = cells[temp];
            data.distanceFromLastSeenPlayerPos = cells[key].distanceFromLastSeenPlayerPos + 1;
            cells[temp] = data;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        timeTillRefresh -= Time.fixedDeltaTime;
        if(timeTillRefresh <= 0)
        {
            RefreshExposure();
            timeTillRefresh = exposureMapRefreshRate;
        }
    }

    void RefreshExposure()
    {
        List<Vector2Int> e = new List<Vector2Int>(cells.Keys);
        foreach (Vector2Int entry in e)
        {
            SearchMapCellData data = cells[entry];
            Vector2 dir = data.pos - hunterAI.transform.position;
            data.isExposed = Vector2.Angle(dir,hunterAI.transform.up) < hunterAI.GetComponent<SightDetector>().viewConeAngle/2 
                                && dir.magnitude <= hunterAI.GetComponent<SightDetector>().viewConeDistance
                                && Physics2D.Raycast(hunterAI.transform.position,dir,dir.magnitude,wallLayer).transform == null;
            if(data.isExposed && hunterAI.GetComponent<InfectedBrain>().currentBehavior == BehaviorTypes.WANDER_TOWARDS && searchCellsLeft.Contains(entry))
            {
                searchCellsLeft.Remove(entry);
            }
            cells[entry] = data;
        }
    }

    public Vector2Int FindCellAtPos(Vector3 pos)
    {
        Vector2Int answer = new Vector2Int(Mathf.FloorToInt((pos.x /*- sizeOfCells.x/2.0f*/) / sizeOfCells.x), 
                                                Mathf.FloorToInt((pos.y /*- sizeOfCells.y/2.0f*/) / sizeOfCells.y));
        //Debug.LogWarning(answer);
        return answer;
    }

    void InitGrid()
    {
        searchCellsLeft = new List<Vector2Int> ();
        searchCells = new List<Vector2Int> ();
        cells = new Dictionary<Vector2Int, SearchMapCellData>();
        numOfCells = new Vector2Int(Mathf.FloorToInt(rectSizeOfMap.x / sizeOfCells.x), Mathf.FloorToInt(rectSizeOfMap.y / sizeOfCells.y));

        for(int i = -(numOfCells.x/2); i < numOfCells.x - (numOfCells.x / 2); i++)
            for(int j = -(numOfCells.y/2); j < numOfCells.y - (numOfCells.y / 2); j++)
            {
                SearchMapCellData data;
                data.isExposed = false;
                data.isSearchable = false;
                data.distanceFromLastSeenPlayerPos = 0;
                data.pos = new Vector2((i + 0.5f) * sizeOfCells.x, (j + 0.5f) * sizeOfCells.y);

                if (Physics2D.Raycast(data.pos, Vector2.up, 0.1f, wallLayer).transform != null)
                { continue; }

                Vector2Int key = new Vector2Int(i, j);
                cells.Add(key, data);
            }
    }

    private void OnDrawGizmos()
    {
        if(cells != null)
        foreach (KeyValuePair<Vector2Int, SearchMapCellData> entry in cells)
        {
            Gizmos.color = Color.white;
            if (entry.Value.isExposed) Gizmos.color = Color.red;
            else if (searchCellsLeft.Contains(entry.Key)) Gizmos.color = Color.Lerp(Color.green,Color.white,searchCellsLeft.IndexOf(entry.Key) * 1.0f /searchCellsLeft.Count);
            Gizmos.DrawSphere(cells[entry.Key].pos, 0.1f);
        }
    }

}

using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] int trashCount = 3;
    [SerializeField] bool replaceOnRespawn = true;
    [SerializeField] GameObject trashPrefab;

    public static GameManager instance;

    SortedSet<int> usedSpawns = new();
    public void Restart()
    {
        if(replaceOnRespawn)
            ReplaceTrashcans();
    }

    private void ReplaceTrashcans()
    {
        usedSpawns.Clear();
        while (trashPool.Count < trashCount)
            trashPool.Add(Instantiate(trashPrefab, transform));
        for (int i = 0; i < trashPool.Count; i++)
        {
            var spawnInx = -1;

            while (spawnInx < 0 || spawnInx >= trashSpawners.Count || usedSpawns.Contains(spawnInx))
                spawnInx = Random.Range(0, trashSpawners.Count);

            trashPool[i].transform.SetPositionAndRotation(trashSpawners[spawnInx].transform.position, trashSpawners[spawnInx].transform.rotation);
        }
    }

    private List<GameObject> trashSpawners = new();
    private List<GameObject> trashPool = new();

    private void Awake()
    {
        instance = this;
        GameObject.FindGameObjectsWithTag("TrashSpawn", trashSpawners);
        var lTerrain = LayerMask.GetMask("Terrain");
        for (int i = 0; i < trashSpawners.Count; i++)
        {
            var tr = trashSpawners[i].transform;
            if (Physics.Raycast(tr.position + Vector3.up * 5, Vector3.down, out var rhi, 100, lTerrain))
                tr.position = rhi.point;
        }
    }

    void Start()
    {
        ReplaceTrashcans();       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

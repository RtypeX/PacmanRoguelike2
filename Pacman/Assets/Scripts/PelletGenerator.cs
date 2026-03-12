using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PelletGenerator : MonoBehaviour
{
    [Header("References")]
    public Tilemap mapTilemap;
    public Tilemap wallsTilemap;
    public GameObject pelletPrefab;
    public GameObject powerPelletPrefab;
    public Transform pelletParent;

    [Header("Generation")]
    public bool generateOnStart = true;

    [Tooltip("Pac-Man spawn cell to skip.")]
    public Vector2Int pacmanSpawnCell = new Vector2Int(0, 0);

    [Tooltip("Optional rectangular area to skip, e.g. ghost house.")]
    public bool skipGhostHouse = false;
    public Vector2Int ghostHouseMin = new Vector2Int(-1, -1);
    public Vector2Int ghostHouseMax = new Vector2Int(1, 1);

    [Header("Power Pellets")]
    [Tooltip("If true, randomly picks some pellet spots as power pellets.")]
    public bool useRandomPowerPellets = true;

    [Tooltip("Maximum number of power pellets to place.")]
    public int powerPelletCount = 4;

    [Tooltip("Minimum distance in cells from Pac-Man spawn.")]
    public float minPowerPelletDistanceFromSpawn = 6f;

    private void Start()
    {
        if (generateOnStart)
            GeneratePellets();
    }

    public void GeneratePellets()
    {
        if (mapTilemap == null || wallsTilemap == null || pelletPrefab == null)
        {
            Debug.LogWarning("PelletGenerator missing references.");
            return;
        }

        ClearExistingPellets();

        List<Vector3Int> validCells = new List<Vector3Int>();
        BoundsInt bounds = mapTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!mapTilemap.HasTile(cell))
                continue;

            if (wallsTilemap.HasTile(cell))
                continue;

            if (cell.x == pacmanSpawnCell.x && cell.y == pacmanSpawnCell.y)
                continue;

            if (skipGhostHouse &&
                cell.x >= ghostHouseMin.x && cell.x <= ghostHouseMax.x &&
                cell.y >= ghostHouseMin.y && cell.y <= ghostHouseMax.y)
            {
                continue;
            }

            validCells.Add(cell);
        }

        HashSet<Vector3Int> powerPelletCells = new HashSet<Vector3Int>();

        if (useRandomPowerPellets && powerPelletPrefab != null && validCells.Count > 0)
        {
            List<Vector3Int> candidates = new List<Vector3Int>();

            foreach (Vector3Int cell in validCells)
            {
                float dist = Vector2.Distance(
                    new Vector2(cell.x, cell.y),
                    new Vector2(pacmanSpawnCell.x, pacmanSpawnCell.y)
                );

                if (dist >= minPowerPelletDistanceFromSpawn)
                    candidates.Add(cell);
            }

            Shuffle(candidates);

            int count = Mathf.Min(powerPelletCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                powerPelletCells.Add(candidates[i]);
            }
        }

        foreach (Vector3Int cell in validCells)
        {
            Vector3 worldPos = mapTilemap.GetCellCenterWorld(cell);

            GameObject prefabToSpawn = powerPelletCells.Contains(cell) ? powerPelletPrefab : pelletPrefab;
            GameObject spawned = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);

            if (pelletParent != null)
                spawned.transform.SetParent(pelletParent);
        }
    }

    private void ClearExistingPellets()
    {
        if (pelletParent == null) return;

        for (int i = pelletParent.childCount - 1; i >= 0; i--)
        {
            Destroy(pelletParent.GetChild(i).gameObject);
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
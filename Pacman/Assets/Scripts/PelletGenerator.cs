using UnityEngine;
using UnityEngine.Tilemaps;

public class PelletGenerator : MonoBehaviour
{
    [Header("References")]
    public Tilemap mapTilemap;       // full maze area / layout tilemap
    public Tilemap wallsTilemap;     // wall tilemap only
    public GameObject pelletPrefab;  // your pellet prefab
    public Transform pelletParent;   // empty parent object for organization

    [Header("Options")]
    public bool generateOnStart = true;

    [Tooltip("Cells to skip even if open. Size is in grid cells.")]
    public Vector2Int pacmanSpawnCell = new Vector2Int(0, 0);

    [Tooltip("Optional rectangular area to skip, e.g. ghost house.")]
    public bool skipGhostHouse = true;
    public Vector2Int ghostHouseMin = new Vector2Int(-10, -2);
    public Vector2Int ghostHouseMax = new Vector2Int(1, 2);

    private void Start()
    {
        if (generateOnStart)
            GeneratePellets();
    }

    public void GeneratePellets()
    {
        if (mapTilemap == null || wallsTilemap == null || pelletPrefab == null)
        {
            Debug.LogWarning("PelletGenerator is missing references.");
            return;
        }

        BoundsInt bounds = mapTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            // only generate inside the actual map footprint
            if (!mapTilemap.HasTile(cell))
                continue;

            // don't generate on walls
            if (wallsTilemap.HasTile(cell))
                continue;

            // skip Pac-Man spawn
            if (cell.x == pacmanSpawnCell.x && cell.y == pacmanSpawnCell.y)
                continue;

            // optionally skip ghost house rectangle
            if (skipGhostHouse &&
                cell.x >= ghostHouseMin.x && cell.x <= ghostHouseMax.x &&
                cell.y >= ghostHouseMin.y && cell.y <= ghostHouseMax.y)
            {
                continue;
            }

            Vector3 worldPos = mapTilemap.GetCellCenterWorld(cell);
            GameObject pellet = Instantiate(pelletPrefab, worldPos, Quaternion.identity);

            if (pelletParent != null)
                pellet.transform.SetParent(pelletParent);
        }
    }
}
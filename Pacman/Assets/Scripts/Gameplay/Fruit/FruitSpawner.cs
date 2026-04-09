using System.Collections.Generic;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    [Header("Spawn Setup")]
    public FruitPickup fruitPrefab;
    [Tooltip("Optional manual fallback points. If empty, the spawner will use a random pellet position instead.")]
    public List<Transform> spawnPoints = new List<Transform>();
    [Tooltip("Order these from worst to best, e.g. Cherry, Strawberry, Orange, Apple, Melon.")]
    public List<FruitData> availableFruits = new List<FruitData>();

    [Header("Behavior")]
    public bool spawnAtLevelStartIfUnlocked = true;
    public bool keepOnlyOneActiveFruit = true;
    [Min(1)] public int levelsPerFruitTier = 2;

    private FruitPickup activeFruit;

    public void TrySpawnForLevelStart()
    {
        if (!spawnAtLevelStartIfUnlocked)
        {
            Debug.Log("FruitSpawner.TrySpawnForLevelStart skipped because spawnAtLevelStartIfUnlocked is disabled.");
            return;
        }

        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.FruitUnlocked)
        {
            Debug.LogWarning($"FruitSpawner.TrySpawnForLevelStart skipped. CurrencyManager exists={CurrencyManager.Instance != null}, fruitUnlocked={(CurrencyManager.Instance != null && CurrencyManager.Instance.FruitUnlocked)}");
            return;
        }

        Debug.Log("FruitSpawner.TrySpawnForLevelStart passed unlock check and will try to spawn fruit.");
        SpawnRandomFruit();
    }

    [ContextMenu("Spawn Random Fruit")]
    public FruitPickup SpawnRandomFruit()
    {
        if (fruitPrefab == null || availableFruits.Count == 0)
        {
            Debug.LogWarning($"FruitSpawner cannot spawn fruit. fruitPrefab assigned={fruitPrefab != null}, availableFruits={availableFruits.Count}");
            return null;
        }

        if (keepOnlyOneActiveFruit && activeFruit != null)
        {
            Destroy(activeFruit.gameObject);
            activeFruit = null;
        }

        FruitData fruitData = GetFruitForCurrentLevel();
        if (fruitData == null)
        {
            Debug.LogWarning("FruitSpawner could not resolve fruit data for this level.");
            return null;
        }

        Vector3 spawnPosition;
        GameObject replacedPellet = null;

        if (!TryGetSpawnLocation(out spawnPosition, out replacedPellet))
        {
            Debug.LogWarning("FruitSpawner could not find a valid spawn location. Add spawn points or make sure pellets exist.");
            return null;
        }

        FruitPickup fruit = Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);
        fruit.Configure(fruitData, replacedPellet);
        activeFruit = fruit;

        int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
        Debug.Log($"FruitSpawner spawned {fruitData.fruitName} at {spawnPosition} for level {level}.");
        return fruit;
    }

    [ContextMenu("Clear Active Fruit")]
    public void ClearActiveFruit()
    {
        if (activeFruit != null)
        {
            activeFruit.RestoreReplacedPellet();
            DestroyImmediate(activeFruit.gameObject);
            activeFruit = null;
        }
    }

    private FruitData GetFruitForCurrentLevel()
    {
        if (availableFruits.Count == 0)
            return null;

        int currentLevel = GameManager.Instance != null ? Mathf.Max(1, GameManager.Instance.CurrentLevel) : 1;
        int highestUnlockedIndex = Mathf.Clamp((currentLevel - 1) / Mathf.Max(1, levelsPerFruitTier), 0, availableFruits.Count - 1);
        int chosenIndex = Random.Range(0, highestUnlockedIndex + 1);

        FruitData chosenFruit = availableFruits[chosenIndex];
        Debug.Log($"FruitSpawner.GetFruitForCurrentLevel level={currentLevel}, highestUnlockedIndex={highestUnlockedIndex}, chosenIndex={chosenIndex}, chosenFruit={(chosenFruit != null ? chosenFruit.fruitName : "NULL")}");
        return chosenFruit != null ? chosenFruit : availableFruits[0];
    }

    private bool TryGetSpawnLocation(out Vector3 spawnPosition, out GameObject replacedPellet)
    {
        spawnPosition = Vector3.zero;
        replacedPellet = null;

        PelletGenerator pelletGenerator = FindObjectOfType<PelletGenerator>();
        if (pelletGenerator != null)
        {
            Debug.Log("FruitSpawner.TryGetSpawnLocation found PelletGenerator and is asking it for a pellet.");
            replacedPellet = pelletGenerator.GetRandomSpawnedPellet(false);
        }
        else
        {
            Debug.LogWarning("FruitSpawner.TryGetSpawnLocation could not find PelletGenerator in the scene.");
        }

        if (replacedPellet == null)
        {
            GameObject[] pellets = GameObject.FindGameObjectsWithTag("Pellet");
            List<GameObject> validPellets = new List<GameObject>();
            foreach (GameObject pellet in pellets)
            {
                if (pellet != null && pellet.activeInHierarchy)
                    validPellets.Add(pellet);
            }

            if (validPellets.Count > 0)
            {
                replacedPellet = validPellets[Random.Range(0, validPellets.Count)];
                Debug.Log($"FruitSpawner.TryGetSpawnLocation fallback chose pellet {replacedPellet.name} from tag search. Candidate count={validPellets.Count}");
            }
            else
            {
                Debug.LogWarning("FruitSpawner.TryGetSpawnLocation fallback tag search found zero active pellets.");
            }
        }

        if (replacedPellet != null)
        {
            spawnPosition = replacedPellet.transform.position;
            replacedPellet.SetActive(false);
            Debug.Log($"FruitSpawner.TryGetSpawnLocation replacing pellet at {spawnPosition}.");
            return true;
        }

        if (spawnPoints.Count > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            if (spawnPoint != null)
            {
                spawnPosition = spawnPoint.position;
                Debug.Log($"FruitSpawner.TryGetSpawnLocation using manual fallback spawn point at {spawnPosition}.");
                return true;
            }
        }

        Debug.LogWarning("FruitSpawner.TryGetSpawnLocation failed to resolve any spawn location.");
        return false;
    }
}

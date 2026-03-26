using UnityEngine;

/// <summary>
/// CurrencyManager - Tracks both currencies across scenes.
/// Attach to the same persistent GameManager GameObject (DontDestroyOnLoad).
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    // Points - earned from pellets, ghosts, fruits
    public int Points { get; private set; } = 0;

    // Fruit Currency - earned from fruits, unlocked via upgrade
    public int FruitCurrency { get; private set; } = 0;

    // Whether fruit currency is unlocked yet
    public bool FruitUnlocked { get; private set; } = false;

    // Events for UI to listen to
    public static event System.Action<int> OnPointsChanged;
    public static event System.Action<int> OnFruitCurrencyChanged;
    public static event System.Action OnFruitUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- Points -------------------------------------------------------------

    public void AddPoints(int amount)
    {
        Points += amount;
        OnPointsChanged?.Invoke(Points);
    }

    public bool SpendPoints(int amount)
    {
        if (Points < amount) return false;
        Points -= amount;
        OnPointsChanged?.Invoke(Points);
        return true;
    }

    // ---- Fruit Currency -----------------------------------------------------

    public void AddFruitCurrency(int amount)
    {
        if (!FruitUnlocked) return;
        FruitCurrency += amount;
        OnFruitCurrencyChanged?.Invoke(FruitCurrency);
    }

    public bool SpendFruitCurrency(int amount)
    {
        if (FruitCurrency < amount) return false;
        FruitCurrency -= amount;
        OnFruitCurrencyChanged?.Invoke(FruitCurrency);
        return true;
    }

    public void UnlockFruit()
    {
        FruitUnlocked = true;
        OnFruitUnlocked?.Invoke();
        GameManager.Instance?.UnlockFruit();
    }

    // ---- Reset (new run) ----------------------------------------------------

    public void ResetForNewRun()
    {
        Points = 0;
        FruitCurrency = 0;
        FruitUnlocked = false;
        OnPointsChanged?.Invoke(Points);
        OnFruitCurrencyChanged?.Invoke(FruitCurrency);
    }
}
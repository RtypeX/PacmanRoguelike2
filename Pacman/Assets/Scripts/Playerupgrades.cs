using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PlayerUpgrades - Stores all upgrades the player has collected this run.
/// Lives on the GameManager GameObject (DontDestroyOnLoad).
///
/// Currency is owned entirely by CurrencyManager — do not store Points or
/// FruitCurrency here. Use CurrencyManager.Instance to read/spend currency.
/// </summary>
public class PlayerUpgrades : MonoBehaviour
{
    public static PlayerUpgrades Instance { get; private set; }

    // Upgrade state
    public float SpeedBonus { get; private set; } = 0f;
    public float TimerBonus { get; private set; } = 0f;
    public float PowerDurationBonus { get; private set; } = 0f;
    public float ScoreMultiplier { get; private set; } = 1f;
    public bool FruitUnlocked { get; private set; } = false;
    public int BonusLives { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- Currency convenience (delegates to CurrencyManager) ---------------

    /// <summary>
    /// Shortcut so callers that previously used PlayerUpgrades.AddPoints still work.
    /// Routes through CurrencyManager so there is only one source of truth.
    /// </summary>
    public void AddPoints(int amount)
    {
        CurrencyManager.Instance?.AddPoints(amount);
    }

    /// <summary>
    /// Shortcut for fruit currency — only adds if fruit is unlocked.
    /// </summary>
    public void AddFruitCurrency(int amount)
    {
        CurrencyManager.Instance?.AddFruitCurrency(amount);
    }

    // ---- Affordability check -----------------------------------------------

    public bool CanAfford(UpgradeData upgrade)
    {
        if (CurrencyManager.Instance == null) return false;

        return upgrade.costType == CurrencyType.Points
            ? CurrencyManager.Instance.Points >= upgrade.cost
            : CurrencyManager.Instance.FruitCurrency >= upgrade.cost;
    }

    // ---- Apply upgrade ------------------------------------------------------

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        if (!CanAfford(upgrade)) return;

        // Deduct cost via CurrencyManager
        if (upgrade.costType == CurrencyType.Points)
            CurrencyManager.Instance.SpendPoints(upgrade.cost);
        else
            CurrencyManager.Instance.SpendFruitCurrency(upgrade.cost);

        // Apply effect
        switch (upgrade.upgradeType)
        {
            case UpgradeType.TimerBonus:
                TimerBonus += upgrade.upgradeValue;
                GameManager.Instance?.UpgradeTimerDuration(upgrade.upgradeValue);
                break;

            case UpgradeType.ExtraLife:
                BonusLives += (int)upgrade.upgradeValue;
                FindObjectOfType<PacmanController>()?.UpgradeMaxLives((int)upgrade.upgradeValue);
                break;

            case UpgradeType.MoveSpeedBonus:
                SpeedBonus += upgrade.upgradeValue;
                FindObjectOfType<PacmanController>()?.UpgradeMoveSpeed(upgrade.upgradeValue);
                break;

            case UpgradeType.PowerPelletDuration:
                PowerDurationBonus += upgrade.upgradeValue;
                FindObjectOfType<PacmanController>()?.UpgradePowerDuration(upgrade.upgradeValue);
                break;

            case UpgradeType.UnlockFruit:
                FruitUnlocked = true;
                CurrencyManager.Instance?.UnlockFruit();   // UnlockFruit also calls GameManager
                break;

            case UpgradeType.ScoreMultiplier:
                ScoreMultiplier += upgrade.upgradeValue;
                break;
        }
    }
}

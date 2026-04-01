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
    private readonly Dictionary<UpgradeType, int> upgradeTiers = new Dictionary<UpgradeType, int>();

    // Upgrade state
    public float SpeedBonus { get; private set; } = 0f;
    public float TimerBonus { get; private set; } = 0f;
    public float PowerDurationBonus { get; private set; } = 0f;
    public float ScoreMultiplier { get; private set; } = 1f;
    public bool FruitUnlocked { get; private set; } = false;
    public int BonusLives { get; private set; } = 0;

    // ExtraPowerPellets: total bonus pellets to spawn each level
    public int BonusPowerPellets { get; private set; } = 0;

    // GhostFreeze: total freeze seconds applied at level start
    public float GhostFreezeDuration { get; private set; } = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- Reset (called by SettingsManager full reset) -----------------------

    public void ResetUpgrades()
    {
        SpeedBonus = 0f;
        TimerBonus = 0f;
        PowerDurationBonus = 0f;
        ScoreMultiplier = 1f;
        FruitUnlocked = false;
        BonusLives = 0;
        BonusPowerPellets = 0;
        GhostFreezeDuration = 0f;
        upgradeTiers.Clear();
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

    public bool CanAfford(CurrencyType costType, int cost)
    {
        if (CurrencyManager.Instance == null) return false;

        return costType == CurrencyType.Points
            ? CurrencyManager.Instance.Points >= cost
            : CurrencyManager.Instance.FruitCurrency >= cost;
    }

    public int GetTier(UpgradeType upgradeType)
    {
        return upgradeTiers.TryGetValue(upgradeType, out int tier) ? tier : 0;
    }

    public int GetTier(UpgradeData upgrade)
    {
        return upgrade == null ? 0 : GetTier(upgrade.upgradeType);
    }

    // ---- Apply upgrade ------------------------------------------------------

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        if (!CanAfford(upgrade)) return;
        ApplyUpgrade(upgrade, upgrade.cost, upgrade.upgradeValue);
    }

    public void ApplyUpgrade(UpgradeData upgrade, int cost, float upgradeValue)
    {
        if (upgrade == null || !CanAfford(upgrade.costType, cost)) return;

        // Deduct cost via CurrencyManager
        if (upgrade.costType == CurrencyType.Points)
            CurrencyManager.Instance.SpendPoints(cost);
        else
            CurrencyManager.Instance.SpendFruitCurrency(cost);

        // Apply effect
        switch (upgrade.upgradeType)
        {
            case UpgradeType.TimerBonus:
                TimerBonus += upgradeValue;
                GameManager.Instance?.UpgradeTimerDuration(upgradeValue);
                break;

            case UpgradeType.ExtraLife:
                BonusLives += (int)upgradeValue;
                FindObjectOfType<testMove>()?.UpgradeMaxLives((int)upgradeValue);
                break;

            case UpgradeType.MoveSpeedBonus:
                SpeedBonus += upgradeValue;
                FindObjectOfType<testMove>()?.UpgradeMoveSpeed(upgradeValue);
                break;

            case UpgradeType.PowerPelletDuration:
                PowerDurationBonus += upgradeValue;
                FindObjectOfType<testMove>()?.UpgradePowerDuration(upgradeValue);
                ManageHUD.Instance?.SetPowerUpMaxDuration(8f + PowerDurationBonus);
                break;

            case UpgradeType.UnlockFruit:
                FruitUnlocked = true;
                CurrencyManager.Instance?.UnlockFruit();   // UnlockFruit also calls GameManager
                break;

            case UpgradeType.ScoreMultiplier:
                ScoreMultiplier += upgradeValue;
                break;

            case UpgradeType.ExtraPowerPellets:
                // Accumulate total bonus pellets; GameManager reads this on level init
                BonusPowerPellets += (int)upgradeValue;
                GameManager.Instance?.UpgradePowerPelletCount((int)upgradeValue);
                break;

            case UpgradeType.GhostFreeze:
                // Accumulate total freeze duration; GameManager applies it on level start
                GhostFreezeDuration += upgradeValue;
                GameManager.Instance?.UpgradeGhostFreeze(upgradeValue);
                break;
        }

        upgradeTiers[upgrade.upgradeType] = GetTier(upgrade.upgradeType) + 1;
    }
}

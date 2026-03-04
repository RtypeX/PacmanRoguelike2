using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PlayerUpgrades - Stores all upgrades the player has collected this run.
/// Lives on the GameManager GameObject (DontDestroyOnLoad).
/// </summary>
public class PlayerUpgrades : MonoBehaviour
{
    public static PlayerUpgrades Instance { get; private set; }

    // Current currencies
    public int Points { get; private set; } = 0;
    public int FruitCurrency { get; private set; } = 0;

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

    // Called by PacmanController event
    public void AddPoints(int amount)
    {
        Points += Mathf.RoundToInt(amount * ScoreMultiplier);
    }

    public void AddFruitCurrency(int amount) => FruitCurrency += amount;

    public bool CanAfford(UpgradeData upgrade)
    {
        return upgrade.costType == CurrencyType.Points
            ? Points >= upgrade.cost
            : FruitCurrency >= upgrade.cost;
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        if (!CanAfford(upgrade)) return;

        // Deduct cost
        if (upgrade.costType == CurrencyType.Points) Points -= upgrade.cost;
        else FruitCurrency -= upgrade.cost;

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
                GameManager.Instance?.UnlockFruit();
                break;

            case UpgradeType.ScoreMultiplier:
                ScoreMultiplier += upgrade.upgradeValue;
                break;
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UpgradeData - ScriptableObject defining a single upgrade.
/// Create via right-click -> Create -> Upgrades -> Upgrade
/// </summary>
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Upgrades/Upgrade")]
public class UpgradeData : ScriptableObject
{
    [Header("Display")]
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Cost")]
    public CurrencyType costType;
    public int cost;

    [Header("Effect")]
    public UpgradeType upgradeType;
    public float upgradeValue;
}

public enum CurrencyType { Points, FruitCurrency }

public enum UpgradeType
{
    TimerBonus,         // +N seconds to timer
    ExtraLife,          // +N lives
    MoveSpeedBonus,     // +N move speed
    PowerPelletDuration,// +N seconds blue ghost duration
    UnlockFruit,        // enables fruit spawning + fruit currency
    ScoreMultiplier,    // pellets worth Nx
    ExtraPowerPellets,  // more power pellets in maze
    GhostFreeze         // ghosts pause on level start
}
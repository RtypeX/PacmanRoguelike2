using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UpgradeScreenManager - Handles the roguelite upgrade screen.
/// Attach to a Canvas in your UpgradeScene.
///
/// SCENE SETUP NEEDED:
/// - Canvas
///   - Background (Image - dark overlay)
///   - HeaderPanel
///     - TitleText          (TMP - "CHOOSE AN UPGRADE")
///     - LevelText          (TMP - "LEVEL 3 COMPLETE")
///     - PointsText         (TMP - shows current points)
///     - FruitCurrencyGroup (GameObject - hidden until fruit unlocked)
///       - FruitCurrencyText (TMP)
///   - CardsContainer       (Horizontal Layout Group)
///     - Card prefab x3 (assigned below)
///   - ContinueButton       (Button - "CONTINUE WITHOUT UPGRADE")
/// </summary>
public class UpgradeScreenManager : MonoBehaviour
{
    [Header("Header UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI pointsText;
    public GameObject fruitCurrencyGroup;
    public TextMeshProUGUI fruitCurrencyText;

    [Header("Cards")]
    public Transform cardsContainer;
    public GameObject upgradeCardPrefab;

    [Header("Continue Button")]
    public Button continueButton;

    [Header("All Possible Upgrades - drag ScriptableObjects here")]
    public List<UpgradeData> allUpgrades;

    private List<UpgradeData> offeredUpgrades = new List<UpgradeData>();

    private void Start()
    {
        RefreshCurrencyDisplay();
        SetupContinueButton();
        SpawnUpgradeCards();

        if (levelText != null)
            levelText.text = "LEVEL " + (GameManager.Instance?.CurrentLevel - 1) + " COMPLETE";

        bool fruitUnlocked = PlayerUpgrades.Instance?.FruitUnlocked ?? false;
        fruitCurrencyGroup?.SetActive(fruitUnlocked);
    }

    private void RefreshCurrencyDisplay()
    {
        if (PlayerUpgrades.Instance == null) return;
        if (pointsText != null)
            pointsText.text = "POINTS: " + PlayerUpgrades.Instance.Points;
        if (fruitCurrencyText != null)
            fruitCurrencyText.text = "FRUIT: " + PlayerUpgrades.Instance.FruitCurrency;
    }

    private void SetupContinueButton()
    {
        continueButton?.onClick.AddListener(() =>
        {
            GameManager.Instance?.OnUpgradesApplied();
        });
    }

    private void SpawnUpgradeCards()
    {
        // Clear existing cards
        foreach (Transform child in cardsContainer) Destroy(child.gameObject);
        offeredUpgrades.Clear();

        // Pick 3 random upgrades the player can see (affordability shown on card)
        List<UpgradeData> pool = new List<UpgradeData>(allUpgrades);
        int cardCount = Mathf.Min(3, pool.Count);

        for (int i = 0; i < cardCount; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            offeredUpgrades.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        // Spawn a card for each
        foreach (var upgrade in offeredUpgrades)
        {
            GameObject cardObj = Instantiate(upgradeCardPrefab, cardsContainer);
            UpgradeCard card = cardObj.GetComponent<UpgradeCard>();
            card?.Setup(upgrade, OnUpgradeSelected);
        }
    }

    private void OnUpgradeSelected(UpgradeData upgrade)
    {
        if (PlayerUpgrades.Instance == null) return;

        if (!PlayerUpgrades.Instance.CanAfford(upgrade))
        {
            // Flash the card red - handled in UpgradeCard
            return;
        }

        PlayerUpgrades.Instance.ApplyUpgrade(upgrade);
        RefreshCurrencyDisplay();

        // Refresh cards so affordability updates, or go straight to next level
        GameManager.Instance?.OnUpgradesApplied();
    }
}
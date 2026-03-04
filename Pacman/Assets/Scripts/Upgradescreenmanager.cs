using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UpgradeScreenManager - Shows one upgrade card at a time.
/// Player cycles through with arrows and picks one.
///
/// SCENE SETUP:
/// Canvas
/// ├── Background
/// ├── TitleText
/// ├── LevelText
/// ├── PointsText
/// ├── FruitCurrencyGroup
/// │   └── FruitCurrencyText
/// ├── CardPanel          (the single visible card)
/// │   ├── NameText
/// │   ├── DescriptionText
/// │   ├── CostText
/// │   └── CurrencyText
/// ├── LeftArrowButton    (< arrow)
/// ├── RightArrowButton   (> arrow)
/// ├── SelectButton       (PICK THIS)
/// ├── CantAffordText     (hidden by default)
/// └── ContinueButton     (skip upgrade)
/// </summary>
public class UpgradeScreenManager : MonoBehaviour
{
    [Header("Header UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI pointsText;
    public GameObject fruitCurrencyGroup;
    public TextMeshProUGUI fruitCurrencyText;

    [Header("Card Display")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI currencyText;
    public Image cardBackground;
    public Color affordableColor = new Color(0.1f, 0.1f, 0.24f, 1f);
    public Color lockedColor = new Color(0.15f, 0.05f, 0.05f, 1f);

    [Header("Navigation")]
    public Button leftArrowButton;
    public Button rightArrowButton;
    public TextMeshProUGUI pageIndicatorText; // shows "2 / 3"

    [Header("Action Buttons")]
    public Button selectButton;
    public TextMeshProUGUI cantAffordText;  // "NOT ENOUGH POINTS" hidden by default
    public Button continueButton;

    [Header("All Possible Upgrades")]
    public List<UpgradeData> allUpgrades;

    // Runtime
    private List<UpgradeData> offeredUpgrades = new List<UpgradeData>();
    private int currentIndex = 0;

    private void Start()
    {
        cantAffordText?.gameObject.SetActive(false);

        RefreshCurrencyDisplay();
        PickRandomUpgrades();
        DisplayCurrentCard();

        if (levelText != null)
        {
            int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel - 1 : 1;
            levelText.text = "LEVEL " + level + " COMPLETE";
        }

        bool fruitUnlocked = PlayerUpgrades.Instance?.FruitUnlocked ?? false;
        fruitCurrencyGroup?.SetActive(fruitUnlocked);

        // Wire buttons
        leftArrowButton?.onClick.AddListener(GoLeft);
        rightArrowButton?.onClick.AddListener(GoRight);
        selectButton?.onClick.AddListener(SelectCurrent);
        continueButton?.onClick.AddListener(() => GameManager.Instance?.OnUpgradesApplied());
    }

    private void PickRandomUpgrades()
    {
        offeredUpgrades.Clear();
        List<UpgradeData> pool = new List<UpgradeData>(allUpgrades);
        int count = Mathf.Min(3, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int r = Random.Range(0, pool.Count);
            offeredUpgrades.Add(pool[r]);
            pool.RemoveAt(r);
        }

        currentIndex = 0;
    }

    private void DisplayCurrentCard()
    {
        if (offeredUpgrades.Count == 0) return;

        UpgradeData upgrade = offeredUpgrades[currentIndex];
        bool canAfford = PlayerUpgrades.Instance?.CanAfford(upgrade) ?? true;

        // Fill card text
        if (nameText != null)        nameText.text = upgrade.upgradeName.ToUpper();
        if (descriptionText != null) descriptionText.text = upgrade.description;
        if (costText != null)        costText.text = upgrade.cost.ToString();
        if (currencyText != null)    currencyText.text = upgrade.costType == CurrencyType.Points ? "POINTS" : "FRUIT";

        // Card color shows affordability
        if (cardBackground != null)
            cardBackground.color = canAfford ? affordableColor : lockedColor;

        // Page indicator e.g. "2 / 3"
        if (pageIndicatorText != null)
            pageIndicatorText.text = (currentIndex + 1) + " / " + offeredUpgrades.Count;

        // Hide/show cant afford text
        cantAffordText?.gameObject.SetActive(!canAfford);

        // Hide arrows if only one upgrade
        leftArrowButton?.gameObject.SetActive(offeredUpgrades.Count > 1);
        rightArrowButton?.gameObject.SetActive(offeredUpgrades.Count > 1);
    }

    private void GoLeft()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = offeredUpgrades.Count - 1;
        cantAffordText?.gameObject.SetActive(false);
        DisplayCurrentCard();
    }

    private void GoRight()
    {
        currentIndex++;
        if (currentIndex >= offeredUpgrades.Count) currentIndex = 0;
        cantAffordText?.gameObject.SetActive(false);
        DisplayCurrentCard();
    }

    private void SelectCurrent()
    {
        if (offeredUpgrades.Count == 0) return;

        UpgradeData upgrade = offeredUpgrades[currentIndex];

        if (PlayerUpgrades.Instance != null && !PlayerUpgrades.Instance.CanAfford(upgrade))
        {
            cantAffordText?.gameObject.SetActive(true);
            return;
        }

        PlayerUpgrades.Instance?.ApplyUpgrade(upgrade);
        GameManager.Instance?.OnUpgradesApplied();
    }

    private void RefreshCurrencyDisplay()
    {
        if (PlayerUpgrades.Instance == null) return;
        if (pointsText != null)      pointsText.text = "POINTS: " + PlayerUpgrades.Instance.Points;
        if (fruitCurrencyText != null) fruitCurrencyText.text = "FRUIT: " + PlayerUpgrades.Instance.FruitCurrency;
    }
}
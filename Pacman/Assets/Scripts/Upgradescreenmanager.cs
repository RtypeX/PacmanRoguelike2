using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UpgradeScreenManager : MonoBehaviour
{
    [Header("Header UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI pointsText;
    public GameObject fruitCurrencyGroup;
    public TextMeshProUGUI fruitCurrencyText;

    [Header("Card Display")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI currencyText;
    public Image cardBackground;
    public Color affordableColor = new Color(0.1f, 0.1f, 0.24f, 1f);
    public Color lockedColor = new Color(0.15f, 0.05f, 0.05f, 1f);
    public Color purchasedColor = new Color(0.05f, 0.2f, 0.05f, 1f);

    [Header("Navigation")]
    public Button leftArrowButton;
    public Button rightArrowButton;
    public TextMeshProUGUI pageIndicatorText;

    [Header("Action Buttons")]
    public Button selectButton;
    public TextMeshProUGUI cantAffordText;
    public TextMeshProUGUI alreadyPurchasedText;
    public Button backButton;

    [Header("Upgrade Icons - assign one per upgrade type")]
    public Sprite iconTimerBonus;
    public Sprite iconExtraLife;
    public Sprite iconMoveSpeed;
    public Sprite iconPowerDuration;
    public Sprite iconUnlockFruit;
    public Sprite iconScoreMultiplier;
    public Sprite iconExtraPowerPellets;
    public Sprite iconGhostFreeze;
    public Sprite iconDefault;

    [Header("All Possible Upgrades")]
    public List<UpgradeData> allUpgrades;

    private List<UpgradeData> offeredUpgrades = new List<UpgradeData>();
    private int currentIndex = 0;
    private HashSet<int> purchasedIndexes = new HashSet<int>();

    private void Start()
    {
        if (PlayerUpgrades.Instance == null)
        {
            GameObject temp = new GameObject("TempPlayerUpgrades");
            temp.AddComponent<PlayerUpgrades>();
        }

        PlayerUpgrades.Instance.AddPoints(500);

        cantAffordText?.gameObject.SetActive(false);
        alreadyPurchasedText?.gameObject.SetActive(false);

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

        leftArrowButton?.onClick.AddListener(GoLeft);
        rightArrowButton?.onClick.AddListener(GoRight);
        selectButton?.onClick.AddListener(SelectCurrent);
        backButton?.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));
    }

    private void PickRandomUpgrades()
    {
        offeredUpgrades.Clear();
        purchasedIndexes.Clear();
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
        bool alreadyBought = purchasedIndexes.Contains(currentIndex);

        if (nameText != null)        nameText.text = upgrade.upgradeName.ToUpper();
        if (descriptionText != null) descriptionText.text = upgrade.description;
        if (costText != null)        costText.text = upgrade.cost.ToString();
        if (currencyText != null)    currencyText.text = upgrade.costType == CurrencyType.Points ? "POINTS" : "FRUIT";

        // Set icon based on upgrade type
        if (iconImage != null)
        {
            iconImage.sprite = GetIconForUpgrade(upgrade.upgradeType);
            iconImage.gameObject.SetActive(iconImage.sprite != null);
        }

        // Card color
        if (cardBackground != null)
        {
            if (alreadyBought)   cardBackground.color = purchasedColor;
            else if (!canAfford) cardBackground.color = lockedColor;
            else                 cardBackground.color = affordableColor;
        }

        if (pageIndicatorText != null)
            pageIndicatorText.text = (currentIndex + 1) + " / " + offeredUpgrades.Count;

        cantAffordText?.gameObject.SetActive(false);
        alreadyPurchasedText?.gameObject.SetActive(false);

        if (selectButton != null) selectButton.interactable = !alreadyBought;

        leftArrowButton?.gameObject.SetActive(offeredUpgrades.Count > 1);
        rightArrowButton?.gameObject.SetActive(offeredUpgrades.Count > 1);
    }

    private Sprite GetIconForUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.TimerBonus:          return iconTimerBonus       != null ? iconTimerBonus       : iconDefault;
            case UpgradeType.ExtraLife:           return iconExtraLife        != null ? iconExtraLife        : iconDefault;
            case UpgradeType.MoveSpeedBonus:      return iconMoveSpeed        != null ? iconMoveSpeed        : iconDefault;
            case UpgradeType.PowerPelletDuration: return iconPowerDuration    != null ? iconPowerDuration    : iconDefault;
            case UpgradeType.UnlockFruit:         return iconUnlockFruit      != null ? iconUnlockFruit      : iconDefault;
            case UpgradeType.ScoreMultiplier:     return iconScoreMultiplier  != null ? iconScoreMultiplier  : iconDefault;
            case UpgradeType.ExtraPowerPellets:   return iconExtraPowerPellets!= null ? iconExtraPowerPellets: iconDefault;
            case UpgradeType.GhostFreeze:         return iconGhostFreeze      != null ? iconGhostFreeze      : iconDefault;
            default:                              return iconDefault;
        }
    }

    private void GoLeft()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = offeredUpgrades.Count - 1;
        DisplayCurrentCard();
    }

    private void GoRight()
    {
        currentIndex++;
        if (currentIndex >= offeredUpgrades.Count) currentIndex = 0;
        DisplayCurrentCard();
    }

    private void SelectCurrent()
    {
        if (offeredUpgrades.Count == 0) return;

        if (purchasedIndexes.Contains(currentIndex))
        {
            alreadyPurchasedText?.gameObject.SetActive(true);
            return;
        }

        UpgradeData upgrade = offeredUpgrades[currentIndex];

        if (PlayerUpgrades.Instance != null && !PlayerUpgrades.Instance.CanAfford(upgrade))
        {
            cantAffordText?.gameObject.SetActive(true);
            return;
        }

        PlayerUpgrades.Instance?.ApplyUpgrade(upgrade);
        purchasedIndexes.Add(currentIndex);

        RefreshCurrencyDisplay();
        DisplayCurrentCard();
    }

    private void RefreshCurrencyDisplay()
    {
        if (PlayerUpgrades.Instance == null) return;
        if (pointsText != null)        pointsText.text = "POINTS: " + PlayerUpgrades.Instance.Points;
        if (fruitCurrencyText != null) fruitCurrencyText.text = "FRUIT: " + PlayerUpgrades.Instance.FruitCurrency;
    }
}
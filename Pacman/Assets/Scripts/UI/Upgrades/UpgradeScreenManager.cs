using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class UpgradeScreenManager : MonoBehaviour
{
    private class UpgradeOffer
    {
        public UpgradeData data;
        public int tier;
        public int maxTier;
        public int cost;
        public float value;
        public string displayName;
        public string displayDescription;
    }

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
    public Button rerollButton;
    public TextMeshProUGUI rerollButtonText;

    [Header("Upgrade Icons")]
    public Sprite iconTimerBonus;
    public Sprite iconExtraLife;
    public Sprite iconMoveSpeed;
    public Sprite iconPowerDuration;
    public Sprite iconUnlockFruit;
    public Sprite iconScoreMultiplier;
    public Sprite iconExtraPowerPellets;
    public Sprite iconGhostFreeze;
    public Sprite iconDefault;

    [Header("Animation")]
    public float slideDistance = 800f;
    public float slideDuration = 0.15f;
    public float rerollButtonOffset = 240f;
    public int rerollBaseCost = 35;
    public int rerollCostStep = 20;

    [Header("All Possible Upgrades")]
    public List<UpgradeData> allUpgrades;

    private List<UpgradeOffer> offeredUpgrades = new List<UpgradeOffer>();
    private int currentIndex = 0;
    private HashSet<int> purchasedIndexes = new HashSet<int>();
    private int rerollCount = 0;

    private void Start()
    {
        cantAffordText?.gameObject.SetActive(false);
        alreadyPurchasedText?.gameObject.SetActive(false);
        EnsureRerollButton();

        PickRandomUpgrades();
        DisplayCurrentCard();

        if (levelText != null)
        {
            int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel - 1 : 0;
            levelText.text = level > 0 ? "LEVEL " + level + " COMPLETE" : "NO LEVEL COMPLETED YET";
        }

        leftArrowButton?.onClick.AddListener(GoLeft);
        rightArrowButton?.onClick.AddListener(GoRight);
        selectButton?.onClick.AddListener(SelectCurrent);
        backButton?.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        rerollButton?.onClick.AddListener(RerollOffers);
        RefreshRerollButton();

        // Wait a frame for GameManager to finish adding test currencies
        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForEndOfFrame();

        RefreshCurrencyDisplay();

        bool fruitUnlocked = PlayerUpgrades.Instance?.FruitUnlocked ?? false;
        bool hasFruitFromTest = GameManager.Instance != null && GameManager.Instance.testStartFruit > 0;
        fruitCurrencyGroup?.SetActive(fruitUnlocked || hasFruitFromTest);
    }

    private void PickRandomUpgrades()
    {
        offeredUpgrades.Clear();
        purchasedIndexes.Clear();
        List<UpgradeOffer> pool = BuildOfferPool();
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
        if (offeredUpgrades.Count == 0)
        {
            if (nameText != null) nameText.text = "NO UPGRADES AVAILABLE";
            if (descriptionText != null) descriptionText.text = "You have reached the current cap for every upgrade.";
            if (costText != null) costText.text = "-";
            if (currencyText != null) currencyText.text = string.Empty;
            if (pageIndicatorText != null) pageIndicatorText.text = "0 / 0";
            if (selectButton != null) selectButton.interactable = false;
            leftArrowButton?.gameObject.SetActive(false);
            rightArrowButton?.gameObject.SetActive(false);
            RefreshRerollButton();
            return;
        }

        UpgradeOffer offer = offeredUpgrades[currentIndex];
        bool canAfford = PlayerUpgrades.Instance?.CanAfford(offer.data.costType, offer.cost) ?? true;
        bool alreadyBought = purchasedIndexes.Contains(currentIndex);

        if (nameText != null) nameText.text = offer.displayName.ToUpper();
        if (descriptionText != null) descriptionText.text = offer.displayDescription;
        if (costText != null) costText.text = offer.cost.ToString();
        if (currencyText != null) currencyText.text = offer.data.costType == CurrencyType.Points ? "POINTS" : "FRUIT";

        if (iconImage != null)
        {
            iconImage.sprite = GetIconForUpgrade(offer.data.upgradeType);
            iconImage.gameObject.SetActive(iconImage.sprite != null);
        }

        if (cardBackground != null)
        {
            if (alreadyBought) cardBackground.color = purchasedColor;
            else if (!canAfford) cardBackground.color = lockedColor;
            else cardBackground.color = affordableColor;
        }

        if (pageIndicatorText != null)
            pageIndicatorText.text = (currentIndex + 1) + " / " + offeredUpgrades.Count;

        cantAffordText?.gameObject.SetActive(false);
        alreadyPurchasedText?.gameObject.SetActive(false);

        if (selectButton != null)
            selectButton.interactable = !alreadyBought;

        leftArrowButton?.gameObject.SetActive(offeredUpgrades.Count > 1);
        rightArrowButton?.gameObject.SetActive(offeredUpgrades.Count > 1);
        RefreshRerollButton();

        // Card pop animation
        if (cardBackground != null)
        {
            cardBackground.transform.localScale = Vector3.one;
            LeanTween.scale(cardBackground.gameObject, Vector3.one * 1.05f, 0.1f).setEaseOutBack();
        }
    }

    private void SlideCard(int direction)
    {
        if (cardBackground == null) return;
        Vector3 startPos = new Vector3(slideDistance * direction, 0f, 0f);
        cardBackground.transform.localPosition = startPos;
        LeanTween.moveLocal(cardBackground.gameObject, Vector3.zero, slideDuration).setEaseOutCubic();
    }

    private Sprite GetIconForUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.TimerBonus: return iconTimerBonus ?? iconDefault;
            case UpgradeType.ExtraLife: return iconExtraLife ?? iconDefault;
            case UpgradeType.MoveSpeedBonus: return iconMoveSpeed ?? iconDefault;
            case UpgradeType.PowerPelletDuration: return iconPowerDuration ?? iconDefault;
            case UpgradeType.UnlockFruit: return iconUnlockFruit ?? iconDefault;
            case UpgradeType.ScoreMultiplier: return iconScoreMultiplier ?? iconDefault;
            case UpgradeType.ExtraPowerPellets: return iconExtraPowerPellets ?? iconDefault;
            case UpgradeType.GhostFreeze: return iconGhostFreeze ?? iconDefault;
            default: return iconDefault;
        }
    }

    private void GoLeft()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = offeredUpgrades.Count - 1;
        DisplayCurrentCard();
        SlideCard(-1);
    }

    private void GoRight()
    {
        currentIndex++;
        if (currentIndex >= offeredUpgrades.Count) currentIndex = 0;
        DisplayCurrentCard();
        SlideCard(1);
    }

    private void SelectCurrent()
    {
        if (offeredUpgrades.Count == 0) return;

        if (purchasedIndexes.Contains(currentIndex))
        {
            alreadyPurchasedText?.gameObject.SetActive(true);
            return;
        }

        UpgradeOffer offer = offeredUpgrades[currentIndex];

        if (PlayerUpgrades.Instance != null && !PlayerUpgrades.Instance.CanAfford(offer.data.costType, offer.cost))
        {
            cantAffordText?.gameObject.SetActive(true);
            return;
        }

        PlayerUpgrades.Instance?.ApplyUpgrade(offer.data, offer.cost, offer.value);
        purchasedIndexes.Add(currentIndex);

        RefreshCurrencyDisplay();
        DisplayCurrentCard();
    }

    private void RefreshCurrencyDisplay()
    {
        if (CurrencyManager.Instance == null) return;
        if (pointsText != null) pointsText.text = "POINTS: " + CurrencyManager.Instance.Points;
        if (fruitCurrencyText != null) fruitCurrencyText.text = "FRUIT: " + CurrencyManager.Instance.FruitCurrency;
    }

    private void RerollOffers()
    {
        int rerollCost = GetCurrentRerollCost();
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendPoints(rerollCost))
        {
            cantAffordText?.gameObject.SetActive(true);
            return;
        }

        rerollCount++;
        currentIndex = 0;
        PickRandomUpgrades();
        RefreshCurrencyDisplay();
        RefreshRerollButton();
        DisplayCurrentCard();
    }

    private void RefreshRerollButton()
    {
        if (rerollButtonText == null && rerollButton != null)
            rerollButtonText = rerollButton.GetComponentInChildren<TextMeshProUGUI>();

        int rerollCost = GetCurrentRerollCost();

        if (rerollButtonText != null)
            rerollButtonText.text = "REROLL " + rerollCost;

        if (rerollButton != null)
        {
            bool hasPool = BuildOfferPool().Count > 0;
            bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.Points >= rerollCost;
            rerollButton.interactable = hasPool && canAfford;
        }
    }

    private int GetCurrentRerollCost()
    {
        return rerollBaseCost + (rerollCount * rerollCostStep);
    }

    private void EnsureRerollButton()
    {
        if (rerollButton != null || backButton == null)
            return;

        rerollButton = Instantiate(backButton, backButton.transform.parent);
        rerollButton.name = "RerollButton";
        rerollButton.onClick.RemoveAllListeners();

        RectTransform backRect = backButton.GetComponent<RectTransform>();
        RectTransform rerollRect = rerollButton.GetComponent<RectTransform>();
        if (backRect != null && rerollRect != null)
            rerollRect.anchoredPosition = backRect.anchoredPosition + Vector2.left * rerollButtonOffset;

        rerollButtonText = rerollButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private List<UpgradeOffer> BuildOfferPool()
    {
        List<UpgradeOffer> pool = new List<UpgradeOffer>();
        foreach (UpgradeData upgrade in allUpgrades)
        {
            UpgradeOffer offer = CreateOffer(upgrade);
            if (offer != null)
                pool.Add(offer);
        }

        return pool;
    }

    private UpgradeOffer CreateOffer(UpgradeData upgrade)
    {
        if (upgrade == null)
            return null;

        int currentTier = PlayerUpgrades.Instance != null ? PlayerUpgrades.Instance.GetTier(upgrade) : 0;
        int maxTier = GetMaxTier(upgrade);

        if (currentTier >= maxTier)
            return null;

        int nextTier = currentTier + 1;
        return new UpgradeOffer
        {
            data = upgrade,
            tier = nextTier,
            maxTier = maxTier,
            cost = GetTierCost(upgrade, nextTier),
            value = GetTierValue(upgrade, nextTier),
            displayName = GetTieredName(upgrade, nextTier),
            displayDescription = GetTieredDescription(upgrade, nextTier, maxTier)
        };
    }

    private int GetMaxTier(UpgradeData upgrade)
    {
        switch (upgrade.upgradeType)
        {
            case UpgradeType.TimerBonus:
                return 5;
            case UpgradeType.MoveSpeedBonus:
                return 4;
            case UpgradeType.PowerPelletDuration:
                return 4;
            case UpgradeType.ExtraLife:
                return 2;
            case UpgradeType.ScoreMultiplier:
                return 3;
            case UpgradeType.ExtraPowerPellets:
                return 3;
            case UpgradeType.GhostFreeze:
                return 3;
            case UpgradeType.UnlockFruit:
            default:
                return 1;
        }
    }

    private int GetTierCost(UpgradeData upgrade, int tier)
    {
        float multiplier = 1f;
        switch (upgrade.costType)
        {
            case CurrencyType.Points:
                multiplier = 1f + ((tier - 1) * 0.55f);
                break;
            case CurrencyType.FruitCurrency:
                multiplier = 1f + ((tier - 1) * 0.7f);
                break;
        }

        if (upgrade.upgradeType == UpgradeType.ExtraLife)
            multiplier += (tier - 1) * 0.2f;

        if (upgrade.upgradeType == UpgradeType.ScoreMultiplier)
            multiplier += (tier - 1) * 0.25f;

        return Mathf.Max(1, Mathf.RoundToInt(upgrade.cost * multiplier));
    }

    private float GetTierValue(UpgradeData upgrade, int tier)
    {
        return upgrade.upgradeValue;
    }

    private string GetTieredName(UpgradeData upgrade, int tier)
    {
        if (GetMaxTier(upgrade) <= 1)
            return upgrade.upgradeName;

        return upgrade.upgradeName + " " + ToRoman(tier);
    }

    private string GetTieredDescription(UpgradeData upgrade, int tier, int maxTier)
    {
        string tierLine = maxTier > 1 ? "Tier " + ToRoman(tier) + " of " + ToRoman(maxTier) : "One-time upgrade";
        return tierLine + "\n" + upgrade.description.Trim();
    }

    private string ToRoman(int value)
    {
        switch (value)
        {
            case 1: return "I";
            case 2: return "II";
            case 3: return "III";
            case 4: return "IV";
            case 5: return "V";
            default: return value.ToString();
        }
    }
}

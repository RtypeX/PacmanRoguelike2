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

    [Header("Owned Upgrades")]
    public TextMeshProUGUI ownedUpgradesTitleText;
    public RectTransform ownedUpgradesContentRoot;
    public Color ownedUpgradeRowColor = new Color(0.08f, 0.08f, 0.18f, 0.92f);
    public Color ownedUpgradeIconBgColor = new Color(0.16f, 0.16f, 0.3f, 1f);
    public Color ownedUpgradeNameColor = new Color(1f, 0.88f, 0.2f, 1f);
    public Color ownedUpgradeDetailColor = new Color(0.88f, 0.92f, 1f, 1f);
    public float ownedUpgradeRowHeight = 46f;
    public float ownedUpgradeRowSpacing = 8f;

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
    private TMP_FontAsset sharedFontAsset;
    private const string OwnedUpgradeRowTemplateName = "OwnedUpgradeRowTemplate";

    private void Start()
    {
        GameManager.EnsureInstance();
        cantAffordText?.gameObject.SetActive(false);
        alreadyPurchasedText?.gameObject.SetActive(false);
        CacheSharedFontAsset();
        EnsureRerollButton();
        EnsureOwnedUpgradesPanel();

        PickRandomUpgrades();
        DisplayCurrentCard();

        if (levelText != null)
        {
            int level = 0;
            if (GameManager.Instance != null && GameManager.Instance.HasCompletedLevel)
                level = Mathf.Max(1, GameManager.Instance.CurrentLevel - 1);
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
        RefreshFruitCurrencyVisibility();
        RefreshOwnedUpgradesPanel();
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
            RefreshFruitCurrencyVisibility();
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
        RefreshFruitCurrencyVisibility();
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
        Debug.Log($"UpgradeScreenManager.SelectCurrent trying to buy {offer.displayName}. Cost={offer.cost} {offer.data.costType}. Current owned tier={PlayerUpgrades.Instance?.GetTier(offer.data) ?? -1}");

        if (PlayerUpgrades.Instance != null && !PlayerUpgrades.Instance.CanAfford(offer.data.costType, offer.cost))
        {
            cantAffordText?.gameObject.SetActive(true);
            Debug.LogWarning($"UpgradeScreenManager.SelectCurrent cannot afford {offer.displayName}.");
            return;
        }

        PlayerUpgrades.Instance?.ApplyUpgrade(offer.data, offer.cost, offer.value);
        purchasedIndexes.Add(currentIndex);
        Debug.Log($"UpgradeScreenManager.SelectCurrent purchased {offer.displayName}. New owned tier={PlayerUpgrades.Instance?.GetTier(offer.data) ?? -1}");

        RefreshCurrencyDisplay();
        RefreshFruitCurrencyVisibility();
        RefreshOwnedUpgradesPanel();
        DisplayCurrentCard();
    }

    private void RefreshCurrencyDisplay()
    {
        if (CurrencyManager.Instance == null) return;
        if (pointsText != null) pointsText.text = "POINTS: " + CurrencyManager.Instance.Points;
        if (fruitCurrencyText != null) fruitCurrencyText.text = "FRUIT: " + CurrencyManager.Instance.FruitCurrency;
    }

    private void RefreshFruitCurrencyVisibility()
    {
        bool fruitUnlocked = (CurrencyManager.Instance != null && CurrencyManager.Instance.FruitUnlocked)
            || (PlayerUpgrades.Instance != null && PlayerUpgrades.Instance.FruitUnlocked);
        bool hasFruitFromTest = GameManager.Instance != null && GameManager.Instance.testStartFruit > 0;
        fruitCurrencyGroup?.SetActive(fruitUnlocked || hasFruitFromTest);
        Debug.Log($"UpgradeScreenManager.RefreshFruitCurrencyVisibility fruitUnlocked={fruitUnlocked} hasFruitFromTest={hasFruitFromTest} groupAssigned={fruitCurrencyGroup != null} groupActive={fruitCurrencyGroup != null && fruitCurrencyGroup.activeSelf}");
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
        RefreshOwnedUpgradesPanel();
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

    private void RefreshOwnedUpgradesPanel()
    {
        if (ownedUpgradesContentRoot == null)
        {
            Debug.LogWarning("UpgradeScreenManager.RefreshOwnedUpgradesPanel aborted because ownedUpgradesContentRoot is null.");
            return;
        }

        EnsureOwnedUpgradesPanelVisible();

        if (ownedUpgradesTitleText != null)
            ownedUpgradesTitleText.text = "OWNED UPGRADES";

        Debug.Log($"UpgradeScreenManager.RefreshOwnedUpgradesPanel root={ownedUpgradesContentRoot.name} childCount(beforeClear)={ownedUpgradesContentRoot.childCount}");
        ClearOwnedUpgradeRows();

        List<UpgradeData> ownedUpgrades = new List<UpgradeData>();
        if (PlayerUpgrades.Instance != null)
        {
            foreach (UpgradeData upgrade in allUpgrades)
            {
                if (upgrade != null)
                {
                    int tier = PlayerUpgrades.Instance.GetTier(upgrade);
                    Debug.Log($"UpgradeScreenManager.RefreshOwnedUpgradesPanel checked {upgrade.upgradeName}: tier={tier}");
                    if (tier > 0)
                        ownedUpgrades.Add(upgrade);
                }
            }
        }
        else
        {
            Debug.LogWarning("UpgradeScreenManager.RefreshOwnedUpgradesPanel found no PlayerUpgrades.Instance.");
        }

        Debug.Log($"UpgradeScreenManager.RefreshOwnedUpgradesPanel owned count={ownedUpgrades.Count}");

        if (ownedUpgrades.Count == 0)
        {
            CreateOwnedUpgradeRow("No upgrades yet", "upgrade stats here", null, 0);
            return;
        }

        for (int i = 0; i < ownedUpgrades.Count; i++)
        {
            UpgradeData upgrade = ownedUpgrades[i];
            int tier = PlayerUpgrades.Instance.GetTier(upgrade);
            string displayName = GetTieredName(upgrade, tier);
            string detail = GetOwnedUpgradeSummary(upgrade, tier);
            Sprite icon = GetIconForUpgrade(upgrade.upgradeType);
            CreateOwnedUpgradeRow(displayName, detail, icon, i);
        }
    }

    private void ClearOwnedUpgradeRows()
    {
        for (int i = ownedUpgradesContentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = ownedUpgradesContentRoot.GetChild(i).gameObject;
            if (child.name == OwnedUpgradeRowTemplateName)
                continue;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void EnsureOwnedUpgradesPanel()
    {
        if (ownedUpgradesContentRoot == null || ownedUpgradesTitleText == null)
        {
            Debug.LogWarning("UpgradeScreenManager.EnsureOwnedUpgradesPanel missing scene refs. Creating runtime fallback panel.");
            CreateOwnedUpgradesPanel();
        }
        else
        {
            Debug.Log($"UpgradeScreenManager.EnsureOwnedUpgradesPanel using scene refs. Title={ownedUpgradesTitleText.name}, Content={ownedUpgradesContentRoot.name}");
        }

        EnsureOwnedUpgradesPanelVisible();
    }

    private void EnsureOwnedUpgradesPanelVisible()
    {
        RectTransform panelRoot = GetOwnedUpgradesPanelRoot();
        if (panelRoot == null)
        {
            Debug.LogWarning("UpgradeScreenManager.EnsureOwnedUpgradesPanelVisible could not find panel root.");
            return;
        }

        panelRoot.gameObject.SetActive(true);
        panelRoot.SetAsLastSibling();
        Debug.Log($"UpgradeScreenManager.EnsureOwnedUpgradesPanelVisible panel={panelRoot.name} position={panelRoot.anchoredPosition} size={panelRoot.sizeDelta}");
    }

    private RectTransform GetOwnedUpgradesPanelRoot()
    {
        if (ownedUpgradesContentRoot != null && ownedUpgradesContentRoot.parent is RectTransform rootFromContent)
            return rootFromContent;

        if (ownedUpgradesTitleText != null && ownedUpgradesTitleText.transform.parent is RectTransform rootFromTitle)
            return rootFromTitle;

        return null;
    }

    private void CreateOwnedUpgradesPanel()
    {
        Transform panelParent = cardBackground != null ? cardBackground.transform.parent : transform;
        RectTransform parentRect = panelParent as RectTransform;
        if (parentRect == null)
            return;

        GameObject panel = new GameObject("OwnedUpgradesPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parentRect, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(48f, 20f);
        panelRect.sizeDelta = new Vector2(360f, 440f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = ownedUpgradeRowColor;

        ownedUpgradesTitleText = CreatePanelText("OwnedUpgradesTitle", panel.transform, 20f, FontStyles.Bold, Color.white);
        RectTransform titleRect = ownedUpgradesTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -14f);
        titleRect.sizeDelta = new Vector2(-18f, 28f);

        GameObject content = new GameObject("OwnedUpgradesContent", typeof(RectTransform));
        content.transform.SetParent(panel.transform, false);
        ownedUpgradesContentRoot = content.GetComponent<RectTransform>();
        ownedUpgradesContentRoot.anchorMin = new Vector2(0f, 1f);
        ownedUpgradesContentRoot.anchorMax = new Vector2(1f, 1f);
        ownedUpgradesContentRoot.pivot = new Vector2(0.5f, 1f);
        ownedUpgradesContentRoot.anchoredPosition = new Vector2(0f, -56f);
        ownedUpgradesContentRoot.sizeDelta = new Vector2(-24f, 356f);
    }

    private TextMeshProUGUI CreatePanelText(string objectName, Transform parent, float fontSize, FontStyles style, Color color)
    {
        GameObject textGo = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.font = GetSharedFontAsset();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private void CreateOwnedUpgradeRow(string title, string detail, Sprite icon, int index)
    {
        string safeTitle = string.IsNullOrWhiteSpace(title) ? "Empty" : title.Replace(" ", "_");
        GameObject row = CreateOwnedUpgradeRowInstance(safeTitle);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.SetParent(ownedUpgradesContentRoot, false);
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -(index * (ownedUpgradeRowHeight + ownedUpgradeRowSpacing)));
        rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, ownedUpgradeRowHeight);

        Image rowImage = row.GetComponent<Image>();
        if (rowImage != null)
            rowImage.color = ownedUpgradeRowColor;

        Image iconBgImage = FindImageByName(row.transform, "IconBg");
        if (iconBgImage != null)
            iconBgImage.color = ownedUpgradeIconBgColor;

        Image iconImage = FindImageByName(row.transform, "Icon");
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.gameObject.SetActive(icon != null);
        }

        TextMeshProUGUI titleText = FindTextByName(row.transform, "Title");
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = ownedUpgradeNameColor;
        }

        TextMeshProUGUI detailText = FindTextByName(row.transform, "Detail");
        if (detailText != null)
        {
            detailText.text = detail;
            detailText.color = ownedUpgradeDetailColor;
        }

        float totalHeight = (index + 1) * ownedUpgradeRowHeight + (index * ownedUpgradeRowSpacing);
        ownedUpgradesContentRoot.sizeDelta = new Vector2(ownedUpgradesContentRoot.sizeDelta.x, totalHeight);
        Debug.Log($"UpgradeScreenManager.CreateOwnedUpgradeRow created {row.name} at index {index}. Root childCount={ownedUpgradesContentRoot.childCount}, rootSize={ownedUpgradesContentRoot.sizeDelta}");
    }

    private GameObject CreateOwnedUpgradeRowInstance(string safeTitle)
    {
        RectTransform template = GetOwnedUpgradeRowTemplate();
        if (template != null)
        {
            GameObject row = Instantiate(template.gameObject, ownedUpgradesContentRoot);
            row.name = "OwnedUpgradeRow_" + safeTitle;
            row.SetActive(true);
            return row;
        }

        GameObject fallback = new GameObject("OwnedUpgradeRow_" + safeTitle, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fallback.transform.SetParent(ownedUpgradesContentRoot, false);
        return fallback;
    }

    private RectTransform GetOwnedUpgradeRowTemplate()
    {
        if (ownedUpgradesContentRoot == null)
            return null;

        Transform template = ownedUpgradesContentRoot.Find(OwnedUpgradeRowTemplateName);
        return template as RectTransform;
    }

    private TextMeshProUGUI FindTextByName(Transform root, string objectName)
    {
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.name == objectName)
                return text;
        }

        return null;
    }

    private Image FindImageByName(Transform root, string objectName)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image.name == objectName)
                return image;
        }

        return null;
    }

    private TextMeshProUGUI CreateOwnedUpgradeText(string objectName, Transform parent, Color color, float fontSize, FontStyles style)
    {
        GameObject textGo = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.font = GetSharedFontAsset();
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private void CacheSharedFontAsset()
    {
        sharedFontAsset = GetSharedFontAsset();
    }

    private TMP_FontAsset GetSharedFontAsset()
    {
        if (sharedFontAsset != null)
            return sharedFontAsset;

        if (titleText != null && titleText.font != null)
        {
            sharedFontAsset = titleText.font;
            return sharedFontAsset;
        }

        if (nameText != null && nameText.font != null)
        {
            sharedFontAsset = nameText.font;
            return sharedFontAsset;
        }

        if (ownedUpgradesTitleText != null && ownedUpgradesTitleText.font != null)
        {
            sharedFontAsset = ownedUpgradesTitleText.font;
            return sharedFontAsset;
        }

        return TMP_Settings.defaultFontAsset;
    }

    private string GetOwnedUpgradeSummary(UpgradeData upgrade, int tier)
    {
        int maxTier = GetMaxTier(upgrade);
        string tierText = maxTier > 1 ? "Tier " + ToRoman(tier) + "/" + ToRoman(maxTier) : "Unlocked";

        switch (upgrade.upgradeType)
        {
            case UpgradeType.TimerBonus:
                return tierText + "  |  +" + PlayerUpgrades.Instance.TimerBonus.ToString("0.#") + " sec total";
            case UpgradeType.ExtraLife:
                return tierText + "  |  +" + PlayerUpgrades.Instance.BonusLives + " life";
            case UpgradeType.MoveSpeedBonus:
                return tierText + "  |  +" + PlayerUpgrades.Instance.SpeedBonus.ToString("0.#") + " speed";
            case UpgradeType.PowerPelletDuration:
                return tierText + "  |  +" + PlayerUpgrades.Instance.PowerDurationBonus.ToString("0.#") + " sec";
            case UpgradeType.UnlockFruit:
                return "Unlocked  |  Fruit can now appear in runs";
            case UpgradeType.ScoreMultiplier:
                return tierText + "  |  x" + PlayerUpgrades.Instance.ScoreMultiplier.ToString("0.#") + " score";
            case UpgradeType.ExtraPowerPellets:
                return tierText + "  |  +" + PlayerUpgrades.Instance.BonusPowerPellets + " maze pellets";
            case UpgradeType.GhostFreeze:
                return tierText + "  |  " + PlayerUpgrades.Instance.GhostFreezeDuration.ToString("0.#") + " sec freeze";
            default:
                return tierText;
        }
    }
}

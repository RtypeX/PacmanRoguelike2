using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
    public Color greyedOutColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("Lock System")]
    public GameObject lockOverlay;
    public Image lockIcon;
    public TextMeshProUGUI playFirstNotification;

    [Header("Navigation")]
    public Button leftArrowButton;
    public Button rightArrowButton;
    public TextMeshProUGUI pageIndicatorText;

    [Header("Action Buttons")]
    public Button selectButton;
    public TextMeshProUGUI cantAffordText;
    public TextMeshProUGUI alreadyPurchasedText;
    public Button backButton;

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

    [Header("All Possible Upgrades")]
    public List<UpgradeData> allUpgrades;

    private List<UpgradeData> offeredUpgrades = new List<UpgradeData>();
    private int currentIndex = 0;
    private HashSet<int> purchasedIndexes = new HashSet<int>();
    private bool hasPlayedLevel = false;

    private void Start()
    {
        hasPlayedLevel = GameManager.Instance == null || GameManager.Instance.CurrentLevel > 1;

        if (PlayerUpgrades.Instance == null)
        {
            GameObject temp = new GameObject("TempPlayerUpgrades");
            temp.AddComponent<PlayerUpgrades>();
        }

        cantAffordText?.gameObject.SetActive(false);
        alreadyPurchasedText?.gameObject.SetActive(false);
        playFirstNotification?.gameObject.SetActive(false);
        lockOverlay?.SetActive(!hasPlayedLevel);

        RefreshCurrencyDisplay();
        PickRandomUpgrades();
        DisplayCurrentCard();

        if (levelText != null)
        {
            int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel - 1 : 0;
            levelText.text = level > 0 ? "LEVEL " + level + " COMPLETE" : "NO LEVEL COMPLETED YET";
        }

        bool fruitUnlocked = PlayerUpgrades.Instance?.FruitUnlocked ?? false;
        fruitCurrencyGroup?.SetActive(fruitUnlocked);

        leftArrowButton?.onClick.AddListener(GoLeft);
        rightArrowButton?.onClick.AddListener(GoRight);
        selectButton?.onClick.AddListener(SelectCurrent);
        backButton?.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));

        // Pulse upgrades button hint if locked
        if (!hasPlayedLevel && lockOverlay != null)
        {
            LeanTween.scale(lockOverlay, Vector3.one * 1.05f, 0.6f)
                .setEaseInOutSine()
                .setLoopPingPong();
        }
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

        if (nameText != null) nameText.text = upgrade.upgradeName.ToUpper();
        if (descriptionText != null) descriptionText.text = upgrade.description;
        if (costText != null) costText.text = upgrade.cost.ToString();
        if (currencyText != null) currencyText.text = upgrade.costType == CurrencyType.Points ? "POINTS" : "FRUIT";

        if (iconImage != null)
        {
            iconImage.sprite = GetIconForUpgrade(upgrade.upgradeType);
            iconImage.gameObject.SetActive(iconImage.sprite != null);
        }

        if (cardBackground != null)
        {
            if (!hasPlayedLevel) cardBackground.color = greyedOutColor;
            else if (alreadyBought) cardBackground.color = purchasedColor;
            else if (!canAfford) cardBackground.color = lockedColor;
            else cardBackground.color = affordableColor;
        }

        if (pageIndicatorText != null)
            pageIndicatorText.text = (currentIndex + 1) + " / " + offeredUpgrades.Count;

        cantAffordText?.gameObject.SetActive(false);
        alreadyPurchasedText?.gameObject.SetActive(false);

        if (selectButton != null)
            selectButton.interactable = hasPlayedLevel && !alreadyBought;

        leftArrowButton?.gameObject.SetActive(offeredUpgrades.Count > 1);
        rightArrowButton?.gameObject.SetActive(offeredUpgrades.Count > 1);

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

        if (!hasPlayedLevel)
        {
            StartCoroutine(ShowPlayFirstNotification());
            return;
        }

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

    private IEnumerator ShowPlayFirstNotification()
    {
        if (playFirstNotification == null) yield break;

        playFirstNotification.gameObject.SetActive(true);
        Color c = playFirstNotification.color;

        // Fade in
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / 0.3f);
            playFirstNotification.color = c;
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(1.5f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            playFirstNotification.color = c;
            yield return null;
        }

        playFirstNotification.gameObject.SetActive(false);
    }

    private void RefreshCurrencyDisplay()
    {
        if (PlayerUpgrades.Instance == null) return;
        if (pointsText != null) pointsText.text = "POINTS: " + PlayerUpgrades.Instance.Points;
        if (fruitCurrencyText != null) fruitCurrencyText.text = "FRUIT: " + PlayerUpgrades.Instance.FruitCurrency;
    }
}
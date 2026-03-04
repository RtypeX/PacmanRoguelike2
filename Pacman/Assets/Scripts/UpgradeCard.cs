using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// UpgradeCard - Controls a single upgrade card UI element.
/// Attach to your Card prefab.
///
/// CARD PREFAB STRUCTURE:
/// - CardRoot (Image - background panel, rounded rect)
///   - IconImage       (Image - upgrade icon)
///   - NameText        (TMP - upgrade name, big font)
///   - DescriptionText (TMP - description, small font)
///   - CostPanel
///     - CurrencyIcon  (Image - coin or fruit icon)
///     - CostText      (TMP - cost number)
///   - LockedOverlay   (Image - dark overlay shown when cant afford)
///     - LockedText    (TMP - "NOT ENOUGH POINTS")
///   - SelectButton    (Button - whole card is the button)
/// </summary>
public class UpgradeCard : MonoBehaviour
{
    [Header("Card UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public Image currencyIcon;
    public Sprite pointsIcon;
    public Sprite fruitIcon;
    public GameObject lockedOverlay;
    public Button selectButton;

    [Header("Colors")]
    public Color affordableColor = Color.white;
    public Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color flashColor = Color.red;

    private UpgradeData upgradeData;
    private Action<UpgradeData> onSelected;

    public void Setup(UpgradeData data, Action<UpgradeData> callback)
    {
        upgradeData = data;
        onSelected = callback;

        // Fill in UI
        if (nameText != null) nameText.text = data.upgradeName.ToUpper();
        if (descriptionText != null) descriptionText.text = data.description;
        if (iconImage != null && data.icon != null) iconImage.sprite = data.icon;
        if (costText != null) costText.text = data.cost.ToString();

        // Currency icon
        if (currencyIcon != null)
            currencyIcon.sprite = data.costType == CurrencyType.Points ? pointsIcon : fruitIcon;

        // Show locked overlay if player cant afford
        bool canAfford = PlayerUpgrades.Instance?.CanAfford(data) ?? false;
        lockedOverlay?.SetActive(!canAfford);
        GetComponent<Image>().color = canAfford ? affordableColor : lockedColor;

        // Wire button
        selectButton?.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        bool canAfford = PlayerUpgrades.Instance?.CanAfford(upgradeData) ?? false;

        if (!canAfford)
        {
            StartCoroutine(FlashRed());
            return;
        }

        onSelected?.Invoke(upgradeData);
    }

    private IEnumerator FlashRed()
    {
        Image bg = GetComponent<Image>();
        Color original = bg.color;

        for (int i = 0; i < 3; i++)
        {
            bg.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            bg.color = original;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
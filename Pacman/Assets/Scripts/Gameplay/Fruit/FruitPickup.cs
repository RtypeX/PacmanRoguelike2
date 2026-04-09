using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FruitPickup : MonoBehaviour
{
    [Header("Assigned Data")]
    public FruitData fruitData;
    public SpriteRenderer targetRenderer;

    [Header("Fallback Rewards")]
    public int fallbackScoreValue = 100;
    public int fallbackFruitCurrencyValue = 1;

    private Sprite activeSprite;
    private GameObject replacedPellet;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ApplyVisual();
    }

    public void Configure(FruitData data, GameObject pelletToReplace = null)
    {
        fruitData = data;
        replacedPellet = pelletToReplace;
        ApplyVisual();
    }

    public int GetScoreValue()
    {
        return fruitData != null ? fruitData.scoreValue : fallbackScoreValue;
    }

    public int GetFruitCurrencyValue()
    {
        return fruitData != null ? fruitData.fruitCurrencyValue : fallbackFruitCurrencyValue;
    }

    public void Collect(testMove player)
    {
        if (player == null)
            return;

        player.AddScore(GetScoreValue());
        player.AddFruitCurrency(GetFruitCurrencyValue());
        ManageHUD.Instance?.UpdateFruitCurrency(
            CurrencyManager.Instance != null ? CurrencyManager.Instance.FruitCurrency : player.FruitCurrency);

        // If this fruit replaced a pellet, consuming it should still advance pellet completion.
        if (replacedPellet != null)
        {
            GameManager.Instance?.OnPelletEaten();
            replacedPellet = null;
        }

        gameObject.SetActive(false);
    }

    public void RestoreReplacedPellet()
    {
        if (replacedPellet != null)
        {
            replacedPellet.SetActive(true);
            replacedPellet = null;
        }
    }

    private void ApplyVisual()
    {
        if (targetRenderer == null || fruitData == null)
            return;

        activeSprite = fruitData.GetRandomVariant();
        if (activeSprite != null)
            targetRenderer.sprite = activeSprite;
    }
}

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFruitData", menuName = "Gameplay/Fruit Data")]
public class FruitData : ScriptableObject
{
    [Header("Display")]
    public string fruitName = "Fruit";
    public List<Sprite> variants = new List<Sprite>();

    [Header("Rewards")]
    public int scoreValue = 100;
    public int fruitCurrencyValue = 1;

    public Sprite GetRandomVariant()
    {
        if (variants == null || variants.Count == 0)
        {
            Debug.LogWarning($"FruitData {fruitName} has no variants assigned.");
            return null;
        }

        List<Sprite> validVariants = new List<Sprite>();
        foreach (Sprite variant in variants)
        {
            if (variant != null)
                validVariants.Add(variant);
        }

        if (validVariants.Count == 0)
        {
            Debug.LogWarning($"FruitData {fruitName} has only empty/null variant slots.");
            return null;
        }

        int index = Random.Range(0, validVariants.Count);
        return validVariants[index];
    }
}

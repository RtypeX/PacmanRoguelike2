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
            return null;

        int index = Random.Range(0, variants.Count);
        return variants[index];
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow;

public class WeightedList<T>
{
    private List<KeyValuePair<T, float>> items = [];

    public void Add(T item, float weight)
    {
        if (weight <= 0) throw new ArgumentException("Weight must be positive");
        items.Add(new KeyValuePair<T, float>(item, weight));
    }

    public T GetRandom()
    {
        float totalWeight = items.Sum(item => item.Value), rand = UnityEngine.Random.Range(0, totalWeight), weightSoFar = 0;
        for (int i = 0; i < items.Count; i++)
        {
            weightSoFar += items[i].Value;
            if (rand <= weightSoFar) return items[i].Key;
        }

        throw new InvalidOperationException("No items in WeightedList");
    }
}
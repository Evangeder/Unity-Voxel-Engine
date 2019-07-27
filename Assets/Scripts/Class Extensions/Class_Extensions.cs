using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ClassExtensions
{
    public static Dictionary<TKey, TValue> Shuffle<TKey, TValue>(
       this Dictionary<TKey, TValue> source)
    {
        //Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)System.Guid.NewGuid().GetHashCode());
        System.Random r = new System.Random();
        return source.OrderBy(item => r.Next())
           .ToDictionary(item => item.Key, item => item.Value);
    }

    public static T GetRandomElement<T>(this T[] array)
    {
        return array[Random.Range(0, array.Length)];
    }
}
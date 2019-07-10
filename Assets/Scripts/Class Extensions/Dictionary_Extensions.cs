using System.Collections.Generic;
using System.Linq;

public static class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> Shuffle<TKey, TValue>(
       this Dictionary<TKey, TValue> source)
    {
        //Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)System.Guid.NewGuid().GetHashCode());
        System.Random r = new System.Random();
        return source.OrderBy(item => r.Next())
           .ToDictionary(item => item.Key, item => item.Value);
    }
}
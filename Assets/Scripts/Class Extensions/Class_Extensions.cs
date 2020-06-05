using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
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
        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    public static bool Remove<TKey, TValue>(
      this ConcurrentDictionary<TKey, TValue> self, TKey key)
    {
        return ((IDictionary<TKey, TValue>)self).Remove(key);
    }
}

public static class EnumerableUtilities
{
    public static IEnumerable<int> RangePython(int start, int stop, int step = 1)
    {
        if (step == 0)
            throw new ArgumentException("Parameter step cannot equal zero.");

        if (start < stop && step > 0)
        {
            for (var i = start; i < stop; i += step)
            {
                yield return i;
            }
        }
        else if (start > stop && step < 0)
        {
            for (var i = start; i > stop; i += step)
            {
                yield return i;
            }
        }
    }

    public static IEnumerable<int> RangePython(int stop)
    {
        return RangePython(0, stop);
    }
}

public static class SpiralCoords
{
    public static IEnumerable<int2> GenerateOutTo(int radius)
    {
        //TODO trap negative radius.  0 is ok.

        foreach (int r in Enumerable.Range(0, radius + 1))
        {
            foreach (int2 coord in GenerateRing(r))
            {
                yield return coord;
            }
        }
    }

    public static IEnumerable<int3> GenerateOutTo3D(int radius)
    {
        //TODO trap negative radius.  0 is ok.

        foreach (int r in Enumerable.Range(0, radius + 1))
        {
            foreach (int3 coord in GenerateRing3D(r))
            {
                yield return coord;
            }
        }
    }

    public static IEnumerable<int2> GenerateRing(int radius)
    {
        //TODO trap negative radius.  0 is ok.

        int2 currentPoint = new int2(radius, 0);
        yield return new int2(currentPoint.x, currentPoint.y);

        //move up while we can
        while (currentPoint.y < radius)
        {
            currentPoint.y += 1;
            yield return new int2(currentPoint.x, currentPoint.y);
        }
        //move left while we can
        while (-radius < currentPoint.x)
        {
            currentPoint.x -= 1;
            yield return new int2(currentPoint.x, currentPoint.y);
        }
        //move down while we can
        while (-radius < currentPoint.y)
        {
            currentPoint.y -= 1;
            yield return new int2(currentPoint.x, currentPoint.y);
        }
        //move right while we can
        while (currentPoint.x < radius)
        {
            currentPoint.x += 1;
            yield return new int2(currentPoint.x, currentPoint.y);
        }
        //move up while we can
        while (currentPoint.y < -1)
        {
            currentPoint.y += 1;
            yield return new int2(currentPoint.x, currentPoint.y);
        }
    }

    public static IEnumerable<int3> GenerateRing3D(int radius)
    {
        //TODO trap negative radius.  0 is ok.

        int3 currentPoint = new int3(radius, 0, 0);
        yield return new int3(currentPoint.x, 0, currentPoint.y);

        //move up while we can
        while (currentPoint.y < radius)
        {
            currentPoint.y += 1;
            yield return new int3(currentPoint.x, 0, currentPoint.y);
        }
        //move left while we can
        while (-radius < currentPoint.x)
        {
            currentPoint.x -= 1;
            yield return new int3(currentPoint.x, 0, currentPoint.y);
        }
        //move down while we can
        while (-radius < currentPoint.y)
        {
            currentPoint.y -= 1;
            yield return new int3(currentPoint.x, 0, currentPoint.y);
        }
        //move right while we can
        while (currentPoint.x < radius)
        {
            currentPoint.x += 1;
            yield return new int3(currentPoint.x, 0, currentPoint.y);
        }
        //move up while we can
        while (currentPoint.y < -1)
        {
            currentPoint.y += 1;
            yield return new int3(currentPoint.x, 0, currentPoint.y);
        }
    }

}
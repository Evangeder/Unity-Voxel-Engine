using CielaSpike;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;

public static class ChunkLOD
{
    public static int3 PlayerPosition_int;
    public static Vector3 PlayerPosition, CameraNormalized;

    public static float CalculateImportance(int3 chunkPosition)
    {
        Vector3 posv = new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z);
        float distanceSqr = (posv - PlayerPosition).sqrMagnitude;
        return Vector3.Dot(posv - PlayerPosition, CameraNormalized) / (float)math.pow(distanceSqr, 0.7);
    }
    public static float Distance(int3 chunkPosition)
    {
        return math.abs(Vector3.Distance(new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z), PlayerPosition));
    }
}

public class ChunkLoading : MonoBehaviour
{
    private readonly int2[] _drawingArray =
    {
        new int2(0, 0), 
        new int2(-1, 0), new int2(0, -1), new int2(0, 1), new int2(1, 0), 
        new int2(-1, -1), new int2(-1, 1), new int2(1, -1), new int2(1, 1), 
        new int2(-2, 0), new int2(0, -2), new int2(0, 2), new int2(2, 0), 
        new int2(-2, -1), new int2(-2, 1), new int2(-1, -2), new int2(-1, 2), 
        new int2(1, -2), new int2(1, 2), new int2(2, -1), new int2(2, 1), 
        new int2(-2, -2), new int2(-2, 2), new int2(2, -2), new int2(2, 2), 
        new int2(-3, 0), new int2(0, -3), new int2(0, 3), new int2(3, 0), 
        new int2(-3, -1), new int2(-3, 1), new int2(-1, -3), new int2(-1, 3), 
        new int2(1, -3), new int2(1, 3), new int2(3, -1), new int2(3, 1), 
        new int2(-3, -2), new int2(-3, 2), new int2(-2, -3), new int2(-2, 3), 
        new int2(2, -3), new int2(2, 3), new int2(3, -2), new int2(3, 2), 
        new int2(-4, 0), new int2(0, -4), new int2(0, 4), new int2(4, 0), 
        new int2(-4, -1), new int2(-4, 1), new int2(-1, -4), new int2(-1, 4), 
        new int2(1, -4), new int2(1, 4), new int2(4, -1), new int2(4, 1), 
        new int2(-3, -3), new int2(-3, 3), new int2(3, -3), new int2(3, 3), 
        new int2(-4, -2), new int2(-4, 2), new int2(-2, -4), new int2(-2, 4), 
        new int2(2, -4), new int2(2, 4), new int2(4, -2), new int2(4, 2), 
        new int2(-5, 0), new int2(-4, -3), new int2(-4, 3), new int2(-3, -4), 
        new int2(-3, 4), new int2(0, -5), new int2(0, 5), new int2(3, -4), 
        new int2(3, 4), new int2(4, -3), new int2(4, 3), new int2(5, 0), 
        new int2(-5, -1), new int2(-5, 1), new int2(-1, -5), new int2(-1, 5), 
        new int2(1, -5), new int2(1, 5), new int2(5, -1), new int2(5, 1), 
        new int2(-5, -2), new int2(-5, 2), new int2(-2, -5), new int2(-2, 5), 
        new int2(2, -5), new int2(2, 5), new int2(5, -2), new int2(5, 2), 
        new int2(-4, -4), new int2(-4, 4), new int2(4, -4), new int2(4, 4), 
        new int2(-5, -3), new int2(-5, 3), new int2(-3, -5), new int2(-3, 5), 
        new int2(3, -5), new int2(3, 5), new int2(5, -3), new int2(5, 3), 
        new int2(-6, 0), new int2(0, -6), new int2(0, 6), new int2(6, 0), 
        new int2(-6, -1), new int2(-6, 1), new int2(-1, -6), new int2(-1, 6), 
        new int2(1, -6), new int2(1, 6), new int2(6, -1), new int2(6, 1), 
        new int2(-6, -2), new int2(-6, 2), new int2(-2, -6), new int2(-2, 6), 
        new int2(2, -6), new int2(2, 6), new int2(6, -2), new int2(6, 2), 
        new int2(-5, -4), new int2(-5, 4), new int2(-4, -5), new int2(-4, 5), 
        new int2(4, -5), new int2(4, 5), new int2(5, -4), new int2(5, 4), 
        new int2(-6, -3), new int2(-6, 3), new int2(-3, -6), new int2(-3, 6), 
        new int2(3, -6), new int2(3, 6), new int2(6, -3), new int2(6, 3), 
        new int2(-7, 0), new int2(0, -7), new int2(0, 7), new int2(7, 0), 
        new int2(-7, -1), new int2(-7, 1), new int2(-5, -5), new int2(-5, 5), 
        new int2(-1, -7), new int2(-1, 7), new int2(1, -7), new int2(1, 7), 
        new int2(5, -5), new int2(5, 5), new int2(7, -1), new int2(7, 1), 
        new int2(-6, -4), new int2(-6, 4), new int2(-4, -6), new int2(-4, 6), 
        new int2(4, -6), new int2(4, 6), new int2(6, -4), new int2(6, 4), 
        new int2(-7, -2), new int2(-7, 2), new int2(-2, -7), new int2(-2, 7), 
        new int2(2, -7), new int2(2, 7), new int2(7, -2), new int2(7, 2), 
        new int2(-7, -3), new int2(-7, 3), new int2(-3, -7), new int2(-3, 7), 
        new int2(3, -7), new int2(3, 7), new int2(7, -3), new int2(7, 3), 
        new int2(-6, -5), new int2(-6, 5), new int2(-5, -6), new int2(-5, 6), 
        new int2(5, -6), new int2(5, 6), new int2(6, -5), new int2(6, 5), 
        new int2(-8, 0), new int2(0, -8), new int2(0, 8), new int2(8, 0), 
        new int2(-8, -1), new int2(-8, 1), new int2(-7, -4), new int2(-7, 4), 
        new int2(-4, -7), new int2(-4, 7), new int2(-1, -8), new int2(-1, 8), 
        new int2(1, -8), new int2(1, 8), new int2(4, -7), new int2(4, 7), 
        new int2(7, -4), new int2(7, 4), new int2(8, -1), new int2(8, 1), 
        new int2(-8, -2), new int2(-8, 2), new int2(-2, -8), new int2(-2, 8), 
        new int2(2, -8), new int2(2, 8), new int2(8, -2), new int2(8, 2), 
        new int2(-6, -6), new int2(-6, 6), new int2(6, -6), new int2(6, 6), 
        new int2(-8, -3), new int2(-8, 3), new int2(-3, -8), new int2(-3, 8), 
        new int2(3, -8), new int2(3, 8), new int2(8, -3), new int2(8, 3), 
        new int2(-7, -5), new int2(-7, 5), new int2(-5, -7), new int2(-5, 7), 
        new int2(5, -7), new int2(5, 7), new int2(7, -5), new int2(7, 5), 
        new int2(-8, -4), new int2(-8, 4), new int2(-4, -8), new int2(-4, 8), 
        new int2(4, -8), new int2(4, 8), new int2(8, -4), new int2(8, 4), 
        new int2(-9, 0), new int2(0, -9), new int2(0, 9), new int2(9, 0), 
        new int2(-9, -1), new int2(-9, 1), new int2(-1, -9), new int2(-1, 9), 
        new int2(1, -9), new int2(1, 9), new int2(9, -1), new int2(9, 1), 
        new int2(-9, -2), new int2(-9, 2), new int2(-7, -6), new int2(-7, 6), 
        new int2(-6, -7), new int2(-6, 7), new int2(-2, -9), new int2(-2, 9), 
        new int2(2, -9), new int2(2, 9), new int2(6, -7), new int2(6, 7), 
        new int2(7, -6), new int2(7, 6), new int2(9, -2), new int2(9, 2), 
        new int2(-8, -5), new int2(-8, 5), new int2(-5, -8), new int2(-5, 8), 
        new int2(5, -8), new int2(5, 8), new int2(8, -5), new int2(8, 5), 
        new int2(-9, -3), new int2(-9, 3), new int2(-3, -9), new int2(-3, 9), 
        new int2(3, -9), new int2(3, 9), new int2(9, -3), new int2(9, 3), 
        new int2(-9, -4), new int2(-9, 4), new int2(-4, -9), new int2(-4, 9), 
        new int2(4, -9), new int2(4, 9), new int2(9, -4), new int2(9, 4), 
        new int2(-7, -7), new int2(-7, 7), new int2(7, -7), new int2(7, 7), 
        new int2(-10, 0), new int2(-8, -6), new int2(-8, 6), new int2(-6, -8), 
        new int2(-6, 8), new int2(0, -10), new int2(0, 10), new int2(6, -8), 
        new int2(6, 8), new int2(8, -6), new int2(8, 6), new int2(10, 0), 
        new int2(-10, -1), new int2(-10, 1), new int2(-1, -10), new int2(-1, 10), 
        new int2(1, -10), new int2(1, 10), new int2(10, -1), new int2(10, 1), 
        new int2(-10, -2), new int2(-10, 2), new int2(-2, -10), new int2(-2, 10), 
        new int2(2, -10), new int2(2, 10), new int2(10, -2), new int2(10, 2), 
        new int2(-9, -5), new int2(-9, 5), new int2(-5, -9), new int2(-5, 9), 
        new int2(5, -9), new int2(5, 9), new int2(9, -5), new int2(9, 5), 
        new int2(-10, -3), new int2(-10, 3), new int2(-3, -10), new int2(-3, 10), 
        new int2(3, -10), new int2(3, 10), new int2(10, -3), new int2(10, 3), 
        new int2(-8, -7), new int2(-8, 7), new int2(-7, -8), new int2(-7, 8), 
        new int2(7, -8), new int2(7, 8), new int2(8, -7), new int2(8, 7), 
        new int2(-10, -4), new int2(-10, 4), new int2(-4, -10), new int2(-4, 10), 
        new int2(4, -10), new int2(4, 10), new int2(10, -4), new int2(10, 4), 
        new int2(-9, -6), new int2(-9, 6), new int2(-6, -9), new int2(-6, 9), 
        new int2(6, -9), new int2(6, 9), new int2(9, -6), new int2(9, 6), 
        new int2(-11, 0), new int2(0, -11), new int2(0, 11), new int2(11, 0), 
        new int2(-11, -1), new int2(-11, 1), new int2(-1, -11), new int2(-1, 11), 
        new int2(1, -11), new int2(1, 11), new int2(11, -1), new int2(11, 1), 
        new int2(-11, -2), new int2(-11, 2), new int2(-10, -5), new int2(-10, 5), 
        new int2(-5, -10), new int2(-5, 10), new int2(-2, -11), new int2(-2, 11), 
        new int2(2, -11), new int2(2, 11), new int2(5, -10), new int2(5, 10), 
        new int2(10, -5), new int2(10, 5), new int2(11, -2), new int2(11, 2), 
        new int2(-8, -8), new int2(-8, 8), new int2(8, -8), new int2(8, 8), 
        new int2(-11, -3), new int2(-11, 3), new int2(-9, -7), new int2(-9, 7), 
        new int2(-7, -9), new int2(-7, 9), new int2(-3, -11), new int2(-3, 11), 
        new int2(3, -11), new int2(3, 11), new int2(7, -9), new int2(7, 9), 
        new int2(9, -7), new int2(9, 7), new int2(11, -3), new int2(11, 3), 
        new int2(-10, -6), new int2(-10, 6), new int2(-6, -10), new int2(-6, 10), 
        new int2(6, -10), new int2(6, 10), new int2(10, -6), new int2(10, 6), 
        new int2(-11, -4), new int2(-11, 4), new int2(-4, -11), new int2(-4, 11), 
        new int2(4, -11), new int2(4, 11), new int2(11, -4), new int2(11, 4), 
        new int2(-12, 0), new int2(0, -12), new int2(0, 12), new int2(12, 0), 
        new int2(-12, -1), new int2(-12, 1), new int2(-9, -8), new int2(-9, 8), 
        new int2(-8, -9), new int2(-8, 9), new int2(-1, -12), new int2(-1, 12), 
        new int2(1, -12), new int2(1, 12), new int2(8, -9), new int2(8, 9), 
        new int2(9, -8), new int2(9, 8), new int2(12, -1), new int2(12, 1), 
        new int2(-11, -5), new int2(-11, 5), new int2(-5, -11), new int2(-5, 11), 
        new int2(5, -11), new int2(5, 11), new int2(11, -5), new int2(11, 5), 
        new int2(-12, -2), new int2(-12, 2), new int2(-2, -12), new int2(-2, 12), 
        new int2(2, -12), new int2(2, 12), new int2(12, -2), new int2(12, 2), 
        new int2(-10, -7), new int2(-10, 7), new int2(-7, -10), new int2(-7, 10), 
        new int2(7, -10), new int2(7, 10), new int2(10, -7), new int2(10, 7), 
        new int2(-12, -3), new int2(-12, 3), new int2(-3, -12), new int2(-3, 12), 
        new int2(3, -12), new int2(3, 12), new int2(12, -3), new int2(12, 3), 
        new int2(-11, -6), new int2(-11, 6), new int2(-6, -11), new int2(-6, 11), 
        new int2(6, -11), new int2(6, 11), new int2(11, -6), new int2(11, 6), 
        new int2(-12, -4), new int2(-12, 4), new int2(-4, -12), new int2(-4, 12), 
        new int2(4, -12), new int2(4, 12), new int2(12, -4), new int2(12, 4), 
        new int2(-9, -9), new int2(-9, 9), new int2(9, -9), new int2(9, 9), 
        new int2(-10, -8), new int2(-10, 8), new int2(-8, -10), new int2(-8, 10), 
        new int2(8, -10), new int2(8, 10), new int2(10, -8), new int2(10, 8), 
        new int2(-13, 0), new int2(-12, -5), new int2(-12, 5), new int2(-5, -12), 
        new int2(-5, 12), new int2(0, -13), new int2(0, 13), new int2(5, -12), 
        new int2(5, 12), new int2(12, -5), new int2(12, 5), new int2(13, 0), 
        new int2(-13, -1), new int2(-13, 1), new int2(-11, -7), new int2(-11, 7), 
        new int2(-7, -11), new int2(-7, 11), new int2(-1, -13), new int2(-1, 13), 
        new int2(1, -13), new int2(1, 13), new int2(7, -11), new int2(7, 11), 
        new int2(11, -7), new int2(11, 7), new int2(13, -1), new int2(13, 1), 
        new int2(-13, -2), new int2(-13, 2), new int2(-2, -13), new int2(-2, 13), 
        new int2(2, -13), new int2(2, 13), new int2(13, -2), new int2(13, 2), 
        new int2(-13, -3), new int2(-13, 3), new int2(-3, -13), new int2(-3, 13), 
        new int2(3, -13), new int2(3, 13), new int2(13, -3), new int2(13, 3), 
        new int2(-12, -6), new int2(-12, 6), new int2(-6, -12), new int2(-6, 12), 
        new int2(6, -12), new int2(6, 12), new int2(12, -6), new int2(12, 6), 
        new int2(-10, -9), new int2(-10, 9), new int2(-9, -10), new int2(-9, 10), 
        new int2(9, -10), new int2(9, 10), new int2(10, -9), new int2(10, 9), 
        new int2(-13, -4), new int2(-13, 4), new int2(-11, -8), new int2(-11, 8), 
        new int2(-8, -11), new int2(-8, 11), new int2(-4, -13), new int2(-4, 13), 
        new int2(4, -13), new int2(4, 13), new int2(8, -11), new int2(8, 11), 
        new int2(11, -8), new int2(11, 8), new int2(13, -4), new int2(13, 4), 
        new int2(-12, -7), new int2(-12, 7), new int2(-7, -12), new int2(-7, 12), 
        new int2(7, -12), new int2(7, 12), new int2(12, -7), new int2(12, 7), 
        new int2(-13, -5), new int2(-13, 5), new int2(-5, -13), new int2(-5, 13), 
        new int2(5, -13), new int2(5, 13), new int2(13, -5), new int2(13, 5), 
        new int2(-14, 0), new int2(0, -14), new int2(0, 14), new int2(14, 0), 
        new int2(-14, -1), new int2(-14, 1), new int2(-1, -14), new int2(-1, 14), 
        new int2(1, -14), new int2(1, 14), new int2(14, -1), new int2(14, 1), 
        new int2(-14, -2), new int2(-14, 2), new int2(-10, -10), new int2(-10, 10), 
        new int2(-2, -14), new int2(-2, 14), new int2(2, -14), new int2(2, 14), 
        new int2(10, -10), new int2(10, 10), new int2(14, -2), new int2(14, 2), 
        new int2(-11, -9), new int2(-11, 9), new int2(-9, -11), new int2(-9, 11), 
        new int2(9, -11), new int2(9, 11), new int2(11, -9), new int2(11, 9), 
        new int2(-14, -3), new int2(-14, 3), new int2(-13, -6), new int2(-13, 6), 
        new int2(-6, -13), new int2(-6, 13), new int2(-3, -14), new int2(-3, 14), 
        new int2(3, -14), new int2(3, 14), new int2(6, -13), new int2(6, 13), 
        new int2(13, -6), new int2(13, 6), new int2(14, -3), new int2(14, 3), 
        new int2(-12, -8), new int2(-12, 8), new int2(-8, -12), new int2(-8, 12), 
        new int2(8, -12), new int2(8, 12), new int2(12, -8), new int2(12, 8), 
        new int2(-14, -4), new int2(-14, 4), new int2(-4, -14), new int2(-4, 14), 
        new int2(4, -14), new int2(4, 14), new int2(14, -4), new int2(14, 4), 
        new int2(-13, -7), new int2(-13, 7), new int2(-7, -13), new int2(-7, 13), 
        new int2(7, -13), new int2(7, 13), new int2(13, -7), new int2(13, 7), 
        new int2(-14, -5), new int2(-14, 5), new int2(-11, -10), new int2(-11, 10), 
        new int2(-10, -11), new int2(-10, 11), new int2(-5, -14), new int2(-5, 14), 
        new int2(5, -14), new int2(5, 14), new int2(10, -11), new int2(10, 11), 
        new int2(11, -10), new int2(11, 10), new int2(14, -5), new int2(14, 5), 
        new int2(-15, 0), new int2(-12, -9), new int2(-12, 9), new int2(-9, -12), 
        new int2(-9, 12), new int2(0, -15), new int2(0, 15), new int2(9, -12), 
        new int2(9, 12), new int2(12, -9), new int2(12, 9), new int2(15, 0), 
        new int2(-15, -1), new int2(-15, 1), new int2(-1, -15), new int2(-1, 15), 
        new int2(1, -15), new int2(1, 15), new int2(15, -1), new int2(15, 1), 
        new int2(-15, -2), new int2(-15, 2), new int2(-2, -15), new int2(-2, 15), 
        new int2(2, -15), new int2(2, 15), new int2(15, -2), new int2(15, 2), 
        new int2(-14, -6), new int2(-14, 6), new int2(-6, -14), new int2(-6, 14), 
        new int2(6, -14), new int2(6, 14), new int2(14, -6), new int2(14, 6), 
        new int2(-13, -8), new int2(-13, 8), new int2(-8, -13), new int2(-8, 13), 
        new int2(8, -13), new int2(8, 13), new int2(13, -8), new int2(13, 8), 
        new int2(-15, -3), new int2(-15, 3), new int2(-3, -15), new int2(-3, 15), 
        new int2(3, -15), new int2(3, 15), new int2(15, -3), new int2(15, 3), 
        new int2(-15, -4), new int2(-15, 4), new int2(-4, -15), new int2(-4, 15), 
        new int2(4, -15), new int2(4, 15), new int2(15, -4), new int2(15, 4), 
        new int2(-11, -11), new int2(-11, 11), new int2(11, -11), new int2(11, 11), 
        new int2(-12, -10), new int2(-12, 10), new int2(-10, -12), new int2(-10, 12), 
        new int2(10, -12), new int2(10, 12), new int2(12, -10), new int2(12, 10), 
        new int2(-14, -7), new int2(-14, 7), new int2(-7, -14), new int2(-7, 14), 
        new int2(7, -14), new int2(7, 14), new int2(14, -7), new int2(14, 7), 
        new int2(-15, -5), new int2(-15, 5), new int2(-13, -9), new int2(-13, 9), 
        new int2(-9, -13), new int2(-9, 13), new int2(-5, -15), new int2(-5, 15), 
        new int2(5, -15), new int2(5, 15), new int2(9, -13), new int2(9, 13), 
        new int2(13, -9), new int2(13, 9), new int2(15, -5), new int2(15, 5), 
        new int2(-16, 0), new int2(0, -16), new int2(0, 16), new int2(16, 0), 
        new int2(-16, -1), new int2(-16, 1), new int2(-1, -16), new int2(-1, 16), 
        new int2(1, -16), new int2(1, 16), new int2(16, -1), new int2(16, 1), 
        new int2(-16, -2), new int2(-16, 2), new int2(-14, -8), new int2(-14, 8), 
        new int2(-8, -14), new int2(-8, 14), new int2(-2, -16), new int2(-2, 16), 
        new int2(2, -16), new int2(2, 16), new int2(8, -14), new int2(8, 14), 
        new int2(14, -8), new int2(14, 8), new int2(16, -2), new int2(16, 2), 
        new int2(-15, -6), new int2(-15, 6), new int2(-6, -15), new int2(-6, 15), 
        new int2(6, -15), new int2(6, 15), new int2(15, -6), new int2(15, 6), 
        new int2(-16, -3), new int2(-16, 3), new int2(-12, -11), new int2(-12, 11), 
        new int2(-11, -12), new int2(-11, 12), new int2(-3, -16), new int2(-3, 16), 
        new int2(3, -16), new int2(3, 16), new int2(11, -12), new int2(11, 12), 
        new int2(12, -11), new int2(12, 11), new int2(16, -3), new int2(16, 3), 
        new int2(-13, -10), new int2(-13, 10), new int2(-10, -13), new int2(-10, 13), 
        new int2(10, -13), new int2(10, 13), new int2(13, -10), new int2(13, 10), 
        new int2(-16, -4), new int2(-16, 4), new int2(-4, -16), new int2(-4, 16), 
        new int2(4, -16), new int2(4, 16), new int2(16, -4), new int2(16, 4), 
        new int2(-15, -7), new int2(-15, 7), new int2(-7, -15), new int2(-7, 15), 
        new int2(7, -15), new int2(7, 15), new int2(15, -7), new int2(15, 7), 
        new int2(-14, -9), new int2(-14, 9), new int2(-9, -14), new int2(-9, 14), 
        new int2(9, -14), new int2(9, 14), new int2(14, -9), new int2(14, 9), 
        new int2(-16, -5), new int2(-16, 5), new int2(-5, -16), new int2(-5, 16), 
        new int2(5, -16), new int2(5, 16), new int2(16, -5), new int2(16, 5), 
        new int2(-12, -12), new int2(-12, 12), new int2(12, -12), new int2(12, 12), 
        new int2(-17, 0), new int2(-15, -8), new int2(-15, 8), new int2(-8, -15), 
        new int2(-8, 15), new int2(0, -17), new int2(0, 17), new int2(8, -15), 
        new int2(8, 15), new int2(15, -8), new int2(15, 8), new int2(17, 0), 
        new int2(-17, -1), new int2(-17, 1), new int2(-13, -11), new int2(-13, 11), 
        new int2(-11, -13), new int2(-11, 13), new int2(-1, -17), new int2(-1, 17), 
        new int2(1, -17), new int2(1, 17), new int2(11, -13), new int2(11, 13), 
        new int2(13, -11), new int2(13, 11), new int2(17, -1), new int2(17, 1), 
        new int2(-16, -6), new int2(-16, 6), new int2(-6, -16), new int2(-6, 16), 
        new int2(6, -16), new int2(6, 16), new int2(16, -6), new int2(16, 6), 
        new int2(-17, -2), new int2(-17, 2), new int2(-2, -17), new int2(-2, 17), 
        new int2(2, -17), new int2(2, 17), new int2(17, -2), new int2(17, 2), 
        new int2(-14, -10), new int2(-14, 10), new int2(-10, -14), new int2(-10, 14), 
        new int2(10, -14), new int2(10, 14), new int2(14, -10), new int2(14, 10), 
        new int2(-17, -3), new int2(-17, 3), new int2(-3, -17), new int2(-3, 17), 
        new int2(3, -17), new int2(3, 17), new int2(17, -3), new int2(17, 3), 
        new int2(-17, -4), new int2(-17, 4), new int2(-16, -7), new int2(-16, 7), 
        new int2(-7, -16), new int2(-7, 16), new int2(-4, -17), new int2(-4, 17), 
        new int2(4, -17), new int2(4, 17), new int2(7, -16), new int2(7, 16), 
        new int2(16, -7), new int2(16, 7), new int2(17, -4), new int2(17, 4), 
        new int2(-15, -9), new int2(-15, 9), new int2(-9, -15), new int2(-9, 15), 
        new int2(9, -15), new int2(9, 15), new int2(15, -9), new int2(15, 9), 
        new int2(-13, -12), new int2(-13, 12), new int2(-12, -13), new int2(-12, 13), 
        new int2(12, -13), new int2(12, 13), new int2(13, -12), new int2(13, 12), 
        new int2(-17, -5), new int2(-17, 5), new int2(-5, -17), new int2(-5, 17), 
        new int2(5, -17), new int2(5, 17), new int2(17, -5), new int2(17, 5), 
        new int2(-14, -11), new int2(-14, 11), new int2(-11, -14), new int2(-11, 14), 
        new int2(11, -14), new int2(11, 14), new int2(14, -11), new int2(14, 11), 
        new int2(-16, -8), new int2(-16, 8), new int2(-8, -16), new int2(-8, 16), 
        new int2(8, -16), new int2(8, 16), new int2(16, -8), new int2(16, 8), 
        new int2(-18, 0), new int2(0, -18), new int2(0, 18), new int2(18, 0), 
        new int2(-18, -1), new int2(-18, 1), new int2(-17, -6), new int2(-17, 6), 
        new int2(-15, -10), new int2(-15, 10), new int2(-10, -15), new int2(-10, 15), 
        new int2(-6, -17), new int2(-6, 17), new int2(-1, -18), new int2(-1, 18), 
        new int2(1, -18), new int2(1, 18), new int2(6, -17), new int2(6, 17), 
        new int2(10, -15), new int2(10, 15), new int2(15, -10), new int2(15, 10), 
        new int2(17, -6), new int2(17, 6), new int2(18, -1), new int2(18, 1), 
        new int2(-18, -2), new int2(-18, 2), new int2(-2, -18), new int2(-2, 18), 
        new int2(2, -18), new int2(2, 18), new int2(18, -2), new int2(18, 2), 
        new int2(-18, -3), new int2(-18, 3), new int2(-3, -18), new int2(-3, 18), 
        new int2(3, -18), new int2(3, 18), new int2(18, -3), new int2(18, 3), 
        new int2(-16, -9), new int2(-16, 9), new int2(-9, -16), new int2(-9, 16), 
        new int2(9, -16), new int2(9, 16), new int2(16, -9), new int2(16, 9), 
        new int2(-17, -7), new int2(-17, 7), new int2(-13, -13), new int2(-13, 13), 
        new int2(-7, -17), new int2(-7, 17), new int2(7, -17), new int2(7, 17), 
        new int2(13, -13), new int2(13, 13), new int2(17, -7), new int2(17, 7), 
        new int2(-18, -4), new int2(-18, 4), new int2(-14, -12), new int2(-14, 12), 
        new int2(-12, -14), new int2(-12, 14), new int2(-4, -18), new int2(-4, 18), 
        new int2(4, -18), new int2(4, 18), new int2(12, -14), new int2(12, 14), 
        new int2(14, -12), new int2(14, 12), new int2(18, -4), new int2(18, 4), 
        new int2(-15, -11), new int2(-15, 11), new int2(-11, -15), new int2(-11, 15), 
        new int2(11, -15), new int2(11, 15), new int2(15, -11), new int2(15, 11), 
        new int2(-18, -5), new int2(-18, 5), new int2(-5, -18), new int2(-5, 18), 
        new int2(5, -18), new int2(5, 18), new int2(18, -5), new int2(18, 5), 
        new int2(-17, -8), new int2(-17, 8), new int2(-8, -17), new int2(-8, 17), 
        new int2(8, -17), new int2(8, 17), new int2(17, -8), new int2(17, 8), 
        new int2(-16, -10), new int2(-16, 10), new int2(-10, -16), new int2(-10, 16), 
        new int2(10, -16), new int2(10, 16), new int2(16, -10), new int2(16, 10), 
        new int2(-18, -6), new int2(-18, 6), new int2(-6, -18), new int2(-6, 18), 
        new int2(6, -18), new int2(6, 18), new int2(18, -6), new int2(18, 6), 
        new int2(-19, 0), new int2(0, -19), new int2(0, 19), new int2(19, 0), 
        new int2(-19, -1), new int2(-19, 1), new int2(-1, -19), new int2(-1, 19), 
        new int2(1, -19), new int2(1, 19), new int2(19, -1), new int2(19, 1), 
        new int2(-19, -2), new int2(-19, 2), new int2(-14, -13), new int2(-14, 13), 
        new int2(-13, -14), new int2(-13, 14), new int2(-2, -19), new int2(-2, 19), 
        new int2(2, -19), new int2(2, 19), new int2(13, -14), new int2(13, 14), 
        new int2(14, -13), new int2(14, 13), new int2(19, -2), new int2(19, 2), 
        new int2(-15, -12), new int2(-15, 12), new int2(-12, -15), new int2(-12, 15), 
        new int2(12, -15), new int2(12, 15), new int2(15, -12), new int2(15, 12), 
        new int2(-19, -3), new int2(-19, 3), new int2(-17, -9), new int2(-17, 9), 
        new int2(-9, -17), new int2(-9, 17), new int2(-3, -19), new int2(-3, 19), 
        new int2(3, -19), new int2(3, 19), new int2(9, -17), new int2(9, 17), 
        new int2(17, -9), new int2(17, 9), new int2(19, -3), new int2(19, 3), 
        new int2(-18, -7), new int2(-18, 7), new int2(-7, -18), new int2(-7, 18), 
        new int2(7, -18), new int2(7, 18), new int2(18, -7), new int2(18, 7), 
        new int2(-19, -4), new int2(-19, 4), new int2(-16, -11), new int2(-16, 11), 
        new int2(-11, -16), new int2(-11, 16), new int2(-4, -19), new int2(-4, 19), 
        new int2(4, -19), new int2(4, 19), new int2(11, -16), new int2(11, 16), 
        new int2(16, -11), new int2(16, 11), new int2(19, -4), new int2(19, 4), 
        new int2(-19, -5), new int2(-19, 5), new int2(-5, -19), new int2(-5, 19), 
        new int2(5, -19), new int2(5, 19), new int2(19, -5), new int2(19, 5), 
        new int2(-18, -8), new int2(-18, 8), new int2(-8, -18), new int2(-8, 18), 
        new int2(8, -18), new int2(8, 18), new int2(18, -8), new int2(18, 8), 
        new int2(-17, -10), new int2(-17, 10), new int2(-10, -17), new int2(-10, 17), 
        new int2(10, -17), new int2(10, 17), new int2(17, -10), new int2(17, 10), 
        new int2(-14, -14), new int2(-14, 14), new int2(14, -14), new int2(14, 14), 
        new int2(-15, -13), new int2(-15, 13), new int2(-13, -15), new int2(-13, 15), 
        new int2(13, -15), new int2(13, 15), new int2(15, -13), new int2(15, 13), 
        new int2(-19, -6), new int2(-19, 6), new int2(-6, -19), new int2(-6, 19), 
        new int2(6, -19), new int2(6, 19), new int2(19, -6), new int2(19, 6), 
        new int2(-20, 0), new int2(-16, -12), new int2(-16, 12), new int2(-12, -16), 
        new int2(-12, 16), new int2(0, -20), new int2(0, 20), new int2(12, -16), 
        new int2(12, 16), new int2(16, -12), new int2(16, 12), new int2(20, 0), 
        new int2(-20, -1), new int2(-20, 1), new int2(-1, -20), new int2(-1, 20), 
        new int2(1, -20), new int2(1, 20), new int2(20, -1), new int2(20, 1), 
        new int2(-20, -2), new int2(-20, 2), new int2(-2, -20), new int2(-2, 20), 
        new int2(2, -20), new int2(2, 20), new int2(20, -2), new int2(20, 2), 
        new int2(-18, -9), new int2(-18, 9), new int2(-9, -18), new int2(-9, 18), 
        new int2(9, -18), new int2(9, 18), new int2(18, -9), new int2(18, 9), 
        new int2(-20, -3), new int2(-20, 3), new int2(-3, -20), new int2(-3, 20), 
        new int2(3, -20), new int2(3, 20), new int2(20, -3), new int2(20, 3), 
        new int2(-19, -7), new int2(-19, 7), new int2(-17, -11), new int2(-17, 11), 
        new int2(-11, -17), new int2(-11, 17), new int2(-7, -19), new int2(-7, 19), 
        new int2(7, -19), new int2(7, 19), new int2(11, -17), new int2(11, 17), 
        new int2(17, -11), new int2(17, 11), new int2(19, -7), new int2(19, 7), 
        new int2(-20, -4), new int2(-20, 4), new int2(-4, -20), new int2(-4, 20), 
        new int2(4, -20), new int2(4, 20), new int2(20, -4), new int2(20, 4), 
        new int2(-15, -14), new int2(-15, 14), new int2(-14, -15), new int2(-14, 15), 
        new int2(14, -15), new int2(14, 15), new int2(15, -14), new int2(15, 14), 
        new int2(-18, -10), new int2(-18, 10), new int2(-10, -18), new int2(-10, 18), 
        new int2(10, -18), new int2(10, 18), new int2(18, -10), new int2(18, 10), 
        new int2(-20, -5), new int2(-20, 5), new int2(-19, -8), new int2(-19, 8), 
        new int2(-16, -13), new int2(-16, 13), new int2(-13, -16), new int2(-13, 16), 
        new int2(-8, -19), new int2(-8, 19), new int2(-5, -20), new int2(-5, 20), 
        new int2(5, -20), new int2(5, 20), new int2(8, -19), new int2(8, 19), 
        new int2(13, -16), new int2(13, 16), new int2(16, -13), new int2(16, 13), 
        new int2(19, -8), new int2(19, 8), new int2(20, -5), new int2(20, 5), 
        new int2(-17, -12), new int2(-17, 12), new int2(-12, -17), new int2(-12, 17), 
        new int2(12, -17), new int2(12, 17), new int2(17, -12), new int2(17, 12), 
        new int2(-20, -6), new int2(-20, 6), new int2(-6, -20), new int2(-6, 20), 
        new int2(6, -20), new int2(6, 20), new int2(20, -6), new int2(20, 6), 
        new int2(-21, 0), new int2(0, -21), new int2(0, 21), new int2(21, 0), 
        new int2(-21, -1), new int2(-21, 1), new int2(-19, -9), new int2(-19, 9), 
        new int2(-9, -19), new int2(-9, 19), new int2(-1, -21), new int2(-1, 21), 
        new int2(1, -21), new int2(1, 21), new int2(9, -19), new int2(9, 19), 
        new int2(19, -9), new int2(19, 9), new int2(21, -1), new int2(21, 1), 
        new int2(-21, -2), new int2(-21, 2), new int2(-18, -11), new int2(-18, 11), 
        new int2(-11, -18), new int2(-11, 18), new int2(-2, -21), new int2(-2, 21), 
        new int2(2, -21), new int2(2, 21), new int2(11, -18), new int2(11, 18), 
        new int2(18, -11), new int2(18, 11), new int2(21, -2), new int2(21, 2), 
        new int2(-20, -7), new int2(-20, 7), new int2(-7, -20), new int2(-7, 20), 
        new int2(7, -20), new int2(7, 20), new int2(20, -7), new int2(20, 7), 
        new int2(-21, -3), new int2(-21, 3), new int2(-15, -15), new int2(-15, 15), 
        new int2(-3, -21), new int2(-3, 21), new int2(3, -21), new int2(3, 21), 
        new int2(15, -15), new int2(15, 15), new int2(21, -3), new int2(21, 3), 
        new int2(-16, -14), new int2(-16, 14), new int2(-14, -16), new int2(-14, 16), 
        new int2(14, -16), new int2(14, 16), new int2(16, -14), new int2(16, 14), 
        new int2(-21, -4), new int2(-21, 4), new int2(-4, -21), new int2(-4, 21), 
        new int2(4, -21), new int2(4, 21), new int2(21, -4), new int2(21, 4), 
        new int2(-17, -13), new int2(-17, 13), new int2(-13, -17), new int2(-13, 17), 
        new int2(13, -17), new int2(13, 17), new int2(17, -13), new int2(17, 13), 
        new int2(-19, -10), new int2(-19, 10), new int2(-10, -19), new int2(-10, 19), 
        new int2(10, -19), new int2(10, 19), new int2(19, -10), new int2(19, 10), 
        new int2(-20, -8), new int2(-20, 8), new int2(-8, -20), new int2(-8, 20), 
        new int2(8, -20), new int2(8, 20), new int2(20, -8), new int2(20, 8), 
        new int2(-21, -5), new int2(-21, 5), new int2(-5, -21), new int2(-5, 21), 
        new int2(5, -21), new int2(5, 21), new int2(21, -5), new int2(21, 5), 
        new int2(-18, -12), new int2(-18, 12), new int2(-12, -18), new int2(-12, 18), 
        new int2(12, -18), new int2(12, 18), new int2(18, -12), new int2(18, 12), 
        new int2(-21, -6), new int2(-21, 6), new int2(-6, -21), new int2(-6, 21), 
        new int2(6, -21), new int2(6, 21), new int2(21, -6), new int2(21, 6), 
        new int2(-20, -9), new int2(-20, 9), new int2(-16, -15), new int2(-16, 15), 
        new int2(-15, -16), new int2(-15, 16), new int2(-9, -20), new int2(-9, 20), 
        new int2(9, -20), new int2(9, 20), new int2(15, -16), new int2(15, 16), 
        new int2(16, -15), new int2(16, 15), new int2(20, -9), new int2(20, 9), 
        new int2(-19, -11), new int2(-19, 11), new int2(-11, -19), new int2(-11, 19), 
        new int2(11, -19), new int2(11, 19), new int2(19, -11), new int2(19, 11), 
        new int2(-22, 0), new int2(0, -22), new int2(0, 22), new int2(22, 0), 
        new int2(-22, -1), new int2(-22, 1), new int2(-17, -14), new int2(-17, 14), 
        new int2(-14, -17), new int2(-14, 17), new int2(-1, -22), new int2(-1, 22), 
        new int2(1, -22), new int2(1, 22), new int2(14, -17), new int2(14, 17), 
        new int2(17, -14), new int2(17, 14), new int2(22, -1), new int2(22, 1), 
        new int2(-22, -2), new int2(-22, 2), new int2(-2, -22), new int2(-2, 22), 
        new int2(2, -22), new int2(2, 22), new int2(22, -2), new int2(22, 2), 
        new int2(-21, -7), new int2(-21, 7), new int2(-7, -21), new int2(-7, 21), 
        new int2(7, -21), new int2(7, 21), new int2(21, -7), new int2(21, 7), 
        new int2(-22, -3), new int2(-22, 3), new int2(-18, -13), new int2(-18, 13), 
        new int2(-13, -18), new int2(-13, 18), new int2(-3, -22), new int2(-3, 22), 
        new int2(3, -22), new int2(3, 22), new int2(13, -18), new int2(13, 18), 
        new int2(18, -13), new int2(18, 13), new int2(22, -3), new int2(22, 3), 
        new int2(-22, -4), new int2(-22, 4), new int2(-20, -10), new int2(-20, 10), 
        new int2(-10, -20), new int2(-10, 20), new int2(-4, -22), new int2(-4, 22), 
        new int2(4, -22), new int2(4, 22), new int2(10, -20), new int2(10, 20), 
        new int2(20, -10), new int2(20, 10), new int2(22, -4), new int2(22, 4), 
        new int2(-21, -8), new int2(-21, 8), new int2(-19, -12), new int2(-19, 12), 
        new int2(-12, -19), new int2(-12, 19), new int2(-8, -21), new int2(-8, 21), 
        new int2(8, -21), new int2(8, 21), new int2(12, -19), new int2(12, 19), 
        new int2(19, -12), new int2(19, 12), new int2(21, -8), new int2(21, 8), 
        new int2(-22, -5), new int2(-22, 5), new int2(-5, -22), new int2(-5, 22), 
        new int2(5, -22), new int2(5, 22), new int2(22, -5), new int2(22, 5), 
        new int2(-16, -16), new int2(-16, 16), new int2(16, -16), new int2(16, 16), 
        new int2(-17, -15), new int2(-17, 15), new int2(-15, -17), new int2(-15, 17), 
        new int2(15, -17), new int2(15, 17), new int2(17, -15), new int2(17, 15), 
        new int2(-22, -6), new int2(-22, 6), new int2(-18, -14), new int2(-18, 14), 
        new int2(-14, -18), new int2(-14, 18), new int2(-6, -22), new int2(-6, 22), 
        new int2(6, -22), new int2(6, 22), new int2(14, -18), new int2(14, 18), 
        new int2(18, -14), new int2(18, 14), new int2(22, -6), new int2(22, 6), 
        new int2(-20, -11), new int2(-20, 11), new int2(-11, -20), new int2(-11, 20), 
        new int2(11, -20), new int2(11, 20), new int2(20, -11), new int2(20, 11), 
        new int2(-21, -9), new int2(-21, 9), new int2(-9, -21), new int2(-9, 21), 
        new int2(9, -21), new int2(9, 21), new int2(21, -9), new int2(21, 9), 
        new int2(-23, 0), new int2(0, -23), new int2(0, 23), new int2(23, 0), 
        new int2(-23, -1), new int2(-23, 1), new int2(-19, -13), new int2(-19, 13), 
        new int2(-13, -19), new int2(-13, 19), new int2(-1, -23), new int2(-1, 23), 
        new int2(1, -23), new int2(1, 23), new int2(13, -19), new int2(13, 19), 
        new int2(19, -13), new int2(19, 13), new int2(23, -1), new int2(23, 1), 
        new int2(-23, -2), new int2(-23, 2), new int2(-22, -7), new int2(-22, 7), 
        new int2(-7, -22), new int2(-7, 22), new int2(-2, -23), new int2(-2, 23), 
        new int2(2, -23), new int2(2, 23), new int2(7, -22), new int2(7, 22), 
        new int2(22, -7), new int2(22, 7), new int2(23, -2), new int2(23, 2), 
        new int2(-23, -3), new int2(-23, 3), new int2(-3, -23), new int2(-3, 23), 
        new int2(3, -23), new int2(3, 23), new int2(23, -3), new int2(23, 3), 
        new int2(-21, -10), new int2(-21, 10), new int2(-10, -21), new int2(-10, 21), 
        new int2(10, -21), new int2(10, 21), new int2(21, -10), new int2(21, 10), 
        new int2(-20, -12), new int2(-20, 12), new int2(-12, -20), new int2(-12, 20), 
        new int2(12, -20), new int2(12, 20), new int2(20, -12), new int2(20, 12), 
        new int2(-23, -4), new int2(-23, 4), new int2(-17, -16), new int2(-17, 16), 
        new int2(-16, -17), new int2(-16, 17), new int2(-4, -23), new int2(-4, 23), 
        new int2(4, -23), new int2(4, 23), new int2(16, -17), new int2(16, 17), 
        new int2(17, -16), new int2(17, 16), new int2(23, -4), new int2(23, 4), 
        new int2(-22, -8), new int2(-22, 8), new int2(-8, -22), new int2(-8, 22), 
        new int2(8, -22), new int2(8, 22), new int2(22, -8), new int2(22, 8), 
        new int2(-18, -15), new int2(-18, 15), new int2(-15, -18), new int2(-15, 18), 
        new int2(15, -18), new int2(15, 18), new int2(18, -15), new int2(18, 15), 
        new int2(-23, -5), new int2(-23, 5), new int2(-5, -23), new int2(-5, 23), 
        new int2(5, -23), new int2(5, 23), new int2(23, -5), new int2(23, 5), 
        new int2(-19, -14), new int2(-19, 14), new int2(-14, -19), new int2(-14, 19), 
        new int2(14, -19), new int2(14, 19), new int2(19, -14), new int2(19, 14), 
        new int2(-21, -11), new int2(-21, 11), new int2(-11, -21), new int2(-11, 21), 
        new int2(11, -21), new int2(11, 21), new int2(21, -11), new int2(21, 11), 
        new int2(-23, -6), new int2(-23, 6), new int2(-22, -9), new int2(-22, 9), 
        new int2(-9, -22), new int2(-9, 22), new int2(-6, -23), new int2(-6, 23), 
        new int2(6, -23), new int2(6, 23), new int2(9, -22), new int2(9, 22), 
        new int2(22, -9), new int2(22, 9), new int2(23, -6), new int2(23, 6), 
        new int2(-20, -13), new int2(-20, 13), new int2(-13, -20), new int2(-13, 20), 
        new int2(13, -20), new int2(13, 20), new int2(20, -13), new int2(20, 13), 
        new int2(-24, 0), new int2(0, -24), new int2(0, 24), new int2(24, 0), 
        new int2(-24, -1), new int2(-24, 1), new int2(-1, -24), new int2(-1, 24), 
        new int2(1, -24), new int2(1, 24), new int2(24, -1), new int2(24, 1), 
        new int2(-23, -7), new int2(-23, 7), new int2(-17, -17), new int2(-17, 17), 
        new int2(-7, -23), new int2(-7, 23), new int2(7, -23), new int2(7, 23), 
        new int2(17, -17), new int2(17, 17), new int2(23, -7), new int2(23, 7), 
        new int2(-24, -2), new int2(-24, 2), new int2(-18, -16), new int2(-18, 16), 
        new int2(-16, -18), new int2(-16, 18), new int2(-2, -24), new int2(-2, 24), 
        new int2(2, -24), new int2(2, 24), new int2(16, -18), new int2(16, 18), 
        new int2(18, -16), new int2(18, 16), new int2(24, -2), new int2(24, 2), 
        new int2(-22, -10), new int2(-22, 10), new int2(-10, -22), new int2(-10, 22), 
        new int2(10, -22), new int2(10, 22), new int2(22, -10), new int2(22, 10), 
        new int2(-24, -3), new int2(-24, 3), new int2(-21, -12), new int2(-21, 12), 
        new int2(-12, -21), new int2(-12, 21), new int2(-3, -24), new int2(-3, 24), 
        new int2(3, -24), new int2(3, 24), new int2(12, -21), new int2(12, 21), 
        new int2(21, -12), new int2(21, 12), new int2(24, -3), new int2(24, 3), 
        new int2(-19, -15), new int2(-19, 15), new int2(-15, -19), new int2(-15, 19), 
        new int2(15, -19), new int2(15, 19), new int2(19, -15), new int2(19, 15), 
        new int2(-24, -4), new int2(-24, 4), new int2(-4, -24), new int2(-4, 24), 
        new int2(4, -24), new int2(4, 24), new int2(24, -4), new int2(24, 4), 
        new int2(-23, -8), new int2(-23, 8), new int2(-8, -23), new int2(-8, 23), 
        new int2(8, -23), new int2(8, 23), new int2(23, -8), new int2(23, 8), 
        new int2(-20, -14), new int2(-20, 14), new int2(-14, -20), new int2(-14, 20), 
        new int2(14, -20), new int2(14, 20), new int2(20, -14), new int2(20, 14), 
        new int2(-24, -5), new int2(-24, 5), new int2(-5, -24), new int2(-5, 24), 
        new int2(5, -24), new int2(5, 24), new int2(24, -5), new int2(24, 5), 
        new int2(-22, -11), new int2(-22, 11), new int2(-11, -22), new int2(-11, 22), 
        new int2(11, -22), new int2(11, 22), new int2(22, -11), new int2(22, 11), 
        new int2(-23, -9), new int2(-23, 9), new int2(-21, -13), new int2(-21, 13), 
        new int2(-13, -21), new int2(-13, 21), new int2(-9, -23), new int2(-9, 23), 
        new int2(9, -23), new int2(9, 23), new int2(13, -21), new int2(13, 21), 
        new int2(21, -13), new int2(21, 13), new int2(23, -9), new int2(23, 9), 
        new int2(-24, -6), new int2(-24, 6), new int2(-6, -24), new int2(-6, 24), 
        new int2(6, -24), new int2(6, 24), new int2(24, -6), new int2(24, 6), 
        new int2(-18, -17), new int2(-18, 17), new int2(-17, -18), new int2(-17, 18), 
        new int2(17, -18), new int2(17, 18), new int2(18, -17), new int2(18, 17), 
        new int2(-19, -16), new int2(-19, 16), new int2(-16, -19), new int2(-16, 19), 
        new int2(16, -19), new int2(16, 19), new int2(19, -16), new int2(19, 16), 
        new int2(-25, 0), new int2(-24, -7), new int2(-24, 7), new int2(-20, -15), 
        new int2(-20, 15), new int2(-15, -20), new int2(-15, 20), new int2(-7, -24), 
        new int2(-7, 24), new int2(0, -25), new int2(0, 25), new int2(7, -24), 
        new int2(7, 24), new int2(15, -20), new int2(15, 20), new int2(20, -15), 
        new int2(20, 15), new int2(24, -7), new int2(24, 7), new int2(25, 0), 
        new int2(-25, -1), new int2(-25, 1), new int2(-1, -25), new int2(-1, 25), 
        new int2(1, -25), new int2(1, 25), new int2(25, -1), new int2(25, 1), 
        new int2(-22, -12), new int2(-22, 12), new int2(-12, -22), new int2(-12, 22), 
        new int2(12, -22), new int2(12, 22), new int2(22, -12), new int2(22, 12), 
        new int2(-25, -2), new int2(-25, 2), new int2(-23, -10), new int2(-23, 10), 
        new int2(-10, -23), new int2(-10, 23), new int2(-2, -25), new int2(-2, 25), 
        new int2(2, -25), new int2(2, 25), new int2(10, -23), new int2(10, 23), 
        new int2(23, -10), new int2(23, 10), new int2(25, -2), new int2(25, 2), 
        new int2(-25, -3), new int2(-25, 3), new int2(-3, -25), new int2(-3, 25), 
        new int2(3, -25), new int2(3, 25), new int2(25, -3), new int2(25, 3), 
        new int2(-21, -14), new int2(-21, 14), new int2(-14, -21), new int2(-14, 21), 
        new int2(14, -21), new int2(14, 21), new int2(21, -14), new int2(21, 14), 
        new int2(-24, -8), new int2(-24, 8), new int2(-8, -24), new int2(-8, 24), 
        new int2(8, -24), new int2(8, 24), new int2(24, -8), new int2(24, 8), 
        new int2(-25, -4), new int2(-25, 4), new int2(-4, -25), new int2(-4, 25), 
        new int2(4, -25), new int2(4, 25), new int2(25, -4), new int2(25, 4), 
        new int2(-18, -18), new int2(-18, 18), new int2(18, -18), new int2(18, 18), 
        new int2(-25, -5), new int2(-25, 5), new int2(-23, -11), new int2(-23, 11), 
        new int2(-19, -17), new int2(-19, 17), new int2(-17, -19), new int2(-17, 19), 
        new int2(-11, -23), new int2(-11, 23), new int2(-5, -25), new int2(-5, 25), 
        new int2(5, -25), new int2(5, 25), new int2(11, -23), new int2(11, 23), 
        new int2(17, -19), new int2(17, 19), new int2(19, -17), new int2(19, 17), 
        new int2(23, -11), new int2(23, 11), new int2(25, -5), new int2(25, 5), 
        new int2(-22, -13), new int2(-22, 13), new int2(-13, -22), new int2(-13, 22), 
        new int2(13, -22), new int2(13, 22), new int2(22, -13), new int2(22, 13), 
        new int2(-20, -16), new int2(-20, 16), new int2(-16, -20), new int2(-16, 20), 
        new int2(16, -20), new int2(16, 20), new int2(20, -16), new int2(20, 16), 
        new int2(-24, -9), new int2(-24, 9), new int2(-9, -24), new int2(-9, 24), 
        new int2(9, -24), new int2(9, 24), new int2(24, -9), new int2(24, 9), 
        new int2(-25, -6), new int2(-25, 6), new int2(-6, -25), new int2(-6, 25), 
        new int2(6, -25), new int2(6, 25), new int2(25, -6), new int2(25, 6), 
        new int2(-21, -15), new int2(-21, 15), new int2(-15, -21), new int2(-15, 21), 
        new int2(15, -21), new int2(15, 21), new int2(21, -15), new int2(21, 15), 
        new int2(-23, -12), new int2(-23, 12), new int2(-12, -23), new int2(-12, 23), 
        new int2(12, -23), new int2(12, 23), new int2(23, -12), new int2(23, 12), 
        new int2(-25, -7), new int2(-25, 7), new int2(-7, -25), new int2(-7, 25), 
        new int2(7, -25), new int2(7, 25), new int2(25, -7), new int2(25, 7), 
        new int2(-26, 0), new int2(-24, -10), new int2(-24, 10), new int2(-10, -24), 
        new int2(-10, 24), new int2(0, -26), new int2(0, 26), new int2(10, -24), 
        new int2(10, 24), new int2(24, -10), new int2(24, 10), new int2(26, 0), 
        new int2(-26, -1), new int2(-26, 1), new int2(-1, -26), new int2(-1, 26), 
        new int2(1, -26), new int2(1, 26), new int2(26, -1), new int2(26, 1), 
        new int2(-26, -2), new int2(-26, 2), new int2(-22, -14), new int2(-22, 14), 
        new int2(-14, -22), new int2(-14, 22), new int2(-2, -26), new int2(-2, 26), 
        new int2(2, -26), new int2(2, 26), new int2(14, -22), new int2(14, 22), 
        new int2(22, -14), new int2(22, 14), new int2(26, -2), new int2(26, 2), 
        new int2(-26, -3), new int2(-26, 3), new int2(-19, -18), new int2(-19, 18), 
        new int2(-18, -19), new int2(-18, 19), new int2(-3, -26), new int2(-3, 26), 
        new int2(3, -26), new int2(3, 26), new int2(18, -19), new int2(18, 19), 
        new int2(19, -18), new int2(19, 18), new int2(26, -3), new int2(26, 3), 
        new int2(-25, -8), new int2(-25, 8), new int2(-20, -17), new int2(-20, 17), 
        new int2(-17, -20), new int2(-17, 20), new int2(-8, -25), new int2(-8, 25), 
        new int2(8, -25), new int2(8, 25), new int2(17, -20), new int2(17, 20), 
        new int2(20, -17), new int2(20, 17), new int2(25, -8), new int2(25, 8), 
        new int2(-26, -4), new int2(-26, 4), new int2(-4, -26), new int2(-4, 26), 
        new int2(4, -26), new int2(4, 26), new int2(26, -4), new int2(26, 4), 
        new int2(-24, -11), new int2(-24, 11), new int2(-21, -16), new int2(-21, 16), 
        new int2(-16, -21), new int2(-16, 21), new int2(-11, -24), new int2(-11, 24), 
        new int2(11, -24), new int2(11, 24), new int2(16, -21), new int2(16, 21), 
        new int2(21, -16), new int2(21, 16), new int2(24, -11), new int2(24, 11), 
        new int2(-23, -13), new int2(-23, 13), new int2(-13, -23), new int2(-13, 23), 
        new int2(13, -23), new int2(13, 23), new int2(23, -13), new int2(23, 13), 
        new int2(-26, -5), new int2(-26, 5), new int2(-5, -26), new int2(-5, 26), 
        new int2(5, -26), new int2(5, 26), new int2(26, -5), new int2(26, 5), 
        new int2(-25, -9), new int2(-25, 9), new int2(-9, -25), new int2(-9, 25), 
        new int2(9, -25), new int2(9, 25), new int2(25, -9), new int2(25, 9), 
        new int2(-22, -15), new int2(-22, 15), new int2(-15, -22), new int2(-15, 22), 
        new int2(15, -22), new int2(15, 22), new int2(22, -15), new int2(22, 15), 
        new int2(-26, -6), new int2(-26, 6), new int2(-6, -26), new int2(-6, 26), 
        new int2(6, -26), new int2(6, 26), new int2(26, -6), new int2(26, 6), 
        new int2(-24, -12), new int2(-24, 12), new int2(-12, -24), new int2(-12, 24), 
        new int2(12, -24), new int2(12, 24), new int2(24, -12), new int2(24, 12), 
        new int2(-19, -19), new int2(-19, 19), new int2(19, -19), new int2(19, 19), 
        new int2(-20, -18), new int2(-20, 18), new int2(-18, -20), new int2(-18, 20), 
        new int2(18, -20), new int2(18, 20), new int2(20, -18), new int2(20, 18), 
        new int2(-26, -7), new int2(-26, 7), new int2(-25, -10), new int2(-25, 10), 
        new int2(-23, -14), new int2(-23, 14), new int2(-14, -23), new int2(-14, 23), 
        new int2(-10, -25), new int2(-10, 25), new int2(-7, -26), new int2(-7, 26), 
        new int2(7, -26), new int2(7, 26), new int2(10, -25), new int2(10, 25), 
        new int2(14, -23), new int2(14, 23), new int2(23, -14), new int2(23, 14), 
        new int2(25, -10), new int2(25, 10), new int2(26, -7), new int2(26, 7), 
        new int2(-27, 0), new int2(0, -27), new int2(0, 27), new int2(27, 0), 
        new int2(-27, -1), new int2(-27, 1), new int2(-21, -17), new int2(-21, 17), 
        new int2(-17, -21), new int2(-17, 21), new int2(-1, -27), new int2(-1, 27), 
        new int2(1, -27), new int2(1, 27), new int2(17, -21), new int2(17, 21), 
        new int2(21, -17), new int2(21, 17), new int2(27, -1), new int2(27, 1), 
        new int2(-27, -2), new int2(-27, 2), new int2(-2, -27), new int2(-2, 27), 
        new int2(2, -27), new int2(2, 27), new int2(27, -2), new int2(27, 2), 
        new int2(-27, -3), new int2(-27, 3), new int2(-3, -27), new int2(-3, 27), 
        new int2(3, -27), new int2(3, 27), new int2(27, -3), new int2(27, 3), 
        new int2(-26, -8), new int2(-26, 8), new int2(-22, -16), new int2(-22, 16), 
        new int2(-16, -22), new int2(-16, 22), new int2(-8, -26), new int2(-8, 26), 
        new int2(8, -26), new int2(8, 26), new int2(16, -22), new int2(16, 22), 
        new int2(22, -16), new int2(22, 16), new int2(26, -8), new int2(26, 8), 
        new int2(-27, -4), new int2(-27, 4), new int2(-24, -13), new int2(-24, 13), 
        new int2(-13, -24), new int2(-13, 24), new int2(-4, -27), new int2(-4, 27), 
        new int2(4, -27), new int2(4, 27), new int2(13, -24), new int2(13, 24), 
        new int2(24, -13), new int2(24, 13), new int2(27, -4), new int2(27, 4), 
        new int2(-25, -11), new int2(-25, 11), new int2(-11, -25), new int2(-11, 25), 
        new int2(11, -25), new int2(11, 25), new int2(25, -11), new int2(25, 11), 
        new int2(-27, -5), new int2(-27, 5), new int2(-23, -15), new int2(-23, 15), 
        new int2(-15, -23), new int2(-15, 23), new int2(-5, -27), new int2(-5, 27), 
        new int2(5, -27), new int2(5, 27), new int2(15, -23), new int2(15, 23), 
        new int2(23, -15), new int2(23, 15), new int2(27, -5), new int2(27, 5), 
        new int2(-26, -9), new int2(-26, 9), new int2(-9, -26), new int2(-9, 26), 
        new int2(9, -26), new int2(9, 26), new int2(26, -9), new int2(26, 9), 
        new int2(-20, -19), new int2(-20, 19), new int2(-19, -20), new int2(-19, 20), 
        new int2(19, -20), new int2(19, 20), new int2(20, -19), new int2(20, 19), 
        new int2(-27, -6), new int2(-27, 6), new int2(-21, -18), new int2(-21, 18), 
        new int2(-18, -21), new int2(-18, 21), new int2(-6, -27), new int2(-6, 27), 
        new int2(6, -27), new int2(6, 27), new int2(18, -21), new int2(18, 21), 
        new int2(21, -18), new int2(21, 18), new int2(27, -6), new int2(27, 6), 
        new int2(-25, -12), new int2(-25, 12), new int2(-12, -25), new int2(-12, 25), 
        new int2(12, -25), new int2(12, 25), new int2(25, -12), new int2(25, 12), 
        new int2(-24, -14), new int2(-24, 14), new int2(-14, -24), new int2(-14, 24), 
        new int2(14, -24), new int2(14, 24), new int2(24, -14), new int2(24, 14), 
        new int2(-22, -17), new int2(-22, 17), new int2(-17, -22), new int2(-17, 22), 
        new int2(17, -22), new int2(17, 22), new int2(22, -17), new int2(22, 17), 
        new int2(-26, -10), new int2(-26, 10), new int2(-10, -26), new int2(-10, 26), 
        new int2(10, -26), new int2(10, 26), new int2(26, -10), new int2(26, 10), 
        new int2(-27, -7), new int2(-27, 7), new int2(-7, -27), new int2(-7, 27), 
        new int2(7, -27), new int2(7, 27), new int2(27, -7), new int2(27, 7), 
        new int2(-28, 0), new int2(0, -28), new int2(0, 28), new int2(28, 0), 
        new int2(-28, -1), new int2(-28, 1), new int2(-23, -16), new int2(-23, 16), 
        new int2(-16, -23), new int2(-16, 23), new int2(-1, -28), new int2(-1, 28), 
        new int2(1, -28), new int2(1, 28), new int2(16, -23), new int2(16, 23), 
        new int2(23, -16), new int2(23, 16), new int2(28, -1), new int2(28, 1), 
        new int2(-28, -2), new int2(-28, 2), new int2(-2, -28), new int2(-2, 28), 
        new int2(2, -28), new int2(2, 28), new int2(28, -2), new int2(28, 2), 
        new int2(-28, -3), new int2(-28, 3), new int2(-27, -8), new int2(-27, 8), 
        new int2(-8, -27), new int2(-8, 27), new int2(-3, -28), new int2(-3, 28), 
        new int2(3, -28), new int2(3, 28), new int2(8, -27), new int2(8, 27), 
        new int2(27, -8), new int2(27, 8), new int2(28, -3), new int2(28, 3), 
        new int2(-25, -13), new int2(-25, 13), new int2(-13, -25), new int2(-13, 25), 
        new int2(13, -25), new int2(13, 25), new int2(25, -13), new int2(25, 13), 
        new int2(-26, -11), new int2(-26, 11), new int2(-11, -26), new int2(-11, 26), 
        new int2(11, -26), new int2(11, 26), new int2(26, -11), new int2(26, 11), 
        new int2(-28, -4), new int2(-28, 4), new int2(-20, -20), new int2(-20, 20), 
        new int2(-4, -28), new int2(-4, 28), new int2(4, -28), new int2(4, 28), 
        new int2(20, -20), new int2(20, 20), new int2(28, -4), new int2(28, 4), 
        new int2(-24, -15), new int2(-24, 15), new int2(-15, -24), new int2(-15, 24), 
        new int2(15, -24), new int2(15, 24), new int2(24, -15), new int2(24, 15), 
        new int2(-21, -19), new int2(-21, 19), new int2(-19, -21), new int2(-19, 21), 
        new int2(19, -21), new int2(19, 21), new int2(21, -19), new int2(21, 19), 
        new int2(-22, -18), new int2(-22, 18), new int2(-18, -22), new int2(-18, 22), 
        new int2(18, -22), new int2(18, 22), new int2(22, -18), new int2(22, 18), 
        new int2(-28, -5), new int2(-28, 5), new int2(-5, -28), new int2(-5, 28), 
        new int2(5, -28), new int2(5, 28), new int2(28, -5), new int2(28, 5), 
        new int2(-27, -9), new int2(-27, 9), new int2(-9, -27), new int2(-9, 27), 
        new int2(9, -27), new int2(9, 27), new int2(27, -9), new int2(27, 9), 
        new int2(-23, -17), new int2(-23, 17), new int2(-17, -23), new int2(-17, 23), 
        new int2(17, -23), new int2(17, 23), new int2(23, -17), new int2(23, 17), 
        new int2(-28, -6), new int2(-28, 6), new int2(-26, -12), new int2(-26, 12), 
        new int2(-12, -26), new int2(-12, 26), new int2(-6, -28), new int2(-6, 28), 
        new int2(6, -28), new int2(6, 28), new int2(12, -26), new int2(12, 26), 
        new int2(26, -12), new int2(26, 12), new int2(28, -6), new int2(28, 6), 
        new int2(-25, -14), new int2(-25, 14), new int2(-14, -25), new int2(-14, 25), 
        new int2(14, -25), new int2(14, 25), new int2(25, -14), new int2(25, 14), 
        new int2(-27, -10), new int2(-27, 10), new int2(-10, -27), new int2(-10, 27), 
        new int2(10, -27), new int2(10, 27), new int2(27, -10), new int2(27, 10), 
        new int2(-24, -16), new int2(-24, 16), new int2(-16, -24), new int2(-16, 24), 
        new int2(16, -24), new int2(16, 24), new int2(24, -16), new int2(24, 16), 
        new int2(-28, -7), new int2(-28, 7), new int2(-7, -28), new int2(-7, 28), 
        new int2(7, -28), new int2(7, 28), new int2(28, -7), new int2(28, 7), 
        new int2(-29, 0), new int2(-21, -20), new int2(-21, 20), new int2(-20, -21), 
        new int2(-20, 21), new int2(0, -29), new int2(0, 29), new int2(20, -21), 
        new int2(20, 21), new int2(21, -20), new int2(21, 20), new int2(29, 0), 
        new int2(-29, -1), new int2(-29, 1), new int2(-1, -29), new int2(-1, 29), 
        new int2(1, -29), new int2(1, 29), new int2(29, -1), new int2(29, 1), 
        new int2(-29, -2), new int2(-29, 2), new int2(-26, -13), new int2(-26, 13), 
        new int2(-22, -19), new int2(-22, 19), new int2(-19, -22), new int2(-19, 22), 
        new int2(-13, -26), new int2(-13, 26), new int2(-2, -29), new int2(-2, 29), 
        new int2(2, -29), new int2(2, 29), new int2(13, -26), new int2(13, 26), 
        new int2(19, -22), new int2(19, 22), new int2(22, -19), new int2(22, 19), 
        new int2(26, -13), new int2(26, 13), new int2(29, -2), new int2(29, 2), 
        new int2(-28, -8), new int2(-28, 8), new int2(-8, -28), new int2(-8, 28), 
        new int2(8, -28), new int2(8, 28), new int2(28, -8), new int2(28, 8), 
        new int2(-29, -3), new int2(-29, 3), new int2(-27, -11), new int2(-27, 11), 
        new int2(-25, -15), new int2(-25, 15), new int2(-15, -25), new int2(-15, 25), 
        new int2(-11, -27), new int2(-11, 27), new int2(-3, -29), new int2(-3, 29), 
        new int2(3, -29), new int2(3, 29), new int2(11, -27), new int2(11, 27), 
        new int2(15, -25), new int2(15, 25), new int2(25, -15), new int2(25, 15), 
        new int2(27, -11), new int2(27, 11), new int2(29, -3), new int2(29, 3), 
        new int2(-23, -18), new int2(-23, 18), new int2(-18, -23), new int2(-18, 23), 
        new int2(18, -23), new int2(18, 23), new int2(23, -18), new int2(23, 18), 
        new int2(-29, -4), new int2(-29, 4), new int2(-4, -29), new int2(-4, 29), 
        new int2(4, -29), new int2(4, 29), new int2(29, -4), new int2(29, 4), 
        new int2(-28, -9), new int2(-28, 9), new int2(-24, -17), new int2(-24, 17), 
        new int2(-17, -24), new int2(-17, 24), new int2(-9, -28), new int2(-9, 28), 
        new int2(9, -28), new int2(9, 28), new int2(17, -24), new int2(17, 24), 
        new int2(24, -17), new int2(24, 17), new int2(28, -9), new int2(28, 9), 
        new int2(-29, -5), new int2(-29, 5), new int2(-5, -29), new int2(-5, 29), 
        new int2(5, -29), new int2(5, 29), new int2(29, -5), new int2(29, 5), 
        new int2(-26, -14), new int2(-26, 14), new int2(-14, -26), new int2(-14, 26), 
        new int2(14, -26), new int2(14, 26), new int2(26, -14), new int2(26, 14), 
        new int2(-27, -12), new int2(-27, 12), new int2(-12, -27), new int2(-12, 27), 
        new int2(12, -27), new int2(12, 27), new int2(27, -12), new int2(27, 12), 
        new int2(-29, -6), new int2(-29, 6), new int2(-6, -29), new int2(-6, 29), 
        new int2(6, -29), new int2(6, 29), new int2(29, -6), new int2(29, 6), 
        new int2(-25, -16), new int2(-25, 16), new int2(-16, -25), new int2(-16, 25), 
        new int2(16, -25), new int2(16, 25), new int2(25, -16), new int2(25, 16), 
        new int2(-21, -21), new int2(-21, 21), new int2(21, -21), new int2(21, 21), 
        new int2(-28, -10), new int2(-28, 10), new int2(-22, -20), new int2(-22, 20), 
        new int2(-20, -22), new int2(-20, 22), new int2(-10, -28), new int2(-10, 28), 
        new int2(10, -28), new int2(10, 28), new int2(20, -22), new int2(20, 22), 
        new int2(22, -20), new int2(22, 20), new int2(28, -10), new int2(28, 10), 
        new int2(-29, -7), new int2(-29, 7), new int2(-23, -19), new int2(-23, 19), 
        new int2(-19, -23), new int2(-19, 23), new int2(-7, -29), new int2(-7, 29), 
        new int2(7, -29), new int2(7, 29), new int2(19, -23), new int2(19, 23), 
        new int2(23, -19), new int2(23, 19), new int2(29, -7), new int2(29, 7), 
        new int2(-27, -13), new int2(-27, 13), new int2(-13, -27), new int2(-13, 27), 
        new int2(13, -27), new int2(13, 27), new int2(27, -13), new int2(27, 13), 
        new int2(-30, 0), new int2(-24, -18), new int2(-24, 18), new int2(-18, -24), 
        new int2(-18, 24), new int2(0, -30), new int2(18, -24), new int2(18, 24), 
        new int2(24, -18), new int2(24, 18), new int2(-30, -1), new int2(-30, 1), 
        new int2(-26, -15), new int2(-26, 15), new int2(-15, -26), new int2(-15, 26), 
        new int2(-1, -30), new int2(1, -30), new int2(15, -26), new int2(15, 26), 
        new int2(26, -15), new int2(26, 15), new int2(-30, -2), new int2(-30, 2), 
        new int2(-2, -30), new int2(2, -30), new int2(-29, -8), new int2(-29, 8), 
        new int2(-28, -11), new int2(-28, 11), new int2(-11, -28), new int2(-11, 28), 
        new int2(-8, -29), new int2(-8, 29), new int2(8, -29), new int2(8, 29), 
        new int2(11, -28), new int2(11, 28), new int2(28, -11), new int2(28, 11), 
        new int2(29, -8), new int2(29, 8), new int2(-30, -3), new int2(-30, 3), 
        new int2(-3, -30), new int2(3, -30), new int2(-25, -17), new int2(-25, 17), 
        new int2(-17, -25), new int2(-17, 25), new int2(17, -25), new int2(17, 25), 
        new int2(25, -17), new int2(25, 17), new int2(-30, -4), new int2(-30, 4), 
        new int2(-4, -30), new int2(4, -30), new int2(-29, -9), new int2(-29, 9), 
        new int2(-9, -29), new int2(-9, 29), new int2(9, -29), new int2(9, 29), 
        new int2(29, -9), new int2(29, 9), new int2(-30, -5), new int2(-30, 5), 
        new int2(-27, -14), new int2(-27, 14), new int2(-22, -21), new int2(-22, 21), 
        new int2(-21, -22), new int2(-21, 22), new int2(-14, -27), new int2(-14, 27), 
        new int2(-5, -30), new int2(5, -30), new int2(14, -27), new int2(14, 27), 
        new int2(21, -22), new int2(21, 22), new int2(22, -21), new int2(22, 21), 
        new int2(27, -14), new int2(27, 14), new int2(-28, -12), new int2(-28, 12), 
        new int2(-12, -28), new int2(-12, 28), new int2(12, -28), new int2(12, 28), 
        new int2(28, -12), new int2(28, 12), new int2(-23, -20), new int2(-23, 20), 
        new int2(-20, -23), new int2(-20, 23), new int2(20, -23), new int2(20, 23), 
        new int2(23, -20), new int2(23, 20), new int2(-26, -16), new int2(-26, 16), 
        new int2(-16, -26), new int2(-16, 26), new int2(16, -26), new int2(16, 26), 
        new int2(26, -16), new int2(26, 16), new int2(-30, -6), new int2(-30, 6), 
        new int2(-6, -30), new int2(6, -30), new int2(-24, -19), new int2(-24, 19), 
        new int2(-19, -24), new int2(-19, 24), new int2(19, -24), new int2(19, 24), 
        new int2(24, -19), new int2(24, 19), new int2(-29, -10), new int2(-29, 10), 
        new int2(-10, -29), new int2(-10, 29), new int2(10, -29), new int2(10, 29), 
        new int2(29, -10), new int2(29, 10), new int2(-30, -7), new int2(-30, 7), 
        new int2(-30, -30), new int2(-30, -29), new int2(-30, -28), new int2(-30, -27), 
        new int2(-30, -26), new int2(-30, -25), new int2(-30, -24), new int2(-30, -23), 
        new int2(-30, -22), new int2(-30, -21), new int2(-30, -20), new int2(-30, -19), 
        new int2(-30, -18), new int2(-30, -17), new int2(-30, -16), new int2(-30, -15), 
        new int2(-30, -14), new int2(-30, -13), new int2(-30, -12), new int2(-30, -11), 
        new int2(-30, -10), new int2(-30, -9), new int2(-30, -8), 
        new int2(-30, 8), new int2(-30, 9), new int2(-30, 10), new int2(-30, 11), 
        new int2(-30, 12), new int2(-30, 13), new int2(-30, 14), new int2(-30, 15), 
        new int2(-30, 16), new int2(-30, 17), new int2(-30, 18), new int2(-30, 19), 
        new int2(-30, 20), new int2(-30, 21), new int2(-30, 22), new int2(-30, 23), 
        new int2(-30, 24), new int2(-30, 25), new int2(-30, 26), new int2(-30, 27), 
        new int2(-30, 28), new int2(-30, 29), new int2(-29, -30), new int2(-29, -29), 
        new int2(-29, -28), new int2(-29, -27), new int2(-29, -26), new int2(-29, -25), 
        new int2(-29, -24), new int2(-29, -23), new int2(-29, -22), new int2(-29, -21), 
        new int2(-29, -20), new int2(-29, -19), new int2(-29, -18), new int2(-29, -17), 
        new int2(-29, -16), new int2(-29, -15), new int2(-29, -14), new int2(-29, -13), 
        new int2(-29, -12), new int2(-29, -11), new int2(-29, 11), new int2(-29, 12), 
        new int2(-29, 13), new int2(-29, 14), new int2(-29, 15), new int2(-29, 16), 
        new int2(-29, 17), new int2(-29, 18), new int2(-29, 19), new int2(-29, 20), 
        new int2(-29, 21), new int2(-29, 22), new int2(-29, 23), new int2(-29, 24), 
        new int2(-29, 25), new int2(-29, 26), new int2(-29, 27), new int2(-29, 28), 
        new int2(-29, 29), new int2(-28, -30), new int2(-28, -29), new int2(-28, -28), 
        new int2(-28, -27), new int2(-28, -26), new int2(-28, -25), new int2(-28, -24), 
        new int2(-28, -23), new int2(-28, -22), new int2(-28, -21), new int2(-28, -20), 
        new int2(-28, -19), new int2(-28, -18), new int2(-28, -17), new int2(-28, -16), 
        new int2(-28, -15), new int2(-28, -14), new int2(-25, -18), new int2(-25, 18), 
        new int2(-18, -25), new int2(-18, 25), new int2(-7, -30), new int2(7, -30), 
        new int2(18, -25), new int2(18, 25), new int2(25, -18), new int2(25, 18), 
        new int2(-28, -13), new int2(-28, 13), new int2(-28, 14), new int2(-28, 15), 
        new int2(-28, 16), new int2(-28, 17), new int2(-28, 18), new int2(-28, 19), 
        new int2(-28, 20), new int2(-28, 21), new int2(-28, 22), new int2(-28, 23), 
        new int2(-28, 24), new int2(-28, 25), new int2(-28, 26), new int2(-28, 27), 
        new int2(-28, 28), new int2(-28, 29), new int2(-27, -30), new int2(-27, -29), 
        new int2(-27, -28), new int2(-27, -27), new int2(-27, -26), new int2(-27, -25), 
        new int2(-27, -24), new int2(-27, -23), new int2(-27, -22), new int2(-27, -21), 
        new int2(-27, -20), new int2(-27, -19), new int2(-27, -18), new int2(-27, -17), 
        new int2(-27, -16), new int2(-13, -28), new int2(-13, 28), new int2(13, -28), 
        new int2(13, 28), new int2(28, -13), new int2(28, 13), new int2(-27, -15), 
        new int2(-27, 15), new int2(-27, 16), new int2(-27, 17), new int2(-27, 18), 
        new int2(-27, 19), new int2(-27, 20), new int2(-27, 21), new int2(-27, 22), 
        new int2(-27, 23), new int2(-27, 24), new int2(-27, 25), new int2(-27, 26), 
        new int2(-27, 27), new int2(-27, 28), new int2(-27, 29), new int2(-26, -30), 
        new int2(-26, -29), new int2(-26, -28), new int2(-26, -27), new int2(-26, -26), 
        new int2(-26, -25), new int2(-26, -24), new int2(-26, -23), new int2(-26, -22), 
        new int2(-26, -21), new int2(-26, -20), new int2(-26, -19), new int2(-26, -18), 
        new int2(-26, -17), new int2(-26, 17), new int2(-26, 18), new int2(-26, 19), 
        new int2(-26, 20), new int2(-26, 21), new int2(-26, 22), new int2(-26, 23), 
        new int2(-26, 24), new int2(-26, 25), new int2(-26, 26), new int2(-26, 27), 
        new int2(-26, 28), new int2(-26, 29), new int2(-25, -30), new int2(-25, -29), 
        new int2(-25, -28), new int2(-25, -27), new int2(-25, -26), new int2(-25, -25), 
        new int2(-25, -24), new int2(-25, -23), new int2(-25, -22), new int2(-25, -21), 
        new int2(-25, -20), new int2(-25, -19), new int2(-25, 19), new int2(-25, 20), 
        new int2(-25, 21), new int2(-25, 22), new int2(-25, 23), new int2(-25, 24), 
        new int2(-25, 25), new int2(-25, 26), new int2(-25, 27), new int2(-25, 28), 
        new int2(-25, 29), new int2(-24, -30), new int2(-24, -29), new int2(-24, -28), 
        new int2(-24, -27), new int2(-24, -26), new int2(-24, -25), new int2(-24, -24), 
        new int2(-24, -23), new int2(-24, -22), new int2(-24, -21), new int2(-24, -20), 
        new int2(-24, 20), new int2(-24, 21), new int2(-24, 22), new int2(-24, 23), 
        new int2(-24, 24), new int2(-24, 25), new int2(-24, 26), new int2(-24, 27), 
        new int2(-24, 28), new int2(-24, 29), new int2(-23, -30), new int2(-23, -29), 
        new int2(-23, -28), new int2(-23, -27), new int2(-23, -26), new int2(-23, -25), 
        new int2(-23, -24), new int2(-23, -23), new int2(-23, -22), new int2(-23, -21), 
        new int2(-23, 21), new int2(-23, 22), new int2(-23, 23), new int2(-23, 24), 
        new int2(-23, 25), new int2(-23, 26), new int2(-23, 27), new int2(-23, 28), 
        new int2(-23, 29), new int2(-22, -30), new int2(-22, -29), new int2(-22, -28), 
        new int2(-22, -27), new int2(-22, -26), new int2(-22, -25), new int2(-22, -24), 
        new int2(-22, -23), new int2(-22, -22), new int2(-22, 22), new int2(-22, 23), 
        new int2(-22, 24), new int2(-22, 25), new int2(-22, 26), new int2(-22, 27), 
        new int2(-22, 28), new int2(-22, 29), new int2(-21, -30), new int2(-21, -29), 
        new int2(-21, -28), new int2(-21, -27), new int2(-21, -26), new int2(-21, -25), 
        new int2(-21, -24), new int2(-21, -23), new int2(-21, 23), new int2(-21, 24), 
        new int2(-21, 25), new int2(-21, 26), new int2(-21, 27), new int2(-21, 28), 
        new int2(-21, 29), new int2(-20, -30), new int2(-20, -29), new int2(-20, -28), 
        new int2(-20, -27), new int2(-20, -26), new int2(-20, -25), new int2(-20, -24), 
        new int2(-20, 24), new int2(-20, 25), new int2(-20, 26), new int2(-20, 27), 
        new int2(-20, 28), new int2(-20, 29), new int2(-19, -30), new int2(-19, -29), 
        new int2(-19, -28), new int2(-19, -27), new int2(-19, -26), new int2(-19, -25), 
        new int2(-19, 25), new int2(-19, 26), new int2(-19, 27), new int2(-19, 28), 
        new int2(-19, 29), new int2(-18, -30), new int2(-18, -29), new int2(-18, -28), 
        new int2(-18, -27), new int2(-18, -26), new int2(-18, 26), new int2(-18, 27), 
        new int2(-18, 28), new int2(-18, 29), new int2(-17, -30), new int2(-17, -29), 
        new int2(-17, -28), new int2(-17, -27), new int2(-17, -26), new int2(-17, 26), 
        new int2(-17, 27), new int2(-17, 28), new int2(-17, 29), new int2(-16, -30), 
        new int2(-16, -29), new int2(-16, -28), new int2(-16, -27), new int2(-16, 27), 
        new int2(-16, 28), new int2(-16, 29), new int2(-15, -30), new int2(-15, -29), 
        new int2(-15, -28), new int2(-15, -27), new int2(-15, 27), new int2(-15, 28), 
        new int2(-15, 29), new int2(-14, -30), new int2(-14, -29), new int2(-14, -28), 
        new int2(-14, 28), new int2(-14, 29), new int2(-13, -30), new int2(-13, -29), 
        new int2(-13, 29), new int2(-12, -30), new int2(-12, -29), new int2(-12, 29), 
        new int2(-11, -30), new int2(-11, -29), new int2(-11, 29), new int2(-10, -30), 
        new int2(-9, -30), new int2(-8, -30), new int2(8, -30), new int2(9, -30), 
        new int2(10, -30), new int2(11, -30), new int2(11, -29), new int2(11, 29), 
        new int2(12, -30), new int2(12, -29), new int2(12, 29), new int2(13, -30), 
        new int2(13, -29), new int2(13, 29), new int2(14, -30), new int2(14, -29), 
        new int2(14, -28), new int2(14, 28), new int2(14, 29), new int2(15, -30), 
        new int2(15, -29), new int2(15, -28), new int2(15, -27), new int2(15, 27), 
        new int2(15, 28), new int2(15, 29), new int2(16, -30), new int2(16, -29), 
        new int2(16, -28), new int2(16, -27), new int2(16, 27), new int2(16, 28), 
        new int2(16, 29), new int2(17, -30), new int2(17, -29), new int2(17, -28), 
        new int2(17, -27), new int2(17, -26), new int2(17, 26), new int2(17, 27), 
        new int2(17, 28), new int2(17, 29), new int2(18, -30), new int2(18, -29), 
        new int2(18, -28), new int2(18, -27), new int2(18, -26), new int2(18, 26), 
        new int2(18, 27), new int2(18, 28), new int2(18, 29), new int2(19, -30), 
        new int2(19, -29), new int2(19, -28), new int2(19, -27), new int2(19, -26), 
        new int2(19, -25), new int2(19, 25), new int2(19, 26), new int2(19, 27), 
        new int2(19, 28), new int2(19, 29), new int2(20, -30), new int2(20, -29), 
        new int2(20, -28), new int2(20, -27), new int2(20, -26), new int2(20, -25), 
        new int2(20, -24), new int2(20, 24), new int2(20, 25), new int2(20, 26), 
        new int2(20, 27), new int2(20, 28), new int2(20, 29), new int2(21, -30), 
        new int2(21, -29), new int2(21, -28), new int2(21, -27), new int2(21, -26), 
        new int2(21, -25), new int2(21, -24), new int2(21, -23), new int2(21, 23), 
        new int2(21, 24), new int2(21, 25), new int2(21, 26), new int2(21, 27), 
        new int2(21, 28), new int2(21, 29), new int2(22, -30), new int2(22, -29), 
        new int2(22, -28), new int2(22, -27), new int2(22, -26), new int2(22, -25), 
        new int2(22, -24), new int2(22, -23), new int2(22, -22), new int2(22, 22), 
        new int2(22, 23), new int2(22, 24), new int2(22, 25), new int2(22, 26), 
        new int2(22, 27), new int2(22, 28), new int2(22, 29), new int2(23, -30), 
        new int2(23, -29), new int2(23, -28), new int2(23, -27), new int2(23, -26), 
        new int2(23, -25), new int2(23, -24), new int2(23, -23), new int2(23, -22), 
        new int2(23, -21), new int2(23, 21), new int2(23, 22), new int2(23, 23), 
        new int2(23, 24), new int2(23, 25), new int2(23, 26), new int2(23, 27), 
        new int2(23, 28), new int2(23, 29), new int2(24, -30), new int2(24, -29), 
        new int2(24, -28), new int2(24, -27), new int2(24, -26), new int2(24, -25), 
        new int2(24, -24), new int2(24, -23), new int2(24, -22), new int2(24, -21), 
        new int2(24, -20), new int2(24, 20), new int2(24, 21), new int2(24, 22), 
        new int2(24, 23), new int2(24, 24), new int2(24, 25), new int2(24, 26), 
        new int2(24, 27), new int2(24, 28), new int2(24, 29), new int2(25, -30), 
        new int2(25, -29), new int2(25, -28), new int2(25, -27), new int2(25, -26), 
        new int2(25, -25), new int2(25, -24), new int2(25, -23), new int2(25, -22), 
        new int2(25, -21), new int2(25, -20), new int2(25, -19), new int2(25, 19), 
        new int2(25, 20), new int2(25, 21), new int2(25, 22), new int2(25, 23), 
        new int2(25, 24), new int2(25, 25), new int2(25, 26), new int2(25, 27), 
        new int2(25, 28), new int2(25, 29), new int2(26, -30), new int2(26, -29), 
        new int2(26, -28), new int2(26, -27), new int2(26, -26), new int2(26, -25), 
        new int2(26, -24), new int2(26, -23), new int2(26, -22), new int2(26, -21), 
        new int2(26, -20), new int2(26, -19), new int2(26, -18), new int2(26, -17), 
        new int2(26, 17), new int2(26, 18), new int2(26, 19), new int2(26, 20), 
        new int2(26, 21), new int2(26, 22), new int2(26, 23), new int2(26, 24), 
        new int2(26, 25), new int2(26, 26), new int2(26, 27), new int2(26, 28), 
        new int2(26, 29), new int2(27, -30), new int2(27, -29), new int2(27, -28), 
        new int2(27, -27), new int2(27, -26), new int2(27, -25), new int2(27, -24), 
        new int2(27, -23), new int2(27, -22), new int2(27, -21), new int2(27, -20), 
        new int2(27, -19), new int2(27, -18), new int2(27, -17), new int2(27, -16), 
        new int2(27, -15), new int2(27, 15), new int2(27, 16), new int2(27, 17), 
        new int2(27, 18), new int2(27, 19), new int2(27, 20), new int2(27, 21), 
        new int2(27, 22), new int2(27, 23), new int2(27, 24), new int2(27, 25), 
        new int2(27, 26), new int2(27, 27), new int2(27, 28), new int2(27, 29), 
        new int2(28, -30), new int2(28, -29), new int2(28, -28), new int2(28, -27), 
        new int2(28, -26), new int2(28, -25), new int2(28, -24), new int2(28, -23), 
        new int2(28, -22), new int2(28, -21), new int2(28, -20), new int2(28, -19), 
        new int2(28, -18), new int2(28, -17), new int2(28, -16), new int2(28, -15), 
        new int2(28, -14), new int2(28, 14), new int2(28, 15), new int2(28, 16), 
        new int2(28, 17), new int2(28, 18), new int2(28, 19), new int2(28, 20), 
        new int2(28, 21), new int2(28, 22), new int2(28, 23), new int2(28, 24), 
        new int2(28, 25), new int2(28, 26), new int2(28, 27), new int2(28, 28), 
        new int2(28, 29), new int2(29, -30), new int2(29, -29), new int2(29, -28), 
        new int2(29, -27), new int2(29, -26), new int2(29, -25), new int2(29, -24), 
        new int2(29, -23), new int2(29, -22), new int2(29, -21), new int2(29, -20), 
        new int2(29, -19), new int2(29, -18), new int2(29, -17), new int2(29, -16), 
        new int2(29, -15), new int2(29, -14), new int2(29, -13), new int2(29, -12), 
        new int2(29, -11), new int2(29, 11), new int2(29, 12), new int2(29, 13), 
        new int2(29, 14), new int2(29, 15), new int2(29, 16), new int2(29, 17), 
        new int2(29, 18), new int2(29, 19), new int2(29, 20), new int2(29, 21), 
        new int2(29, 22), new int2(29, 23), new int2(29, 24), new int2(29, 25), 
        new int2(29, 26), new int2(29, 27), new int2(29, 28), new int2(29, 29), 
    };
    
    ConcurrentDictionary<int3, Chunk> updateDict = new ConcurrentDictionary<int3, Chunk>();
    World world;

    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        StartCoroutine(UpdateChunkQueue());
        this.StartCoroutineAsync(FindChunksToDeleteCoroutine());
        StartCoroutine(ChunkDeleter());
        StartCoroutine(FindChunksToLoadCoroutine());
        this.StartCoroutineAsync(RenderQueuedChunks());
    }

    /*
    /// <summary>
    /// In case of needing to redraw the render queue array
    /// </summary>
    public void RecalculateDrawingArray()
    {
        List<int2> temp = new List<int2>();
        for (int x = -30; x < 30; x++)
            for (int y = -30; y < 30; y++)
                temp.Add(new int2(x, y));
    
        List<int2> recalculatedDrawingArray = new List<int2>();
        for (int i = 0; i < temp.Count; i++)
        {
            float distance = float.MaxValue;
            int V = -1;
            for (int v = 0; v < temp.Count; v++)
            {
                float tempdist = Vector2.Distance(new Vector2(0, 0), new Vector2(temp[v].x, temp[v].y));
                if (tempdist < distance && !recalculatedDrawingArray.Contains(temp[v]))
                {
                    distance = tempdist;
                    V = v;
                }
            }
            if (V > -1) recalculatedDrawingArray.Add(temp[V]);
        }
        _drawingArray = recalculatedDrawingArray.ToArray();
    
        temp.Clear();
        recalculatedDrawingArray.Clear();
    }
    */

    void Update()
    {
        Vector3 position = transform.position;
        ChunkLOD.PlayerPosition_int = new int3(
                Mathf.FloorToInt(position.x / BlockData.ChunkSize) * BlockData.ChunkSize,
                Mathf.FloorToInt(position.y / BlockData.ChunkSize) * BlockData.ChunkSize,
                Mathf.FloorToInt(position.z / BlockData.ChunkSize) * BlockData.ChunkSize);

        ChunkLOD.PlayerPosition = position;
        ChunkLOD.CameraNormalized = transform.rotation.normalized * Vector3.forward;
    }

    IEnumerator UpdateChunkQueue()
    {
        while (true)
        {
            foreach (Chunk chunk in world.chunks.Values)
            {
                if (chunk.BlockchangeQueue.Count > 0 && chunk.isReading == 0 && !chunk.isGenerating && chunk.rendered && !chunk.isRenderQueued)
                {
                    Tuple<int, BlockMetadata> tempBlockData = chunk.BlockchangeQueue.Dequeue();
                    chunk.SetBlock(tempBlockData.Item1, tempBlockData.Item2);
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator FindChunksToLoadCoroutine()
    {
        yield return new WaitForSeconds(1f);
        int3 newChunkPos;
        Chunk newChunk = null, tempChunk = null;

        while (true)
        {
            int Waiter = 0;
            for (int coord = 0; coord < _drawingArray.Length; coord++)
            {
                newChunkPos = new int3(
                    _drawingArray[coord].x * BlockData.ChunkSize + ChunkLOD.PlayerPosition_int.x, 
                    ChunkLOD.PlayerPosition_int.y, 
                    _drawingArray[coord].y * BlockData.ChunkSize + ChunkLOD.PlayerPosition_int.z);

                if (ChunkLOD.Distance(newChunkPos) > PlayerSettings.Chunk_DrawDistance * BlockData.ChunkSize) continue;
                
                bool shouldSpawn = false;
                for (int i = -4; i < 4; i++)
                    if (!updateDict.ContainsKey(newChunkPos) && !world.CheckChunk(newChunkPos.x, newChunkPos.y + i * BlockData.ChunkSize, newChunkPos.z))
                        shouldSpawn = true;
                if (!shouldSpawn) continue;
                
                if (PlayerSettings.Chunk_LoadingSpeed <= 100)
                {
                    if (Waiter > PlayerSettings.Chunk_LoadingSpeed)
                    {
                        Waiter = 0; 
                        yield return null;
                    }
                    else
                        Waiter++;
                }

                for (int y = -4; y < 4; y++)
                {
                    int3 pos = new int3(newChunkPos.x, y * BlockData.ChunkSize + ChunkLOD.PlayerPosition_int.y, newChunkPos.z);
                    if (!updateDict.ContainsKey(pos))
                    {
                        tempChunk = world.CreateChunk(pos.x, pos.y, pos.z, true);
                        if (tempChunk != null)
                            updateDict.TryAdd(pos, tempChunk);
                    }
                }
                //break;
            }
            yield return null;
        }
    }

    IEnumerator RenderQueuedChunks()
    {
        yield return Ninja.JumpBack;
        Chunk tempChunk = null, tempChunk2 = null;
        bool tempChunkBool = false,
            render = true;
        
        while (true)
        {
            if (updateDict.Count > 0)
            {
                foreach (var item in updateDict)
                {
                    tempChunk = world.GetChunk(item.Key.x, item.Key.y, item.Key.z);
                    tempChunkBool = world.CheckChunk(item.Key.x, item.Key.y, item.Key.z);
                    if ((tempChunkBool && ((tempChunk.isQueuedForDeletion || tempChunk.isEmpty) || tempChunk.rendered)) || !tempChunkBool)
                    {
                        updateDict.TryRemove(item.Key, out _);
                        continue;
                    }

                    render = true;
                    for (int ix = -1; ix <= 1; ix++)
                    {
                        for (int iy = -1; iy <= 1; iy++)
                        {
                            for (int iz = -1; iz <= 1; iz++)
                            {
                                if (ix != 0 && iy != 0 && iz != 0)
                                {
                                    tempChunk2 = world.GetChunk(item.Key.x + ix * 16, item.Key.y + iy * 16, item.Key.z + iz * 16);
                                    if (!world.CheckChunk(item.Key.x + ix * 16, item.Key.y + iy * 16, item.Key.z + iz * 16))
                                    {
                                        render = false;
                                        break;
                                    } 
                                    else
                                    {
                                        if (!tempChunk2.generated)
                                        {
                                            render = false;
                                            break;
                                        }
                                    }
                                }
                                if (!render) break;
                            }
                            if (!render) break;
                        }
                        if (!render) break;
                    }

                    tempChunk2 = null;
                    
                    if (render)
                    {
                        tempChunk = world.GetChunk(item.Key.x, item.Key.y, item.Key.z);
                        if (tempChunk != null)
                        {
                            tempChunk.UpdateChunk(ChunkUpdateMode.ForceSingle);
                            tempChunk.rendered = true;
                        }
                        updateDict.TryRemove(item.Key, out _);
                    }
                }
            }
            yield return null;
        }
    }

    Queue<int3> chunksToDelete = new Queue<int3>();
    IEnumerator FindChunksToDeleteCoroutine()
    {
        yield return Ninja.JumpBack;
        while (true)
        {
            foreach (var chunk in world.chunks)
            {
                if (ChunkLOD.Distance(chunk.Value.pos) > BlockData.ChunkSize * (PlayerSettings.Chunk_DrawDistance + 1))
                    chunksToDelete.Enqueue(chunk.Key);
                //else
                //{
                //    //byte PreviousLOD = chunk.Value.LOD;
                //    if (distance > BlockData.ChunkSize * (PlayerSettings.Chunk_DrawDistance * 0.4f))
                //        chunk.Value.LOD = 2;
                //    //else if (distance > BlockData.ChunkSize * (PlayerSettings.Chunk_DrawDistance * 0.5f))
                //    //    chunk.Value.LOD = 4;
                //    //else if (distance > BlockData.ChunkSize * (PlayerSettings.Chunk_DrawDistance * 0.6f))
                //    //    chunk.Value.LOD = 8;
                //    else
                //        chunk.Value.LOD = 1;
                //}
            }

            //foreach (var chunk in chunksToDelete)
            //{
            //    int3 pos = new int3(chunk.x, chunk.y, chunk.z);
            //    UnityMainThreadDispatcher.Instance().Enqueue(() => {
            //        if (!updateDict.ContainsKey(pos))
            //            updateDict.TryRemove(pos, out _);
            //        world.DestroyChunk(chunk.x, chunk.y, chunk.z);
            //    });
            //    
            //}
            for (int i = 0; i < 10; i++)
                yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator ChunkDeleter()
    {
        while (true)
        {
            if (chunksToDelete.Count > 0)
                for (int i = 0; i < chunksToDelete.Count; i++)
                {
                    int3 chunk = chunksToDelete.Dequeue();
                    if (!updateDict.ContainsKey(chunk))
                        updateDict.TryRemove(chunk, out _);

                    if (world.CheckChunk(chunk.x, chunk.y, chunk.z))
                        world.DestroyChunk(chunk.x, chunk.y, chunk.z);
                }
            yield return new WaitForEndOfFrame();
        }

    }
}

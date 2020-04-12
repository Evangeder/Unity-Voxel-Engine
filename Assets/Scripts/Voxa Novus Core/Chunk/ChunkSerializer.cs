using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace VoxaNovus
{
    public static class ChunkSerializer
    {
        public static int SerializeChunk()
        {
            List<byte> encodedData = new List<byte>();
            return 0;
        }

        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        struct SerializeChunkJob : IJob
        {
            public void Execute()
            {

            }
        }
    }
}
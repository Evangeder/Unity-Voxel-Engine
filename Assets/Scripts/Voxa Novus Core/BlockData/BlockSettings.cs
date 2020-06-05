using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;

namespace VoxaNovus
{
    [Serializable]
    public static class BlockSettings
    {
        public const float TextureOffset = 0.005f;
        public const ushort BlockAirID = 0;

        public const int ChunkSize = 16;

        /// <summary>
        /// Valid values are only 1.0f and 0.5f
        /// </summary>
        public static float ChunkScale = 1f;

        public const float LargestValidMarchingValue = 0.996f;

        public static List<BlockData> byID = new List<BlockData>();
        public static NativeArray<BlockData> NativeByID;
        public static BlockPhysics[] PhysicsFunctions;
        public static bool[] PhysicsBound;
        public static sbyte MarchingLayers;
        public static float BlockTileSize = 0.25f;
        public static float TextureSize = 0.5f;
        public static Texture2D BlockTexture;

        public static string[] BlockNames, FullNames;

        public static bool SoundsLoaded = false;
        public static List<AudioClip>[] BlockSounds;

        public static ulong Timestamp; // for physics sortedlist

        public static World world;

        public static int worldGen = 0;
    }
}
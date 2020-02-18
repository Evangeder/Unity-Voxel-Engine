using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;

[Serializable]
public enum BlockTextures : int
{
    Up = 0,
    Down = 1,
    North = 2,
    South = 3,
    East = 4,
    West = 5,
    Marched = 6
}

[Serializable]
public struct BlockMetadata
{
    public BlockMetadata(ushort ID, BlockSwitches switches, byte MarchedValue = 0)
    {
        this.ID = ID;
        Switches = switches;
        this.MarchedValue = MarchedValue;
    }

    /// <summary>
    /// Old version that is using float value, this gets converted to byte
    /// </summary>
    public BlockMetadata(ushort ID, BlockSwitches switches, float MarchedValue = 0f)
    {
        this.ID = ID;
        Switches = switches;
        //Switches = BlockSwitches.None;
        //Switches |= marched ? BlockSwitches.Marched : BlockSwitches.None;
        this.MarchedValue = (byte)(MarchedValue * 255f);
    }

    public BlockMetadata(BlockMetadata T)
    {
        this = T;
    }
    
    public void SetMarchedValue(float f)
    {
        if (f >= 1f)
            MarchedValue = 254;
        else if (f <= 0f)
            MarchedValue = 0;
        else
            MarchedValue = (byte)(f * 255f);
    }

    public float GetMarchedValue()
    {
        return (float)(MarchedValue / 255f);
    }

    public ushort ID { get; set; }
    public byte MarchedValue { get; set; }
    public BlockSwitches Switches { get; set; }

    public static bool operator == (BlockMetadata operand1, BlockMetadata operand2)
    {
        return 
            operand1.ID == operand2.ID 
            && operand1.Switches == operand2.Switches 
            && operand1.MarchedValue == operand2.MarchedValue;
    }

    public static bool operator != (BlockMetadata operand1, BlockMetadata operand2)
    {
        return
            !(operand1.ID == operand2.ID
            && operand1.Switches == operand2.Switches
            && operand1.MarchedValue == operand2.MarchedValue);
    }

    public static BlockMetadata EmptyPhysicsTrigger()
    {
        return new BlockMetadata
        {
            ID = 0,
            MarchedValue = 0,
            Switches = BlockSwitches.PhysicsTrigger
        };
    }
}

[Serializable]
public static partial class BlockData
{
    public const float TextureOffset = 0.005f;
    public const ushort BlockAirID = 0;
    public const int ChunkSize = 16;
    public const float LargestValidMarchingValue = 0.996f;

    public static List<BlockTypes> byID = new List<BlockTypes>();
    public static NativeArray<BlockTypes> NativeByID;
    public static BlockPhysics[] PhysicsFunctions;
    public static bool[] PhysicsBound;
    public static sbyte MarchingLayers;
    public static float BlockTileSize = 0.25f;
    public static float TextureSize = 1f;
    public static Texture2D BlockTexture;

    public static string[] BlockNames;

    public static bool SoundsLoaded = false;
    public static List<AudioClip>[] BlockSounds;

    public static ulong Timestamp; // for physics sortedlist

    public static World world;
}
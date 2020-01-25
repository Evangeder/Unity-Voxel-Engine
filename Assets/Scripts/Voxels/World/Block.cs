﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
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

[Flags]
public enum BlockSwitches : byte // up to 7 booleans
{
    None              = 0,
    Marched           = 1 << 1,
    PhysicsTriggered  = 1 << 2,
    Undefined         = 1 << 3,
    Undefined1        = 1 << 4,
    Undefined2        = 1 << 5,
    Undefined3        = 1 << 6,
    Undefined4        = 1 << 7,
    // usage: BlockSwitches.Marched | BlockSwitches.PhysicsTriggered
    // usage: BlockSwitches.Marched & BlockSwitches.PhysicsTriggered
}

public static class BlockSwitchesClass
{
    public static bool Get(this BlockSwitches blockSwitches, BlockSwitches switches)
    {
        if ((switches & blockSwitches) == switches) return true;
        return false;
    }

    public static void Clear(this BlockSwitches blockSwitches)
    {
        blockSwitches = BlockSwitches.None;
    }

    public static void SetTrue(this BlockSwitches blockSwitches, BlockSwitches switches)
    {
        blockSwitches |= switches;
    }

    public static void SetFalse(this BlockSwitches blockSwitches, BlockSwitches switches)
    {
        blockSwitches |= ~switches;
    }
}

//BlockSwitches b = BlockSwitches.Marched | BlockSwitches.PhysicsTriggered;
//if ((b & BlockSwitches.Marched) == BlockSwitches.Marched)
//    Debug.Log("Test: Marched");
//if ((b & BlockSwitches.PhysicsTriggered) == BlockSwitches.PhysicsTriggered)
//    Debug.Log("Test: PhysicsTriggered");

[Serializable]
public readonly struct BlockTypes
{

    public BlockTypes(bool Solid, int2 Texture, bool UsePhysics = false, float PhysicsTime = 0f, int CullingMode = 0, bool Foliage = false, sbyte MarchingCubesLayer = 0)
    {
        this.Solid = Solid;
        this.Texture_Up = Texture;
        this.Texture_Down = Texture;
        this.Texture_North = Texture;
        this.Texture_South = Texture;
        this.Texture_East = Texture;
        this.Texture_West = Texture;
        this.Texture_Marched = Texture;
        this.UsePhysics = UsePhysics;
        this.PhysicsTime = PhysicsTime;
        this.CullingMode = CullingMode;
        this.MarchingCubesLayer = MarchingCubesLayer;
        this.Foliage = Foliage;
    }

    public BlockTypes(bool Solid, int2[] Textures, bool UsePhysics = false, float PhysicsTime = 0f, int CullingMode = 0, bool Foliage = false, sbyte MarchingCubesLayer = 0)
    {
        this.Solid = Solid;
        this.Texture_Up = Textures[(int)BlockTextures.Up];
        this.Texture_Down = Textures[(int)BlockTextures.Down];
        this.Texture_North = Textures[(int)BlockTextures.North];
        this.Texture_South = Textures[(int)BlockTextures.South];
        this.Texture_East = Textures[(int)BlockTextures.East];
        this.Texture_West = Textures[(int)BlockTextures.West];
        this.Texture_Marched = Textures[(int)BlockTextures.Marched];
        this.UsePhysics = UsePhysics;
        this.PhysicsTime = PhysicsTime;
        this.CullingMode = CullingMode;
        this.MarchingCubesLayer = MarchingCubesLayer;
        this.Foliage = Foliage;
    }

    public BlockTypes(bool Solid, List<int2> Textures, bool UsePhysics = false, float PhysicsTime = 0f, int CullingMode = 0, bool Foliage = false, sbyte MarchingCubesLayer = 0)
    {
        this.Solid = Solid;
        this.Texture_Up = Textures[(int)BlockTextures.Up];
        this.Texture_Down = Textures[(int)BlockTextures.Down];
        this.Texture_North = Textures[(int)BlockTextures.North];
        this.Texture_South = Textures[(int)BlockTextures.South];
        this.Texture_East = Textures[(int)BlockTextures.East];
        this.Texture_West = Textures[(int)BlockTextures.West];
        this.Texture_Marched = Textures[(int)BlockTextures.Marched];
        this.UsePhysics = UsePhysics;
        this.PhysicsTime = PhysicsTime;
        this.CullingMode = CullingMode;
        this.MarchingCubesLayer = MarchingCubesLayer;
        this.Foliage = Foliage;
    }

    public BlockTypes(bool Solid = false)
    {
        this.Solid = Solid;
        this.Texture_Up = new int2();
        this.Texture_Down = new int2();
        this.Texture_North = new int2();
        this.Texture_South = new int2();
        this.Texture_East = new int2();
        this.Texture_West = new int2();
        this.Texture_Marched = new int2();
        this.PhysicsTime = new float();
        this.CullingMode = new int();
        this.MarchingCubesLayer = new sbyte();
        this.UsePhysics = new boolean();
        this.Foliage = new boolean();
    }

    public int2 Texture_Up { get; }
    public int2 Texture_Down { get; }
    public int2 Texture_North { get; }
    public int2 Texture_South { get; }
    public int2 Texture_East { get; }
    public int2 Texture_West { get; }
    public int2 Texture_Marched { get; }
    public int CullingMode { get; }
    public float PhysicsTime { get; }
    public sbyte MarchingCubesLayer { get; }
    public boolean UsePhysics { get; }
    public boolean Solid { get; }
    public boolean Foliage { get; }
}

[Serializable]
public struct BlockMetadata
{
    public BlockMetadata(ushort ID, bool marched = false, byte MarchedValue = 0)
    {
        this.ID = ID;
        Switches = BlockSwitches.None;
        Switches |= marched ? BlockSwitches.Marched : BlockSwitches.None;
        this.MarchedValue = MarchedValue;
    }

    /// <summary>
    /// Old version that is using float value
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="marched"></param>
    /// <param name="MarchedValue"></param>
    public BlockMetadata(ushort ID, bool marched = false, float MarchedValue = 0f)
    {
        this.ID = ID;
        Switches = BlockSwitches.None;
        Switches |= marched ? BlockSwitches.Marched : BlockSwitches.None;
        this.MarchedValue = (byte)(MarchedValue * 255f);
    }

    // For copying
    public BlockMetadata(BlockMetadata T)
    {
        this = T;
    }

    public BlockMetadata(PhysicsQueuedBlockMetadata T)
    {
        ID = T.ID;
        MarchedValue = T.MarchedValue;
        Switches = T.Switches;
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
    
    public static explicit operator BlockMetadata(PhysicsQueuedBlockMetadata T) => new BlockMetadata(T);
}

public struct PhysicsQueuedBlockMetadata
{
    public PhysicsQueuedBlockMetadata(long timestamp, ushort ID, bool marched = false, byte MarchedValue = 0)
    {
        this.Timestamp = timestamp;
        this.ID = ID;
        Switches = BlockSwitches.None;
        Switches |= marched ? BlockSwitches.Marched : BlockSwitches.None;
        this.MarchedValue = MarchedValue;
    }

    /// <summary>
    /// Old version that is using float value
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="marched"></param>
    /// <param name="MarchedValue"></param>
    public PhysicsQueuedBlockMetadata(long timestamp, ushort ID, bool marched = false, float MarchedValue = 0f)
    {
        this.Timestamp = timestamp;
        this.ID = ID;
        Switches = BlockSwitches.None;
        Switches |= marched ? BlockSwitches.Marched : BlockSwitches.None;
        this.MarchedValue = (byte)(MarchedValue * 255f);
    }

    // For copying
    public PhysicsQueuedBlockMetadata(PhysicsQueuedBlockMetadata T)
    {
        this = T;
    }

    public PhysicsQueuedBlockMetadata(long timestamp, BlockMetadata T)
    {
        this.Timestamp = timestamp;
        ID = T.ID;
        MarchedValue = T.MarchedValue;
        Switches = T.Switches;
    }

    public long Timestamp;
    public ushort ID { get; set; }
    public byte MarchedValue { get; set; }
    public BlockSwitches Switches { get; set; }
}

[Serializable]
public static class BlockData
{
    public const float TextureOffset = 0.005f;
    public const ushort BlockAirID = 0;
    public const int ChunkSize = 16;
    public const float LargestValidMarchingValue = 0.996f;

    public static List<BlockTypes> byID = new List<BlockTypes>();
    public static NativeArray<BlockTypes> NativeByID;
    public static sbyte MarchingLayers;
    public static float BlockTileSize = 0.25f;
    public static float TextureSize = 1f;
    public static Texture2D BlockTexture;
    public static string[] BlockNames;
    public static bool SoundsLoaded = false;

    public static List<AudioClip>[] BlockSounds;
    
    public static int GetAddress(int x, int y, int z, int size = 16)
    {
        return (x + y * size + z * size * size);
    }

    public static void InitalizeClient()
    {
        byID.Clear();
        BlockTexture = new Texture2D(128, 128);
        if (!Directory.Exists(Application.dataPath + "/Mods"))
            Directory.CreateDirectory(Application.dataPath + "/Mods");

        IniFile Blocks_INI = new IniFile("/Mods/Blocks.ini");
        if (File.Exists(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File"))))
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File")));
            BlockTexture = new Texture2D(int.Parse(Blocks_INI.Read("Texture_Width")), int.Parse(Blocks_INI.Read("Texture_Height")), TextureFormat.RGB24, false);
            BlockTexture.filterMode = FilterMode.Point;
            BlockTexture.LoadImage(bytes);
            BlockTexture.Apply();
        } else {
            Debug.LogError($"<color=red><b>COULD NOT FIND TEXTURES FILE!</b></color>, game will continue to run without texturing.\nPlease create or drag Blocks.png file to path: <i>{Application.dataPath}/Mods/</i>.");
        }

        if (Blocks_INI.KeyExists("TileSize"))
            BlockTileSize = float.Parse(Blocks_INI.Read("TileSize"), CultureInfo.InvariantCulture);
        if (Blocks_INI.KeyExists("TextureSize"))
            TextureSize = float.Parse(Blocks_INI.Read("TextureSize"), CultureInfo.InvariantCulture);
    }

    public static IEnumerator InitializeSounds()
    {
        IniFile Blocks_INI = new IniFile("/Mods/Blocks.ini");
        for (int i = 0; i < BlockNames.Length; i++)
        {
            BlockSounds[i] = new List<AudioClip>();
            if (Blocks_INI.KeyExists("Sounds", i.ToString()))
            {
                int sounds = int.Parse(Blocks_INI.Read("Sounds", i.ToString()));
                for (int i2 = 0; i2 < sounds; i2++)
                {
                    string path =
                        $"{Application.dataPath}/Mods/{Blocks_INI.Read($"Sound{i2}", i.ToString()).Replace('\\', '/')}";
                    using (var download = new WWW(path))
                    {
                        yield return download;
                        var clip = download.GetAudioClip();
                        if (clip != null)
                            BlockSounds[i].Add(clip);
                    }
                }
                Debug.Log($"[BlockData] Loaded {BlockSounds[i].Count} sounds for block {BlockNames[i]}");
            }
        }
        SoundsLoaded = true;
    }

    public static void InitializeBlocks(bool IsServer = true)
    {
        byID.Clear();

        BlockTexture = new Texture2D(128, 128);

        if (!Directory.Exists(Application.dataPath + "/Mods"))
            Directory.CreateDirectory(Application.dataPath + "/Mods");


        IniFile Blocks_INI = new IniFile("/Mods/Blocks.ini");
        if (Blocks_INI.KeyExists("TileSize"))
        {
            // Data exists, parse.
            var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture;
            //culture.NumberFormat.NumberDecimalSeparator = ".";
            BlockTileSize = float.Parse(Blocks_INI.Read("TileSize"), culture);

            if (File.Exists(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File"))))
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File")));
                BlockTexture = new Texture2D(int.Parse(Blocks_INI.Read("Texture_Width")), int.Parse(Blocks_INI.Read("Texture_Height")), TextureFormat.RGB24, false);
                BlockTexture.filterMode = FilterMode.Point;
                BlockTexture.LoadImage(bytes);
            } else {
                Debug.LogError($"<color=red><b>COULD NOT FIND TEXTURES FILE!</b></color>, game will continue to run without texturing.\nPlease create or drag Blocks.png file to path: <i>{Application.dataPath}/Mods/</i>.");
            }

            if (Blocks_INI.KeyExists("TextureSize"))
            {
                TextureSize = float.Parse(Blocks_INI.Read("TextureSize"), culture);
            }

            int MaxBlocks = int.Parse(Blocks_INI.Read("Blocktypes"));
            BlockSounds = new List<AudioClip>[MaxBlocks];
            BlockNames = new string[MaxBlocks];

            for (int i = 0; i < MaxBlocks; i++)
            {
                BlockNames[i] = Blocks_INI.Read("Name", i.ToString());
                
                //if (Blocks_INI.KeyExists("Sounds",i.ToString()))
                //{
                //    int sounds = int.Parse(Blocks_INI.Read("Sounds", i.ToString()));
                //    for (int i2 = 0; i2 < sounds; i2++)
                //    {
                //        string path = $"{Application.dataPath}/Mods/{Blocks_INI.Read($"Sound{i2}", i.ToString()).Replace('\\', '/')}";
                //        Debug.Log($"[BlockData] Loading sound: {path}");
                //        using (var download = new WWW(path))
                //        {
                //            var clip = download.GetAudioClip();
                //            if (clip != null)
                //                BlockSounds[i].Add(clip);
                //        }
                //    }
                //    Debug.Log($"[BlockData] Loaded {BlockSounds[i].Count} sounds for block {BlockNames[i]}");
                //}
                
                if (Blocks_INI.KeyExists("Texture_Up", i.ToString()))
                {
                    int2[] tex = new int2[7];
                    string[] tempstr2 = Blocks_INI.Read("Texture_Up", i.ToString()).Split(',');
                    tex[0] = new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1]));
                    tempstr2 = Blocks_INI.Read("Texture_Down", i.ToString()).Split(',');
                    tex[1] = new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1]));
                    tempstr2 = Blocks_INI.Read("Texture_North", i.ToString()).Split(',');
                    tex[2] = new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1]));
                    tempstr2 = Blocks_INI.Read("Texture_South", i.ToString()).Split(',');
                    tex[3] = new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1]));
                    tempstr2 = Blocks_INI.Read("Texture_East", i.ToString()).Split(',');
                    tex[4] = new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1]));
                    tempstr2 = Blocks_INI.Read("Texture_West", i.ToString()).Split(',');
                    tex[5] = new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1]));

                    if (Blocks_INI.KeyExists("Texture_Marched", i.ToString())) {
                        tempstr2 = Blocks_INI.Read("Texture_Marched", i.ToString()).Split(',');
                        tex[6] = new int2(new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1])));
                    } else
                        tex[6] = tex[0];
 
                    tempstr2 = Blocks_INI.Read("Texture_West", i.ToString()).Split(',');
                    tex[5] = new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1]));

                    byID.Add(new BlockTypes(
                        bool.Parse(Blocks_INI.Read("Solid", i.ToString())),
                        tex,
                        bool.Parse(Blocks_INI.Read("Uses_Physics", i.ToString())),
                        int.Parse(Blocks_INI.Read("Physics_Time", i.ToString())),
                        int.Parse(Blocks_INI.Read("CullingMode", i.ToString())),
                        bool.Parse(Blocks_INI.Read("Foliage", i.ToString())),
                        sbyte.Parse(Blocks_INI.Read("MarchingLayer", i.ToString()))));
                } else {
                    string[] tempstr2 = Blocks_INI.Read("Texture", i.ToString()).Split(',');
                    byID.Add(new BlockTypes(
                        bool.Parse(Blocks_INI.Read("Solid", i.ToString())),
                        new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1])),
                        bool.Parse(Blocks_INI.Read("Uses_Physics", i.ToString())),
                        int.Parse(Blocks_INI.Read("Physics_Time",  i.ToString())),
                        int.Parse(Blocks_INI.Read("CullingMode",i.ToString())),
                        bool.Parse(Blocks_INI.Read("Foliage", i.ToString())),
                        sbyte.Parse(Blocks_INI.Read("MarchingLayer", i.ToString()))));
                }
                sbyte CurrentBlockLayer = sbyte.Parse(Blocks_INI.Read("MarchingLayer", i.ToString()));
                if (MarchingLayers < CurrentBlockLayer) MarchingLayers = CurrentBlockLayer;
            }
        } else {
            // Create new, sample file.

            Blocks_INI.Write("Texture_File", "Blocks.png");
            Blocks_INI.Write("Texture_Width", "128");
            Blocks_INI.Write("Texture_Height", "128");
            Blocks_INI.Write("TileSize", "0.25");

            List<int2> textures;                 // This is for custom blocks
            textures = new List<int2>() {
                new int2(2, 0), // up
                new int2(1, 0), // down
                new int2(3, 0), // north
                new int2(3, 0), // south
                new int2(3, 0), // east
                new int2(3, 0),  // west
                new int2(2, 0) // marched
            };

            byID.Add(new BlockTypes(false));                                        //       AIR
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       STONE
            byID.Add(new BlockTypes(true, textures));                               //       GRASS
            byID.Add(new BlockTypes(true, new int2(1, 0)));                         //       DIRT
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       COBBLESTONE
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       PLANKS
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       SAPPLING
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       BEDROCK
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       FLOWING WATER
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       STATIONARY WATER
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       FLOWING LAVA
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       STATIONARY LAVA
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       SAND
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       GRAVEL
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       GOLD ORE
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       IRON ORE
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       COAL ORE
            textures = new List<int2>() { new int2(2, 1), new int2(2, 1), new int2(1, 1), new int2(1, 1), new int2(1, 1), new int2(1, 1), new int2(1, 1) };
            byID.Add(new BlockTypes(true, textures));                               //       WOOD (LOG)
            byID.Add(new BlockTypes(true, new int2(0, 0), false, 0f, 1, false));    //       LEAVES
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       SPONGE
            byID.Add(new BlockTypes(true, new int2(0, 0), false, 0f, 2, false));    //       GLASS
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (RED)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (ORANGE)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (YELLOW)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (CHARTREUSE)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (GREEN)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (SPRING GREEN)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (CYAN)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (CAPRI)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (ULTRAMARINE)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (VIOLET)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (PURPLE)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (MAGNETA)
            byID.Add(new BlockTypes(true, new int2(0, 0)));                         //       CLOTH (ROSE)

            Blocks_INI.Write("Blocktypes", (byID.Count - 1).ToString());

            for (int i = 0; i < byID.Count; i++)
            {
                Blocks_INI.Write("Solid", byID[i].Solid + "", i + "");
                Blocks_INI.Write("CullingMode", (byID[i].CullingMode).ToString(), i.ToString());
                if ((byID[i].Texture_East.x == byID[i].Texture_Up.x && byID[i].Texture_East.y == byID[i].Texture_Up.y) &&
                        (byID[i].Texture_West.x == byID[i].Texture_Up.x && byID[i].Texture_West.y == byID[i].Texture_Up.y) &&
                        (byID[i].Texture_North.x == byID[i].Texture_Up.x && byID[i].Texture_North.y == byID[i].Texture_Up.y) &&
                        (byID[i].Texture_South.x == byID[i].Texture_Up.x && byID[i].Texture_South.y == byID[i].Texture_Up.y) &&
                        (byID[i].Texture_Down.x == byID[i].Texture_Up.x && byID[i].Texture_Down.y == byID[i].Texture_Up.y))
                {
                    Blocks_INI.Write("Texture", $"{(byID[i].Texture_Up.x).ToString()},{(byID[i].Texture_Up.y).ToString()}", i.ToString());
                }
                else {
                    Blocks_INI.Write("Texture_Up", $"{byID[i].Texture_Up.x.ToString()},{byID[i].Texture_Up.y.ToString()}", i.ToString());
                    Blocks_INI.Write("Texture_Down", $"{byID[i].Texture_Down.x.ToString()},{byID[i].Texture_Down.y.ToString()}", i.ToString());
                    Blocks_INI.Write("Texture_North", $"{byID[i].Texture_North.x.ToString()},{byID[i].Texture_North.y.ToString()}", i.ToString());
                    Blocks_INI.Write("Texture_South", $"{byID[i].Texture_South.x.ToString()},{byID[i].Texture_South.y.ToString()}", i.ToString());
                    Blocks_INI.Write("Texture_East", $"{byID[i].Texture_East.x.ToString()},{byID[i].Texture_East.y.ToString()}", i.ToString());
                    Blocks_INI.Write("Texture_West", $"{byID[i].Texture_West.x.ToString()},{byID[i].Texture_West.y.ToString()}", i.ToString());
                }
                Blocks_INI.Write("Foliage", byID[i].Foliage.ToString(), i.ToString());
                Blocks_INI.Write("Uses_Physics", byID[i].UsePhysics.ToString(), i.ToString());
                Blocks_INI.Write("Physics_Time", byID[i].PhysicsTime.ToString(), i.ToString());
            }

            if (File.Exists(Application.dataPath + "/Mods/Blocks.png"))
            {
                byte[] bytes = File.ReadAllBytes(Application.dataPath + "/Mods/Blocks.png");
                Texture2D BlockTexture = new Texture2D(128, 128, TextureFormat.RGB24, false);
                BlockTexture.filterMode = FilterMode.Trilinear;
                BlockTexture.LoadImage(bytes);
            } else {
                Debug.Log("<color=red><b>COULD NOT FIND TEXTURES FILE!</b></color>, game will continue to run without texturing.\n"
                    + "Please create or drag Blocks.png file to path: <i>" + Application.dataPath + "/Mods/</i>.");
            }
        }
        NativeByID = new NativeArray<BlockTypes>(byID.Count, Allocator.Persistent);
        NativeByID.CopyFrom(byID.ToArray());

        
    }

    public static int GetIDFromName(string Name)
    {
        for (int i = 0; i < byID.Count; i++)
        {
            if (BlockNames[i] == Name)
            {
                return i;
            }
        }
        return 0;    
    }
    public static string GetNameFromID(int ID)
    {
        return BlockNames[ID];
    }
}

public static class MarchingCubesTables
{
    private static bool ConvertedToNative = false;

    public static NativeArray<int> 
        T_CubeEdgeFlags, 
        T_EdgeConnection, 
        T_TriangleConnectionTable, 
        T_VertexOffset;

    public static NativeArray<float> T_EdgeDirection;

    public static void ConvertToNative()
    {
        if (!ConvertedToNative)
        {
            T_CubeEdgeFlags = new NativeArray<int>(CubeEdgeFlags.Length, Allocator.Persistent);
            T_CubeEdgeFlags.CopyFrom(CubeEdgeFlags);

            T_EdgeConnection = new NativeArray<int>(EdgeConnection.Length, Allocator.Persistent);
            T_EdgeConnection.CopyFrom(EdgeConnection);

            T_EdgeDirection = new NativeArray<float>(EdgeDirection.Length, Allocator.Persistent);
            T_EdgeDirection.CopyFrom(EdgeDirection);

            T_TriangleConnectionTable = new NativeArray<int>(TriangleConnectionTable.Length, Allocator.Persistent);
            T_TriangleConnectionTable.CopyFrom(TriangleConnectionTable);

            T_VertexOffset = new NativeArray<int>(VertexOffset.Length, Allocator.Persistent);
            T_VertexOffset.CopyFrom(VertexOffset);
        }
        else
            Debug.LogWarning("Marching Cubes Tables are already converted to Native!");
    }

    /// <summary>
    /// VertexOffset lists the positions, relative to vertex0, 
    /// of each of the 8 vertices of a cube.
    /// vertexOffset[8][3]
    /// </summary>
    public static readonly int[] VertexOffset = new int[]
    {
            0, 0, 0,   1, 0, 0,     1, 1, 0,    0, 1, 0,
            0, 0, 1,   1, 0, 1,     1, 1, 1,    0, 1, 1
    };

    /// <summary>
    /// EdgeConnection lists the index of the endpoint vertices for each 
    /// of the 12 edges of the cube.
    /// edgeConnection[12][2]
    /// </summary>
    public static readonly int[] EdgeConnection = new int[]
    {
        0,1,    1,2,    2,3,    3,0,
        4,5,    5,6,    6,7,    7,4,
        0,4,    1,5,    2,6,    3,7
    };

    /// <summary>
    /// edgeDirection lists the direction vector (vertex1-vertex0) for each edge in the cube.
    /// edgeDirection[12][3]
    /// </summary>
    public static readonly float[] EdgeDirection = new float[]
    {
        1.0f, 0.0f, 0.0f,   0.0f, 1.0f, 0.0f,   -1.0f, 0.0f, 0.0f,    0.0f, -1.0f, 0.0f,
        1.0f, 0.0f, 0.0f,   0.0f, 1.0f, 0.0f,   -1.0f, 0.0f, 0.0f,    0.0f, -1.0f, 0.0f,
        0.0f, 0.0f, 1.0f,   0.0f, 0.0f, 1.0f,    0.0f, 0.0f, 1.0f,    0.0f,  0.0f, 1.0f
    };


    /// <summary>
    /// For any edge, if one vertex is inside of the surface and the other 
    /// is outside of the surface then the edge intersects the surface.
    /// For each of the 8 vertices of the cube can be two possible states,
    /// either inside or outside of the surface.
    /// For any cube the are 2^8=256 possible sets of vertex states.
    /// This table lists the edges intersected by the surface for all 256 
    /// possible vertex states. There are 12 edges.  
    /// For each entry in the table, if edge #n is intersected, then bit #n is set to 1.
    /// cubeEdgeFlags[256]
    /// </summary>
    public static readonly int[] CubeEdgeFlags = new int[]
    {
        0x000, 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c, 0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
        0x190, 0x099, 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c, 0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
        0x230, 0x339, 0x033, 0x13a, 0x636, 0x73f, 0x435, 0x53c, 0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
        0x3a0, 0x2a9, 0x1a3, 0x0aa, 0x7a6, 0x6af, 0x5a5, 0x4ac, 0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
        0x460, 0x569, 0x663, 0x76a, 0x066, 0x16f, 0x265, 0x36c, 0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
        0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0x0ff, 0x3f5, 0x2fc, 0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
        0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x055, 0x15c, 0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
        0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0x0cc, 0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
        0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc, 0x0cc, 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
        0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c, 0x15c, 0x055, 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
        0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc, 0x2fc, 0x3f5, 0x0ff, 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
        0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c, 0x36c, 0x265, 0x16f, 0x066, 0x76a, 0x663, 0x569, 0x460,
        0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac, 0x4ac, 0x5a5, 0x6af, 0x7a6, 0x0aa, 0x1a3, 0x2a9, 0x3a0,
        0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c, 0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x033, 0x339, 0x230,
        0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c, 0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x099, 0x190,
        0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c, 0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x000
    };


    /// <summary>
    /// For each of the possible vertex states listed in cubeEdgeFlags there is a specific triangulation
    /// of the edge intersection points.  triangleConnectionTable lists all of them in the form of
    /// 0-5 edge triples with the list terminated by the invalid value -1.
    /// For example: triangleConnectionTable[3] list the 2 triangles formed when corner[0] 
    /// and corner[1] are inside of the surface, but the rest of the cube is not.
    /// triangleConnectionTable[256][16]
    /// </summary>
    public static readonly int[] TriangleConnectionTable = new int[]
    {
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1,
        3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1,
        3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1,
        3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1,
        9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1,
        9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1,
        2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1,
        8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1,
        9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1,
        4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1,
        3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1,
        1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1,
        4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1,
        4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1,
        5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1,
        2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1,
        9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1,
        0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1,
        2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1,
        10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1,
        5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1,
        5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1,
        9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1,
        0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1,
        1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1,
        10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1,
        8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1,
        2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1,
        7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1,
        2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1,
        11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1,
        5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1,
        11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1,
        11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1,
        1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1,
        9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1,
        5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1,
        2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1,
        5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1,
        6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1,
        3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1,
        6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1,
        5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1,
        10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1,
        6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1,
        8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1,
        7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1,
        3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1,
        5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1,
        0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1,
        9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1,
        8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1,
        5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1,
        0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1,
        6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1,
        10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1,
        10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1,
        8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1,
        1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1,
        0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1,
        10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1,
        3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1,
        6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1,
        9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1,
        8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1,
        3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1,
        6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1,
        0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1,
        10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1,
        10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1,
        2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1,
        7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1,
        7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1,
        2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1,
        1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1,
        11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1,
        8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1,
        0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1,
        7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1,
        10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1,
        2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1,
        6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1,
        7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1,
        2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1,
        1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1,
        10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1,
        10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1,
        0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1,
        7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1,
        6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1,
        8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1,
        9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1,
        6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1,
        4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1,
        10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1,
        8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1,
        0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1,
        1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1,
        8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1,
        10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1,
        4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1,
        10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1,
        5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1,
        11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1,
        9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1,
        6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1,
        7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1,
        3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1,
        7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1,
        3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1,
        6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1,
        9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1,
        1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1,
        4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1,
        7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1,
        6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1,
        3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1,
        0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1,
        6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1,
        0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1,
        11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1,
        6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1,
        5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1,
        9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1,
        1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1,
        1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1,
        10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1,
        0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1,
        5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1,
        10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1,
        11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1,
        9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1,
        7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1,
        2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1,
        8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1,
        9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1,
        9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1,
        1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1,
        9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1,
        9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1,
        5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1,
        0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1,
        10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1,
        2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1,
        0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1,
        0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1,
        9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1,
        5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1,
        3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1,
        5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1,
        8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1,
        0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1,
        9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1,
        1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1,
        3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1,
        4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1,
        9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1,
        11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1,
        11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1,
        2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1,
        9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1,
        3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1,
        1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1,
        4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1,
        3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1,
        0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1,
        9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1,
        1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
    };
}
 
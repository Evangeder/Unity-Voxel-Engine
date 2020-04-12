using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Globalization;
using System;

namespace VoxaNovus
{
    public static class BlockConfigurationReader
    {
        public static int GetAddress(int x, int y, int z, int size = 16)
        {
            return (x + y * size + z * size * size);
        }

        public static void InitalizeClient()
        {
            BlockSettings.byID.Clear();
            BlockSettings.BlockTexture = new Texture2D(128, 128);
            if (!Directory.Exists(Application.dataPath + "/Mods"))
                Directory.CreateDirectory(Application.dataPath + "/Mods");

            IniFile Blocks_INI = new IniFile("/Mods/Blocks.ini");
            if (File.Exists(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File"))))
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File")));
                BlockSettings.BlockTexture = new Texture2D(int.Parse(Blocks_INI.Read("Texture_Width")), int.Parse(Blocks_INI.Read("Texture_Height")), TextureFormat.RGB24, false);
                BlockSettings.BlockTexture.filterMode = FilterMode.Point;
                BlockSettings.BlockTexture.LoadImage(bytes);
                BlockSettings.BlockTexture.Apply();
            }
            else
                Debug.LogError($"<color=red><b>COULD NOT FIND TEXTURES FILE!</b></color>, game will continue to run without texturing.\nPlease create or drag Blocks.png file to path: <i>{Application.dataPath}/Mods/</i>.");

            if (Blocks_INI.KeyExists("TileSize"))
                BlockSettings.BlockTileSize = float.Parse(Blocks_INI.Read("TileSize"), CultureInfo.InvariantCulture);
            if (Blocks_INI.KeyExists("TextureSize"))
                BlockSettings.TextureSize = float.Parse(Blocks_INI.Read("TextureSize"), CultureInfo.InvariantCulture);
        }

        public static IEnumerator InitializeSounds()
        {
            IniFile Blocks_INI = new IniFile("/Mods/Blocks.ini");
            for (int i = 0; i < BlockSettings.BlockNames.Length; i++)
            {
                BlockSettings.BlockSounds[i] = new List<AudioClip>();
                if (Blocks_INI.KeyExists("Sounds", i.ToString()))
                {
                    int sounds = int.Parse(Blocks_INI.Read("Sounds", i.ToString()));
                    for (int i2 = 0; i2 < sounds; i2++)
                    {
                        string path = $"{Application.dataPath}/Mods/{Blocks_INI.Read($"Sound{i2}", i.ToString()).Replace('\\', '/')}";
                        using (var download = new WWW(path))
                        {
                            yield return download;
                            var clip = download.GetAudioClip();
                            if (clip != null)
                                BlockSettings.BlockSounds[i].Add(clip);
                        }
                    }
                }
            }
            BlockSettings.SoundsLoaded = true;
        }

        public static void InitializeBlocks()
        {
            BlockSettings.byID.Clear();

            BlockSettings.BlockTexture = new Texture2D(128, 128);

            if (!Directory.Exists(Application.dataPath + "/Mods"))
                Directory.CreateDirectory(Application.dataPath + "/Mods");


            IniFile Blocks_INI = new IniFile("/Mods/Blocks.ini");
            if (Blocks_INI.KeyExists("TileSize"))
            {
                // Data exists, parse.
                var culture = CultureInfo.InvariantCulture;
                //culture.NumberFormat.NumberDecimalSeparator = ".";
                BlockSettings.BlockTileSize = float.Parse(Blocks_INI.Read("TileSize"), culture);

                if (File.Exists(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File"))))
                {
                    byte[] bytes = File.ReadAllBytes(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File")));
                    BlockSettings.BlockTexture = new Texture2D(int.Parse(Blocks_INI.Read("Texture_Width")), int.Parse(Blocks_INI.Read("Texture_Height")), TextureFormat.RGB24, false);
                    BlockSettings.BlockTexture.filterMode = FilterMode.Point;
                    BlockSettings.BlockTexture.LoadImage(bytes);
                }
                else
                {
                    Debug.LogError($"<color=red><b>COULD NOT FIND TEXTURES FILE!</b></color>, game will continue to run without texturing.\nPlease create or drag Blocks.png file to path: <i>{Application.dataPath}/Mods/</i>.");
                }

                if (Blocks_INI.KeyExists("WorldGen"))
                    BlockSettings.worldGen = int.Parse(Blocks_INI.Read("WorldGen"));
                else
                    BlockSettings.worldGen = 0;

                if (Blocks_INI.KeyExists("TextureSize"))
                    BlockSettings.TextureSize = float.Parse(Blocks_INI.Read("TextureSize"), culture);

                if (Blocks_INI.KeyExists("ChunkScale"))
                    BlockSettings.ChunkScale = float.Parse(Blocks_INI.Read("ChunkScale"), culture);

                int MaxBlocks = int.Parse(Blocks_INI.Read("Blocktypes"));
                BlockSettings.BlockSounds = new List<AudioClip>[MaxBlocks];
                BlockSettings.BlockNames = new string[MaxBlocks];
                BlockSettings.PhysicsFunctions = new BlockPhysics[MaxBlocks];
                BlockSettings.PhysicsBound = new bool[MaxBlocks];

                for (int i = 0; i < MaxBlocks; i++)
                {
                    BlockSettings.BlockNames[i] = Blocks_INI.Read("Name", i.ToString());

                    bool liquid = false;
                    if (Blocks_INI.KeyExists("Liquid", i.ToString()))
                        liquid = bool.Parse(Blocks_INI.Read("Liquid", i.ToString()));

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

                        if (Blocks_INI.KeyExists("Texture_Marched", i.ToString()))
                        {
                            tempstr2 = Blocks_INI.Read("Texture_Marched", i.ToString()).Split(',');
                            tex[6] = new int2(new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1])));
                        }
                        else
                            tex[6] = tex[0];

                        tempstr2 = Blocks_INI.Read("Texture_West", i.ToString()).Split(',');
                        tex[5] = new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1]));

                        BlockPhysics bp = null;
                        if (Blocks_INI.KeyExists("Physics_Function", i.ToString()))
                        {
                            string PF = Blocks_INI.Read("Physics_Function", i.ToString());
                            Type type = Type.GetType(PF);
                            if (type == null)
                                Debug.Log($"Can't bind Physics Function -> {PF}");
                            else
                            {
                                bp = (BlockPhysics)Activator.CreateInstance(type);
                                bp.Init(BlockSettings.world);
                                BlockSettings.PhysicsBound[i] = true;
                                Debug.Log($"Bound {PF} to blockid {i} ({BlockSettings.BlockNames[i]})");
                            }
                        }
                        BlockSettings.PhysicsFunctions[i] = bp;

                        BlockSettings.byID.Add(new BlockData(
                        bool.Parse(Blocks_INI.Read("Solid", i.ToString())),
                        tex,
                        bool.Parse(Blocks_INI.Read("Uses_Physics", i.ToString())),
                        float.Parse(Blocks_INI.Read("Physics_Time", i.ToString()), culture),
                        int.Parse(Blocks_INI.Read("CullingMode", i.ToString())),
                        bool.Parse(Blocks_INI.Read("Foliage", i.ToString())),
                        sbyte.Parse(Blocks_INI.Read("MarchingLayer", i.ToString())),
                        liquid));
                    }
                    else
                    {
                        BlockPhysics bp = null;
                        if (Blocks_INI.KeyExists("Physics_Function", i.ToString()))
                        {
                            string PF = Blocks_INI.Read("Physics_Function", i.ToString());
                            Type type = Type.GetType(PF);
                            if (type == null)
                                Debug.Log($"Can't bind Physics Function -> {PF}");
                            else
                            {
                                bp = (BlockPhysics)Activator.CreateInstance(type);
                                bp.Init(BlockSettings.world);
                                BlockSettings.PhysicsBound[i] = true;
                                Debug.Log($"Bound {PF} to blockid {i} ({BlockSettings.BlockNames[i]})");
                            }
                        }
                        BlockSettings.PhysicsFunctions[i] = bp;

                        string[] tempstr2 = Blocks_INI.Read("Texture", i.ToString()).Split(',');
                        BlockSettings.byID.Add(new BlockData(
                            bool.Parse(Blocks_INI.Read("Solid", i.ToString())),
                            new int2(int.Parse(tempstr2[0]), int.Parse(tempstr2[1])),
                            bool.Parse(Blocks_INI.Read("Uses_Physics", i.ToString())),
                            float.Parse(Blocks_INI.Read("Physics_Time", i.ToString()), culture),
                            int.Parse(Blocks_INI.Read("CullingMode", i.ToString())),
                            bool.Parse(Blocks_INI.Read("Foliage", i.ToString())),
                            sbyte.Parse(Blocks_INI.Read("MarchingLayer", i.ToString())),
                            liquid));
                    }
                    sbyte CurrentBlockLayer = sbyte.Parse(Blocks_INI.Read("MarchingLayer", i.ToString()));
                    if (BlockSettings.MarchingLayers < CurrentBlockLayer) BlockSettings.MarchingLayers = CurrentBlockLayer;
                }
            }
            else
            {
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

                BlockSettings.byID.Add(new BlockData(false));                                        //       AIR
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       STONE
                BlockSettings.byID.Add(new BlockData(true, textures));                               //       GRASS
                BlockSettings.byID.Add(new BlockData(true, new int2(1, 0)));                         //       DIRT
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       COBBLESTONE
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       PLANKS
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       SAPPLING
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       BEDROCK
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       FLOWING WATER
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       STATIONARY WATER
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       FLOWING LAVA
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       STATIONARY LAVA
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       SAND
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       GRAVEL
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       GOLD ORE
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       IRON ORE
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       COAL ORE
                textures = new List<int2>() { new int2(2, 1), new int2(2, 1), new int2(1, 1), new int2(1, 1), new int2(1, 1), new int2(1, 1), new int2(1, 1) };
                BlockSettings.byID.Add(new BlockData(true, textures));                               //       WOOD (LOG)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0), false, 0f, 1, false));    //       LEAVES
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       SPONGE
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0), false, 0f, 2, false));    //       GLASS
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (RED)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (ORANGE)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (YELLOW)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (CHARTREUSE)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (GREEN)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (SPRING GREEN)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (CYAN)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (CAPRI)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (ULTRAMARINE)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (VIOLET)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (PURPLE)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (MAGNETA)
                BlockSettings.byID.Add(new BlockData(true, new int2(0, 0)));                         //       CLOTH (ROSE)

                Blocks_INI.Write("Blocktypes", (BlockSettings.byID.Count - 1).ToString());

                for (int i = 0; i < BlockSettings.byID.Count; i++)
                {
                    Blocks_INI.Write("Solid", BlockSettings.byID[i].Solid + "", i + "");
                    Blocks_INI.Write("CullingMode", (BlockSettings.byID[i].CullingMode).ToString(), i.ToString());
                    if ((BlockSettings.byID[i].Texture_East.x == BlockSettings.byID[i].Texture_Up.x && BlockSettings.byID[i].Texture_East.y == BlockSettings.byID[i].Texture_Up.y) &&
                            (BlockSettings.byID[i].Texture_West.x == BlockSettings.byID[i].Texture_Up.x && BlockSettings.byID[i].Texture_West.y == BlockSettings.byID[i].Texture_Up.y) &&
                            (BlockSettings.byID[i].Texture_North.x == BlockSettings.byID[i].Texture_Up.x && BlockSettings.byID[i].Texture_North.y == BlockSettings.byID[i].Texture_Up.y) &&
                            (BlockSettings.byID[i].Texture_South.x == BlockSettings.byID[i].Texture_Up.x && BlockSettings.byID[i].Texture_South.y == BlockSettings.byID[i].Texture_Up.y) &&
                            (BlockSettings.byID[i].Texture_Down.x == BlockSettings.byID[i].Texture_Up.x && BlockSettings.byID[i].Texture_Down.y == BlockSettings.byID[i].Texture_Up.y))
                    {
                        Blocks_INI.Write("Texture", $"{(BlockSettings.byID[i].Texture_Up.x).ToString()},{(BlockSettings.byID[i].Texture_Up.y).ToString()}", i.ToString());
                    }
                    else
                    {
                        Blocks_INI.Write("Texture_Up", $"{BlockSettings.byID[i].Texture_Up.x.ToString()},{BlockSettings.byID[i].Texture_Up.y.ToString()}", i.ToString());
                        Blocks_INI.Write("Texture_Down", $"{BlockSettings.byID[i].Texture_Down.x.ToString()},{BlockSettings.byID[i].Texture_Down.y.ToString()}", i.ToString());
                        Blocks_INI.Write("Texture_North", $"{BlockSettings.byID[i].Texture_North.x.ToString()},{BlockSettings.byID[i].Texture_North.y.ToString()}", i.ToString());
                        Blocks_INI.Write("Texture_South", $"{BlockSettings.byID[i].Texture_South.x.ToString()},{BlockSettings.byID[i].Texture_South.y.ToString()}", i.ToString());
                        Blocks_INI.Write("Texture_East", $"{BlockSettings.byID[i].Texture_East.x.ToString()},{BlockSettings.byID[i].Texture_East.y.ToString()}", i.ToString());
                        Blocks_INI.Write("Texture_West", $"{BlockSettings.byID[i].Texture_West.x.ToString()},{BlockSettings.byID[i].Texture_West.y.ToString()}", i.ToString());
                    }
                    Blocks_INI.Write("Foliage", BlockSettings.byID[i].Foliage.ToString(), i.ToString());
                    Blocks_INI.Write("Uses_Physics", BlockSettings.byID[i].UsePhysics.ToString(), i.ToString());
                    Blocks_INI.Write("Physics_Time", BlockSettings.byID[i].PhysicsTime.ToString(), i.ToString());
                }

                if (File.Exists(Application.dataPath + "/Mods/Blocks.png"))
                {
                    byte[] bytes = File.ReadAllBytes(Application.dataPath + "/Mods/Blocks.png");
                    Texture2D BlockTexture = new Texture2D(128, 128, TextureFormat.RGB24, false);
                    BlockTexture.filterMode = FilterMode.Trilinear;
                    BlockTexture.LoadImage(bytes);
                }
                else
                {
                    Debug.Log("<color=red><b>COULD NOT FIND TEXTURES FILE!</b></color>, game will continue to run without texturing.\n"
                        + "Please create or drag Blocks.png file to path: <i>" + Application.dataPath + "/Mods/</i>.");
                }
            }
            BlockSettings.NativeByID = new NativeArray<BlockData>(BlockSettings.byID.Count, Allocator.Persistent);
            BlockSettings.NativeByID.CopyFrom(BlockSettings.byID.ToArray());
        }

        public static int GetIDFromName(string Name)
        {
            for (int i = 0; i < BlockSettings.byID.Count; i++)
            {
                if (BlockSettings.BlockNames[i] == Name)
                {
                    return i;
                }
            }
            return 0;
        }
        public static string GetNameFromID(int ID)
        {
            return BlockSettings.BlockNames[ID];
        }
    }
}
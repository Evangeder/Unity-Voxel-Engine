using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Globalization;
using System;

public static partial class BlockData
{
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
        }
        else
            Debug.LogError($"<color=red><b>COULD NOT FIND TEXTURES FILE!</b></color>, game will continue to run without texturing.\nPlease create or drag Blocks.png file to path: <i>{Application.dataPath}/Mods/</i>.");

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
                    string path = $"{Application.dataPath}/Mods/{Blocks_INI.Read($"Sound{i2}", i.ToString()).Replace('\\', '/')}";
                    using (var download = new WWW(path))
                    {
                        yield return download;
                        var clip = download.GetAudioClip();
                        if (clip != null)
                            BlockSounds[i].Add(clip);
                    }
                }
            }
        }
        SoundsLoaded = true;
    }

    public static void InitializeBlocks()
    {
        byID.Clear();

        BlockTexture = new Texture2D(128, 128);

        if (!Directory.Exists(Application.dataPath + "/Mods"))
            Directory.CreateDirectory(Application.dataPath + "/Mods");


        IniFile Blocks_INI = new IniFile("/Mods/Blocks.ini");
        if (Blocks_INI.KeyExists("TileSize"))
        {
            // Data exists, parse.
            var culture = CultureInfo.InvariantCulture;
            //culture.NumberFormat.NumberDecimalSeparator = ".";
            BlockTileSize = float.Parse(Blocks_INI.Read("TileSize"), culture);

            if (File.Exists(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File"))))
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(Application.dataPath, "Mods", Blocks_INI.Read("Texture_File")));
                BlockTexture = new Texture2D(int.Parse(Blocks_INI.Read("Texture_Width")), int.Parse(Blocks_INI.Read("Texture_Height")), TextureFormat.RGB24, false);
                BlockTexture.filterMode = FilterMode.Point;
                BlockTexture.LoadImage(bytes);
            }
            else
            {
                Debug.LogError($"<color=red><b>COULD NOT FIND TEXTURES FILE!</b></color>, game will continue to run without texturing.\nPlease create or drag Blocks.png file to path: <i>{Application.dataPath}/Mods/</i>.");
            }

            if (Blocks_INI.KeyExists("TextureSize"))
            {
                TextureSize = float.Parse(Blocks_INI.Read("TextureSize"), culture);
            }

            int MaxBlocks = int.Parse(Blocks_INI.Read("Blocktypes"));
            BlockSounds = new List<AudioClip>[MaxBlocks];
            BlockNames = new string[MaxBlocks];
            PhysicsFunctions = new BlockPhysics[MaxBlocks];
            PhysicsBound = new bool[MaxBlocks];

            for (int i = 0; i < MaxBlocks; i++)
            {
                BlockNames[i] = Blocks_INI.Read("Name", i.ToString());

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
                        if(type == null)
                            Debug.Log($"Can't bind Physics Function -> {PF}");
                        else
                        {
                            bp = (BlockPhysics)Activator.CreateInstance(type);
                            bp.Init(world);
                            PhysicsBound[i] = true;
                            Debug.Log($"Bound {PF} to blockid {i} ({BlockNames[i]})");
                        }
                    }
                    PhysicsFunctions[i] = bp;

                    byID.Add(new BlockTypes(
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
                            bp.Init(world);
                            PhysicsBound[i] = true;
                            Debug.Log($"Bound {PF} to blockid {i} ({BlockNames[i]})");
                        }
                    }
                    PhysicsFunctions[i] = bp;

                    string[] tempstr2 = Blocks_INI.Read("Texture", i.ToString()).Split(',');
                    byID.Add(new BlockTypes(
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
                if (MarchingLayers < CurrentBlockLayer) MarchingLayers = CurrentBlockLayer;
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
                else
                {
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
            }
            else
            {
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

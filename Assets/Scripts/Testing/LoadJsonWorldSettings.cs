using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using VoxaNovus;

public class LoadJsonWorldSettings : MonoBehaviour
{
    void OnDestroy()
    {
        MarchingCubesTables.DisposeTables();
    }

    void Awake()
    {
        // Make sure that loaded data stays intact when player changes scene
        DontDestroyOnLoad(gameObject);

        if (!Directory.Exists(Path.Combine(Application.dataPath, "AssetPacks")))
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "AssetPacks"));

        if (!File.Exists(Path.Combine(Application.dataPath, "AssetPacks", "Asset.json")))
        {
            // Create default pack
            RawBlockData[] blocklist = new RawBlockData[]
            {
                new RawBlockData()
                {
                    ID = 1,
                    Name = "Stone",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(1, 15),
                        Down =      new TexturePosition(1, 15),
                        North =     new TexturePosition(1, 15),
                        South =     new TexturePosition(1, 15),
                        East =      new TexturePosition(1, 15),
                        West =      new TexturePosition(1, 15),
                        Marched =   new TexturePosition(1, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 2,
                    Name = "Grass",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(0, 15),
                        Down =      new TexturePosition(2, 15),
                        North =     new TexturePosition(3, 15),
                        South =     new TexturePosition(3, 15),
                        East =      new TexturePosition(3, 15),
                        West =      new TexturePosition(3, 15),
                        Marched =   new TexturePosition(0, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 3,
                    Name = "Dirt",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(2, 15),
                        Down =      new TexturePosition(2, 15),
                        North =     new TexturePosition(2, 15),
                        South =     new TexturePosition(2, 15),
                        East =      new TexturePosition(2, 15),
                        West =      new TexturePosition(2, 15),
                        Marched =   new TexturePosition(2, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 4,
                    Name = "Cobblestone",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(0, 14),
                        Down =      new TexturePosition(0, 14),
                        North =     new TexturePosition(0, 14),
                        South =     new TexturePosition(0, 14),
                        East =      new TexturePosition(0, 14),
                        West =      new TexturePosition(0, 14),
                        Marched =   new TexturePosition(0, 14)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 5,
                    Name = "Planks",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(4, 15),
                        Down =      new TexturePosition(4, 15),
                        North =     new TexturePosition(4, 15),
                        South =     new TexturePosition(4, 15),
                        East =      new TexturePosition(4, 15),
                        West =      new TexturePosition(4, 15),
                        Marched =   new TexturePosition(4, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactWood_medium_000.ogg",
                        "Kenney/impactWood_medium_001.ogg",
                        "Kenney/impactWood_medium_002.ogg",
                        "Kenney/impactWood_medium_003.ogg",
                        "Kenney/impactWood_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 6,
                    Name = "Bedrock",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(1, 14),
                        Down =      new TexturePosition(1, 14),
                        North =     new TexturePosition(1, 14),
                        South =     new TexturePosition(1, 14),
                        East =      new TexturePosition(1, 14),
                        West =      new TexturePosition(1, 14),
                        Marched =   new TexturePosition(1, 14)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 7,
                    Name = "Sand",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(2, 14),
                        Down =      new TexturePosition(2, 14),
                        North =     new TexturePosition(2, 14),
                        South =     new TexturePosition(2, 14),
                        East =      new TexturePosition(2, 14),
                        West =      new TexturePosition(2, 14),
                        Marched =   new TexturePosition(2, 14)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    Physics = new RawPhysicsData
                    {
                        UsePhysics = true,
                        PhysicsTime = 0.5f,
                        PhysicsFunction = "SandfallTest"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 8,
                    Name = "Gravel",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(3, 14),
                        Down =      new TexturePosition(3, 14),
                        North =     new TexturePosition(3, 14),
                        South =     new TexturePosition(3, 14),
                        East =      new TexturePosition(3, 14),
                        West =      new TexturePosition(3, 14),
                        Marched =   new TexturePosition(3, 14)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 9,
                    Name = "Gold Ore",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(0, 13),
                        Down =      new TexturePosition(0, 13),
                        North =     new TexturePosition(0, 13),
                        South =     new TexturePosition(0, 13),
                        East =      new TexturePosition(0, 13),
                        West =      new TexturePosition(0, 13),
                        Marched =   new TexturePosition(0, 13)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 10,
                    Name = "Iron Ore",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(1, 13),
                        Down =      new TexturePosition(1, 13),
                        North =     new TexturePosition(1, 13),
                        South =     new TexturePosition(1, 13),
                        East =      new TexturePosition(1, 13),
                        West =      new TexturePosition(1, 13),
                        Marched =   new TexturePosition(1, 13)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 11,
                    Name = "Coal Ore",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(2, 13),
                        Down =      new TexturePosition(2, 13),
                        North =     new TexturePosition(2, 13),
                        South =     new TexturePosition(2, 13),
                        East =      new TexturePosition(2, 13),
                        West =      new TexturePosition(2, 13),
                        Marched =   new TexturePosition(2, 13)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 12,
                    Name = "Log",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(5, 14),
                        Down =      new TexturePosition(5, 14),
                        North =     new TexturePosition(4, 14),
                        South =     new TexturePosition(4, 14),
                        East =      new TexturePosition(4, 14),
                        West =      new TexturePosition(4, 14),
                        Marched =   new TexturePosition(4, 14)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactWood_medium_000.ogg",
                        "Kenney/impactWood_medium_001.ogg",
                        "Kenney/impactWood_medium_002.ogg",
                        "Kenney/impactWood_medium_003.ogg",
                        "Kenney/impactWood_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 13,
                    Name = "Leaves",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(6, 14),
                        Down =      new TexturePosition(6, 14),
                        North =     new TexturePosition(6, 14),
                        South =     new TexturePosition(6, 14),
                        East =      new TexturePosition(6, 14),
                        West =      new TexturePosition(6, 14),
                        Marched =   new TexturePosition(6, 14)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 1,
                    MarchingCubesLayer = 1,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 14,
                    Name = "Sponge",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(0, 12),
                        Down =      new TexturePosition(0, 12),
                        North =     new TexturePosition(0, 12),
                        South =     new TexturePosition(0, 12),
                        East =      new TexturePosition(0, 12),
                        West =      new TexturePosition(0, 12),
                        Marched =   new TexturePosition(0, 12)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 15,
                    Name = "Glass",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(1, 12),
                        Down =      new TexturePosition(1, 12),
                        North =     new TexturePosition(1, 12),
                        South =     new TexturePosition(1, 12),
                        East =      new TexturePosition(1, 12),
                        West =      new TexturePosition(1, 12),
                        Marched =   new TexturePosition(1, 12)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactGlass_medium_000.ogg",
                        "Kenney/impactGlass_medium_001.ogg",
                        "Kenney/impactGlass_medium_002.ogg",
                        "Kenney/impactGlass_medium_003.ogg",
                        "Kenney/impactGlass_medium_004.ogg"
                    },
                    CullingMode = 2,
                    MarchingCubesLayer = 2,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 16,
                    Name = "Red Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(0, 11),
                        Down =      new TexturePosition(0, 11),
                        North =     new TexturePosition(0, 11),
                        South =     new TexturePosition(0, 11),
                        East =      new TexturePosition(0, 11),
                        West =      new TexturePosition(0, 11),
                        Marched =   new TexturePosition(0, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 17,
                    Name = "Orange Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(1, 11),
                        Down =      new TexturePosition(1, 11),
                        North =     new TexturePosition(1, 11),
                        South =     new TexturePosition(1, 11),
                        East =      new TexturePosition(1, 11),
                        West =      new TexturePosition(1, 11),
                        Marched =   new TexturePosition(1, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 18,
                    Name = "Yellow Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(2, 11),
                        Down =      new TexturePosition(2, 11),
                        North =     new TexturePosition(2, 11),
                        South =     new TexturePosition(2, 11),
                        East =      new TexturePosition(2, 11),
                        West =      new TexturePosition(2, 11),
                        Marched =   new TexturePosition(2, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 19,
                    Name = "Chartreuse Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(3, 11),
                        Down =      new TexturePosition(3, 11),
                        North =     new TexturePosition(3, 11),
                        South =     new TexturePosition(3, 11),
                        East =      new TexturePosition(3, 11),
                        West =      new TexturePosition(3, 11),
                        Marched =   new TexturePosition(3, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 20,
                    Name = "Green Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(4, 11),
                        Down =      new TexturePosition(4, 11),
                        North =     new TexturePosition(4, 11),
                        South =     new TexturePosition(4, 11),
                        East =      new TexturePosition(4, 11),
                        West =      new TexturePosition(4, 11),
                        Marched =   new TexturePosition(4, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 21,
                    Name = "Spring Green Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(5, 11),
                        Down =      new TexturePosition(5, 11),
                        North =     new TexturePosition(5, 11),
                        South =     new TexturePosition(5, 11),
                        East =      new TexturePosition(5, 11),
                        West =      new TexturePosition(5, 11),
                        Marched =   new TexturePosition(5, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 22,
                    Name = "Cyan Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(6, 11),
                        Down =      new TexturePosition(6, 11),
                        North =     new TexturePosition(6, 11),
                        South =     new TexturePosition(6, 11),
                        East =      new TexturePosition(6, 11),
                        West =      new TexturePosition(6, 11),
                        Marched =   new TexturePosition(6, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 23,
                    Name = "Capri Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(7, 11),
                        Down =      new TexturePosition(7, 11),
                        North =     new TexturePosition(7, 11),
                        South =     new TexturePosition(7, 11),
                        East =      new TexturePosition(7, 11),
                        West =      new TexturePosition(7, 11),
                        Marched =   new TexturePosition(7, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 24,
                    Name = "Ultramarine Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(8, 11),
                        Down =      new TexturePosition(8, 11),
                        North =     new TexturePosition(8, 11),
                        South =     new TexturePosition(8, 11),
                        East =      new TexturePosition(8, 11),
                        West =      new TexturePosition(8, 11),
                        Marched =   new TexturePosition(8, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 25,
                    Name = "Violet Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(9, 11),
                        Down =      new TexturePosition(9, 11),
                        North =     new TexturePosition(9, 11),
                        South =     new TexturePosition(9, 11),
                        East =      new TexturePosition(9, 11),
                        West =      new TexturePosition(9, 11),
                        Marched =   new TexturePosition(9, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 26,
                    Name = "Purple Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(10, 11),
                        Down =      new TexturePosition(10, 11),
                        North =     new TexturePosition(10, 11),
                        South =     new TexturePosition(10, 11),
                        East =      new TexturePosition(10, 11),
                        West =      new TexturePosition(10, 11),
                        Marched =   new TexturePosition(10, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 27,
                    Name = "Magneta Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(11, 11),
                        Down =      new TexturePosition(11, 11),
                        North =     new TexturePosition(11, 11),
                        South =     new TexturePosition(11, 11),
                        East =      new TexturePosition(11, 11),
                        West =      new TexturePosition(11, 11),
                        Marched =   new TexturePosition(11, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 28,
                    Name = "Rose Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(12, 11),
                        Down =      new TexturePosition(12, 11),
                        North =     new TexturePosition(12, 11),
                        South =     new TexturePosition(12, 11),
                        East =      new TexturePosition(12, 11),
                        West =      new TexturePosition(12, 11),
                        Marched =   new TexturePosition(12, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 29,
                    Name = "Dark Grey Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(13, 11),
                        Down =      new TexturePosition(13, 11),
                        North =     new TexturePosition(13, 11),
                        South =     new TexturePosition(13, 11),
                        East =      new TexturePosition(13, 11),
                        West =      new TexturePosition(13, 11),
                        Marched =   new TexturePosition(13, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 30,
                    Name = "Light Gray Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(14, 11),
                        Down =      new TexturePosition(14, 11),
                        North =     new TexturePosition(14, 11),
                        South =     new TexturePosition(14, 11),
                        East =      new TexturePosition(14, 11),
                        West =      new TexturePosition(14, 11),
                        Marched =   new TexturePosition(14, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 31,
                    Name = "White Wool",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(15, 11),
                        Down =      new TexturePosition(15, 11),
                        North =     new TexturePosition(15, 11),
                        South =     new TexturePosition(15, 11),
                        East =      new TexturePosition(15, 11),
                        West =      new TexturePosition(15, 11),
                        Marched =   new TexturePosition(15, 11)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactPlank_medium_000.ogg",
                        "Kenney/impactPlank_medium_001.ogg",
                        "Kenney/impactPlank_medium_002.ogg",
                        "Kenney/impactPlank_medium_003.ogg",
                        "Kenney/impactPlank_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 32,
                    Name = "Gold",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(8, 14),
                        Down =      new TexturePosition(8, 12),
                        North =     new TexturePosition(8, 13),
                        South =     new TexturePosition(8, 13),
                        East =      new TexturePosition(8, 13),
                        West =      new TexturePosition(8, 13),
                        Marched =   new TexturePosition(8, 13)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactMetal_heavy_000.ogg",
                        "Kenney/impactMetal_heavy_001.ogg",
                        "Kenney/impactMetal_heavy_002.ogg",
                        "Kenney/impactMetal_heavy_003.ogg",
                        "Kenney/impactMetal_heavy_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 33,
                    Name = "Iron",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(7, 14),
                        Down =      new TexturePosition(7, 12),
                        North =     new TexturePosition(7, 13),
                        South =     new TexturePosition(7, 13),
                        East =      new TexturePosition(7, 13),
                        West =      new TexturePosition(7, 13),
                        Marched =   new TexturePosition(7, 13)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactMetal_heavy_000.ogg",
                        "Kenney/impactMetal_heavy_001.ogg",
                        "Kenney/impactMetal_heavy_002.ogg",
                        "Kenney/impactMetal_heavy_003.ogg",
                        "Kenney/impactMetal_heavy_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 34,
                    Name = "Slab",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(6, 15),
                        Down =      new TexturePosition(6, 15),
                        North =     new TexturePosition(5, 15),
                        South =     new TexturePosition(5, 15),
                        East =      new TexturePosition(5, 15),
                        West =      new TexturePosition(5, 15),
                        Marched =   new TexturePosition(5, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 35,
                    Name = "Bricks",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(7, 15),
                        Down =      new TexturePosition(7, 15),
                        North =     new TexturePosition(7, 15),
                        South =     new TexturePosition(7, 15),
                        East =      new TexturePosition(7, 15),
                        West =      new TexturePosition(7, 15),
                        Marched =   new TexturePosition(7, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 36,
                    Name = "TNT",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(9, 15),
                        Down =      new TexturePosition(10, 15),
                        North =     new TexturePosition(8, 15),
                        South =     new TexturePosition(8, 15),
                        East =      new TexturePosition(8, 15),
                        West =      new TexturePosition(8, 15),
                        Marched =   new TexturePosition(8, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 37,
                    Name = "Bookshelf",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(4, 15),
                        Down =      new TexturePosition(4, 15),
                        North =     new TexturePosition(3, 13),
                        South =     new TexturePosition(3, 13),
                        East =      new TexturePosition(3, 13),
                        West =      new TexturePosition(3, 13),
                        Marched =   new TexturePosition(3, 13)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/impactWood_medium_000.ogg",
                        "Kenney/impactWood_medium_001.ogg",
                        "Kenney/impactWood_medium_002.ogg",
                        "Kenney/impactWood_medium_003.ogg",
                        "Kenney/impactWood_medium_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 38,
                    Name = "Mossy Cobblestone",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(4, 13),
                        Down =      new TexturePosition(4, 13),
                        North =     new TexturePosition(4, 13),
                        South =     new TexturePosition(4, 13),
                        East =      new TexturePosition(4, 13),
                        West =      new TexturePosition(4, 13),
                        Marched =   new TexturePosition(4, 13)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 39,
                    Name = "Obsidian",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(5, 13),
                        Down =      new TexturePosition(5, 13),
                        North =     new TexturePosition(5, 13),
                        South =     new TexturePosition(5, 13),
                        East =      new TexturePosition(5, 13),
                        West =      new TexturePosition(5, 13),
                        Marched =   new TexturePosition(5, 13)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_carpet_000.ogg",
                        "Kenney/footstep_carpet_001.ogg",
                        "Kenney/footstep_carpet_002.ogg",
                        "Kenney/footstep_carpet_003.ogg",
                        "Kenney/footstep_carpet_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 40,
                    Name = "Flower",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(13, 15),
                        Down =      new TexturePosition(13, 15),
                        North =     new TexturePosition(13, 15),
                        South =     new TexturePosition(13, 15),
                        East =      new TexturePosition(13, 15),
                        West =      new TexturePosition(13, 15),
                        Marched =   new TexturePosition(13, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 1,
                    MarchingCubesLayer = -1,
                    Solid = true,
                    Foliage = true,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 41,
                    Name = "Rose",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(12, 15),
                        Down =      new TexturePosition(12, 15),
                        North =     new TexturePosition(12, 15),
                        South =     new TexturePosition(12, 15),
                        East =      new TexturePosition(12, 15),
                        West =      new TexturePosition(12, 15),
                        Marched =   new TexturePosition(12, 15)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 1,
                    MarchingCubesLayer = -1,
                    Solid = true,
                    Foliage = true,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 42,
                    Name = "Brown Mushroom",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(13, 14),
                        Down =      new TexturePosition(13, 14),
                        North =     new TexturePosition(13, 14),
                        South =     new TexturePosition(13, 14),
                        East =      new TexturePosition(13, 14),
                        West =      new TexturePosition(13, 14),
                        Marched =   new TexturePosition(13, 14)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 1,
                    MarchingCubesLayer = -1,
                    Solid = true,
                    Foliage = true,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 43,
                    Name = "Red Mushroom",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(12, 14),
                        Down =      new TexturePosition(12, 14),
                        North =     new TexturePosition(12, 14),
                        South =     new TexturePosition(12, 14),
                        East =      new TexturePosition(12, 14),
                        West =      new TexturePosition(12, 14),
                        Marched =   new TexturePosition(12, 14)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 1,
                    MarchingCubesLayer = -1,
                    Solid = true,
                    Foliage = true,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 44,
                    Name = "Tall Grass",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(2, 12),
                        Down =      new TexturePosition(2, 12),
                        North =     new TexturePosition(2, 12),
                        South =     new TexturePosition(2, 12),
                        East =      new TexturePosition(2, 12),
                        West =      new TexturePosition(2, 12),
                        Marched =   new TexturePosition(2, 12)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 1,
                    MarchingCubesLayer = -1,
                    Solid = true,
                    Foliage = true,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 45,
                    Name = "Test Block",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(0, 10),
                        Down =      new TexturePosition(1, 10),
                        North =     new TexturePosition(4, 10),
                        South =     new TexturePosition(5, 10),
                        East =      new TexturePosition(2, 10),
                        West =      new TexturePosition(3, 10),
                        Marched =   new TexturePosition(0, 10)
                    },
                    Sounds = new string[]
                    {
                        "Kenney/footstep_grass_000.ogg",
                        "Kenney/footstep_grass_001.ogg",
                        "Kenney/footstep_grass_002.ogg",
                        "Kenney/footstep_grass_003.ogg",
                        "Kenney/footstep_grass_004.ogg"
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Solid = true,
                    Foliage = false,
                    Liquid = false
                },

                new RawBlockData()
                {
                    ID = 46,
                    Name = "Flood Water",
                    Namespace = "Default",
                    Textures = new RawTextureInfo()
                    {
                        Up =        new TexturePosition(0, 10),
                        Down =      new TexturePosition(1, 10),
                        North =     new TexturePosition(4, 10),
                        South =     new TexturePosition(5, 10),
                        East =      new TexturePosition(2, 10),
                        West =      new TexturePosition(3, 10),
                        Marched =   new TexturePosition(0, 10)
                    },
                    CullingMode = 0,
                    MarchingCubesLayer = 0,
                    Physics = new RawPhysicsData
                    {
                        UsePhysics = true,
                        PhysicsTime = 1f,
                        PhysicsFunction = "WaterFloodFill"
                    },
                    Solid = true,
                    Foliage = false,
                    Liquid = true
                },
            };

            RawAssetPackData rawAssetPackData = new RawAssetPackData()
            {
                AssetName = "Default",
                TextureFile = "Kenney.png",
                TextureDimension = new TextureRect(2048, 2048),
                TextureSize = 1f,
                TileSize = 0.0625f,
                rawBlockData = blocklist
            };

            using (StreamWriter sw = new StreamWriter(Path.Combine(Application.dataPath, "AssetPacks", "Asset.json")))
            {
                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(rawAssetPackData, Newtonsoft.Json.Formatting.Indented);
                sw.Write(serialized);
            }
        }

        BlockSettings.byID.Clear();
        RawAssetPackData rawData;

        BlockSettings.BlockTexture = new Texture2D(128, 128);

        using (StreamReader sr = new StreamReader(Path.Combine(Application.dataPath, "AssetPacks", "Asset.json")))
            rawData = Newtonsoft.Json.JsonConvert.DeserializeObject<RawAssetPackData>(sr.ReadToEnd());

        BlockSettings.BlockNames = new string[rawData.rawBlockData.Length + 1];
        BlockSettings.FullNames = new string[rawData.rawBlockData.Length + 1];
        BlockSettings.BlockSounds = new List<AudioClip>[rawData.rawBlockData.Length + 1];
        BlockSettings.PhysicsBound = new bool[rawData.rawBlockData.Length + 1];
        BlockSettings.PhysicsFunctions = new BlockPhysics[rawData.rawBlockData.Length + 1];

        if (File.Exists(Path.Combine(Application.dataPath, "AssetPacks", rawData.TextureFile)))
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(Application.dataPath, "AssetPacks", rawData.TextureFile));
            BlockSettings.BlockTexture = new Texture2D(rawData.TextureDimension.Width, rawData.TextureDimension.Height, TextureFormat.RGB24, false);
            BlockSettings.BlockTexture.filterMode = FilterMode.Point;
            BlockSettings.BlockTexture.LoadImage(bytes);
            BlockSettings.BlockTexture.Apply();
        }
        else
            Debug.LogError($"Missing texture file. {Path.Combine(Application.dataPath, "AssetPacks", rawData.TextureFile)}");

        BlockSettings.BlockTileSize = rawData.TileSize;
        BlockSettings.TextureSize = rawData.TextureSize;
        BlockSettings.ChunkScale = 0.5f;

        BlockSettings.worldGen = 1;

        BlockSettings.BlockNames[0] = "Air";
        BlockSettings.FullNames[0] = "Air";
        BlockSettings.byID.Add(new BlockData(false));

        foreach (var rawBlock in rawData.rawBlockData)
        {
            BlockSettings.byID.Add(new BlockData(
                rawBlock.Solid, 
                rawBlock.Textures.ToInt2Array(), 
                rawBlock.Physics.UsePhysics,
                rawBlock.Physics.PhysicsFunction == null ? 0f : rawBlock.Physics.PhysicsTime,
                rawBlock.CullingMode,
                rawBlock.Foliage,
                rawBlock.MarchingCubesLayer,
                rawBlock.Liquid));

            if (rawBlock.MarchingCubesLayer > BlockSettings.MarchingLayers)
                BlockSettings.MarchingLayers = rawBlock.MarchingCubesLayer;

            BlockSettings.BlockNames[rawBlock.ID] = rawBlock.Name;
            string Namespace = rawBlock.Namespace == null ? string.Empty : $"{rawBlock.Namespace}:";
            BlockSettings.FullNames[rawBlock.ID] = $"{Namespace}{rawBlock.Name.Replace(' ', '_')}";

            BlockSettings.BlockSounds[rawBlock.ID] = new List<AudioClip>();
            if (rawBlock.Sounds != null)
                StartCoroutine(InitializeSounds(rawBlock.Sounds, rawBlock.ID));

            if (rawBlock.Physics.PhysicsFunction != null)
            {
                BlockPhysics blockPhysics;
                Type type = Type.GetType(rawBlock.Physics.PhysicsFunction);
                if (type == null)
                {
                    Debug.Log($"Can't bind Physics Function -> {rawBlock.Physics.PhysicsFunction}");
                    BlockSettings.PhysicsFunctions[rawBlock.ID] = null;
                }
                else
                {
                    blockPhysics = (BlockPhysics)Activator.CreateInstance(type);
                    BlockSettings.PhysicsFunctions[rawBlock.ID] = blockPhysics;
                    BlockSettings.PhysicsBound[rawBlock.ID] = true;
                    Debug.Log($"Bound {blockPhysics} to blockid {rawBlock.ID} ({BlockSettings.FullNames[rawBlock.ID]})");
                }
            }
        }

        BlockSettings.NativeByID = new NativeArray<BlockData>(BlockSettings.byID.Count, Allocator.Persistent);
        BlockSettings.NativeByID.CopyFrom(BlockSettings.byID.ToArray());

        MarchingCubesTables.ConvertToNative();
    }

    public IEnumerator InitializeSounds(string[] fileNames, int blockId)
    {
        foreach (var file in fileNames)
        {
            using (var download = new WWW($"{Application.dataPath}/AssetPacks/{file.Replace('\\', '/')}"))
            {
                yield return download;
                var clip = download.GetAudioClip();
                if (clip != null)
                    BlockSettings.BlockSounds[blockId].Add(clip);
            }
        }
        Debug.Log($"{BlockSettings.BlockSounds[blockId].Count} sounds loaded for block {BlockSettings.FullNames[blockId]}");
    }
}

using System;
using System.Collections.Generic;
using Unity.Mathematics;

[Serializable]
public readonly struct BlockTypes
{
    public BlockTypes(bool solid, int2 texture, bool usePhysics = false, float physicsTime = 0f, int cullingMode = 0, bool foliage = false, sbyte marchingCubesLayer = 0, bool liquid = false)
    {
        Solid = solid;
        Texture_Up = texture;
        Texture_Down = texture;
        Texture_North = texture;
        Texture_South = texture;
        Texture_East = texture;
        Texture_West = texture;
        Texture_Marched = texture;
        UsePhysics = usePhysics;
        PhysicsTime = physicsTime;
        CullingMode = cullingMode;
        MarchingCubesLayer = marchingCubesLayer;
        Foliage = foliage;
        Liquid = liquid;
    }

    public BlockTypes(bool solid, int2[] textures, bool usePhysics = false, float physicsTime = 0f, int cullingMode = 0, bool foliage = false, sbyte marchingCubesLayer = 0, bool liquid = false)
    {
        Solid = solid;
        Texture_Up = textures[(int)BlockTextures.Up];
        Texture_Down = textures[(int)BlockTextures.Down];
        Texture_North = textures[(int)BlockTextures.North];
        Texture_South = textures[(int)BlockTextures.South];
        Texture_East = textures[(int)BlockTextures.East];
        Texture_West = textures[(int)BlockTextures.West];
        Texture_Marched = textures[(int)BlockTextures.Marched];
        UsePhysics = usePhysics;
        PhysicsTime = physicsTime;
        CullingMode = cullingMode;
        MarchingCubesLayer = marchingCubesLayer;
        Foliage = foliage;
        Liquid = liquid;
    }

    public BlockTypes(bool solid, List<int2> textures, bool usePhysics = false, float physicsTime = 0f, int cullingMode = 0, bool foliage = false, sbyte marchingCubesLayer = 0, bool liquid = false)
    {
        Solid = solid;
        Texture_Up = textures[(int)BlockTextures.Up];
        Texture_Down = textures[(int)BlockTextures.Down];
        Texture_North = textures[(int)BlockTextures.North];
        Texture_South = textures[(int)BlockTextures.South];
        Texture_East = textures[(int)BlockTextures.East];
        Texture_West = textures[(int)BlockTextures.West];
        Texture_Marched = textures[(int)BlockTextures.Marched];
        UsePhysics = usePhysics;
        PhysicsTime = physicsTime;
        CullingMode = cullingMode;
        MarchingCubesLayer = marchingCubesLayer;
        Foliage = foliage;
        Liquid = liquid;
    }

    public BlockTypes(bool solid = false)
    {
        Solid = solid;
        Texture_Up = new int2();
        Texture_Down = new int2();
        Texture_North = new int2();
        Texture_South = new int2();
        Texture_East = new int2();
        Texture_West = new int2();
        Texture_Marched = new int2();
        PhysicsTime = new float();
        CullingMode = new int();
        MarchingCubesLayer = new sbyte();
        UsePhysics = new boolean();
        Foliage = new boolean();
        Liquid = new boolean();
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
    public boolean Liquid { get; }
}

namespace VoxaNovus
{
    public class RawConfigFile
    {
        public float ChunkScale { get; set; }
        public float WorldGen { get; set; }
    }

    public class RawAssetPackData
    {
        public string AssetName { get; set; }
        public string TextureFile { get; set; }
        public TextureRect TextureDimension { get; set; }
        public float TextureSize { get; set; }
        public float TileSize { get; set; }
        public RawBlockData[] rawBlockData { get; set; }
    }

    public class RawBlockData
    {
        public ushort ID { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public RawTextureInfo Textures { get; set; }
        public string[] Sounds { get; set; }
        public int CullingMode { get; set; }
        public RawPhysicsData Physics { get; set; }
        public sbyte MarchingCubesLayer { get; set; }
        public bool Solid { get; set; }
        public bool Foliage { get; set; }
        public bool Liquid { get; set; }
    }

    public struct RawPhysicsData
    {
        public bool UsePhysics { get; set; }
        public float PhysicsTime { get; set; }
        public string PhysicsFunction { get; set; }
    }
}
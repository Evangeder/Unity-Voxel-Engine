namespace VoxaNovus
{
    public interface BlockPhysics
    {
        void Init(World world);
        void Tick(BlockMetadata block, int x, int y, int z);
    }
}
using System.Collections;

namespace VoxaNovus
{
    public interface BlockPhysics
    {
        void Init(World world);
        IEnumerator Tick(BlockMetadata block, int x, int y, int z);
    }
}
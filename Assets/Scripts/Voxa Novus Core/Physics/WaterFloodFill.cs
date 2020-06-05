using System.Collections;

namespace VoxaNovus
{
    public class WaterFloodfill : BlockPhysics
    {
        World world;

        public void Init(World world)
        {
            this.world = world;
        }

        public IEnumerator Tick(BlockMetadata block, int x, int y, int z)
        {
            Chunk ch = world.GetChunk(x, y, z);
            while (ch.isWriting || ch.IsRendering || ch.ioRenderValue > 0) yield return null;

            for (int ix = -1; ix < 2; ix++)
                for (int iy = -1; iy < 1; iy++)
                    for (int iz = -1; iz < 2; iz++)
                        if (((ix == 0 && iy == 0) || (iy == 0 && iz == 0) || (iz == 0 && ix == 0)) && !(ix == 0 && iy == 0 && iz == 0) && (world.GetBlock(x + ix, y + iy, z + iz).ID == 0))
                        {
                            block.Switches |= BlockSwitches.PhysicsTrigger;
                            world.SetBlock(x + ix, y + iy, z + iz, block, false, BlockUpdateMode.None);
                            PhysicsQueue.Push(block, x + ix, y + iy, z + iz);
                        }

            block.Switches &= ~BlockSwitches.PhysicsTrigger;
            world.SetBlock(x, y, z, block, false, BlockUpdateMode.None);
        }
    }
}
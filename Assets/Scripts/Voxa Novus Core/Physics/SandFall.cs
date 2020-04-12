namespace VoxaNovus
{
    public class SandfallTest : BlockPhysics
    {
        World world;

        public void Init(World world)
        {
            this.world = world;
        }

        public void Tick(BlockMetadata block, int x, int y, int z)
        {
            if (world.GetBlock(x, y - 1, z).ID == 0)
            {
                block.Switches |= BlockSwitches.PhysicsTrigger;
                world.SetBlock(x, y, z, BlockMetadata.EmptyPhysicsTrigger(), false, BlockUpdateMode.None);
                world.SetBlock(x, y - 1, z, block);
                PhysicsQueue.Push(block, x, y - 1, z);
            }
            else
                world.SetBlock(x, y, z, block, false, BlockUpdateMode.None);
        }
    }
}
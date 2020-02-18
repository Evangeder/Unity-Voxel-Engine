using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;


public interface BlockPhysics
{
    void Init(World world);
    void Tick(BlockMetadata block, int x, int y, int z);
}

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
        {
            world.SetBlock(x, y, z, block, false, BlockUpdateMode.None);
        }
        return;
    }
}

public class WaterFloodfill : BlockPhysics
{
    World world;

    public void Init(World world)
    {
        this.world = world;
    }

    public void Tick(BlockMetadata block, int x, int y, int z)
    {
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
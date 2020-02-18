using System;

[Flags]
public enum BlockSwitches : byte
{
    None                = 0,
    Marched             = 1 << 1, // For Marching Cubes blocks
    Static              = 1 << 2, // When you don't want to trigger physics for this block
    PhysicsTrigger      = 1 << 3,
    Undefined2          = 1 << 4,
    Interactive         = 1 << 5,
    Undefined3          = 1 << 6,
    Undefined4          = 1 << 7,
}

public static class BlockSwitchesClass
{
    public static bool Get(this BlockSwitches blockSwitches, BlockSwitches switches)
    {
        if ((switches & blockSwitches) == switches) return true;
        return false;
    }

    public static void Clear(this BlockSwitches blockSwitches)
    {
        blockSwitches = BlockSwitches.None;
    }

    // To set FALSE: blockSwitches &= ~switches;
    // To set TRUE:  blockSwitches |= switches;
}
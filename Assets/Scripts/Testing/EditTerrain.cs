using UnityEngine;
using System.Collections;
using Unity.Mathematics;

public static class EditTerrain
{
    public static int3 GetBlockPos(Vector3 pos)
    {
        int3 blockPos = new int3(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y),
            Mathf.RoundToInt(pos.z)
            );

        return blockPos;
    }

    public static int3 GetBlockPos(RaycastHit hit, bool adjacent = false)
    {
        Vector3 pos = new Vector3(
            MoveWithinBlock(hit.point.x, hit.normal.x, adjacent),
            MoveWithinBlock(hit.point.y, hit.normal.y, adjacent),
            MoveWithinBlock(hit.point.z, hit.normal.z, adjacent)
            );

        return GetBlockPos(pos);
    }

    static float MoveWithinBlock(float pos, float norm, bool adjacent = false)
    {
        if (pos - (int)pos == 0.5f || pos - (int)pos == -0.5f)
        {
            if (adjacent)
            {
                pos += (norm / 2);
            }
            else
            {
                pos -= (norm / 2);
            }
        }

        return (float)pos;
    }

    public static bool SetBlock(RaycastHit hit, BlockMetadata metadata, bool adjacent = false, bool UsePhysics = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
        {
            return false;
        }

        int3 pos = GetBlockPos(hit, adjacent);

        chunk.world.SetBlock(pos.x, pos.y, pos.z, metadata);

        return true;
    }

    public static BlockMetadata GetBlock(RaycastHit hit, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return new BlockMetadata { MarchedValue = 0, Switches = BlockSwitches.None, ID = 0 };

        int3 pos = GetBlockPos(hit, adjacent);

        BlockMetadata block = chunk.world.GetBlock(pos.x, pos.y, pos.z);

        return block;
    }
}
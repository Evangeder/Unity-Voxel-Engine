using UnityEngine;
using Unity.Mathematics;

namespace VoxaNovus
{
    public static class BlockRaycast
    {
        public static World world;

        public static int3 GetBlockPos(Vector3 pos)
        {
            return new int3(
               BlockSettings.ChunkScale == 1f ? Mathf.RoundToInt(pos.x) : (int)(pos.x / BlockSettings.ChunkScale + BlockSettings.ChunkScale),
               BlockSettings.ChunkScale == 1f ? Mathf.RoundToInt(pos.y) : (int)(pos.y / BlockSettings.ChunkScale + BlockSettings.ChunkScale),
               BlockSettings.ChunkScale == 1f ? Mathf.RoundToInt(pos.z) : (int)(pos.z / BlockSettings.ChunkScale + BlockSettings.ChunkScale));
        }

        public static int3 GetBlockPos(RaycastHit hit, bool adjacent = false)
        {
            Vector3 pos = new Vector3(
                GetBlockOffset(hit.point.x, hit.normal.x, adjacent),
                GetBlockOffset(hit.point.y, hit.normal.y, adjacent),
                GetBlockOffset(hit.point.z, hit.normal.z, adjacent)
                );

            if (BlockSettings.ChunkScale != 1f)
            {
                if (hit.normal.x == 1) pos.x -= BlockSettings.ChunkScale / 2f;
                if (hit.normal.y == 1) pos.y -= BlockSettings.ChunkScale / 2f;
                if (hit.normal.z == 1) pos.z -= BlockSettings.ChunkScale / 2f;

                if (pos.x < 0) pos.x -= BlockSettings.ChunkScale * 0.999f;
                if (pos.y < 0) pos.y -= BlockSettings.ChunkScale * 0.999f;
                if (pos.z < 0) pos.z -= BlockSettings.ChunkScale * 0.999f;
            }

            return GetBlockPos(pos);
        }

        static float GetBlockOffset(float pos, float norm, bool adjacent = false)
        {
            if (pos - (int)pos == (0.5f * BlockSettings.ChunkScale) || pos - (int)pos == (-0.5f * BlockSettings.ChunkScale))
            {
                if (adjacent)
                    pos += norm / (2 / BlockSettings.ChunkScale);
                else
                    pos -= norm / (2 / BlockSettings.ChunkScale);
            }
            return pos;
        }

        public static bool SetBlock(RaycastHit hit, BlockMetadata metadata, bool adjacent = false)
        {
            Chunk chunk = hit.collider.GetComponent<Chunk>();
            if (chunk == null)
                return false;

            chunk.world.SetBlock(GetBlockPos(hit, adjacent), metadata);

            return true;
        }

        public static bool SetBlock(Chunk chunk, RaycastHit hit, BlockMetadata metadata, bool adjacent = false)
        {
            if (chunk == null)
                return false;

            chunk.world.SetBlock(GetBlockPos(hit, adjacent), metadata);

            return true;
        }

        public static BlockMetadata GetBlock(RaycastHit hit, bool adjacent = false)
        {
            Chunk chunk = hit.collider.GetComponent<Chunk>();
            if (chunk == null)
                return new BlockMetadata();

            world.debugCMcount2.text = $"Chunk position: {chunk.pos}";

            BlockMetadata block = chunk.world.GetBlock(GetBlockPos(hit, adjacent));

            return block;
        }
    }
}
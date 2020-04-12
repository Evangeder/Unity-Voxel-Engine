using CielaSpike;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxaNovus {

    public struct QueuedBlock
    {
        public static int CalculatePosition(int3 pos)
        {
            return pos.x + pos.y * BlockSettings.ChunkSize + pos.z * BlockSettings.ChunkSize * BlockSettings.ChunkSize;
        }
        public QueuedBlock(int3 position)
        {
            Position = CalculatePosition(position);
            Data = new BlockMetadata();
        }
        public QueuedBlock(int3 position, BlockMetadata data)
        {
            Position = CalculatePosition(position);
            Data = data;
        }
        public QueuedBlock(int position, BlockMetadata data)
        {
            Position = position;
            Data = data;
        }

        public int Position { get; }
        public BlockMetadata Data { get; }
    }


    public static class BlockUpdateQueue
    {
        public static ConcurrentDictionary<int3, Chunk> chunksWithBlockQueue = new ConcurrentDictionary<int3, Chunk>();

        public static bool StopExecuting = false;

        public static bool Push(Chunk chunk)
        {
            if (chunksWithBlockQueue.ContainsKey(chunk.pos)) return false;
            return chunksWithBlockQueue.TryAdd(chunk.pos, chunk);
        }

        public static bool Pop(Chunk chunk)
        {
            return Pop(chunk.pos);
        }

        public static bool Pop(int3 position)
        {
            if (!chunksWithBlockQueue.ContainsKey(position)) return false;
            return chunksWithBlockQueue.TryRemove(position, out _);
        }

        public static bool Peek(int3 position)
        {
            if (chunksWithBlockQueue.ContainsKey(position)) 
                return true;
            return false;
        }

        [BurstCompile]
        struct JobCopyBlocks : IJob
        {
            public NativeArray<BlockMetadata> chunk;
            [DeallocateOnJobCompletion] public NativeArray<QueuedBlock> blocks;

            public void Execute()
            {
                for (int i = 0; i < blocks.Length; i++)
                    chunk[blocks[i].Position] = blocks[i].Data;
            }
        }

        public static IEnumerator CoroutineIterateThroughQueue()
        {
            yield return Ninja.JumpBack;
            Debug.Log("BlockQueue started.");
            List<Chunk> chunksToUpdate = new List<Chunk>();
            List<int3> chunksToRemove = new List<int3>();
            JobHandle BlockUpdate_JobHandle;

            while (!StopExecuting)
            {
                while (chunksWithBlockQueue.Count <= 0) yield return new WaitForEndOfFrame();

                foreach (var chunk in chunksWithBlockQueue.Values)
                {
                    if (chunk.ioRenderValue > 0 || chunk.isWriting) continue;
                    chunk.isWriting = true;

                    if (chunk.BlockchangeQueue.Count < 1)
                    {
                        chunksToRemove.Add(chunk.pos);
                        chunk.isWriting = false;
                        continue;
                    }

                    NativeArray<QueuedBlock> queuedBlocks = new NativeArray<QueuedBlock>(chunk.BlockchangeQueue.Count, Allocator.TempJob);
                    queuedBlocks.CopyFrom(chunk.BlockchangeQueue.ToArray());
                    chunk.BlockchangeQueue.Clear();

                    BlockUpdate_JobHandle = new JobCopyBlocks()
                    {
                        blocks = queuedBlocks,
                        chunk = chunk.BlocksN,
                    }.Schedule();
                    
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    BlockUpdate_JobHandle.Complete();

                    chunksToUpdate.Add(chunk);
                    chunksToRemove.Add(chunk.pos);

                    chunk.isWriting = false;

                    Pop(chunk.pos);
                }

                if (chunksToUpdate.Count > 0)
                {
                    Debug.Log($"BlockQueue > Starting chunk updates");
                    foreach (var chunk in chunksToUpdate)
                        chunk.UpdateChunk();
                    
                    chunksToUpdate.Clear();
                }

                if (chunksToRemove.Count > 0)
                {
                    foreach (var chunkPos in chunksToRemove)
                        chunksWithBlockQueue.TryRemove(chunkPos, out _);

                    chunksToRemove.Clear();
                }

                yield return new WaitForEndOfFrame();
            }
        }
    }
}
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace VoxaNovus.WorldGen
{
    public class WorldGen
    {
        public WorldGen() { }

        public Queue<Chunk> ChunkQueue = new Queue<Chunk>();

        /// <summary>
        /// Queue chunk generation at given chunk
        /// </summary>
        /// <param name="chunk">Chunk that will be queued for world generation</param>
        public void QueueChunk(Chunk chunk, bool MainThread = true)
        {
            ChunkQueue.Enqueue(chunk);
        }

        public IEnumerator GenerateChunk(Chunk chunk)
        {
            bool SafetyCheck()
            {
                if (chunk.isQueuedForDeletion) return false;

                bool reads = false;
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        for (int z = -1; z <= 1; z++)
                        {
                            if (chunk.world.CheckChunk(chunk.pos.x + x * BlockSettings.ChunkSize, chunk.pos.y + y * BlockSettings.ChunkSize, chunk.pos.z + z * BlockSettings.ChunkSize)
                                && chunk.world.GetChunk(chunk.pos.x + x * BlockSettings.ChunkSize, chunk.pos.y + y * BlockSettings.ChunkSize, chunk.pos.z + z * BlockSettings.ChunkSize).ioRenderValue > 0)
                                reads = true;
                        }
                return reads;
            }

            chunk.generated = false;
            chunk.isGenerating = true;

            while (SafetyCheck())
                yield return null;

            if (chunk.WorldGen_JobHandle.IsCompleted 
                && chunk != null 
                && !chunk.isQueuedForDeletion 
                && !chunk.isEmpty 
                && chunk.world.CheckChunk(chunk.pos.x, chunk.pos.y, chunk.pos.z) 
                && chunk.BlocksN.IsCreated
                && !chunk.generated)
            {
                while (SafetyCheck())
                    yield return null;
                PrepareJob(chunk);

                for (int i = 0; i < 2; i++)
                    yield return new WaitForEndOfFrame();

                chunk.WorldGen_JobHandle.Complete();
                chunk.generated = true;
                chunk.isGenerating = false;
            }
            chunk.isGenerating = false;
        }

        public virtual void PrepareJob(Chunk chunk)
        {
            var job = new EmptyJob();
            chunk.WorldGen_JobHandle = job.Schedule();
        }

        [BurstCompile]
        struct EmptyJob : IJob
        {
            public void Execute() { }
        }
    }
}
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Collections;
using Random = Unity.Mathematics.Random;

public class WorldGen
{
    public NativeArray<BlockTypes> blockTypes;
    private AutoResetEvent autoResetEvent = new AutoResetEvent(true); // Adds safety to multithreaded code, so we don't try to write multiple things to list at the same time
    public List<Chunk> ChunkQueue = new List<Chunk>();

    public WorldGen() {}

    /// <summary>
    /// Prepare blockdata NativeArray for later use in jobs
    /// </summary>
    public void PrepareBlockInfo()
    {
        blockTypes = new NativeArray<BlockTypes>(BlockData.byID.Count, Allocator.Persistent);
        blockTypes.CopyFrom(BlockData.byID.ToArray());
    }

    /// <summary>
    /// Queue chunk generation at given chunk
    /// </summary>
    /// <param name="chunk">Chunk that will be queued for world generation</param>
    public void QueueChunk(Chunk chunk, bool MainThread = true)
    {
        autoResetEvent.WaitOne();
        ChunkQueue.Add(chunk);
        autoResetEvent.Set();
    }

    /// <summary>
    /// Dispose NativeArrays, so we don't get memory leaks.
    /// </summary>
    void OnDestroy()
    {
        if (blockTypes.IsCreated) blockTypes.Dispose();
    }

    /*
    public virtual IEnumerator GenerateChunk(Chunk chunk)
    {
        if (chunk.WorldGen_JobHandle.IsCompleted && !chunk.nBlocks.IsCreated)
        {
            chunk.nBlocks = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            var job = new ChunkGen()
            {
                _blocksNew = chunk.nBlocks,
                ChunkCoordinates = new float3(chunk.pos.x, chunk.pos.y, chunk.pos.z)
            };
            chunk.WorldGen_JobHandle = job.Schedule();
        }
        yield return new WaitForEndOfFrame();
        chunk.WorldGen_JobHandle.Complete();
        chunk.Blocks = new BlockMetadata[chunk.nBlocks.Length];
        chunk.nBlocks.CopyTo(chunk.Blocks);
        chunk.nBlocks.Dispose();
    }

    [BurstCompile]
    struct ChunkGen : IJob
    {
        [DeallocateOnJobCompletion] [ReadOnly] public float3 ChunkCoordinates;
        [ReadOnly] public float2 seed;

        public NativeArray<BlockMetadata> _blocksNew;

        public void Execute()
        {
            BlockMetadata WorkerBlock = default;

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        if (ChunkCoordinates.y + y == 25)
                        {
                            WorkerBlock.ID = 2;
                            WorkerBlock.Marched = false;
                            WorkerBlock.MarchedValue = 0;
                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                        }
                        else if (ChunkCoordinates.y + y < 25)
                        {
                            WorkerBlock.ID = 3;
                            WorkerBlock.Marched = false;
                            WorkerBlock.MarchedValue = 0;
                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                        }
                        
                    }
                }
            }
        }
    }
    */

    public virtual IEnumerator GenerateChunk(Chunk chunk)
    {
        if (chunk.WorldGen_JobHandle.IsCompleted && !chunk.nBlocks.IsCreated)
        {
            chunk.nBlocks = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            var job = new ChunkGenFloatingIslands()
            {
                _blocksNew = chunk.nBlocks,
                ChunkCoordinates = new float3(chunk.pos.x, chunk.pos.y, chunk.pos.z),
                blocktype = blockTypes, // BlockData values
                seed = chunk.world.WorldSeed
            };
            chunk.WorldGen_JobHandle = job.Schedule();
        }
        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame();
        chunk.WorldGen_JobHandle.Complete();
        chunk.Blocks = new BlockMetadata[chunk.nBlocks.Length];
        chunk.nBlocks.CopyTo(chunk.Blocks);
        chunk.nBlocks.Dispose();
        chunk.generated = true;
    }

    [BurstCompile]
    private struct ChunkGenFloatingIslands : IJob
    {
        Random random;
        [DeallocateOnJobCompletion] [ReadOnly] public float3 ChunkCoordinates;

        [ReadOnly] public NativeArray<BlockTypes> blocktype;
        [ReadOnly] public float2 seed;

        public NativeArray<BlockMetadata> _blocksNew;

        public void Execute()
        {
            random = new Random(0x6E624EB7u);
            BlockMetadata WorkerBlock = new BlockMetadata();

            float scale = 5f;
            float scale2 = 1f;
            int power = 2;
            int power2 = 2;

            float scale3 = 4f;
            float scale4 = 8f;
            int power3 = 1;
            int power4 = 1;

            float extrudeBottom = 0.05f;
            float extrudeBottom2 = 1f;

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {

                    float SurfaceNoise = noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power)) * scale2) / 1000, ((ChunkCoordinates.z + z + math.pow(seed.y, power)) / 1000)) * scale2) * 10;
                    SurfaceNoise += noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power) + 250) * scale) / 200, ((ChunkCoordinates.z + z + math.pow(seed.y, power) + 250) / 200) * scale)) * 5;
                    SurfaceNoise += noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power) + 1000) * scale) / 200, ((ChunkCoordinates.z + z + math.pow(seed.y, power) + 1000) * scale) / 200)) * 10;
                    SurfaceNoise -= noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power) - 50) * scale2) / 100, ((ChunkCoordinates.z + z + math.pow(seed.y, power) - 50) / 100) * scale2)) * 5;
                    SurfaceNoise -= noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power2) - 10) * scale2) / 25, ((ChunkCoordinates.z + z + math.pow(seed.y, power2) - 10) / 25) * scale2)) * 5;

                    float SurfaceNoise2 = noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power) + 250) * scale) / 200, ((ChunkCoordinates.z + z + math.pow(seed.x, power) + 250) / 200) * scale)) * 5;
                    SurfaceNoise2 -= noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power) + 1000) * scale) / 200, ((ChunkCoordinates.z + z + math.pow(seed.x, power) + 1000) * scale) / 200)) * 10;
                    SurfaceNoise2 += noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power) - 50) * scale2) / 100, ((ChunkCoordinates.z + z + math.pow(seed.x, power) - 50) / 100) * scale2)) * 5;
                    SurfaceNoise2 += noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power2) - 10) * scale2) / 25, ((ChunkCoordinates.z + z + math.pow(seed.x, power2) - 10) / 25) * scale2)) * 5;

                    float botnoise_inception = (noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power3) - 512) * scale3) / 500, ((ChunkCoordinates.z + z + math.pow(seed.y, power3) + 250) * scale3) / 500)) * 1.33f);
                    float botnoise_inception2 = (noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) + 250) * scale3) / 500, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) - 512) * scale3) / 500)) * 2.5f);

                    float BottomNoise = math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) - 200) * scale4) / 10, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) - 200) / 10) * scale4)) * 2) * extrudeBottom2;
                    BottomNoise += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power3) + 512) * scale2) / 25, ((ChunkCoordinates.z + z + math.pow(seed.y, power3) + 512) / 25) * scale2)) * 2) * extrudeBottom;
                    BottomNoise += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power3) + 10) * scale3) / 100, ((ChunkCoordinates.z + z + math.pow(seed.y, power3) + 10) / 100) * scale3)) * 7) * extrudeBottom2;
                    BottomNoise -= (noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power3) + 10) * scale3) / 1000, ((ChunkCoordinates.z + z + math.pow(seed.y, power3) + 10) / 1000) * scale3)) * 25) * extrudeBottom;
                    BottomNoise += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power3) + 100) * scale3) / 200, ((ChunkCoordinates.z + z + math.pow(seed.y, power3) + 100) / 200) * scale3)) * 15) * extrudeBottom2;
                    BottomNoise *= (noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power3) + 100) * scale3) / 200, ((ChunkCoordinates.z + z + math.pow(seed.y, power3) + 100) / 200) * scale3)) * 5) * extrudeBottom2;
                    BottomNoise += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception))) * 10);
                    BottomNoise += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception2))) * 10);
                    BottomNoise -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception2), math.abs(botnoise_inception))) * 10);
                    BottomNoise -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception))) * 10);
                    BottomNoise += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception2), math.abs(botnoise_inception2))) * 10);

                    float BottomNoise_2 = (noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power4) - 200) * scale4) / 10, ((ChunkCoordinates.z + z + math.pow(seed.x, power4) - 200) / 10) * scale4)) * 2) * extrudeBottom2;
                    BottomNoise_2 -= math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power3) + 512) * scale2) / 25, ((ChunkCoordinates.z + z + math.pow(seed.x, power3) + 512) / 25) * scale2)) * 2) * extrudeBottom;
                    BottomNoise_2 -= math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power3) + 10) * scale3) / 100, ((ChunkCoordinates.z + z + math.pow(seed.x, power3) + 10) / 100) * scale3)) * 7) * extrudeBottom2;
                    BottomNoise_2 += (noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power3) + 10) * scale3) / 1000, ((ChunkCoordinates.z + z + math.pow(seed.x, power3) + 10) / 1000) * scale3)) * 25) * extrudeBottom;
                    BottomNoise_2 -= math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power3) + 100) * scale3) / 200, ((ChunkCoordinates.z + z + math.pow(seed.x, power3) + 100) / 200) * scale3)) * 15) * extrudeBottom2;
                    BottomNoise_2 *= (noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power3) + 100) * scale3) / 200, ((ChunkCoordinates.z + z + math.pow(seed.x, power3) + 100) / 200) * scale3)));
                    BottomNoise_2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception))) * 10);
                    BottomNoise_2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception2))) * 10);
                    BottomNoise_2 += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception2), math.abs(botnoise_inception))) * 10);
                    BottomNoise_2 += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception))) * 10);
                    BottomNoise_2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception2), math.abs(botnoise_inception2))) * 10);

                    float BottomNoise2_2 = math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power4) - 200) * scale4) / 10, ((ChunkCoordinates.z + z + math.pow(seed.x, power4) - 200) / 10) * scale4)) * 2) * extrudeBottom2;
                    BottomNoise2_2 -= math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power4) + 200) * scale4) / 25, ((ChunkCoordinates.z + z + math.pow(seed.x, power4) + 200) / 25) * scale4)) * 4) * extrudeBottom2;
                    BottomNoise2_2 -= math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.y, power4) + 15) * 15) / 1000, ((ChunkCoordinates.z + z + math.pow(seed.x, power4) + 15) / 1000) * 15)) * 15) * 4;
                    BottomNoise2_2 += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception))) * 10);
                    BottomNoise2_2 += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception2))) * 10);
                    BottomNoise2_2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception2), math.abs(botnoise_inception))) * 10);
                    BottomNoise2_2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception))) * 10);
                    BottomNoise2_2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception2), math.abs(botnoise_inception2))) * 10);

                    float BottomNoise2 = math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) - 200) * scale4) / 10, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) - 200) / 10) * scale4)) * 2) * extrudeBottom2;
                    BottomNoise2 += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) + 200) * scale4) / 25, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) + 200) / 25) * scale4)) * 4) * extrudeBottom2;
                    BottomNoise2 += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) + 15) * scale4) / 100, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) + 15) / 100) * scale4)) * 7) * extrudeBottom2;
                    BottomNoise2 += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception))) * 10);
                    BottomNoise2 += math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception2))) * 10);
                    BottomNoise2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception2), math.abs(botnoise_inception))) * 10);
                    BottomNoise2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception), math.abs(botnoise_inception))) * 10);
                    BottomNoise2 -= math.abs(noise.cnoise(new float2(math.abs(botnoise_inception2), math.abs(botnoise_inception2))) * 10);

                    float StoneLayer = math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) + 1) * scale4) / 10, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) - 2) / 10) * scale4)) * 2) * extrudeBottom2;
                    StoneLayer += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) + 128) * scale4) / 25, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) - 256) / 25) * scale4)) * 6) * extrudeBottom2;
                    StoneLayer += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) - 512) * 5) / 500, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) + 1024) / 500) * 5)) * 7) * extrudeBottom2;
                    StoneLayer += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) - 512) * scale4) / 2, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) + 1024) / 2) * scale4)) * 7) * extrudeBottom2;
                    StoneLayer += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) - 512) * 2) / 2, ((ChunkCoordinates.z + z + math.pow(seed.y, 2) + 1024) / 2) * scale4)) * 7) * extrudeBottom2;

                    float TreeGen = noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) + 64) * 15) / 250, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) - 64) / 250) * 15)) * 25;
                    TreeGen += math.abs(noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, power4) + 64) * 2f) / 5, ((ChunkCoordinates.z + z + math.pow(seed.y, power4) - 64) / 5) * 2f)));

                    int diff = 35;

                    StoneLayer -= ChunkCoordinates.y;
                    StoneLayer += 35 + diff;
                    SurfaceNoise -= ChunkCoordinates.y;
                    SurfaceNoise += 50 + diff;
                    BottomNoise -= ChunkCoordinates.y;
                    BottomNoise += 52 + diff;
                    BottomNoise2 -= ChunkCoordinates.y;
                    BottomNoise2 += 52 + diff;

                    SurfaceNoise2 -= ChunkCoordinates.y;
                    SurfaceNoise2 += 50;
                    BottomNoise_2 -= ChunkCoordinates.y;
                    BottomNoise_2 += 52;
                    BottomNoise2_2 -= ChunkCoordinates.y;
                    BottomNoise2_2 += 52;

                    int Surface_int = (int)math.floor(SurfaceNoise);
                    int Bottom_int = (int)math.floor(BottomNoise);
                    int Bottom2_int = (int)math.floor(BottomNoise2);

                    int Surface_2_int = (int)math.floor(SurfaceNoise2);
                    int Bottom_2_int = (int)math.floor(BottomNoise_2);
                    int Bottom2_2_int = (int)math.floor(BottomNoise2_2);

                    if (Bottom_int < Bottom2_int + 1) Bottom2_int = Bottom_int;

                    for (int y = 0; y < 16; y++)
                    {
                        if (_blocksNew[x + y * 16 + z * 256].ID == 0)
                        {
                            if (SurfaceNoise2 * 2 > BottomNoise_2)
                            {
                                if (y > Bottom_2_int && Bottom_2_int <= Bottom2_2_int)
                                {
                                    if (y == Surface_2_int || y == Surface_2_int - 1)
                                    {
                                        if (TreeGen >= 3f && y == Surface_2_int && random.NextInt(0, 100) > 95)
                                        {
                                            for (int iy = 0; iy < random.NextInt(3, 6); iy++)
                                            {
                                                if (y + iy < 15)
                                                {
                                                    WorkerBlock.ID = 12;
                                                    WorkerBlock.Marched = true;
                                                    WorkerBlock.SetMarchedValue(random.NextFloat(0.7f, 1f));
                                                    _blocksNew[x + (y + iy) * 16 + z * 256] = WorkerBlock;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            WorkerBlock.ID = 2;
                                            WorkerBlock.Marched = true;
                                            WorkerBlock.SetMarchedValue((SurfaceNoise2 - Surface_2_int) / 2 + 0.5f);
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }

                                    }
                                    else if (y < Surface_2_int)
                                    {
                                        if (y > StoneLayer - diff)
                                        {
                                            WorkerBlock.ID = 3;
                                            WorkerBlock.Marched = true;
                                            WorkerBlock.SetMarchedValue((SurfaceNoise2 - Surface_2_int) / 2 + 0.5f);
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }
                                        else
                                        {
                                            WorkerBlock.ID = 1;
                                            WorkerBlock.Marched = true;
                                            WorkerBlock.SetMarchedValue((SurfaceNoise2 - Surface_2_int) / 2 + 0.5f);
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }
                                    }
                                    else if (y == Surface_2_int + 1)
                                    {
                                        float newmarchval = (SurfaceNoise2 - math.floor(SurfaceNoise2)) / 2;
                                        if (newmarchval > 1f) newmarchval = 1f;
                                        else if (newmarchval < 0f) newmarchval = 0f;

                                        WorkerBlock.ID = 2;

                                        WorkerBlock.Marched = true;
                                        WorkerBlock.SetMarchedValue(newmarchval);

                                        if ((random.NextInt(0, 80) > 55) && WorkerBlock.ID == 2 && y > 0 && y < 15
                                            && _blocksNew[x + y * 16 + z * 256].ID == 0)
                                        {
                                            int randomizer = random.NextInt(0, 7);
                                            if (randomizer == 0) WorkerBlock.ID = 40;
                                            else if (randomizer == 1) WorkerBlock.ID = 41;
                                            else if (randomizer == 2) WorkerBlock.ID = 42;
                                            else if (randomizer == 3) WorkerBlock.ID = 43;
                                            else WorkerBlock.ID = 44;

                                            WorkerBlock.Marched = false;
                                            WorkerBlock.SetMarchedValue(1f);
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }
                                        else
                                        {
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }
                                    }
                                }
                            }


                            if (SurfaceNoise > BottomNoise)
                            {
                                if (y > Bottom_int && Bottom_int <= Bottom2_int)
                                {
                                    if (y == Surface_int || y == Surface_int - 1)
                                    {
                                        if (TreeGen >= 3f && y == Surface_int && random.NextInt(0, 100) > 95)
                                        {
                                            for (int iy = 0; iy < random.NextInt(3, 6); iy++)
                                            {
                                                if (y + iy < 15)
                                                {
                                                    WorkerBlock.ID = 12;
                                                    WorkerBlock.Marched = true;
                                                    WorkerBlock.SetMarchedValue(random.NextFloat(0.7f, 1f));
                                                    _blocksNew[x + (y + iy) * 16 + z * 256] = WorkerBlock;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            WorkerBlock.ID = 2;
                                            WorkerBlock.Marched = true;
                                            WorkerBlock.SetMarchedValue((SurfaceNoise - Surface_int) / 2 + 0.5f);
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }

                                    }
                                    else if (y < Surface_int)
                                    {
                                        if (y > StoneLayer)
                                        {
                                            WorkerBlock.ID = 3;
                                            WorkerBlock.Marched = true;
                                            WorkerBlock.SetMarchedValue((SurfaceNoise - Surface_int) / 2 + 0.5f);
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }
                                        else
                                        {
                                            WorkerBlock.ID = 1;
                                            WorkerBlock.Marched = true;
                                            WorkerBlock.SetMarchedValue((SurfaceNoise - Surface_int) / 2 + 0.5f);
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }
                                    }
                                    else if (y == Surface_int + 1)
                                    {
                                        float newmarchval = (SurfaceNoise - math.floor(SurfaceNoise)) / 2;
                                        if (newmarchval > 1f) newmarchval = 1f;
                                        else if (newmarchval < 0f) newmarchval = 0f;

                                        WorkerBlock.ID = 2;

                                        WorkerBlock.Marched = true;
                                        WorkerBlock.SetMarchedValue(newmarchval);

                                        if ((random.NextInt(0, 80) > 45) && WorkerBlock.ID == 2 && y > 0 && y < 15
                                            && _blocksNew[x + y * 16 + z * 256].ID == 0)
                                        {
                                            int randomizer = random.NextInt(0, 7);
                                            if (randomizer == 0) WorkerBlock.ID = 40;
                                            else if (randomizer == 1) WorkerBlock.ID = 41;
                                            else if (randomizer == 2) WorkerBlock.ID = 42;
                                            else if (randomizer == 3) WorkerBlock.ID = 43;
                                            else WorkerBlock.ID = 44;

                                            WorkerBlock.Marched = false;
                                            WorkerBlock.SetMarchedValue(0f);
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }
                                        else
                                        {
                                            _blocksNew[x + y * 16 + z * 256] = WorkerBlock;
                                        }
                                    }
                                }
                            }
                        }
                        else
                            continue;
                    }
                }
            }
        }
    }
}

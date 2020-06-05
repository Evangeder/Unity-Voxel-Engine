using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxaNovus.WorldGen
{
    public class FlatgrassWithChunkBorder : WorldGen
    {
        public override void PrepareJob(Chunk chunk)
        {
            chunk.WorldGen_JobHandle = new sFlatgrassWithChunkBorder() 
            {

                _blocksNew = chunk.BlocksN,
                ChunkCoordinates = chunk.pos,

            }.Schedule();
        }

        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        struct sFlatgrassWithChunkBorder : IJob
        {
            [DeallocateOnJobCompletion] [ReadOnly] public float3 ChunkCoordinates;
            public NativeArray<BlockMetadata> _blocksNew;

            public void Execute()
            {
                BlockMetadata WorkerBlock = new BlockMetadata();

                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            if (ChunkCoordinates.y + y == 25)
                            {
                                if (x == 15 || z == 15)
                                    WorkerBlock.ID = 5;
                                else
                                    WorkerBlock.ID = 2;
                                WorkerBlock.Switches = BlockSwitches.None;
                                WorkerBlock.MarchedValue = 0;
                                _blocksNew[x + y * BlockSettings.ChunkSize + z * (int)math.pow(BlockSettings.ChunkSize, 2)] = WorkerBlock;
                            }
                            else if (ChunkCoordinates.y + y < 25)
                            {
                                WorkerBlock.ID = 3;
                                WorkerBlock.Switches = BlockSwitches.None;
                                WorkerBlock.MarchedValue = 0;
                                _blocksNew[x + y * BlockSettings.ChunkSize + z * (int)math.pow(BlockSettings.ChunkSize, 2)] = WorkerBlock;
                            }
                        }
                    }
                }
            }
        }
    }
}
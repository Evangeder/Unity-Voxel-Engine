using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxaNovus.WorldGen
{
    public class Flatgrass : WorldGen
    {
        public override void PrepareJob(Chunk chunk)
        {
            chunk.world.AppendLog(chunk.pos.ToString());
            chunk.WorldGen_JobHandle = new sFlatgrass()
            {

                _blocksNew = chunk.BlocksN,
                ChunkCoordinates = chunk.pos,

            }.Schedule();
        }

        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        struct sFlatgrass : IJob
        {
            Random random;
            [DeallocateOnJobCompletion] [ReadOnly] public float3 ChunkCoordinates;
            public NativeArray<BlockMetadata> _blocksNew;

            public void Execute()
            {
                random = new Random(0x6E624EB7u);
                BlockMetadata WorkerBlock = new BlockMetadata();

                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            if (ChunkCoordinates.y + y == 25)
                            {
                                WorkerBlock.ID = 2;
                                WorkerBlock.Switches = BlockSwitches.Marched;
                                //WorkerBlock.MarchedValue = (byte)random.NextInt(128, 180);
                                WorkerBlock.SetMarchedValue(random.NextFloat(0.51f, 0.6f));
                                _blocksNew[x + y * BlockSettings.ChunkSize + z * (int)math.pow(BlockSettings.ChunkSize, 2)] = WorkerBlock;
                            }
                            else if (ChunkCoordinates.y + y < 25)
                            {
                                WorkerBlock.ID = 3;
                                WorkerBlock.Switches = BlockSwitches.Marched;
                                //WorkerBlock.MarchedValue = (byte)random.NextInt(160, 254);
                                WorkerBlock.SetMarchedValue(random.NextFloat(0.51f, 1f));
                                _blocksNew[x + y * BlockSettings.ChunkSize + z * (int)math.pow(BlockSettings.ChunkSize, 2)] = WorkerBlock;
                            }
                        }
                    }
                }
            }
        }
    }
}
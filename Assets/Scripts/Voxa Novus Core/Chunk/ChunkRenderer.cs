using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Linq;
using System;

namespace VoxaNovus
{
    public static class ChunkRenderer
    {
        static Dictionary<int3, bool> renderingChunks = new Dictionary<int3, bool>();

        public static bool QueryIsChunkRendering(int3 position)
        {
            return renderingChunks.TryGetValue(position, out _);
        }

        public static IEnumerator RenderChunk(Chunk chunk)
        {
            if (chunk.isQueuedForDeletion || chunk.isEmpty)
                yield break;

            JobHandle Render_JobHandle;
            World world = chunk.world;
            List<Chunk> readedChunks = new List<Chunk>();

            chunk.IsRendering = true;
            chunk.isRenderQueued = false;

            while (true)
            {
                if (chunk.BlocksN.Length == (int)math.pow(BlockSettings.ChunkSize, 3)
                    && !chunk.isGenerating
                    && chunk.generated
                    && !chunk.isWriting
                    && chunk.ioRenderValue < 1
                    && !renderingChunks.ContainsKey(chunk.pos)
                    && !BlockUpdateQueue.Peek(chunk.pos))
                    break;
                yield return null;
            }

            renderingChunks.Add(chunk.pos, true);

            MeshFilter chunkFilter = chunk.gameObject.GetComponent<MeshFilter>();
            MeshCollider chunkCollider = chunk.gameObject.GetComponent<MeshCollider>();
            NativeArray<BlockMetadata>[] neighbourChunks = new NativeArray<BlockMetadata>[26];
            bool[] neighbourChunksAlloc = new bool[26];


            chunk.ioRenderValue += 1;
            // Grab data from surrounding chunks
            byte iterator = 0;
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 & z == 0) continue;
                        if (!CheckChunkAvailability(chunk, x, y, z))
                        {
                            neighbourChunks[iterator] = new NativeArray<BlockMetadata>(0, Allocator.TempJob);
                            neighbourChunksAlloc[iterator] = true;
                        }
                        else
                        {
                            Chunk neighbour = world.GetChunk(chunk.pos.x + x * BlockSettings.ChunkSize, chunk.pos.y + y * BlockSettings.ChunkSize, chunk.pos.z + z * BlockSettings.ChunkSize);
                            neighbour.ioRenderValue += 1;
                            readedChunks.Add(neighbour);
                        }
                        iterator++;
                    }

            // Mesh info for non-marching voxels
            NativeList<Vector3> nativeVertices = new NativeList<Vector3>(Allocator.TempJob);
            NativeList<Vector2> nativeUVs = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<int> 
                nativeTriangles = new NativeList<int>(Allocator.TempJob),
                nativeLiquidTriangles = new NativeList<int>(Allocator.TempJob),
                nativeMarchingCubesTriangles = new NativeList<int>(Allocator.TempJob),
                nativeMarchingCubesTransparentTriangles = new NativeList<int>(Allocator.TempJob),
                nativeFoliageTriangles = new NativeList<int>(Allocator.TempJob);

            void DisposeNatives()
            {
                nativeVertices.Dispose();
                nativeUVs.Dispose();
                nativeTriangles.Dispose();
                nativeLiquidTriangles.Dispose();
                nativeMarchingCubesTriangles.Dispose();
                nativeMarchingCubesTransparentTriangles.Dispose();
                nativeFoliageTriangles.Dispose();

                if (chunk.ioRenderValue > 0) 
                    chunk.ioRenderValue -= 1;

                //foreach (var neighbour in readedChunks)
                //{
                //    if (neighbour.ioRenderValue > 0)
                //        neighbour.ioRenderValue -= 1;
                //}
                readedChunks.Clear();

                byte disposalIterator = 0;
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        for (int z = -1; z <= 1; z++)
                        {
                            if (x == 0 && y == 0 & z == 0) continue;
                            int chunkX = chunk.pos.x + x * BlockSettings.ChunkSize,
                                chunkY = chunk.pos.y + y * BlockSettings.ChunkSize,
                                chunkZ = chunk.pos.z + z * BlockSettings.ChunkSize;

                            Chunk tempChunk = world.GetChunk(chunkX, chunkY, chunkZ);

                            if (world.CheckChunk(chunkX, chunkY, chunkZ) && tempChunk.ioRenderValue > 0)
                                tempChunk.ioRenderValue -= 1;
                    
                            disposalIterator++;
                        }

                for (int i = 0; i < neighbourChunksAlloc.Length; i++)
                {
                    if (neighbourChunksAlloc[i] && neighbourChunks[i].IsCreated) 
                        neighbourChunks[i].Dispose();
                }
                renderingChunks.Remove(chunk.pos);
                chunk.isRenderQueued = false;
                chunk.IsRendering = false;
            }

            while (chunk.isWriting)
            {
                if (chunk.isQueuedForDeletion || chunk.isEmpty)
                {
                    DisposeNatives();
                    yield break;
                }
                yield return null;
            }

            Render_JobHandle = new Job_RenderChunk()
            {
                BlockTypes =                        BlockSettings.byID.Count,                   // Amount of blocktypes
                blocktype =                         BlockSettings.NativeByID,                   // Shared blockdata
                MarchingCubesLayerAmount =          BlockSettings.MarchingLayers,               // Amount of marching cubes layers (they don't connect to other layers)
                tileSize =                          BlockSettings.BlockTileSize,                // Texture tiling size
                chunkSize =                         BlockSettings.ChunkSize,                    // Size of the chunks
                chunkScale =                        BlockSettings.ChunkScale,                   // Scale of the chunks (either 0.5f or 1.0f)
                LevelofDetail =                     chunk.LOD,                                  // Level of Detail for LOD rendering
                vertices =                          nativeVertices,                             // STANDARD VOXEL MESHDATA
                triangles =                         nativeTriangles,                            // Triangles for regular voxels (greedy mesh)
                trianglesLiquid =                   nativeLiquidTriangles,                      // Triangles for liquids (regular voxels)
                uvs =                               nativeUVs,                                  // Texture data
                trianglesMarchingCubes =            nativeMarchingCubesTriangles,               // Triangles for marching cubes
                trianglesTransparentMarchingCubes = nativeMarchingCubesTransparentTriangles,    // Triangles for transparent marching cubes (like glass)
                trianglesFoliage =                  nativeFoliageTriangles,                     // Triangles for foliage (grass, flowers etc)

                blocks_CurX_CurY_CurZ = chunk.BlocksN,                                          // TARGET AND NEIGHBOUR CHUNKS
                blocks_MinX_MinY_MinZ = neighbourChunksAlloc[0] ? neighbourChunks[0] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_MinX_MinY_CurZ = neighbourChunksAlloc[1] ? neighbourChunks[1] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + 0 * BlockSettings.ChunkSize).BlocksN,
                blocks_MinX_MinY_PluZ = neighbourChunksAlloc[2] ? neighbourChunks[2] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,
                blocks_MinX_CurY_MinZ = neighbourChunksAlloc[3] ? neighbourChunks[3] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + 0 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_MinX_CurY_CurZ = neighbourChunksAlloc[4] ? neighbourChunks[4] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + 0 * BlockSettings.ChunkSize, chunk.pos.z + 0 * BlockSettings.ChunkSize).BlocksN,
                blocks_MinX_CurY_PluZ = neighbourChunksAlloc[5] ? neighbourChunks[5] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + 0 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,
                blocks_MinX_PluY_MinZ = neighbourChunksAlloc[6] ? neighbourChunks[6] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_MinX_PluY_CurZ = neighbourChunksAlloc[7] ? neighbourChunks[7] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + 0 * BlockSettings.ChunkSize).BlocksN,
                blocks_MinX_PluY_PluZ = neighbourChunksAlloc[8] ? neighbourChunks[8] : world.GetChunk(chunk.pos.x + -1 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,
                blocks_CurX_MinY_MinZ = neighbourChunksAlloc[9] ? neighbourChunks[9] : world.GetChunk(chunk.pos.x + 0 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_CurX_MinY_CurZ = neighbourChunksAlloc[10] ? neighbourChunks[10] : world.GetChunk(chunk.pos.x + 0 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + 0 * BlockSettings.ChunkSize).BlocksN,
                blocks_CurX_MinY_PluZ = neighbourChunksAlloc[11] ? neighbourChunks[11] : world.GetChunk(chunk.pos.x + 0 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,
                blocks_CurX_CurY_MinZ = neighbourChunksAlloc[12] ? neighbourChunks[12] : world.GetChunk(chunk.pos.x + 0 * BlockSettings.ChunkSize, chunk.pos.y + 0 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_CurX_CurY_PluZ = neighbourChunksAlloc[13] ? neighbourChunks[13] : world.GetChunk(chunk.pos.x + 0 * BlockSettings.ChunkSize, chunk.pos.y + 0 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,
                blocks_CurX_PluY_MinZ = neighbourChunksAlloc[14] ? neighbourChunks[14] : world.GetChunk(chunk.pos.x + 0 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_CurX_PluY_CurZ = neighbourChunksAlloc[15] ? neighbourChunks[15] : world.GetChunk(chunk.pos.x + 0 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + 0 * BlockSettings.ChunkSize).BlocksN,
                blocks_CurX_PluY_PluZ = neighbourChunksAlloc[16] ? neighbourChunks[16] : world.GetChunk(chunk.pos.x + 0 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_MinY_MinZ = neighbourChunksAlloc[17] ? neighbourChunks[17] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_MinY_CurZ = neighbourChunksAlloc[18] ? neighbourChunks[18] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + 0 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_MinY_PluZ = neighbourChunksAlloc[19] ? neighbourChunks[19] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + -1 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_CurY_MinZ = neighbourChunksAlloc[20] ? neighbourChunks[20] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + 0 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_CurY_CurZ = neighbourChunksAlloc[21] ? neighbourChunks[21] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + 0 * BlockSettings.ChunkSize, chunk.pos.z + 0 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_CurY_PluZ = neighbourChunksAlloc[22] ? neighbourChunks[22] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + 0 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_PluY_MinZ = neighbourChunksAlloc[23] ? neighbourChunks[23] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + -1 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_PluY_CurZ = neighbourChunksAlloc[24] ? neighbourChunks[24] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + 0 * BlockSettings.ChunkSize).BlocksN,
                blocks_PluX_PluY_PluZ = neighbourChunksAlloc[25] ? neighbourChunks[25] : world.GetChunk(chunk.pos.x + 1 * BlockSettings.ChunkSize, chunk.pos.y + 1 * BlockSettings.ChunkSize, chunk.pos.z + 1 * BlockSettings.ChunkSize).BlocksN,

                // MARCHING CUBES DATA TABLES
                Table_CubeEdgeFlags =       MarchingCubesTables.nativeArrayCubeEdgeFlags,
                Table_EdgeConnection =      MarchingCubesTables.nativeArrayEdgeConnection,
                Table_EdgeDirection =       MarchingCubesTables.nativeArrayEdgeDirection,
                Table_TriangleConnection =  MarchingCubesTables.nativeArrayTriangleConnectionTable,
                Table_VertexOffset =        MarchingCubesTables.nativeArrayVertexOffset

            }.Schedule();
                

            while (!Render_JobHandle.IsCompleted) 
            {
                if (chunk.isQueuedForDeletion)
                {
                    Render_JobHandle.Complete();
                    DisposeNatives();
                    yield break;
                }
                yield return null; 
            }

            Render_JobHandle.Complete();

            // Clear the mesh
            chunkFilter.mesh.Clear();
            chunkCollider.sharedMesh = null;

            if (nativeVertices.Length > 0)
            {
                // subMeshCount is actually how many materials you can use inside that particular mesh
                chunkFilter.mesh.subMeshCount = 5;

                // Vertices are shared between all subMeshes
                chunkFilter.mesh.vertices = nativeVertices.ToArray();
                // You have to set triangles for every subMesh you created, you can skip those if you want ofc.
                chunkFilter.mesh.SetTriangles(nativeTriangles.ToArray(), 0);
                chunkFilter.mesh.SetTriangles(nativeFoliageTriangles.ToArray(), 1);
                chunkFilter.mesh.SetTriangles(nativeMarchingCubesTriangles.ToArray(), 2);
                chunkFilter.mesh.SetTriangles(nativeMarchingCubesTransparentTriangles.ToArray(), 3);
                chunkFilter.mesh.SetTriangles(nativeLiquidTriangles.ToArray(), 4);
                Vector2[] uvs = nativeUVs.ToArray();

                chunkFilter.mesh.uv = uvs;
                chunkFilter.mesh.MarkDynamic();
                chunkFilter.mesh.RecalculateNormals();

                Mesh mesh = new Mesh();
                mesh.vertices = nativeVertices.ToArray();

                mesh.triangles = nativeTriangles.ToArray()
                    .Concat(nativeMarchingCubesTriangles.ToArray())
                    .Concat(nativeMarchingCubesTransparentTriangles.ToArray())
                    .ToArray();

                mesh.MarkDynamic();
                mesh.RecalculateNormals();

                chunkCollider.sharedMesh = mesh;
            }

            DisposeNatives();

            chunk.rendered = true;
            chunk.isRenderQueued = false;
            chunk.IsRendering = false;

            yield return null;
        }

        static bool CheckChunkAvailability(Chunk refChunk, int x, int y, int z)
        {
            int3 position = new int3(refChunk.pos.x + x * BlockSettings.ChunkSize, refChunk.pos.y + y * BlockSettings.ChunkSize, refChunk.pos.z + z * BlockSettings.ChunkSize);
            Chunk chunk = refChunk.world.GetChunk(position.x, position.y, position.z);

            if (!refChunk.world.CheckChunk(position)
                ||  chunk.BlocksN.Length != (int)math.pow(BlockSettings.ChunkSize, 3)
                || !chunk.generated
                ||  chunk.isGenerating
                ||  chunk.isWriting
                ||  chunk.isQueuedForDeletion
                || !chunk.WorldGen_JobHandle.IsCompleted)
                return false;
            return true;
        }

        // Enable burst compilation for better performace
        // Warning: when burst is ENABLED, you may not refference MonoBehaviour directly.
        // For example: Debug.Log("test"); WILL throw out an exception.
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
        private struct Job_RenderChunk : IJob
        {
            #region Declarations

            public int chunkSize, BlockTypes;                                                                                                               // Value of BlockTypes is BlockSettings.ByID.Count()
            public float chunkScale;                                                                                                                        // Scale of the chunk model (for rendering)
            public byte LevelofDetail;                                                                                                                      // Level of detail (range 1 - 4)
            public sbyte MarchingCubesLayerAmount;                                                                                                          // Amount of layers for Marching cubes algorithm
            public NativeList<Vector3> vertices;                                                                                                            // MeshData
            public NativeList<Vector2> uvs;                                                                                                                 // MeshData
            public NativeList<int>                                                                                                                          // MeshData
                triangles,
                trianglesLiquid,
                trianglesMarchingCubes,
                trianglesTransparentMarchingCubes,
                trianglesFoliage;
            [ReadOnly] public float tileSize;                                                                                                               // Size of a tile for texturing
            [ReadOnly] public NativeArray<BlockData> blocktype;                                                                                            // Blocktype data
            [ReadOnly]
            public NativeArray<BlockMetadata>
                blocks_CurX_CurY_CurZ, // Cur = Current, Plu = +1, Min = -1                                                                                 // Current chunk
                blocks_PluX_CurY_CurZ, blocks_MinX_CurY_CurZ, blocks_CurX_PluY_CurZ, blocks_CurX_MinY_CurZ, blocks_CurX_CurY_PluZ, blocks_CurX_CurY_MinZ,   // Direct neighbour chunks
                blocks_PluX_CurY_PluZ, blocks_MinX_CurY_MinZ, blocks_MinX_CurY_PluZ, blocks_PluX_CurY_MinZ,                                                 // Edge chunks (XZ)
                blocks_PluX_PluY_CurZ, blocks_MinX_MinY_CurZ, blocks_MinX_PluY_CurZ, blocks_PluX_MinY_CurZ,                                                 // Edge chunks (XY)
                blocks_CurX_PluY_PluZ, blocks_CurX_MinY_MinZ, blocks_CurX_MinY_PluZ, blocks_CurX_PluY_MinZ,                                                 // Edge chunks (YZ)
                blocks_PluX_PluY_PluZ, blocks_MinX_PluY_MinZ, blocks_MinX_PluY_PluZ, blocks_PluX_PluY_MinZ,                                                 // Corner chunks (XYZ)
                blocks_PluX_MinY_PluZ, blocks_MinX_MinY_MinZ, blocks_MinX_MinY_PluZ, blocks_PluX_MinY_MinZ;                                                 // Corner chunks (XYZ)
            [ReadOnly] public NativeArray<float> Table_EdgeDirection;                                                                                       // Lookup tables for Marching Cubes
            [ReadOnly] public NativeArray<int> Table_EdgeConnection, Table_CubeEdgeFlags, Table_TriangleConnection, Table_VertexOffset;

            enum FaceDirection : byte
            {
                North = 0,
                South,
                East,
                West,
                Up,
                Down,
            }

            #endregion

            #region Macro functions

            /// <summary>
            /// Calculates 3D coordinate to 1D array index
            /// <para>Redundant: Use GetBlock(int x, int y, int z)</para>
            /// </summary>
            /// <param name="x">X (0 to 15)</param>
            /// <param name="y">Y (0 to 15)</param>
            /// <param name="z">Z (0 to 15)</param>
            /// <param name="size">Size of the chunk (Default: 16)</param>
            /// <returns></returns>
            static int GetAddress(int x, int y, int z, int size)
            {
                return (x + y * size + z * size * size);
            }

            /// <summary>
            /// <para>For greedy meshing.</para>
            /// It rotates the coordinate inputs around to get correct block Address for given rotation.
            /// </summary>
            /// <param name="face1">First coordinate. By default (FaceDirection = Up) that's X</param>
            /// <param name="face2">First coordinate. By default (FaceDirection = Up) that's Z</param>
            /// <param name="face3">First coordinate. By default (FaceDirection = Up) that's Y</param>
            /// <param name="direction">Direction of the face you are checking right now</param>
            /// <param name="size">Size of the chunk. By default it's set to 16</param>
            /// <returns></returns>
            static int GetAddressWithDirection(int face1, int face2, int face3, FaceDirection direction, int size)
            {
                switch (direction)
                {
                    case FaceDirection.Up:
                    case FaceDirection.Down:
                        return GetAddress(face1, face2, face3, size); //XZ
                    case FaceDirection.East:
                    case FaceDirection.West:
                        return GetAddress(face1, face3, face2, size); //XY
                    case FaceDirection.North:
                    case FaceDirection.South:
                        return GetAddress(face2, face1, face3, size); //YZ
                }
                return 0;
            }

            /// <summary>
            /// This is the whole logic behind getting block from our chunks. It checks if the coordinates are out of bounds for our chunk and recalculates them to grab correct block from correct chunk.
            /// </summary>
            /// <param name="x">X relative coordinate</param>
            /// <param name="y">Y relative coordinate</param>
            /// <param name="z">Z relative coordinate</param>
            /// <returns></returns>
            BlockMetadata GetBlock(int x, int y, int z, byte lod)
            {
                int cs = chunkSize - 1;
                if (lod > 1)
                {
                    x *= lod;
                    y *= lod;
                    z *= lod;
                }
                if      (x >= 0 && x <= cs  && y >= 0   && y <= cs  && z >= 0   && z <= cs) return blocks_CurX_CurY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_CurY_CurZ[GetAddress(x,                y,                  z,              chunkSize)] : new BlockMetadata();
                else if (x > cs             && y >= 0   && y <= cs  && z >= 0   && z <= cs) return blocks_PluX_CurY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_CurY_CurZ[GetAddress(x - chunkSize,    y,                  z,              chunkSize)] : new BlockMetadata();
                else if (x < 0              && y >= 0   && y <= cs  && z >= 0   && z <= cs) return blocks_MinX_CurY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_CurY_CurZ[GetAddress(x + chunkSize,    y,                  z,              chunkSize)] : new BlockMetadata();
                else if (x >= 0 && x <= cs  && y > cs               && z >= 0   && z <= cs) return blocks_CurX_PluY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_PluY_CurZ[GetAddress(x,                y - chunkSize,      z,              chunkSize)] : new BlockMetadata();
                else if (x >= 0 && x <= cs  && y < 0                && z >= 0   && z <= cs) return blocks_CurX_MinY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_MinY_CurZ[GetAddress(x,                y + chunkSize,      z,              chunkSize)] : new BlockMetadata();
                else if (x >= 0 && x <= cs  && y >= 0   && y <= cs  && z > cs             ) return blocks_CurX_CurY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_CurY_PluZ[GetAddress(x,                y,                  z - chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x >= 0 && x <= cs  && y >= 0   && y <= cs  && z < 0              ) return blocks_CurX_CurY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_CurY_MinZ[GetAddress(x,                y,                  z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x < 0              && y >= 0   && y <= cs  && z < 0              ) return blocks_MinX_CurY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_CurY_MinZ[GetAddress(x + chunkSize,    y,                  z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x > cs             && y >= 0   && y <= cs  && z > cs             ) return blocks_PluX_CurY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_CurY_PluZ[GetAddress(x - chunkSize,    y,                  z - chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x > cs             && y >= 0   && y <= cs  && z < 0              ) return blocks_PluX_CurY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_CurY_MinZ[GetAddress(x - chunkSize,    y,                  z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x < 0              && y >= 0   && y <= cs  && z > cs             ) return blocks_MinX_CurY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_CurY_PluZ[GetAddress(x + chunkSize,    y,                  z - chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x < 0              && y < 0                && z >= 0   && z <= cs) return blocks_MinX_MinY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_MinY_CurZ[GetAddress(x + chunkSize,    y + chunkSize,      z,              chunkSize)] : new BlockMetadata();
                else if (x > cs             && y > cs               && z >= 0   && z <= cs) return blocks_PluX_PluY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_PluY_CurZ[GetAddress(x - chunkSize,    y - chunkSize,      z,              chunkSize)] : new BlockMetadata();
                else if (x > cs             && y < 0                && z >= 0   && z <= cs) return blocks_PluX_MinY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_MinY_CurZ[GetAddress(x - chunkSize,    y + chunkSize,      z,              chunkSize)] : new BlockMetadata();
                else if (x < 0              && y > cs               && z >= 0   && z <= cs) return blocks_MinX_PluY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_PluY_CurZ[GetAddress(x + chunkSize,    y - chunkSize,      z,              chunkSize)] : new BlockMetadata();
                else if (x >= 0 && x <= cs  && y > cs               && z > cs             ) return blocks_CurX_PluY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_PluY_PluZ[GetAddress(x,                y - chunkSize,      z - chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x >= 0             && x <= cs  && y < 0    && z < 0              ) return blocks_CurX_MinY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_MinY_MinZ[GetAddress(x,                y + chunkSize,      z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x >= 0             && x <= cs  && y < 0    && z > cs             ) return blocks_CurX_MinY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_MinY_PluZ[GetAddress(x,                y + chunkSize,      z - chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x >= 0             && x <= cs  && y > cs   && z < 0              ) return blocks_CurX_PluY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_PluY_MinZ[GetAddress(x,                y - chunkSize,      z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x < 0              && y < 0                && z < 0              ) return blocks_MinX_MinY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_MinY_MinZ[GetAddress(x + chunkSize,    y + chunkSize,      z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x < 0              && y > cs               && z < 0              ) return blocks_MinX_PluY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_PluY_MinZ[GetAddress(x + chunkSize,    y - chunkSize,      z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x > cs             && y > cs               && z > cs             ) return blocks_PluX_PluY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_PluY_PluZ[GetAddress(x - chunkSize,    y - chunkSize,      z - chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x > cs             && y < 0                && z > cs             ) return blocks_PluX_MinY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_MinY_PluZ[GetAddress(x - chunkSize,    y + chunkSize,      z - chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x > cs             && y < 0                && z < 0              ) return blocks_PluX_MinY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_MinY_MinZ[GetAddress(x - chunkSize,    y + chunkSize,      z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x > cs             && y > cs               && z < 0              ) return blocks_PluX_PluY_MinZ.Length == (int)math.pow(chunkSize, 3) ? blocks_PluX_PluY_MinZ[GetAddress(x - chunkSize,    y - chunkSize,      z + chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x < 0              && y < 0                && z > cs             ) return blocks_MinX_MinY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_MinY_PluZ[GetAddress(x + chunkSize,    y + chunkSize,      z - chunkSize,  chunkSize)] : new BlockMetadata();
                else if (x < 0              && y > cs               && z > cs             ) return blocks_MinX_PluY_PluZ.Length == (int)math.pow(chunkSize, 3) ? blocks_MinX_PluY_PluZ[GetAddress(x + chunkSize,    y - chunkSize,      z - chunkSize,  chunkSize)] : new BlockMetadata();
                else                                                                        return blocks_CurX_CurY_CurZ.Length == (int)math.pow(chunkSize, 3) ? blocks_CurX_CurY_CurZ[GetAddress(x,                y,                  z,              chunkSize)] : new BlockMetadata();
            }

            /// <summary>
            /// <para>For greedy meshing.</para>
            /// It rotates the coordinate inputs around to get correct block Address for given rotation.
            /// </summary>
            /// <param name="face1">First coordinate. By default (FaceDirection = Up) that's X</param>
            /// <param name="face2">First coordinate. By default (FaceDirection = Up) that's Z</param>
            /// <param name="face3">First coordinate. By default (FaceDirection = Up) that's Y</param>
            /// <param name="direction">Direction of the face you are checking right now</param>
            /// <returns></returns>
            BlockMetadata GetBlockWithDirection(int face1, int face2, int face3, FaceDirection direction)
            {
                switch (direction)
                {
                    case FaceDirection.Up:
                    case FaceDirection.Down:
                        return GetBlock(face1, face2, face3, LevelofDetail); //XZY
                    case FaceDirection.East:
                    case FaceDirection.West:
                        return GetBlock(face1, face3, face2, LevelofDetail); //XYZ
                    case FaceDirection.North:
                    case FaceDirection.South:
                        return GetBlock(face2, face1, face3, LevelofDetail); //YZX
                    default:
                        return new BlockMetadata();
                }
            }

            int3 GetBlockLocationFromDirection(int x, int y, int z, FaceDirection direction)
            {
                switch (direction)
                {
                    default:
                    case FaceDirection.Up:
                    case FaceDirection.Down:
                        return new int3(x, z, y);
                    case FaceDirection.East:
                    case FaceDirection.West:
                        return new int3(x, y, z);
                    case FaceDirection.North:
                    case FaceDirection.South:
                        return new int3(y, z, x);
                }
            }

            /// <summary>
            /// Macro to easily create quad from last two triangles.
            /// </summary>
            /// <param name="triangles">Your NativeList of triangles</param>
            void AddQuadTriangles(ref NativeList<int> triangles)
            {
                triangles.Add(vertices.Length - 4);
                triangles.Add(vertices.Length - 3);
                triangles.Add(vertices.Length - 2);
                triangles.Add(vertices.Length - 4);
                triangles.Add(vertices.Length - 2);
                triangles.Add(vertices.Length - 1);
            }

            /// <summary>
            /// Used for both regular blocks and greedy meshing.
            /// <para>It performs calculations relative to coordinate inputs and rotation in order to create correct vertices.</para>
            /// </summary>
            /// <param name="face1">First coordinate. By default (FaceDirection = Up) that's X</param>
            /// <param name="face2">First coordinate. By default (FaceDirection = Up) that's Z</param>
            /// <param name="face3">First coordinate. By default (FaceDirection = Up) that's Y</param>
            /// <param name="direction">Direction of the face you are checking right now</param>
            void AddRelativeVertices(float2 face1, float face2, float2 face3, FaceDirection direction, float liquidHeight = 1f)
            {
                if (chunkScale != 1f)
                {
                    face1 = new float2(face1.x * chunkScale, face1.y * chunkScale);
                    face2 *= chunkScale;
                    face3 = new float2(face3.x * chunkScale, face3.y * chunkScale);
                }

                switch (direction)
                {
                    case FaceDirection.Up:
                        vertices.Add(new Vector3(face1.x - 0.5f * chunkScale, face2 + 0.5f * chunkScale * liquidHeight, face3.y + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.y + 0.5f * chunkScale, face2 + 0.5f * chunkScale * liquidHeight, face3.y + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.y + 0.5f * chunkScale, face2 + 0.5f * chunkScale * liquidHeight, face3.x - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.x - 0.5f * chunkScale, face2 + 0.5f * chunkScale * liquidHeight, face3.x - 0.5f * chunkScale));
                        break;
                    case FaceDirection.Down:
                        vertices.Add(new Vector3(face1.x - 0.5f * chunkScale, face2 - 0.5f * chunkScale, face3.x - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.y + 0.5f * chunkScale, face2 - 0.5f * chunkScale, face3.x - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.y + 0.5f * chunkScale, face2 - 0.5f * chunkScale, face3.y + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.x - 0.5f * chunkScale, face2 - 0.5f * chunkScale, face3.y + 0.5f * chunkScale));
                        break;
                    case FaceDirection.East:
                        vertices.Add(new Vector3(face1.y + 0.5f * chunkScale, face3.x - 0.5f * chunkScale, face2 + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.y + 0.5f * chunkScale, face3.y + 0.5f * chunkScale * liquidHeight, face2 + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.x - 0.5f * chunkScale, face3.y + 0.5f * chunkScale * liquidHeight, face2 + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.x - 0.5f * chunkScale, face3.x - 0.5f * chunkScale, face2 + 0.5f * chunkScale));
                        break;
                    case FaceDirection.West:
                        vertices.Add(new Vector3(face1.x - 0.5f * chunkScale, face3.x - 0.5f * chunkScale, face2 - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.x - 0.5f * chunkScale, face3.y + 0.5f * chunkScale * liquidHeight, face2 - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.y + 0.5f * chunkScale, face3.y + 0.5f * chunkScale * liquidHeight, face2 - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face1.y + 0.5f * chunkScale, face3.x - 0.5f * chunkScale, face2 - 0.5f * chunkScale));
                        break;
                    case FaceDirection.North:
                        vertices.Add(new Vector3(face2 + 0.5f * chunkScale, face1.x - 0.5f * chunkScale, face3.x - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face2 + 0.5f * chunkScale, face1.y + 0.5f * chunkScale * liquidHeight, face3.x - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face2 + 0.5f * chunkScale, face1.y + 0.5f * chunkScale * liquidHeight, face3.y + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face2 + 0.5f * chunkScale, face1.x - 0.5f * chunkScale, face3.y + 0.5f * chunkScale));
                        break;
                    case FaceDirection.South:
                        vertices.Add(new Vector3(face2 - 0.5f * chunkScale, face1.x - 0.5f * chunkScale, face3.y + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face2 - 0.5f * chunkScale, face1.y + 0.5f * chunkScale * liquidHeight, face3.y + 0.5f * chunkScale));
                        vertices.Add(new Vector3(face2 - 0.5f * chunkScale, face1.y + 0.5f * chunkScale * liquidHeight, face3.x - 0.5f * chunkScale));
                        vertices.Add(new Vector3(face2 - 0.5f * chunkScale, face1.x - 0.5f * chunkScale, face3.x - 0.5f * chunkScale));
                        break;
                }
            }

            void AddRelativeVertices(int3 position, FaceDirection direction, float liquidHeight = 1f)
            {
                AddRelativeVertices(new float2(position.x, position.x), position.y, new float2(position.z, position.z), direction, liquidHeight);
            }

            /// <summary>
            /// Apply texture to quad created with AddQuadTriangles()
            /// </summary>
            /// <param name="block">Block Metadata to refference textures</param>
            /// <param name="direction">Direction of the face</param>
            void TextureTheQuad(BlockMetadata block, FaceDirection direction, bool liquid = false)
            {
                float customTileSize = liquid ? 1f : tileSize;
                switch (direction)
                {
                    case FaceDirection.Up:
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Up.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Up.y) + 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Up.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Up.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Up.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Up.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Up.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Up.y) + 0.005f));
                        break;
                    case FaceDirection.Down:
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Down.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Down.y) + 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Down.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Down.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Down.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Down.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Down.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_Down.y) + 0.005f));
                        break;
                    case FaceDirection.East:
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_East.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_East.y) + 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_East.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_East.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_East.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_East.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_East.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_East.y) + 0.005f));
                        break;
                    case FaceDirection.West:
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_West.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_West.y) + 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_West.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_West.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_West.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_West.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_West.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_West.y) + 0.005f));
                        break;
                    case FaceDirection.North:
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_North.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_North.y) + 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_North.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_North.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_North.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_North.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_North.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_North.y) + 0.005f));
                        break;
                    case FaceDirection.South:
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_South.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_South.y) + 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_South.x) + customTileSize - 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_South.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_South.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_South.y) + customTileSize - 0.005f));
                        uvs.Add(new Vector2(customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_South.x) + 0.005f, customTileSize * (liquid ? 0f : blocktype[block.ID].Texture_South.y) + 0.005f));
                        break;
                }
            }

            /// <summary>
            /// Greedy Meshing algorithm.
            /// <para>This algorithm optimizes the mesh by reducing number of both vertices and triangles.</para>
            /// </summary>
            /// <param name="x">Chunk relative X coordinate</param>
            /// <param name="y">Chunk relative Y coordinate</param>
            /// <param name="z">Chunk relative Z coordinate</param>
            /// <param name="blocks_greedy">NativeArray of booleans as a refference table
            /// <para>"I've been here before, stop the iterating"</para></param>
            /// <param name="direction">Face direction of the block</param>
            void PerformGreedyMeshing(int x, int y, int z, ref NativeArray<boolean> blocks_greedy, FaceDirection direction)
            {
                BlockMetadata originBlock = GetBlock(x, y, z, LevelofDetail),
                              neighbourOriginBlock = GetBlock(x, y, z, LevelofDetail); // temporary assignment

                int face1 = int.MinValue, // Default values will crash or cause errors if SOMEHOW they stay this way
                    face2 = int.MinValue,
                    face3 = int.MinValue,
                    neighbourModifier = int.MinValue;

                switch (direction)
                {
                    case FaceDirection.Up: neighbourOriginBlock = GetBlock(x, y + 1, z, LevelofDetail); face1 = x; face2 = z; face3 = y; neighbourModifier = 1; break;
                    case FaceDirection.Down: neighbourOriginBlock = GetBlock(x, y - 1, z, LevelofDetail); face1 = x; face2 = z; face3 = y; neighbourModifier = -1; break;
                    case FaceDirection.East: neighbourOriginBlock = GetBlock(x, y, z + 1, LevelofDetail); face1 = x; face2 = y; face3 = z; neighbourModifier = 1; break;
                    case FaceDirection.West: neighbourOriginBlock = GetBlock(x, y, z - 1, LevelofDetail); face1 = x; face2 = y; face3 = z; neighbourModifier = -1; break;
                    case FaceDirection.North: neighbourOriginBlock = GetBlock(x + 1, y, z, LevelofDetail); face1 = y; face2 = z; face3 = x; neighbourModifier = 1; break;
                    case FaceDirection.South: neighbourOriginBlock = GetBlock(x - 1, y, z, LevelofDetail); face1 = y; face2 = z; face3 = x; neighbourModifier = -1; break;
                }

                if (
                    blocktype[originBlock.ID].Solid &&
                    (
                        !blocktype[neighbourOriginBlock.ID].Solid
                        || neighbourOriginBlock.Switches.Get(BlockSwitches.Marched)
                        || blocktype[neighbourOriginBlock.ID].Foliage
                        ||
                        (
                            blocktype[neighbourOriginBlock.ID].CullingMode == 2
                            && neighbourOriginBlock.ID != originBlock.ID
                        )
                        || blocktype[neighbourOriginBlock.ID].CullingMode == 1
                    )
                    && !blocks_greedy[GetAddress(x, y, z, chunkSize)]
                )
                {
                    int face2Max = chunkSize / LevelofDetail - 1,    // Declare maximum value for Z, just for first iteration it has to be ChunkSize-1
                        face2MaxTemporary = chunkSize / LevelofDetail - 1,    // Temporary maximum value for Z, gets bigger only at first iteration and then moves into max_z
                        face1Greed = face1;                            // greed_x is just a value to store vertex's max X position
                    bool broken = false;                                        // and last but not least, broken. This is needed to break out of both loops (X and Z)

                    // Iterate Face1 -> ChunkSize - 1
                    for (int greedyFace1 = face1; greedyFace1 < chunkSize / LevelofDetail; greedyFace1++)
                    {
                        // Check if current's iteration Face1 is bigger than starting Face1 and if first block in this iteration is the same, if not - break.
                        if (greedyFace1 > face1)
                        {
                            if (GetBlockWithDirection(greedyFace1, face3, face2, direction).ID != originBlock.ID) break;
                            if (blocktype[GetBlockWithDirection(greedyFace1, face3, face2, direction).ID].Foliage) break;
                            if (GetBlockWithDirection(greedyFace1, face3, face2, direction).Switches.Get(BlockSwitches.Marched)) break;
                        }

                        // Iterate Face2 -> Face2_Max (ChunkSize - 1 for first iteration)
                        for (int greedyFace2 = face2; greedyFace2 <= face2Max; greedyFace2++)
                        {
                            // Get info about neighbour block
                            bool Neighbour = false;

                            BlockMetadata neighbourBlock = GetBlockWithDirection(greedyFace1, face3 + neighbourModifier, greedyFace2, direction),
                                          workerBlock = GetBlockWithDirection(greedyFace1, face3, greedyFace2, direction);

                            if (blocktype[neighbourBlock.ID].CullingMode != 1 && blocktype[neighbourBlock.ID].CullingMode != 2
                                    && !neighbourBlock.Switches.Get(BlockSwitches.Marched)
                                    && neighbourBlock.ID > 0
                                    && !blocktype[neighbourBlock.ID].Foliage)
                                Neighbour = blocktype[neighbourBlock.ID].Solid; // Check if the neighbour block is solid or not

                            // Check if this block is marked as greedy, compare blockID with starting block and check if Neighbour is solid.
                            if (!blocks_greedy[GetAddressWithDirection(greedyFace1, face3, greedyFace2, direction, chunkSize)]
                                && originBlock.ID == workerBlock.ID
                                && !Neighbour
                                && !workerBlock.Switches.Get(BlockSwitches.Marched)
                                && !blocktype[workerBlock.ID].Foliage)
                            {
                                // Set the temporary value of Max_Z to current iteration of Z
                                if (greedyFace1 == face1) face2MaxTemporary = greedyFace2;
                                // Mark the current block as greedy
                                blocks_greedy[GetAddressWithDirection(greedyFace1, face3, greedyFace2, direction, chunkSize)] = true;
                            }
                            else
                            {
                                // If block in current iteration was different or already greedy, break
                                // Then, reverse last iteration to non-greedy state.
                                if (greedyFace2 <= face2Max && greedyFace1 > face1)
                                {
                                    // Reverse the greedy to false
                                    for (int face2Revert = face2; face2Revert < greedyFace2; face2Revert++)
                                        blocks_greedy[GetAddressWithDirection(greedyFace1, face3, face2Revert, direction, chunkSize)] = false;

                                    // Break out of both loops
                                    broken = true;
                                }
                                break;
                            }
                        }
                        // Next, after iterations are finished (or broken), move the temporary value of Max_Z to non-temporary
                        face2Max = face2MaxTemporary;
                        if (broken) break;
                        // If both loops weren't broken, set vertex' max Face1 value to current Face1 iteration
                        face1Greed = greedyFace1;
                    }

                    // Create the vertices
                    AddRelativeVertices(new float2(face1, face1Greed), face3, new float2(face2, face2Max), direction);

                    // Create Quad and texture it
                    AddQuadTriangles(ref triangles);
                    TextureTheQuad(originBlock, direction);
                }
            }

            /// <summary>
            /// Find largest integer in NativeArray(int)
            /// </summary>
            /// <param name="array"></param>
            /// <param name="Largest"></param>
            void FindLargestValueInArray(NativeArray<int> array, out int Largest, int Except = 0)
            {
                NativeArray<int> counter = new NativeArray<int>(blocktype.Length, Allocator.Temp);  // Get the block ID that appears the most in the array.
                int largest;

                for (int i = 0; i < array.Length; i++)
                    if (array[i] != 0 && array[i] < array.Length)
                        counter[array[i]]++;                                                   // Use value from numbers as the index for Count and increment the count

                largest = counter[array[0]];
                Largest = array[0];

                for (int i = 0; i < array.Length; i++)
                    if (largest < counter[array[i]] && array[i] != Except)
                    {
                        largest = counter[array[i]];
                        Largest = array[i];
                    }
                counter.Dispose();
            }

            void SetMarchingTriangles(sbyte state, int triangle)
            {
                if (state == 1)
                    trianglesTransparentMarchingCubes.Add(triangle);
                else
                    trianglesMarchingCubes.Add(triangle);
            }

            #endregion

            public void Execute()
            {
                NativeArray<boolean> // Greedy mesh flag for every direction of block                                                                                           
                    greedyTilesUp = new NativeArray<boolean>((int)math.pow(BlockSettings.ChunkSize, 3), Allocator.Temp),
                    greedyTilesDown = new NativeArray<boolean>((int)math.pow(BlockSettings.ChunkSize, 3), Allocator.Temp),
                    greedyTilesNorth = new NativeArray<boolean>((int)math.pow(BlockSettings.ChunkSize, 3), Allocator.Temp),
                    greedyTilesSouth = new NativeArray<boolean>((int)math.pow(BlockSettings.ChunkSize, 3), Allocator.Temp),
                    greedyTilesEast = new NativeArray<boolean>((int)math.pow(BlockSettings.ChunkSize, 3), Allocator.Temp),
                    greedyTilesWest = new NativeArray<boolean>((int)math.pow(BlockSettings.ChunkSize, 3), Allocator.Temp);

                Unity.Mathematics.Random rand = new Unity.Mathematics.Random(0x6E624EB7u);

                float surface = 0.5f;
                NativeArray<float> cube = new NativeArray<float>(8, Allocator.Temp);
                NativeArray<int> windingOrder = new NativeArray<int>(3, Allocator.Temp);
                windingOrder[0] = 0;
                windingOrder[1] = 1;
                windingOrder[2] = 2;

                if (surface > 0.0f)
                {
                    windingOrder[0] = 0;
                    windingOrder[1] = 1;
                    windingOrder[2] = 2;
                }
                else
                {
                    windingOrder[0] = 2;
                    windingOrder[1] = 1;
                    windingOrder[2] = 0;
                }

                for (int x = 0; x < chunkSize / LevelofDetail; x++)
                    for (int y = 0; y < chunkSize / LevelofDetail; y++)
                        for (int z = 0; z < chunkSize / LevelofDetail; z++)
                        {
                            if (GetBlock(x, y, z, LevelofDetail).ID != 0 && !GetBlock(x, y, z, LevelofDetail).Switches.Get(BlockSwitches.Marched))
                            {
                                BlockMetadata originBlock = GetBlock(x, y, z, LevelofDetail);

                                if (!blocktype[originBlock.ID].Foliage && !blocktype[originBlock.ID].Liquid)
                                {
                                    if (!originBlock.Switches.Get(BlockSwitches.Marched))
                                    {
                                        // Preform greedy meshing for all faces available
                                        PerformGreedyMeshing(x, y, z, ref greedyTilesUp, FaceDirection.Up);
                                        PerformGreedyMeshing(x, y, z, ref greedyTilesDown, FaceDirection.Down);
                                        PerformGreedyMeshing(x, y, z, ref greedyTilesEast, FaceDirection.East);
                                        PerformGreedyMeshing(x, y, z, ref greedyTilesWest, FaceDirection.West);
                                        PerformGreedyMeshing(x, y, z, ref greedyTilesNorth, FaceDirection.North);
                                        PerformGreedyMeshing(x, y, z, ref greedyTilesSouth, FaceDirection.South);
                                    }
                                }
                                else if (blocktype[originBlock.ID].Foliage && !blocktype[originBlock.ID].Liquid)
                                {
                                    if (LevelofDetail == 1) // LOD needs to be at 1, so we don't get omegaupscaled foliage blocks
                                    {
                                        // Foliage block offsets
                                        float
                                            offsetX = rand.NextFloat(-0.25f, 0.25f) * chunkScale,
                                            offsetY = rand.NextFloat(-0.05f, 0.05f) * chunkScale,
                                            offsetZ = rand.NextFloat(-0.25f, 0.25f) * chunkScale;

                                        // This isn't standard block. This is the cause of writing this part of renderer manually instead of macroing it.
                                        if (blocktype[originBlock.ID].Solid)
                                        {
                                            vertices.Add(new Vector3(x + 0.5f * chunkScale + offsetX, y - 1f * chunkScale + offsetY, z + 0.5f * chunkScale + offsetZ));
                                            vertices.Add(new Vector3(x + 0.5f * chunkScale + offsetX, y + 0.5f * chunkScale + offsetY, z + 0.5f * chunkScale + offsetZ));
                                            vertices.Add(new Vector3(x - 0.5f * chunkScale + offsetX, y + 0.5f * chunkScale + offsetY, z - 0.5f * chunkScale + offsetZ));
                                            vertices.Add(new Vector3(x - 0.5f * chunkScale + offsetX, y - 1f * chunkScale + offsetY, z - 0.5f * chunkScale + offsetZ));

                                            AddQuadTriangles(ref trianglesFoliage);

                                            uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_Up.x + tileSize - 0.005f, tileSize * blocktype[originBlock.ID].Texture_Up.y + 0.005f));
                                            uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_Up.x + tileSize - 0.005f, tileSize * blocktype[originBlock.ID].Texture_Up.y + tileSize - 0.005f));
                                            uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_Up.x + 0.005f, tileSize * blocktype[originBlock.ID].Texture_Up.y + tileSize - 0.005f));
                                            uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_Up.x + 0.005f, tileSize * blocktype[originBlock.ID].Texture_Up.y + 0.005f));

                                            vertices.Add(new Vector3(x - 0.5f * chunkScale + offsetX, y - 1f * chunkScale + offsetY, z + 0.5f * chunkScale + offsetZ));
                                            vertices.Add(new Vector3(x - 0.5f * chunkScale + offsetX, y + 0.5f * chunkScale + offsetY, z + 0.5f * chunkScale + offsetZ));
                                            vertices.Add(new Vector3(x + 0.5f * chunkScale + offsetX, y + 0.5f * chunkScale + offsetY, z - 0.5f * chunkScale + offsetZ));
                                            vertices.Add(new Vector3(x + 0.5f * chunkScale + offsetX, y - 1f * chunkScale + offsetY, z - 0.5f * chunkScale + offsetZ));

                                            AddQuadTriangles(ref trianglesFoliage);

                                            uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_South.x + tileSize - 0.005f, tileSize * blocktype[originBlock.ID].Texture_South.y + 0.005f));
                                            uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_South.x + tileSize - 0.005f, tileSize * blocktype[originBlock.ID].Texture_South.y + tileSize - 0.005f));
                                            uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_South.x + 0.005f, tileSize * blocktype[originBlock.ID].Texture_South.y + tileSize - 0.005f));
                                            uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_South.x + 0.005f, tileSize * blocktype[originBlock.ID].Texture_South.y + 0.005f));
                                        }
                                    }
                                }
                                else if (!blocktype[originBlock.ID].Foliage && blocktype[originBlock.ID].Liquid)
                                {
                                    float liquidHeight = GetBlock(x, y + 1, z, LevelofDetail).ID == originBlock.ID ? 1f : 0.8f;
                                    //vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
                                    //vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                                    //vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
                                    //vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
                                    //AddQuadTriangles(ref trianglesLiquid);
                                    //TextureTheQuad(originBlock, FaceDirection.Up);

                                    if (GetBlock(x, y + 1, z, LevelofDetail).ID != originBlock.ID)
                                    {
                                        AddRelativeVertices(new int3(x, y, z), FaceDirection.Up, liquidHeight);
                                        AddQuadTriangles(ref trianglesLiquid);
                                        TextureTheQuad(originBlock, FaceDirection.Up, true);
                                    }
                                    if (GetBlock(x, y - 1, z, LevelofDetail).ID == 0)
                                    {
                                        AddRelativeVertices(new int3(x, y, z), FaceDirection.Down, liquidHeight);
                                        AddQuadTriangles(ref trianglesLiquid);
                                        TextureTheQuad(originBlock, FaceDirection.Down, true);
                                    }
                                    if (GetBlock(x + 1, y, z, LevelofDetail).ID == 0)
                                    {
                                        AddRelativeVertices(new int3(y, x, z), FaceDirection.North, liquidHeight);
                                        AddQuadTriangles(ref trianglesLiquid);
                                        TextureTheQuad(originBlock, FaceDirection.North, true);
                                    }
                                    if (GetBlock(x - 1, y, z, LevelofDetail).ID == 0)
                                    {
                                        AddRelativeVertices(new int3(y, x, z), FaceDirection.South, liquidHeight);
                                        AddQuadTriangles(ref trianglesLiquid);
                                        TextureTheQuad(originBlock, FaceDirection.South, true);
                                    }
                                    if (GetBlock(x, y, z + 1, LevelofDetail).ID == 0)
                                    {
                                        AddRelativeVertices(new int3(x, z, y), FaceDirection.East, liquidHeight);
                                        AddQuadTriangles(ref trianglesLiquid);
                                        TextureTheQuad(originBlock, FaceDirection.East, true);
                                    }
                                    if (GetBlock(x, y, z - 1, LevelofDetail).ID == 0)
                                    {
                                        AddRelativeVertices(new int3(x, z, y), FaceDirection.West, liquidHeight);
                                        AddQuadTriangles(ref trianglesLiquid);
                                        TextureTheQuad(originBlock, FaceDirection.West, true);
                                    }
                                    //advanced (minecraft infdev+) liquid block
                                }
                            }

                            #region Marching Cubes
                            for (sbyte layer = 0; layer < MarchingCubesLayerAmount; layer++)
                            {
                                #region Marching Cubes Algorithm

                                //Get the values in the 8 neighbours which make up a cube
                                for (int i = 0; i < 8; i++)
                                {
                                    int ix = x + Table_VertexOffset[i * 3 + 0];
                                    int iy = y + Table_VertexOffset[i * 3 + 1];
                                    int iz = z + Table_VertexOffset[i * 3 + 2];

                                    if (!blocktype[GetBlock(ix, iy, iz, LevelofDetail).ID].Foliage && blocktype[GetBlock(ix, iy, iz, LevelofDetail).ID].MarchingCubesLayer == layer)
                                        cube[i] = GetBlock(ix, iy, iz, LevelofDetail).GetMarchedValue();
                                    else
                                        cube[i] = 0f;
                                }

                                NativeArray<float3> edgeVertex = new NativeArray<float3>(12, Allocator.Temp);
                                int flagIndex = 0;

                                for (int i2 = 0; i2 < 8; i2++) if (cube[i2] <= surface) flagIndex |= 1 << i2;   // Find which vertices are inside of the surface and which are outside
                                int edgeFlags = Table_CubeEdgeFlags[flagIndex];                                 // Find which edges are intersected by the surface
                                if (edgeFlags != 0)                                                             // If the cube is entirely inside or outside of the surface, then there will be no intersections
                                {
                                    for (int i2 = 0; i2 < 12; i2++)                                             // Find the point of intersection of the surface with each edge
                                        if ((edgeFlags & (1 << i2)) != 0)                                       // if there is an intersection on this edge
                                        {
                                            float delta = cube[Table_EdgeConnection[i2 * 2 + 1]] - cube[Table_EdgeConnection[i2 * 2 + 0]];
                                            float offset = (delta == 0.0f) ? surface : (surface - cube[Table_EdgeConnection[i2 * 2 + 0]]) / delta;
                                            float3 ev = new float3(
                                                x + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 0] + offset * Table_EdgeDirection[i2 * 3 + 0]),
                                                y + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 1] + offset * Table_EdgeDirection[i2 * 3 + 1]),
                                                z + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 2] + offset * Table_EdgeDirection[i2 * 3 + 2]));
                                            ev *= LevelofDetail;
                                            ev *= chunkScale;
                                            edgeVertex[i2] = ev;
                                        }

                                    int trisfound = 0;
                                    for (int i2 = 0; i2 < 5; i2++)                                              // Save the triangles that were found. There can be up to five per cube
                                    {
                                        if (Table_TriangleConnection[flagIndex * chunkSize + (3 * i2)] < 0) break;
                                        trisfound += 1;
                                        int idx = vertices.Length;

                                        for (int j = 0; j < 3; j++)
                                        {
                                            Vector2 uvPosition = new Vector2(0f, 0f);

                                            NativeList<int> blockIDs = new NativeList<int>(Allocator.Temp);
                                            for (int k = 0; k < 3; k++)
                                            {
                                                int3 tempCoordinates = new int3(
                                                    (int)math.round(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].x / LevelofDetail / chunkScale),
                                                    (int)math.round(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].y / LevelofDetail / chunkScale),
                                                    (int)math.round(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].z / LevelofDetail / chunkScale));

                                                if (GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID > 0 &&
                                                    GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).Switches.Get(BlockSwitches.Marched))
                                                    blockIDs.Add(GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID);
                                            }

                                            if (blockIDs.Length < 1)
                                            {
                                                for (int k = 0; k < 3; k++)
                                                {
                                                    int3 tempCoordinates = new int3(
                                                        (int)math.floor(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].x / LevelofDetail / chunkScale),
                                                        (int)math.floor(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].y / LevelofDetail / chunkScale),
                                                        (int)math.floor(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].z / LevelofDetail / chunkScale));

                                                    if (GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID > 0 &&
                                                        GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).Switches.Get(BlockSwitches.Marched))
                                                        blockIDs.Add(GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID);
                                                }
                                                if (blockIDs.Length < 1)
                                                {
                                                    for (int k = 0; k < 3; k++)
                                                    {
                                                        int3 tempCoordinates = new int3(
                                                            (int)math.ceil(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].x / LevelofDetail / chunkScale),
                                                            (int)math.ceil(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].y / LevelofDetail / chunkScale),
                                                            (int)math.ceil(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + k)]].z / LevelofDetail / chunkScale));

                                                        if (GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID > 0 &&
                                                            GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).Switches.Get(BlockSwitches.Marched))
                                                            blockIDs.Add(GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID);
                                                    }
                                                }
                                            }

                                            if (blockIDs.Length > 1)
                                            {
                                                FindLargestValueInArray(blockIDs, out int largest_id, 0);
                                                uvPosition = new Vector2(tileSize * blocktype[largest_id].Texture_Marched.x + tileSize - 0.005f, tileSize * blocktype[largest_id].Texture_Marched.y + tileSize - 0.005f);

                                            }
                                            else if (blockIDs.Length == 1)
                                                uvPosition = new Vector2(tileSize * blocktype[blockIDs[0]].Texture_Marched.x + tileSize - 0.005f, tileSize * blocktype[blockIDs[0]].Texture_Marched.y + 0.005f);

                                            blockIDs.Dispose();

                                            SetMarchingTriangles(layer, idx + windingOrder[j]);
                                            vertices.Add(edgeVertex[Table_TriangleConnection[flagIndex * chunkSize + (3 * i2 + j)]]);
                                            uvs.Add(uvPosition);
                                        }
                                    }
                                }
                                edgeVertex.Dispose();
                                #endregion
                            }


                            #endregion
                        }

                cube.Dispose();
                windingOrder.Dispose();
                greedyTilesUp.Dispose();
                greedyTilesDown.Dispose();
                greedyTilesNorth.Dispose();
                greedyTilesSouth.Dispose();
                greedyTilesEast.Dispose();
                greedyTilesWest.Dispose();
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using CielaSpike;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    #region Declarations
    [HideInInspector] public World world;
    [HideInInspector] public int3 pos;
    [HideInInspector] public BlockMetadata[] Blocks = new BlockMetadata[(int)math.pow(BlockData.ChunkSize, 3)];

    #region Chunk generation
    [HideInInspector] public JobHandle WorldGen_JobHandle = new JobHandle();
    [HideInInspector] public NativeArray<BlockMetadata> nBlocks;
    [HideInInspector] public bool generated = false;
    #endregion

    #region Chunk rendering
    // Level of Detail for rendering far chunks (the higher number, the less detail), default 1.
    [HideInInspector] public byte LOD = 1;
    byte PreviousLOD = 1;

    // Job system
    NativeArray<BlockMetadata> Native_blocks;
    NativeArray<BlockTypes> blocktypes;
    public bool rendered;
    
    private JobHandle Render_JobHandle = new JobHandle();
    NativeList<Vector3> verts;
    NativeList<int> tris;
    NativeList<Vector2> uv;
    NativeList<int> March_tris;
    NativeList<int> March_transparent_tris;
    NativeList<int> Foliage_tris;
    [HideInInspector] public bool isMapgenUpdate = true;
    private bool IsRendering = false;
    
    // Mesh info
    MeshFilter filter;
    MeshCollider coll;
    #endregion

    #region Safety - Multithreading
    bool _ForceUpdate = false;
    #endregion

    #endregion

    void Awake()
    {
        filter = gameObject.GetComponent<MeshFilter>();
        coll = gameObject.GetComponent<MeshCollider>();

        blocktypes = new NativeArray<BlockTypes>(BlockData.byID.Count, Allocator.Persistent);
        blocktypes.CopyFrom(BlockData.byID.ToArray());

        float distance = Vector3.Distance(
                new Vector3(pos.x, pos.y, pos.z),
                ChunkLOD.playerPosition);
        if (distance > BlockData.ChunkSize * 10)
            LOD = 2;
        else if (distance > BlockData.ChunkSize * 15)
            LOD = 4;
        else if (distance > BlockData.ChunkSize * 20)
            LOD = 8;
        else if (distance > BlockData.ChunkSize * 25)
            LOD = 16;
        else
            LOD = 1;
    }

    void Update()
    {
        if (PreviousLOD != LOD)
        {
            PreviousLOD = LOD;
            Job_UpdateChunk();
        }

        if (_ForceUpdate)
        {
            _ForceUpdate = false;
            Job_UpdateChunk();
        }
    }

    #region "Block Functions"

    public BlockMetadata GetBlock(int x, int y, int z)
    {
        if (InRange(x) && InRange(y) && InRange(z))
            return Blocks[x + y * 16 + z * 256];
        return world.GetBlock(pos.x + x, pos.y + y, pos.z + z);
    }

    public static bool InRange(int index)
    {
        if (index < 0 || index >= BlockData.ChunkSize)
            return false;

        return true;
    }

#pragma warning disable 168
    public void SetBlock(int x, int y, int z, BlockMetadata metadata, bool FromNetwork = false, BlockUpdateMode UpdateMode = BlockUpdateMode.ForceUpdate)
    {
        try {
            if (Blocks[x + y * 16 + z * 256].ID != 0)
            {
                //blocks[x, y, z].OnDelete(this, x, y, z);
            }
        } catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        if (InRange(x) && InRange(y) && InRange(z))
        {
            Blocks[x + y * 16 + z * 256] = metadata;
        }
        else
        {
            world.SetBlock(pos.x + x, pos.y + y, pos.z + z, metadata, FromNetwork, UpdateMode);
        }

        /*try
        {
            if (blocks[x + y * 16 + z * 256].ID != 0 && Physics == true)
            {
                //blocks[x, y, z].OnPlace(this, x, y, z);
            }

        }
        catch (Exception e)
        {
            //Debug.Log ("Can't run OnDelete() on block " + this.GetType ().ToString () + ", at " + x + ", " + y + ", " + z);
        }*/
    }
#pragma warning restore 168

    public void SetBlockFromGen(int x, int y, int z, BlockMetadata metadata, byte modifier = 0)
    {
        if (InRange(x) && InRange(y) && InRange(z))
        {
            Blocks[x + y * 16 + z * 256] = metadata;
        }
        else
        {
            world.SetBlock(pos.x + x, pos.y + y, pos.z + z, metadata);
        }
    }

    #endregion
    
    #region "Job System - Main stuff"
    void OnDestroy()
    {
        // When closing the game or switching to menu/new map, regain ownership of jobs if they are present and dispose every NativeArray/NativeList.
        // This is to prevent memory leaking.
        Render_JobHandle.Complete();
        //if (Native_blocks.IsCreated) Native_blocks.Dispose();
        if (blocktypes.IsCreated) blocktypes.Dispose();
        if (verts.IsCreated) verts.Dispose();
        if (tris.IsCreated) tris.Dispose();
        if (March_tris.IsCreated) March_tris.Dispose();
        if (March_transparent_tris.IsCreated) March_transparent_tris.Dispose();
        if (uv.IsCreated) uv.Dispose();
        if (Foliage_tris.IsCreated) Foliage_tris.Dispose();
    }

    void LateUpdate()
    {
        // Rendering job, check if the handle.IsCompleted and Complete the IJob
        if (IsRendering && Render_JobHandle.IsCompleted)
        {
            
            IsRendering = false;
            Render_JobHandle.Complete();

            if ((verts.Length > 0 && isMapgenUpdate) || !isMapgenUpdate)
            {
                filter.mesh.Clear();
                // subMeshCount is actually how many materials you can use inside that particular mesh
                filter.mesh.subMeshCount = 4;

                // Vertices are shared between all subMeshes
                filter.mesh.vertices = verts.ToArray();
                // You have to set triangles for every subMesh you created, you can skip those if you want ofc.
                filter.mesh.SetTriangles(tris.ToArray(), 0);
                filter.mesh.SetTriangles(Foliage_tris.ToArray(), 1);
                filter.mesh.SetTriangles(March_tris.ToArray(), 2);
                filter.mesh.SetTriangles(March_transparent_tris.ToArray(), 3);
                Vector2[] uvs = uv.ToArray();

                gameObject.transform.localScale = new Vector3(LOD, LOD, LOD);

                filter.mesh.uv = uvs;
                filter.mesh.MarkDynamic();
                filter.mesh.RecalculateNormals();

                coll.sharedMesh = null;

                Mesh mesh = new Mesh();
                mesh.vertices = verts.ToArray();
                mesh.triangles = tris.ToArray().Concat(March_tris.ToArray()).Concat(March_transparent_tris.ToArray()).ToArray();
                mesh.MarkDynamic();
                mesh.RecalculateNormals();

                coll.sharedMesh = mesh;
            }
            verts.Dispose(); tris.Dispose(); uv.Dispose(); March_tris.Dispose(); March_transparent_tris.Dispose(); Foliage_tris.Dispose();
            isMapgenUpdate = false;
        }
    }
    #endregion

    #region "Chunk Update Function"
    public void UpdateChunk()
    {
        UpdateChunk(ChunkUpdateMode.QueueNeighbours);
    }

    public void UpdateChunk(ChunkUpdateMode UM)
    {
        switch (UM)
        {
            case ChunkUpdateMode.ForceNeighbours:
                for (int ix = -1; ix <= 1; ix++)
                    for (int iy = -1; iy <= 1; iy++)
                        for (int iz = -1; ix <= 1; iz++)
                        {
                            Chunk ch = world.GetChunk(pos.x + ix * 16, pos.y + iy * 16, pos.z + iz * 16);
                            if (ch != null)
                            {
                                ch.UpdateChunk(ChunkUpdateMode.ForceSingle);
                                int index = world.ChunkUpdateQueue.BinarySearch(ch);
                                if (index > -1) world.ChunkUpdateQueue.RemoveAt(index);
                            }
                        }
                break;
            case ChunkUpdateMode.ForceSingle:
                _ForceUpdate = true;
                break;
            case ChunkUpdateMode.QueueNeighbours:
                for (int ix = -1; ix <= 1; ix++)
                    for (int iy = -1; iy <= 1; iy++)
                        for (int iz = -1; ix <= 1; iz++)
                        {
                            Chunk ch = world.GetChunk(pos.x + ix * 16, pos.y + iy * 16, pos.z + iz * 16);
                            if (ch != null)
                                world.AddToChunkUpdateQueue(ch);
                        }
                break;
            case ChunkUpdateMode.QueueSingle:
                world.AddToChunkUpdateQueue(this);
                break;

            case ChunkUpdateMode.QueueNeighboursForceSingle:
                for (int ix = -1; ix <= 1; ix++)
                    for (int iy = -1; iy <= 1; iy++)
                        for (int iz = -1; ix <= 1; iz++)
                        {
                            Chunk ch = world.GetChunk(pos.x + ix * 16, pos.y + iy * 16, pos.z + iz * 16);
                            if (ch != null)
                                if (ch == this)
                                {
                                    _ForceUpdate = true;
                                    int index = world.ChunkUpdateQueue.BinarySearch(ch);
                                    if (index > -1) world.ChunkUpdateQueue.RemoveAt(index);
                                }
                                else
                                    world.AddToChunkUpdateQueue(ch);
                        }
                break;
            case ChunkUpdateMode.QueueMarchingCubesFix:
                for (int ix = -1; ix <= 0; ix++)
                    for (int iy = -1; iy <= 0; iy++)
                        for (int iz = -1; ix <= 0; iz++)
                        {
                            Chunk ch = world.GetChunk(pos.x + ix * 16, pos.y + iy * 16, pos.z + iz * 16);
                            if (ch != null && ch != this)
                                world.AddToChunkUpdateQueue(ch);
                        }
                break;
            case ChunkUpdateMode.ForceMarchingCubesFix:
                for (int ix = -1; ix <= 0; ix++)
                    for (int iy = -1; iy <= 0; iy++)
                        for (int iz = -1; ix <= 0; iz++)
                        {
                            Chunk ch = world.GetChunk(pos.x + ix * 16, pos.y + iy * 16, pos.z + iz * 16);
                            if (ch != null && ch != this)
                                ch.UpdateChunk(ChunkUpdateMode.ForceSingle);
                        }
                break;
        }
    }
    #endregion

    #region "Job System/Unity ECS - Chunk Rendering"

    public void Job_UpdateChunk()
    {
        if (Render_JobHandle.IsCompleted && !IsRendering)
        {
            // Grab data from surrounding chunks
            // SIDE CHUNKS (X or Y or Z)
            NativeArray<BlockMetadata> Chunk_MinusX = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x - 16, pos.y, pos.z) != null) Chunk_MinusX.CopyFrom(world.GetChunk(pos.x - 16, pos.y, pos.z).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusX = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x + 16, pos.y, pos.z) != null) Chunk_PlusX.CopyFrom(world.GetChunk(pos.x + 16, pos.y, pos.z).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, pos.y - 16, pos.z) != null) Chunk_MinusY.CopyFrom(world.GetChunk(pos.x, pos.y - 16, pos.z).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, pos.y + 16, pos.z) != null) Chunk_PlusY.CopyFrom(world.GetChunk(pos.x, pos.y + 16, pos.z).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, pos.y, pos.z - 16) != null) Chunk_MinusZ.CopyFrom(world.GetChunk(pos.x, pos.y, pos.z - 16).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, pos.y, pos.z + 16) != null)  Chunk_PlusZ.CopyFrom(world.GetChunk(pos.x, pos.y, pos.z + 16).Blocks);

            // EDGE CHUNKS (XZ)
            NativeArray<BlockMetadata> Chunk_PlusXZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), pos.y, (pos.z + 16)) != null) Chunk_PlusXZ.CopyFrom(world.GetChunk((pos.x + 16), pos.y, (pos.z + 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusXMinusZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), pos.y, (pos.z - 16)) != null) Chunk_PlusXMinusZ.CopyFrom(world.GetChunk((pos.x + 16), pos.y, (pos.z - 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusXPlusZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), pos.y, (pos.z + 16)) != null) Chunk_MinusXPlusZ.CopyFrom(world.GetChunk((pos.x - 16), pos.y, (pos.z + 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusXZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), pos.y, (pos.z - 16)) != null) Chunk_MinusXZ.CopyFrom(world.GetChunk((pos.x - 16), pos.y, (pos.z - 16)).Blocks);

            // EDGE CHUNKS (XY)
            NativeArray<BlockMetadata> Chunk_PlusXY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y + 16), pos.z) != null) Chunk_PlusXY.CopyFrom(world.GetChunk((pos.x + 16), (pos.y + 16), pos.z).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusXMinusY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y - 16), pos.z) != null) Chunk_PlusXMinusY.CopyFrom(world.GetChunk((pos.x + 16), (pos.y - 16), pos.z).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusXPlusY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y + 16), pos.z) != null) Chunk_MinusXPlusY.CopyFrom(world.GetChunk((pos.x - 16), (pos.y + 16), pos.z).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusXY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y - 16), pos.z) != null) Chunk_MinusXY.CopyFrom(world.GetChunk((pos.x - 16), (pos.y - 16), pos.z).Blocks);

            // EDGE CHUNKS (ZY)
            NativeArray<BlockMetadata> Chunk_PlusZY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y + 16), (pos.z + 16)) != null) Chunk_PlusZY.CopyFrom(world.GetChunk(pos.x, (pos.y + 16), (pos.z + 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusZMinusY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y - 16), (pos.z + 16)) != null) Chunk_PlusZMinusY.CopyFrom(world.GetChunk(pos.x, (pos.y - 16), (pos.z + 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusZPlusY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y + 16), (pos.z - 16)) != null) Chunk_MinusZPlusY.CopyFrom(world.GetChunk(pos.x, (pos.y + 16), (pos.z - 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusZY = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y - 16), (pos.z - 16)) != null) Chunk_MinusZY.CopyFrom(world.GetChunk(pos.x, (pos.y - 16), (pos.z - 16)).Blocks);

            // CORNER CHUNKS (XYZ)
            NativeArray<BlockMetadata> Chunk_MinusY_PlusXZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y - 16), (pos.z + 16)) != null) Chunk_MinusY_PlusXZ.CopyFrom(world.GetChunk((pos.x + 16), (pos.y - 16), (pos.z + 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusY_MinusXZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y - 16), (pos.z - 16)) != null) Chunk_MinusY_MinusXZ.CopyFrom(world.GetChunk((pos.x - 16), (pos.y - 16), (pos.z - 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusY_MinusXPlusZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y - 16), (pos.z + 16)) != null) Chunk_MinusY_MinusXPlusZ.CopyFrom(world.GetChunk((pos.x - 16), (pos.y - 16), (pos.z + 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_MinusY_PlusXMinusZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y - 16), (pos.z - 16)) != null) Chunk_MinusY_PlusXMinusZ.CopyFrom(world.GetChunk((pos.x + 16), (pos.y - 16), (pos.z - 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusY_PlusXZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y + 16), (pos.z + 16)) != null) Chunk_PlusY_PlusXZ.CopyFrom(world.GetChunk((pos.x + 16), (pos.y + 16), (pos.z + 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusY_MinusXZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y + 16), (pos.z - 16)) != null) Chunk_PlusY_MinusXZ.CopyFrom(world.GetChunk((pos.x - 16), (pos.y + 16), (pos.z - 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusY_MinusXPlusZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y + 16), (pos.z + 16)) != null) Chunk_PlusY_MinusXPlusZ.CopyFrom(world.GetChunk((pos.x - 16), (pos.y + 16), (pos.z + 16)).Blocks);
            NativeArray<BlockMetadata> Chunk_PlusY_PlusXMinusZ = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y + 16), (pos.z - 16)) != null) Chunk_PlusY_PlusXMinusZ.CopyFrom(world.GetChunk((pos.x + 16), (pos.y + 16), (pos.z - 16)).Blocks);

            // WORKER CHUNK (CURRENT)
            Native_blocks = new NativeArray<BlockMetadata>(4096, Allocator.TempJob);
            Native_blocks.CopyFrom(Blocks);

            // Mesh info for non-marching voxels
            verts = new NativeList<Vector3>(Allocator.Persistent);
            tris = new NativeList<int>(Allocator.Persistent);
            uv = new NativeList<Vector2>(Allocator.Persistent);

            // Marching cubes mesh info
            March_tris = new NativeList<int>(Allocator.Persistent);
            March_transparent_tris = new NativeList<int>(Allocator.Persistent);

            // Foliage mesh info
            Foliage_tris = new NativeList<int>(Allocator.Persistent);

            // Greedy mesh flags
            NativeArray<bool> GreedyBlocks_U = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_D = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_N = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_S = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_E = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_W = new NativeArray<bool>(4096, Allocator.TempJob);

            // Marching cubes tables
            NativeArray<int> T_CubeEdgeFlags = new NativeArray<int>(MarchingCubesTables.CubeEdgeFlags.Length, Allocator.TempJob);
            T_CubeEdgeFlags.CopyFrom(MarchingCubesTables.CubeEdgeFlags);
            NativeArray<int> T_EdgeConnection = new NativeArray<int>(MarchingCubesTables.EdgeConnection.Length, Allocator.TempJob);
            T_EdgeConnection.CopyFrom(MarchingCubesTables.EdgeConnection);
            NativeArray<float> T_EdgeDirection = new NativeArray<float>(MarchingCubesTables.EdgeDirection.Length, Allocator.TempJob);
            T_EdgeDirection.CopyFrom(MarchingCubesTables.EdgeDirection);
            NativeArray<int> T_TriangleConnectionTable = new NativeArray<int>(MarchingCubesTables.TriangleConnectionTable.Length, Allocator.TempJob);
            T_TriangleConnectionTable.CopyFrom(MarchingCubesTables.TriangleConnectionTable);
            NativeArray<int> T_VertexOffset = new NativeArray<int>(MarchingCubesTables.VertexOffset.Length, Allocator.TempJob);
            T_VertexOffset.CopyFrom(MarchingCubesTables.VertexOffset);

            var job = new Job_RenderChunk()
            {
                BlockTypes = BlockData.byID.Count(),

                // AMOUNT OF LAYERS FOR MARCHING CUBES ALGORITHM
                MarchingCubesLayerAmount = BlockData.MarchingLayers,

                // TILING SIZE FOR TEXTURING
                tileSize = BlockData.BlockTileSize,

                // SIZE OF THE CHUNKS THAT WE ARE WORKING WITH
                chunkSize = BlockData.ChunkSize,
                LevelofDetail = LOD,

                // BLOCKDATA
                blocktype = blocktypes,

                // STANDARD VOXEL MESHDATA
                vertices = verts,
                triangles = tris,
                uvs = uv,

                Marching_triangles = March_tris,
                Marching_transparent_triangles = March_transparent_tris,
                Foliage_triangles = Foliage_tris,

                // TARGET AND NEIGHBOUR CHUNKS
                blocks_CurX_CurY_CurZ = Native_blocks,
                blocks_MinX_CurY_CurZ = Chunk_MinusX,
                blocks_CurX_MinY_CurZ = Chunk_MinusY,
                blocks_CurX_CurY_MinZ = Chunk_MinusZ,
                blocks_PluX_CurY_CurZ = Chunk_PlusX,
                blocks_CurX_PluY_CurZ = Chunk_PlusY,
                blocks_CurX_CurY_PluZ = Chunk_PlusZ,

                // EDGE CHUNKS
                blocks_MinX_PluY_CurZ = Chunk_MinusXPlusY,
                blocks_MinX_CurY_PluZ = Chunk_MinusXPlusZ,
                blocks_CurX_MinY_PluZ = Chunk_PlusZMinusY,
                blocks_MinX_CurY_MinZ = Chunk_MinusXZ,
                blocks_MinX_MinY_CurZ = Chunk_MinusXY,
                blocks_CurX_MinY_MinZ = Chunk_MinusZY,
                blocks_PluX_MinY_CurZ = Chunk_PlusXMinusY,
                blocks_PluX_CurY_MinZ = Chunk_PlusXMinusZ,
                blocks_CurX_PluY_MinZ = Chunk_MinusZPlusY,
                blocks_PluX_PluY_CurZ = Chunk_PlusXY,
                blocks_CurX_PluY_PluZ = Chunk_PlusZY,
                blocks_PluX_CurY_PluZ = Chunk_PlusXZ,

                // CORNER CHUNKS 
                blocks_MinX_MinY_MinZ = Chunk_MinusY_MinusXZ,
                blocks_PluX_MinY_PluZ = Chunk_MinusY_PlusXZ,
                blocks_MinX_MinY_PluZ = Chunk_MinusY_MinusXPlusZ,
                blocks_PluX_MinY_MinZ = Chunk_MinusY_PlusXMinusZ,
                blocks_MinX_PluY_MinZ = Chunk_PlusY_MinusXZ,
                blocks_PluX_PluY_PluZ = Chunk_PlusY_PlusXZ,
                blocks_MinX_PluY_PluZ = Chunk_PlusY_MinusXPlusZ,
                blocks_PluX_PluY_MinZ = Chunk_PlusY_PlusXMinusZ,

                // GREEDY MESHING FACE FLAGS
                _blocks_greedy_U = GreedyBlocks_U,
                _blocks_greedy_D = GreedyBlocks_D,
                _blocks_greedy_N = GreedyBlocks_N,
                _blocks_greedy_S = GreedyBlocks_S,
                _blocks_greedy_E = GreedyBlocks_E,
                _blocks_greedy_W = GreedyBlocks_W,

                // MARCHING CUBES
                Table_CubeEdgeFlags = T_CubeEdgeFlags,
                Table_EdgeConnection = T_EdgeConnection,
                Table_EdgeDirection = T_EdgeDirection,
                Table_TriangleConnection = T_TriangleConnectionTable,
                Table_VertexOffset = T_VertexOffset,
                MarchedBlocks = new NativeArray<float>(5832, Allocator.TempJob)

            };

            Render_JobHandle = job.Schedule();
            IsRendering = true;
            
        } else {
            UpdateChunk(ChunkUpdateMode.ForceSingle);
        }
    }

    // Enable burst compilation for better performace
    // Warning: when burst is ENABLED, you may not refference MonoBehaviour directly.
    // For example: Debug.Log("test"); WILL throw out an exception.
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
    private struct Job_RenderChunk : IJob
    {
        #region Declarations

        public int chunkSize, BlockTypes;                                                                                                               // Value of BlockTypes is BlockData.ByID.Count()
        public int LevelofDetail;                                                                                                                       // Level of detail (range 1 - 4)
        public sbyte MarchingCubesLayerAmount;                                                                                                          // Amount of layers for Marching cubes algorithm
        public NativeList<Vector3> vertices;                                                                                                            // MeshData
        public NativeList<Vector2> uvs;                                                                                                                 // MeshData
        public NativeList<int> triangles, Marching_triangles, Marching_transparent_triangles, Foliage_triangles;                                        // MeshData
        [ReadOnly] public float tileSize;                                                                                                               // Size of a tile for texturing
        [ReadOnly] public NativeArray<BlockTypes> blocktype;                                                                                            // Blocktype data
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<BlockMetadata>
            blocks_CurX_CurY_CurZ,                                                                                                                      // Current chunk
            blocks_PluX_CurY_CurZ, blocks_MinX_CurY_CurZ, blocks_CurX_PluY_CurZ, blocks_CurX_MinY_CurZ, blocks_CurX_CurY_PluZ, blocks_CurX_CurY_MinZ,   // Direct neighbour chunks
            blocks_PluX_CurY_PluZ, blocks_MinX_CurY_MinZ, blocks_MinX_CurY_PluZ, blocks_PluX_CurY_MinZ,                                                 // Edge chunks (XZ)
            blocks_PluX_PluY_CurZ, blocks_MinX_MinY_CurZ, blocks_MinX_PluY_CurZ, blocks_PluX_MinY_CurZ,                                                 // Edge chunks (XY)
            blocks_CurX_PluY_PluZ, blocks_CurX_MinY_MinZ, blocks_CurX_MinY_PluZ, blocks_CurX_PluY_MinZ,                                                 // Edge chunks (YZ)
            blocks_PluX_PluY_PluZ, blocks_MinX_PluY_MinZ, blocks_MinX_PluY_PluZ, blocks_PluX_PluY_MinZ,                                                 // Corner chunks (XYZ)
            blocks_PluX_MinY_PluZ, blocks_MinX_MinY_MinZ, blocks_MinX_MinY_PluZ, blocks_PluX_MinY_MinZ;                                                 // Corner chunks (XYZ)
        [DeallocateOnJobCompletion] public NativeArray<bool>                                                                                            // Greedy mesh flag for every direction of block
            _blocks_greedy_U, _blocks_greedy_D, _blocks_greedy_N, _blocks_greedy_S, _blocks_greedy_E, _blocks_greedy_W;
        [DeallocateOnJobCompletion] public NativeArray<float> MarchedBlocks;                                                                            // MARCHING CUBES
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float> Table_EdgeDirection;                                                           // Lookup tables for Marching Cubes
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> Table_EdgeConnection, Table_CubeEdgeFlags, Table_TriangleConnection, Table_VertexOffset;

        enum FaceDirection : byte
        {
            North = 0,
            South,
            East,
            West,
            Up,
            Down
        }

        enum MarchingTrianglesState : byte
        {
            Default = 0,
            Transparent,
            Water
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
        /// <param name="Face1">First coordinate. By default (FaceDirection = Up) that's X</param>
        /// <param name="Face2">First coordinate. By default (FaceDirection = Up) that's Z</param>
        /// <param name="Face3">First coordinate. By default (FaceDirection = Up) that's Y</param>
        /// <param name="direction">Direction of the face you are checking right now</param>
        /// <param name="size">Size of the chunk. By default it's set to 16</param>
        /// <returns></returns>
        static int GetAddressWithDirection(int Face1, int Face2, int Face3, FaceDirection direction, int LOD, int size)
        {
            switch (direction)
            {
                case FaceDirection.Up:
                case FaceDirection.Down:
                    return GetAddress(Face1, Face2, Face3, size); //XZ
                case FaceDirection.East:
                case FaceDirection.West:
                    return GetAddress(Face1, Face3, Face2, size); //XY
                case FaceDirection.North:
                case FaceDirection.South:
                    return GetAddress(Face2, Face1, Face3, size); //YZ
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
        BlockMetadata GetBlock(int x, int y, int z, int lod)
        {
            int cs = chunkSize - 1;
            if (lod > 1)
            {
                x *= lod;
                y *= lod;
                z *= lod;
            }
            if      (x >= 0 && x <= cs && y >= 0 && y <= cs && z >= 0 && z <= cs) return blocks_CurX_CurY_CurZ[GetAddress(x,             y,             z            , chunkSize)];
            else if (x > cs            && y >= 0 && y <= cs && z >= 0 && z <= cs) return blocks_PluX_CurY_CurZ[GetAddress(x - chunkSize, y,             z            , chunkSize)];
            else if (x < 0             && y >= 0 && y <= cs && z >= 0 && z <= cs) return blocks_MinX_CurY_CurZ[GetAddress(x + chunkSize, y,             z            , chunkSize)];
            else if (x >= 0 && x <= cs && y > cs            && z >= 0 && z <= cs) return blocks_CurX_PluY_CurZ[GetAddress(x,             y - chunkSize, z            , chunkSize)];
            else if (x >= 0 && x <= cs && y < 0             && z >= 0 && z <= cs) return blocks_CurX_MinY_CurZ[GetAddress(x,             y + chunkSize, z            , chunkSize)];
            else if (x >= 0 && x <= cs && y >= 0 && y <= cs && z > cs           ) return blocks_CurX_CurY_PluZ[GetAddress(x,             y,             z - chunkSize, chunkSize)];
            else if (x >= 0 && x <= cs && y >= 0 && y <= cs && z < 0            ) return blocks_CurX_CurY_MinZ[GetAddress(x,             y,             z + chunkSize, chunkSize)];
            else if (x < 0             && y >= 0 && y <= cs && z < 0            ) return blocks_MinX_CurY_MinZ[GetAddress(x + chunkSize, y,             z + chunkSize, chunkSize)];
            else if (x > cs            && y >= 0 && y <= cs && z > cs           ) return blocks_PluX_CurY_PluZ[GetAddress(x - chunkSize, y,             z - chunkSize, chunkSize)];
            else if (x > cs            && y >= 0 && y <= cs && z < 0            ) return blocks_PluX_CurY_MinZ[GetAddress(x - chunkSize, y,             z + chunkSize, chunkSize)];
            else if (x < 0             && y >= 0 && y <= cs && z > cs           ) return blocks_MinX_CurY_PluZ[GetAddress(x + chunkSize, y,             z - chunkSize, chunkSize)];
            else if (x < 0             && y < 0             && z >= 0 && z <= cs) return blocks_MinX_MinY_CurZ[GetAddress(x + chunkSize, y + chunkSize, z            , chunkSize)];
            else if (x > cs            && y > cs            && z >= 0 && z <= cs) return blocks_PluX_PluY_CurZ[GetAddress(x - chunkSize, y - chunkSize, z            , chunkSize)];
            else if (x > cs            && y < 0             && z >= 0 && z <= cs) return blocks_PluX_MinY_CurZ[GetAddress(x - chunkSize, y + chunkSize, z            , chunkSize)];
            else if (x < 0             && y > cs            && z >= 0 && z <= cs) return blocks_MinX_PluY_CurZ[GetAddress(x + chunkSize, y - chunkSize, z            , chunkSize)];
            else if (x >= 0 && x <= cs && y > cs            && z > cs           ) return blocks_CurX_PluY_PluZ[GetAddress(x,             y - chunkSize, z - chunkSize, chunkSize)];
            else if (x >= 0 && x <= cs && y < 0             && z < 0            ) return blocks_CurX_MinY_MinZ[GetAddress(x,             y + chunkSize, z + chunkSize, chunkSize)];
            else if (x >= 0 && x <= cs && y < 0             && z > cs           ) return blocks_CurX_MinY_PluZ[GetAddress(x,             y + chunkSize, z - chunkSize, chunkSize)];
            else if (x >= 0 && x <= cs && y > cs            && z < 0            ) return blocks_CurX_PluY_MinZ[GetAddress(x,             y - chunkSize, z + chunkSize, chunkSize)];
            else if (x < 0             && y < 0             && z < 0            ) return blocks_MinX_MinY_MinZ[GetAddress(x + chunkSize, y + chunkSize, z + chunkSize, chunkSize)];
            else if (x < 0             && y > cs            && z < 0            ) return blocks_MinX_PluY_MinZ[GetAddress(x + chunkSize, y - chunkSize, z + chunkSize, chunkSize)];
            else if (x > cs            && y > cs            && z > cs           ) return blocks_PluX_PluY_PluZ[GetAddress(x - chunkSize, y - chunkSize, z - chunkSize, chunkSize)];
            else if (x > cs            && y < 0             && z > cs           ) return blocks_PluX_MinY_PluZ[GetAddress(x - chunkSize, y + chunkSize, z - chunkSize, chunkSize)];
            else if (x > cs            && y < 0             && z < 0            ) return blocks_PluX_MinY_MinZ[GetAddress(x - chunkSize, y + chunkSize, z + chunkSize, chunkSize)];
            else if (x > cs            && y > cs            && z < 0            ) return blocks_PluX_PluY_MinZ[GetAddress(x - chunkSize, y - chunkSize, z + chunkSize, chunkSize)];
            else if (x < 0             && y < 0             && z > cs           ) return blocks_MinX_MinY_PluZ[GetAddress(x + chunkSize, y + chunkSize, z - chunkSize, chunkSize)];
            else if (x < 0             && y > cs            && z > cs           ) return blocks_MinX_PluY_PluZ[GetAddress(x + chunkSize, y - chunkSize, z - chunkSize, chunkSize)];
            else                                                                  return blocks_CurX_CurY_CurZ[GetAddress(x,             y,             z            , chunkSize)];
        }

        /// <summary>
        /// <para>For greedy meshing.</para>
        /// It rotates the coordinate inputs around to get correct block Address for given rotation.
        /// </summary>
        /// <param name="Face1">First coordinate. By default (FaceDirection = Up) that's X</param>
        /// <param name="Face2">First coordinate. By default (FaceDirection = Up) that's Z</param>
        /// <param name="Face3">First coordinate. By default (FaceDirection = Up) that's Y</param>
        /// <param name="direction">Direction of the face you are checking right now</param>
        /// <returns></returns>
        BlockMetadata GetBlockWithDirection(int Face1, int Face2, int Face3, FaceDirection direction)
        {
            switch (direction)
            {
                case FaceDirection.Up:
                case FaceDirection.Down:
                    return GetBlock(Face1, Face2, Face3, LevelofDetail); //XZY
                case FaceDirection.East:
                case FaceDirection.West:
                    return GetBlock(Face1, Face3, Face2, LevelofDetail); //XYZ
                case FaceDirection.North:
                case FaceDirection.South:
                    return GetBlock(Face2, Face1, Face3, LevelofDetail); //YZX
                default:
                    return new BlockMetadata();
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
        /// <param name="Face1">First coordinate. By default (FaceDirection = Up) that's X</param>
        /// <param name="Face2">First coordinate. By default (FaceDirection = Up) that's Z</param>
        /// <param name="Face3">First coordinate. By default (FaceDirection = Up) that's Y</param>
        /// <param name="direction">Direction of the face you are checking right now</param>
        void AddRelativeVertices(int2 Face1, int Face2, int2 Face3, FaceDirection direction)
        {
            switch (direction)
            {
                case FaceDirection.Up:
                    vertices.Add(new Vector3(Face1.x - 0.5f, Face2 + 0.5f, Face3.y + 0.5f));
                    vertices.Add(new Vector3(Face1.y + 0.5f, Face2 + 0.5f, Face3.y + 0.5f));
                    vertices.Add(new Vector3(Face1.y + 0.5f, Face2 + 0.5f, Face3.x - 0.5f));
                    vertices.Add(new Vector3(Face1.x - 0.5f, Face2 + 0.5f, Face3.x - 0.5f));
                    break;
                case FaceDirection.Down:
                    vertices.Add(new Vector3(Face1.x - 0.5f, Face2 - 0.5f, Face3.x - 0.5f));
                    vertices.Add(new Vector3(Face1.y + 0.5f, Face2 - 0.5f, Face3.x - 0.5f));
                    vertices.Add(new Vector3(Face1.y + 0.5f, Face2 - 0.5f, Face3.y + 0.5f));
                    vertices.Add(new Vector3(Face1.x - 0.5f, Face2 - 0.5f, Face3.y + 0.5f));
                    break;
                case FaceDirection.East:
                    vertices.Add(new Vector3(Face1.y + 0.5f, Face3.x - 0.5f, Face2 + 0.5f));
                    vertices.Add(new Vector3(Face1.y + 0.5f, Face3.y + 0.5f, Face2 + 0.5f));
                    vertices.Add(new Vector3(Face1.x - 0.5f, Face3.y + 0.5f, Face2 + 0.5f));
                    vertices.Add(new Vector3(Face1.x - 0.5f, Face3.x - 0.5f, Face2 + 0.5f));
                    break;
                case FaceDirection.West:
                    vertices.Add(new Vector3(Face1.x - 0.5f, Face3.x - 0.5f, Face2 - 0.5f));
                    vertices.Add(new Vector3(Face1.x - 0.5f, Face3.y + 0.5f, Face2 - 0.5f));
                    vertices.Add(new Vector3(Face1.y + 0.5f, Face3.y + 0.5f, Face2 - 0.5f));
                    vertices.Add(new Vector3(Face1.y + 0.5f, Face3.x - 0.5f, Face2 - 0.5f));
                    break;
                case FaceDirection.North:
                    vertices.Add(new Vector3(Face2 + 0.5f, Face1.x - 0.5f, Face3.x - 0.5f));
                    vertices.Add(new Vector3(Face2 + 0.5f, Face1.y + 0.5f, Face3.x - 0.5f));
                    vertices.Add(new Vector3(Face2 + 0.5f, Face1.y + 0.5f, Face3.y + 0.5f));
                    vertices.Add(new Vector3(Face2 + 0.5f, Face1.x - 0.5f, Face3.y + 0.5f));
                    break;
                case FaceDirection.South:
                    vertices.Add(new Vector3(Face2 - 0.5f, Face1.x - 0.5f, Face3.y + 0.5f));
                    vertices.Add(new Vector3(Face2 - 0.5f, Face1.y + 0.5f, Face3.y + 0.5f));
                    vertices.Add(new Vector3(Face2 - 0.5f, Face1.y + 0.5f, Face3.x - 0.5f));
                    vertices.Add(new Vector3(Face2 - 0.5f, Face1.x - 0.5f, Face3.x - 0.5f));
                    break;
            }
        }

        /// <summary>
        /// Apply texture to quad created with AddQuadTriangles()
        /// </summary>
        /// <param name="block">Block Metadata to refference textures</param>
        /// <param name="direction">Direction of the face</param>
        void TextureTheQuad(BlockMetadata block, FaceDirection direction)
        {
            switch (direction)
            {
                case FaceDirection.Up:
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_Up.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_Up.y + 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_Up.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_Up.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_Up.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_Up.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_Up.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_Up.y + 0.005f));
                    break;
                case FaceDirection.Down:
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_Down.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_Down.y + 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_Down.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_Down.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_Down.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_Down.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_Down.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_Down.y + 0.005f));
                    break;
                case FaceDirection.East:
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_East.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_East.y + 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_East.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_East.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_East.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_East.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_East.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_East.y + 0.005f));
                    break;
                case FaceDirection.West:
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_West.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_West.y + 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_West.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_West.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_West.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_West.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_West.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_West.y + 0.005f));
                    break;
                case FaceDirection.North:
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_North.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_North.y + 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_North.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_North.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_North.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_North.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_North.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_North.y + 0.005f));
                    break;
                case FaceDirection.South:
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_South.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_South.y + 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_South.x + tileSize - 0.005f,
                                        tileSize * blocktype[block.ID].Texture_South.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_South.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_South.y + tileSize - 0.005f));
                    uvs.Add(new Vector2(tileSize * blocktype[block.ID].Texture_South.x + 0.005f,
                                        tileSize * blocktype[block.ID].Texture_South.y + 0.005f));
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
        void PerformGreedyMeshing(int x, int y, int z, ref NativeArray<bool> blocks_greedy, FaceDirection direction)
        {
            BlockMetadata
                originBlock = GetBlock(x, y, z, LevelofDetail),
                NeighbourBlock = GetBlock(x, y, z, LevelofDetail); // temporary assignment

            int Face1 = int.MinValue, // Default values will crash or cause errors if SOMEHOW they stay this way
                Face2 = int.MinValue,
                Face3 = int.MinValue,
                NeighbourModifier = int.MinValue;

            switch (direction)
            {
                case FaceDirection.Up:
                    NeighbourBlock = GetBlock(x, y + 1, z, LevelofDetail); Face1 = x; Face2 = z; Face3 = y; NeighbourModifier = 1;
                    break;
                case FaceDirection.Down:
                    NeighbourBlock = GetBlock(x, y - 1, z, LevelofDetail); Face1 = x; Face2 = z; Face3 = y; NeighbourModifier = -1;
                    break;
                case FaceDirection.East:
                    NeighbourBlock = GetBlock(x, y, z + 1, LevelofDetail); Face1 = x; Face2 = y; Face3 = z; NeighbourModifier = 1;
                    break;
                case FaceDirection.West:
                    NeighbourBlock = GetBlock(x, y, z - 1, LevelofDetail); Face1 = x; Face2 = y; Face3 = z; NeighbourModifier = -1;
                    break;
                case FaceDirection.North:
                    NeighbourBlock = GetBlock(x + 1, y, z, LevelofDetail); Face1 = y; Face2 = z; Face3 = x; NeighbourModifier = 1;
                    break;
                case FaceDirection.South:
                    NeighbourBlock = GetBlock(x - 1, y, z, LevelofDetail); Face1 = y; Face2 = z; Face3 = x; NeighbourModifier = -1;
                    break;
            }

            if (
                blocktype[originBlock.ID].Solid &&
                (
                    !blocktype[NeighbourBlock.ID].Solid ||
                    NeighbourBlock.Marched ||
                    blocktype[NeighbourBlock.ID].Foliage ||
                    (
                        blocktype[NeighbourBlock.ID].CullingMode == 2 &&
                        NeighbourBlock.ID != originBlock.ID
                    ) ||
                    blocktype[NeighbourBlock.ID].CullingMode == 1
                ) &&
                !blocks_greedy[GetAddress(x, y, z, chunkSize)]
            )
            {
                int Face2_Max = chunkSize / LevelofDetail - 1;      // Declare maximum value for Z, just for first iteration it has to be ChunkSize-1
                int Temp_Face2_Max = chunkSize / LevelofDetail - 1; // Temporary maximum value for Z, gets bigger only at first iteration and then moves into max_z
                int Face1_Greed = Face1;            // greed_x is just a value to store vertex's max X position
                bool broken = false;                // and last but not least, broken. This is needed to break out of both loops (X and Z)

                // Iterate Face1 -> ChunkSize - 1
                for (int Face1_Greedy = Face1; Face1_Greedy < chunkSize / LevelofDetail; Face1_Greedy++)
                {
                    // Check if current's iteration Face1 is bigger than starting Face1 and if first block in this iteration is the same, if not - break.
                    if (Face1_Greedy > Face1)
                    {
                        if (GetBlockWithDirection(Face1_Greedy, Face3, Face2, direction).ID != originBlock.ID) break;
                        if (blocktype[GetBlockWithDirection(Face1_Greedy, Face3, Face2, direction).ID].Foliage) break;
                        if (GetBlockWithDirection(Face1_Greedy, Face3, Face2, direction).Marched) break;
                    }

                    // Iterate Face2 -> Face2_Max (ChunkSize - 1 for first iteration)
                    for (int Face2_Greedy = Face2; Face2_Greedy <= Face2_Max; Face2_Greedy++)
                    {
                        // Get info about neighbour block
                        bool Neighbour = false;

                        BlockMetadata
                            neighbourBlock = GetBlockWithDirection(Face1_Greedy, Face3 + NeighbourModifier, Face2_Greedy, direction),
                            workerBlock = GetBlockWithDirection(Face1_Greedy, Face3, Face2_Greedy, direction);

                        if (blocktype[neighbourBlock.ID].CullingMode != 1 && !neighbourBlock.Marched && neighbourBlock.ID > 0 && !blocktype[neighbourBlock.ID].Foliage)
                            Neighbour = blocktype[neighbourBlock.ID].Solid; // Check if the neighbour block is solid or not

                        // Check if this block is marked as greedy, compare blockID with starting block and check if Neighbour is solid.
                        if (!blocks_greedy[GetAddressWithDirection(Face1_Greedy, Face3, Face2_Greedy, direction, LevelofDetail, chunkSize)] &&
                            originBlock.ID == workerBlock.ID &&
                            !Neighbour &&
                            !workerBlock.Marched &&
                            !blocktype[workerBlock.ID].Foliage
                        )
                        {
                            // Set the temporary value of Max_Z to current iteration of Z
                            if (Face1_Greedy == Face1)
                                Temp_Face2_Max = Face2_Greedy;
                            // Mark the current block as greedy
                            blocks_greedy[GetAddressWithDirection(Face1_Greedy, Face3, Face2_Greedy, direction, LevelofDetail, chunkSize)] = true;
                        }
                        else
                        {
                            // If block in current iteration was different or already greedy, break
                            // Then, reverse last iteration to non-greedy state.
                            if (Face2_Greedy <= Face2_Max && Face1_Greedy > Face1)
                            {
                                // Reverse the greedy to false
                                for (int Face2_ = Face2; Face2_ < Face2_Greedy; Face2_++)
                                    blocks_greedy[GetAddressWithDirection(Face1_Greedy, Face3, Face2_, direction, LevelofDetail, chunkSize)] = false;

                                // Break out of both loops
                                broken = true;
                            }
                            break;
                        }
                    }
                    // Next, after an iterations is done (or broken), move the temporary value of Max_Z to non-temporary
                    Face2_Max = Temp_Face2_Max;
                    if (broken) break;
                    // If both loops weren't broken, set vertex' max Face1 value to current Face1 iteration
                    Face1_Greed = Face1_Greedy;
                }

                // Create the vertices
                AddRelativeVertices(new int2(Face1, Face1_Greed), Face3, new int2(Face2, Face2_Max), direction);

                // Create Quad and texture it
                AddQuadTriangles(ref triangles);
                TextureTheQuad(originBlock, direction);
            }
        }

        /// <summary>
        /// Find largest integer in NativeArray<int>
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
            switch (state)
            {
                case 1:
                    Marching_transparent_triangles.Add(triangle);
                    break;

                default:
                    Marching_triangles.Add(triangle);
                    break;
            }
        }

        #endregion

        public void Execute()
        {
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random(0x6E624EB7u);

            float Surface = 0.5f;
            NativeArray<float> Cube = new NativeArray<float>(8, Allocator.Temp);
            NativeArray<int> WindingOrder = new NativeArray<int>(3, Allocator.Temp);
            WindingOrder[0] = 0;
            WindingOrder[1] = 1;
            WindingOrder[2] = 2;

            if (Surface > 0.0f)
            {
                WindingOrder[0] = 0;
                WindingOrder[1] = 1;
                WindingOrder[2] = 2;
            }
            else
            {
                WindingOrder[0] = 2;
                WindingOrder[1] = 1;
                WindingOrder[2] = 0;
            }

            for (int x = 0; x < chunkSize / LevelofDetail; x++)
                for (int y = 0; y < chunkSize / LevelofDetail; y++)
                    for (int z = 0; z < chunkSize / LevelofDetail; z++)
                    {
                        if (GetBlock(x, y, z, LevelofDetail).ID != 0 && !GetBlock(x, y, z, LevelofDetail).Marched) {

                            BlockMetadata originBlock = GetBlock(x, y, z, LevelofDetail);

                            if (!blocktype[originBlock.ID].Foliage)
                            {
                                if (!originBlock.Marched)
                                {
                                    // Preform greedy meshing for all faces available
                                    PerformGreedyMeshing(x, y, z, ref _blocks_greedy_U, FaceDirection.Up);
                                    PerformGreedyMeshing(x, y, z, ref _blocks_greedy_D, FaceDirection.Down);
                                    PerformGreedyMeshing(x, y, z, ref _blocks_greedy_E, FaceDirection.East);
                                    PerformGreedyMeshing(x, y, z, ref _blocks_greedy_W, FaceDirection.West);
                                    PerformGreedyMeshing(x, y, z, ref _blocks_greedy_N, FaceDirection.North);
                                    PerformGreedyMeshing(x, y, z, ref _blocks_greedy_S, FaceDirection.South);
                                }
                            } else {
                                if (LevelofDetail == 1) // LOD needs to be at 1, so we don't get omegaupscaled foliage blocks
                                {
                                    // Foliage block offsets
                                    float X_Offset = rand.NextFloat(-0.25f, 0.25f);
                                    float Y_Offset = rand.NextFloat(-0.05f, 0.05f);
                                    float Z_Offset = rand.NextFloat(-0.25f, 0.25f);

                                    // This isn't standard block. This is the cause of writing this part of renderer manually instead of macroing it.
                                    if (blocktype[originBlock.ID].Solid)
                                    {
                                        vertices.Add(new Vector3(x + 0.5f + X_Offset, y - 1f + Y_Offset, z + 0.5f + Z_Offset));
                                        vertices.Add(new Vector3(x + 0.5f + X_Offset, y + 0.5f + Y_Offset, z + 0.5f + Z_Offset));
                                        vertices.Add(new Vector3(x - 0.5f + X_Offset, y + 0.5f + Y_Offset, z - 0.5f + Z_Offset));
                                        vertices.Add(new Vector3(x - 0.5f + X_Offset, y - 1f + Y_Offset, z - 0.5f + Z_Offset));

                                        AddQuadTriangles(ref Foliage_triangles);

                                        uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_Up.x + tileSize - 0.005f, tileSize * blocktype[originBlock.ID].Texture_Up.y + 0.005f));
                                        uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_Up.x + tileSize - 0.005f, tileSize * blocktype[originBlock.ID].Texture_Up.y + tileSize - 0.005f));
                                        uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_Up.x + 0.005f, tileSize * blocktype[originBlock.ID].Texture_Up.y + tileSize - 0.005f));
                                        uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_Up.x + 0.005f, tileSize * blocktype[originBlock.ID].Texture_Up.y + 0.005f));

                                        vertices.Add(new Vector3(x - 0.5f + X_Offset, y - 1f + Y_Offset, z + 0.5f + Z_Offset));
                                        vertices.Add(new Vector3(x - 0.5f + X_Offset, y + 0.5f + Y_Offset, z + 0.5f + Z_Offset));
                                        vertices.Add(new Vector3(x + 0.5f + X_Offset, y + 0.5f + Y_Offset, z - 0.5f + Z_Offset));
                                        vertices.Add(new Vector3(x + 0.5f + X_Offset, y - 1f + Y_Offset, z - 0.5f + Z_Offset));

                                        AddQuadTriangles(ref Foliage_triangles);

                                        uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_South.x + tileSize - 0.005f, tileSize * blocktype[originBlock.ID].Texture_South.y + 0.005f));
                                        uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_South.x + tileSize - 0.005f, tileSize * blocktype[originBlock.ID].Texture_South.y + tileSize - 0.005f));
                                        uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_South.x + 0.005f, tileSize * blocktype[originBlock.ID].Texture_South.y + tileSize - 0.005f));
                                        uvs.Add(new Vector2(tileSize * blocktype[originBlock.ID].Texture_South.x + 0.005f, tileSize * blocktype[originBlock.ID].Texture_South.y + 0.005f));
                                    }
                                }
                            }
                        }

                        #region Marching Cubes
                        for (sbyte Layer = 0; Layer < MarchingCubesLayerAmount; Layer++)
                        {
                            #region Marching Cubes Algorithm

                            //Get the values in the 8 neighbours which make up a cube
                            for (int i = 0; i < 8; i++)
                            {
                                int ix = x + Table_VertexOffset[i * 3 + 0];
                                int iy = y + Table_VertexOffset[i * 3 + 1];
                                int iz = z + Table_VertexOffset[i * 3 + 2];

                                if (!blocktype[GetBlock(ix, iy, iz, LevelofDetail).ID].Foliage && blocktype[GetBlock(ix, iy, iz, LevelofDetail).ID].MarchingCubesLayer == Layer)
                                    Cube[i] = GetBlock(ix, iy, iz, LevelofDetail).GetMarchedValue();
                                else
                                    Cube[i] = 0f;
                            }

                            NativeArray<float3> EdgeVertex = new NativeArray<float3>(12, Allocator.Temp);
                            int flagIndex = 0;

                            for (int i2 = 0; i2 < 8; i2++) if (Cube[i2] <= Surface) flagIndex |= 1 << i2;   // Find which vertices are inside of the surface and which are outside
                            int edgeFlags = Table_CubeEdgeFlags[flagIndex];                                 // Find which edges are intersected by the surface
                            if (edgeFlags != 0)                                                             // If the cube is entirely inside or outside of the surface, then there will be no intersections
                            {
                                for (int i2 = 0; i2 < 12; i2++)                                             // Find the point of intersection of the surface with each edge
                                    if ((edgeFlags & (1 << i2)) != 0)                                       // if there is an intersection on this edge
                                    {
                                        float delta = Cube[Table_EdgeConnection[i2 * 2 + 1]] - Cube[Table_EdgeConnection[i2 * 2 + 0]];
                                        float offset = (delta == 0.0f) ? Surface : (Surface - Cube[Table_EdgeConnection[i2 * 2 + 0]]) / delta;
                                        float3 EV = new float3(
                                            x + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 0] + offset * Table_EdgeDirection[i2 * 3 + 0]),
                                            y + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 1] + offset * Table_EdgeDirection[i2 * 3 + 1]),
                                            z + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 2] + offset * Table_EdgeDirection[i2 * 3 + 2]));
                                        EdgeVertex[i2] = EV;
                                    }

                                int trisfound = 0;
                                for (int i2 = 0; i2 < 5; i2++)                                              // Save the triangles that were found. There can be up to five per cube
                                {
                                    if (Table_TriangleConnection[flagIndex * 16 + (3 * i2)] < 0) break;
                                    trisfound += 1;
                                    int idx = vertices.Length;

                                    for (int j = 0; j < 3; j++)
                                    {
                                        Vector2 UVpos = new Vector2(0f, 0f);

                                        NativeList<int> block_ids = new NativeList<int>(Allocator.Temp);
                                        for (int k = 0; k < 3; k++)
                                        {
                                            int3 tempCoordinates = new int3(
                                                (int)math.round(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].x),
                                                (int)math.round(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].y),
                                                (int)math.round(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].z));

                                            if (GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID > 0 && 
                                                GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).Marched)
                                                block_ids.Add(GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID);
                                        }

                                        if (block_ids.Length < 1)
                                        {
                                            for (int k = 0; k < 3; k++)
                                            {
                                                int3 tempCoordinates = new int3(
                                                    (int)math.floor(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].x),
                                                    (int)math.floor(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].y),
                                                    (int)math.floor(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].z));

                                                if (GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID > 0 &&
                                                    GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).Marched)
                                                    block_ids.Add(GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID);
                                            }
                                            if (block_ids.Length < 1)
                                            {
                                                for (int k = 0; k < 3; k++)
                                                {
                                                    int3 tempCoordinates = new int3(
                                                        (int)math.ceil(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].x),
                                                        (int)math.ceil(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].y),
                                                        (int)math.ceil(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + k)]].z));

                                                    if (GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID > 0 &&
                                                        GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).Marched)
                                                        block_ids.Add(GetBlock(tempCoordinates.x, tempCoordinates.y, tempCoordinates.z, LevelofDetail).ID);
                                                }
                                            }
                                        }

                                        if (block_ids.Length > 1)
                                        {
                                            FindLargestValueInArray(block_ids, out int largest_id, 0);
                                            UVpos = new Vector2(tileSize * blocktype[largest_id].Texture_Marched.x + tileSize - 0.005f, tileSize * blocktype[largest_id].Texture_Marched.y + tileSize - 0.005f);
                                            
                                        }
                                        else if (block_ids.Length == 1)
                                            UVpos = new Vector2(tileSize * blocktype[block_ids[0]].Texture_Marched.x + tileSize - 0.005f, tileSize * blocktype[block_ids[0]].Texture_Marched.y + 0.005f);

                                        block_ids.Dispose();

                                        SetMarchingTriangles(Layer, idx + WindingOrder[j]);
                                        vertices.Add(EdgeVertex[Table_TriangleConnection[flagIndex * 16 + (3 * i2 + j)]]);
                                        uvs.Add(UVpos);
                                    }
                                }
                            }
                            EdgeVertex.Dispose();
                            #endregion
                        }


                        #endregion
                    }

            Cube.Dispose();
            WindingOrder.Dispose();
        }
    }

    #endregion

    #region "Job System/Unity ECS - Chunk Generation"

    public void GenerateChunk()
    {
        if (world.worldGen != null)
        {
            world.worldGen.QueueChunk(this);
            /*if (!worldGen.IsGenerating && !world.MainMenuWorld)
            {
                worldGen.chunk = this;
                worldGen.ChunkCoordinates = new float3(pos.x, pos.y, pos.z);
                worldGen.GenerateChunk();
            }*/
        }
    }

    #endregion

}

public enum ChunkUpdateMode
{
    ForceSingle = 0,
    ForceNeighbours,
    QueueSingle,
    QueueNeighbours,
    QueueNeighboursForceSingle,
    QueueMarchingCubesFix,
    ForceMarchingCubesFix,
    DontUpdate
}

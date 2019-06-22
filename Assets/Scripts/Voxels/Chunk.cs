using MarchingCubesProject;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    [Header("Base Settings")]
    public World world;
    public WorldPos pos;
    [ReadOnly] public static int chunkSize = BlockData.ChunkSize;
    
    public Block[] blocks = new Block[4096];

    [Header("Sub-Chunk prefabs")]
    public GameObject Chunk_Water;
    public GameObject Chunk_Smoothed;

    [Header("Marching Cubes Settings")]
    [Range(0.501f, 1f)]
    public float MarchingCubesSmoothness = 1f;

    [Header("Force Update and Testing")]
    public bool update = false;
    public bool TestMapgen = false;

    [Header("Job System")]
    NativeArray<Block> Native_blocks;
    NativeArray<Block> Native_blocks2;
    NativeArray<Block> blocktypes;
    NativeArray<float> _counter;
    private JobHandle MapGen_JobHandle = new JobHandle();
    private JobHandle Render_JobHandle = new JobHandle();
    NativeList<Vector3> verts;
    NativeList<int> tris;
    NativeList<Vector2> uv;
    NativeList<int> March_tris;
    private bool IsGenerating = false;
    private bool IsRendering = false;


    [Header("Mesh")]
    MeshFilter filter;
    MeshCollider coll;
    
    void Awake()
    {
        filter = gameObject.GetComponent<MeshFilter>();
        coll = gameObject.GetComponent<MeshCollider>();

        blocktypes = new NativeArray<Block>(BlockData.byID.Count, Allocator.Persistent);
        blocktypes.CopyFrom(BlockData.byID.ToArray());
    }
    
    void Update()
    {
        if (update)
        {
            update = false;
            UpdateChunk();
        }
        if (TestMapgen)
        {
            TestMapgen = false;
            GenerateChunk();
        }
    }

    #region "Block Functions"

    public Block GetBlock(int x, int y, int z)
    {
        if (InRange(x) && InRange(y) && InRange(z))
            return blocks[x + y * 16 + z * 256];
        return world.GetBlock(pos.x + x, pos.y + y, pos.z + z);
    }

    public static bool InRange(int index)
    {
        if (index < 0 || index >= chunkSize)
            return false;

        return true;
    }

#pragma warning disable 168
    public void SetBlock(int x, int y, int z, Block block, bool Physics = false)
    {
        try {
            if (blocks[x + y * 16 + z * 256].GetID != 0)
            {
                //blocks[x, y, z].OnDelete(this, x, y, z);
            }
        } catch (Exception e) { }

        if (InRange(x) && InRange(y) && InRange(z))
        {
            blocks[x + y * 16 + z * 256] = block;
        }
        else
        {
            world.SetBlock(pos.x + x, pos.y + y, pos.z + z, block, 0, Physics);
        }

        try
        {
            if (blocks[x + y * 16 + z * 256].GetID != 0 && Physics == true)
            {
                //blocks[x, y, z].OnPlace(this, x, y, z);
            }

        }
        catch (Exception e)
        {
            //Debug.Log ("Can't run OnDelete() on block " + this.GetType ().ToString () + ", at " + x + ", " + y + ", " + z);
        }
        Debug.Log(blocks[x + y * 16 + z * 256].GetID + ", Conditions for update: Render_JobHandle.IsCompleted " + Render_JobHandle.IsCompleted + ", Native_blocks2.IsCreated " + Native_blocks2.IsCreated);
    }
#pragma warning restore 168

    public void SetBlockFromGen(int x, int y, int z, Block block, byte modifier = 0)
    {
        if (InRange(x) && InRange(y) && InRange(z))
        {
            blocks[x + y * 16 + z * 256] = block;
        }
        else
        {
            world.SetBlock(pos.x + x, pos.y + y, pos.z + z, block);
        }
    }

    #endregion
    
    #region "Job System - Main stuff"
    void OnDestroy()
    {
        if (!MapGen_JobHandle.IsCompleted) MapGen_JobHandle.Complete();
        if (!Render_JobHandle.IsCompleted) Render_JobHandle.Complete();
        if (Native_blocks.IsCreated) Native_blocks.Dispose();
        if (blocktypes.IsCreated) blocktypes.Dispose();
        if (verts.IsCreated) verts.Dispose();
        if (tris.IsCreated) tris.Dispose();
        if (March_tris.IsCreated) March_tris.Dispose();
        if (uv.IsCreated) uv.Dispose();
    }

    void LateUpdate()
    {
        if (IsGenerating)
        {
            IsGenerating = false;
            MapGen_JobHandle.Complete();
            blocks = new Block[Native_blocks.Length];
            Native_blocks.CopyTo(blocks);
            Native_blocks.Dispose();
            world.GeneratedChunks += 1;
        }
        if (IsRendering && Render_JobHandle.IsCompleted)
        {
            IsRendering = false;
            Render_JobHandle.Complete();

            filter.mesh.Clear();
            filter.mesh.subMeshCount = 2;

            filter.mesh.vertices = verts.ToArray();
            filter.mesh.SetTriangles(tris.ToArray(), 0);
            filter.mesh.SetTriangles(March_tris.ToArray(), 1);
            Vector2[] uvs = uv.ToArray();
            System.Array.Resize(ref uvs, verts.Length);
            filter.mesh.uv = uvs;
            filter.mesh.MarkDynamic();
            filter.mesh.RecalculateNormals();

            coll.sharedMesh = null;

            Mesh mesh = new Mesh();
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray().Concat(March_tris.ToArray()).ToArray();
            mesh.MarkDynamic();
            mesh.RecalculateNormals();

            coll.sharedMesh = mesh;

            verts.Dispose(); tris.Dispose(); uv.Dispose(); March_tris.Dispose();
        }
    }
    #endregion

    #region "Job System/Unity ECS - Chunk Rendering"

    void UpdateChunk()
    {
        Debug.Log("Before chunk update: " + Render_JobHandle.IsCompleted + ", " + Native_blocks2.IsCreated);
        if (Render_JobHandle.IsCompleted && !IsRendering)
        {
            Debug.Log("Updating chunk");
            NativeArray<Block> Chunk_MinusX = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), pos.y, pos.z) != null)
                Chunk_MinusX.CopyFrom(world.GetChunk((pos.x - 16), pos.y, pos.z).blocks);
            
            NativeArray<Block> Chunk_PlusX = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), pos.y, pos.z) != null)
                Chunk_PlusX.CopyFrom(world.GetChunk((pos.x + 16), pos.y, pos.z).blocks);

            NativeArray<Block> Chunk_MinusY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y - 16), pos.z) != null)
                Chunk_MinusY.CopyFrom(world.GetChunk(pos.x, (pos.y - 16), pos.z).blocks);

            NativeArray<Block> Chunk_PlusY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y + 16), pos.z) != null)
                Chunk_PlusY.CopyFrom(world.GetChunk(pos.x, (pos.y + 16), pos.z).blocks);

            NativeArray<Block> Chunk_MinusZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, pos.y, (pos.z - 16)) != null)
                Chunk_MinusZ.CopyFrom(world.GetChunk(pos.x, pos.y, (pos.z - 16)).blocks);

            NativeArray<Block> Chunk_PlusZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, pos.y, (pos.z + 16)) != null) 
                Chunk_PlusZ.CopyFrom(world.GetChunk(pos.x, pos.y, (pos.z + 16)).blocks);

            // CORNER CHUNKS (XZ)
            
            NativeArray<Block> Chunk_PlusXZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), pos.y, (pos.z + 16)) != null)
                Chunk_PlusXZ.CopyFrom(world.GetChunk((pos.x + 16), pos.y, (pos.z + 16)).blocks);

            NativeArray<Block> Chunk_PlusXMinusZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), pos.y, (pos.z - 16)) != null)
                Chunk_PlusXMinusZ.CopyFrom(world.GetChunk((pos.x + 16), pos.y, (pos.z - 16)).blocks);

            NativeArray<Block> Chunk_MinusXPlusZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), pos.y, (pos.z + 16)) != null)
                Chunk_MinusXPlusZ.CopyFrom(world.GetChunk((pos.x - 16), pos.y, (pos.z + 16)).blocks);

            NativeArray<Block> Chunk_MinusXZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), pos.y, (pos.z - 16)) != null)
                Chunk_MinusXZ.CopyFrom(world.GetChunk((pos.x - 16), pos.y, (pos.z - 16)).blocks);

            // CORNER CHUNKS (XY)

            NativeArray<Block> Chunk_PlusXY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y + 16), pos.z) != null)
                Chunk_PlusXY.CopyFrom(world.GetChunk((pos.x + 16), (pos.y + 16), pos.z).blocks);

            NativeArray<Block> Chunk_PlusXMinusY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y - 16), pos.z) != null)
                Chunk_PlusXMinusY.CopyFrom(world.GetChunk((pos.x + 16), (pos.y - 16), pos.z).blocks);

            NativeArray<Block> Chunk_MinusXPlusY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y + 16), pos.z) != null)
                Chunk_MinusXPlusY.CopyFrom(world.GetChunk((pos.x - 16), (pos.y + 16), pos.z).blocks);

            NativeArray<Block> Chunk_MinusXY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y - 16), pos.z) != null)
                Chunk_MinusXY.CopyFrom(world.GetChunk((pos.x - 16), (pos.y - 16), pos.z).blocks);

            // CORNER CHUNKS (ZY)

            NativeArray<Block> Chunk_PlusZY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y + 16), (pos.z + 16)) != null)
                Chunk_PlusZY.CopyFrom(world.GetChunk(pos.x, (pos.y + 16), (pos.z + 16)).blocks);

            NativeArray<Block> Chunk_PlusZMinusY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y - 16), (pos.z + 16)) != null)
                Chunk_PlusZMinusY.CopyFrom(world.GetChunk(pos.x, (pos.y - 16), (pos.z + 16)).blocks);

            NativeArray<Block> Chunk_MinusZPlusY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y + 16), (pos.z - 16)) != null)
                Chunk_MinusZPlusY.CopyFrom(world.GetChunk(pos.x, (pos.y + 16), (pos.z - 16)).blocks);

            NativeArray<Block> Chunk_MinusZY = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk(pos.x, (pos.y - 16), (pos.z - 16)) != null)
                Chunk_MinusZY.CopyFrom(world.GetChunk(pos.x, (pos.y - 16), (pos.z - 16)).blocks);

            // CORNER CHUNKS (XYZ)

            NativeArray<Block> Chunk_MinusY_PlusXZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y - 16), (pos.z + 16)) != null)
                Chunk_MinusY_PlusXZ.CopyFrom(world.GetChunk((pos.x + 16), (pos.y - 16), (pos.z + 16)).blocks);

            NativeArray<Block> Chunk_MinusY_MinusXZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y - 16), (pos.z - 16)) != null)
                Chunk_MinusY_MinusXZ.CopyFrom(world.GetChunk((pos.x - 16), (pos.y - 16), (pos.z - 16)).blocks);

            NativeArray<Block> Chunk_MinusY_MinusXPlusZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y - 16), (pos.z + 16)) != null)
                Chunk_MinusY_MinusXPlusZ.CopyFrom(world.GetChunk((pos.x - 16), (pos.y - 16), (pos.z + 16)).blocks);

            NativeArray<Block> Chunk_MinusY_PlusXMinusZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y - 16), (pos.z - 16)) != null)
                Chunk_MinusY_PlusXMinusZ.CopyFrom(world.GetChunk((pos.x + 16), (pos.y - 16), (pos.z - 16)).blocks);

            NativeArray<Block> Chunk_PlusY_PlusXZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y + 16), (pos.z + 16)) != null)
                Chunk_PlusY_PlusXZ.CopyFrom(world.GetChunk((pos.x + 16), (pos.y + 16), (pos.z + 16)).blocks);

            NativeArray<Block> Chunk_PlusY_MinusXZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y + 16), (pos.z - 16)) != null)
                Chunk_PlusY_MinusXZ.CopyFrom(world.GetChunk((pos.x - 16), (pos.y + 16), (pos.z - 16)).blocks);

            NativeArray<Block> Chunk_PlusY_MinusXPlusZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x - 16), (pos.y + 16), (pos.z + 16)) != null)
                Chunk_PlusY_MinusXPlusZ.CopyFrom(world.GetChunk((pos.x - 16), (pos.y + 16), (pos.z + 16)).blocks);

            NativeArray<Block> Chunk_PlusY_PlusXMinusZ = new NativeArray<Block>(4096, Allocator.TempJob);
            if (world.GetChunk((pos.x + 16), (pos.y + 16), (pos.z - 16)) != null)
                Chunk_PlusY_PlusXMinusZ.CopyFrom(world.GetChunk((pos.x + 16), (pos.y + 16), (pos.z - 16)).blocks);

            Native_blocks2 = new NativeArray<Block>(4096, Allocator.TempJob);
            Native_blocks2.CopyFrom(blocks);

            verts = new NativeList<Vector3>(Allocator.TempJob);
            tris = new NativeList<int>(Allocator.TempJob);
            uv = new NativeList<Vector2>(Allocator.TempJob);
            
            March_tris = new NativeList<int>(Allocator.TempJob);

            NativeArray<bool> GreedyBlocks_U = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_D = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_N = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_S = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_E = new NativeArray<bool>(4096, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_W = new NativeArray<bool>(4096, Allocator.TempJob);

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
                // TILING SIZE FOR TEXTURING
                tileSize = BlockData.BlockTileSize,

                chunkSize = chunkSize,

                // BLOCKDATA
                blocktype = blocktypes,

                // STANDART VOXEL MESHDATA
                vertices = verts,
                triangles = tris,
                uvs = uv,

                // BLOCKS OF TARGET AND NEIGHBOUR CHUNKS
                _blocks = Native_blocks2,
                _blocks_MinusX = Chunk_MinusX,
                _blocks_MinusY = Chunk_MinusY,
                _blocks_MinusZ = Chunk_MinusZ,
                _blocks_PlusX = Chunk_PlusX,
                _blocks_PlusY = Chunk_PlusY,
                _blocks_PlusZ = Chunk_PlusZ,

                // CORNER CHUNKS
                _blocks_MinusXPlusY = Chunk_MinusXPlusY,
                _blocks_MinusXPlusZ = Chunk_MinusXPlusZ,
                _blocks_MinusYPlusZ = Chunk_PlusZMinusY,
                _blocks_MinusXZ = Chunk_MinusXZ,
                _blocks_MinusXY = Chunk_MinusXY,
                _blocks_MinusYZ = Chunk_MinusZY,
                _blocks_PlusXMinusY = Chunk_PlusXMinusY,
                _blocks_PlusXMinusZ = Chunk_PlusXMinusZ,
                _blocks_PlusYMinusZ = Chunk_MinusZPlusY,
                _blocks_PlusXY = Chunk_PlusXY,
                _blocks_PlusYZ = Chunk_PlusZY,
                _blocks_PlusXZ = Chunk_PlusXZ,
                _blocks_MinusY_MinusXZ = Chunk_MinusY_MinusXZ,
                _blocks_MinusY_PlusXZ = Chunk_MinusY_PlusXZ,
                _blocks_MinusY_MinusX_PlusZ = Chunk_MinusY_MinusXPlusZ,
                _blocks_MinusY_PlusX_MinusZ = Chunk_MinusY_PlusXMinusZ,
                _blocks_PlusY_MinusXZ = Chunk_PlusY_MinusXZ,
                _blocks_PlusY_PlusXZ = Chunk_PlusY_PlusXZ,
                _blocks_PlusY_MinusX_PlusZ = Chunk_PlusY_MinusXPlusZ,
                _blocks_PlusY_PlusX_MinusZ = Chunk_PlusY_PlusXMinusZ,

                // USE GREEDY MESHING ON STANDART VOXELS (EXPERIMENTAL)
                UseGreedyMeshing = true,
                // GREEDY MESHING FACE FLAGS
                _blocks_greedy_U = GreedyBlocks_U,
                _blocks_greedy_D = GreedyBlocks_D,
                _blocks_greedy_N = GreedyBlocks_N,
                _blocks_greedy_S = GreedyBlocks_S,
                _blocks_greedy_E = GreedyBlocks_E,
                _blocks_greedy_W = GreedyBlocks_W,

                // MARCHING CUBES
                UseMarchingCubes = true,
                Table_CubeEdgeFlags = T_CubeEdgeFlags,
                Table_EdgeConnection = T_EdgeConnection,
                Table_EdgeDirection = T_EdgeDirection,
                Table_TriangleConnection = T_TriangleConnectionTable,
                Table_VertexOffset = T_VertexOffset,
                Marching_triangles = March_tris,
                MarchedBlocks = new NativeArray<float>(5832, Allocator.TempJob)

            };

            Render_JobHandle = job.Schedule();
            IsRendering = true;
            
        } else {
            if (IsRendering || Native_blocks2.IsCreated)
            {
                IsRendering = false;
                Render_JobHandle.Complete();

                filter.mesh.Clear();
                filter.mesh.subMeshCount = 2;

                filter.mesh.vertices = verts.ToArray();
                filter.mesh.SetTriangles(tris.ToArray(), 0);
                filter.mesh.SetTriangles(March_tris.ToArray(), 1);
                Vector2[] uvs = uv.ToArray();
                System.Array.Resize(ref uvs, verts.Length);
                filter.mesh.uv = uvs;
                filter.mesh.MarkDynamic();
                filter.mesh.RecalculateNormals();

                coll.sharedMesh = null;

                Mesh mesh = new Mesh();
                mesh.vertices = verts.ToArray();
                mesh.triangles = tris.ToArray().Concat(March_tris.ToArray()).ToArray();
                mesh.MarkDynamic();
                mesh.RecalculateNormals();

                coll.sharedMesh = mesh;

                March_tris.Dispose();
                verts.Dispose(); tris.Dispose(); uv.Dispose();
            } else {
                Render_JobHandle.Complete();
                IsRendering = false;
                if (verts.IsCreated) verts.Dispose();
                if (tris.IsCreated) tris.Dispose();
                if (uv.IsCreated) uv.Dispose();
                if (Native_blocks2.IsCreated) Native_blocks2.Dispose();
                if (March_tris.IsCreated) March_tris.Dispose();
                Debug.Log("Something weird happened with rendering code.");
            }
        }

    }

    [BurstCompile]
    private struct Job_RenderChunk : IJob
    {
        public int chunkSize;

        // MeshData to return
        public NativeList<Vector3> vertices;
        public NativeList<int> triangles;
        public NativeList<Vector2> uvs;

        // MeshData for Marching Cubes terrain
        public NativeList<int> Marching_triangles;

        // Size of a tile for texturing
        [ReadOnly] public float tileSize;

        // Blocktype data
        [ReadOnly] public NativeArray<Block> blocktype;

        // Block arrays from THIS chunk + neighbour chunks
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_PlusX;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_MinusX;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_PlusY;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_MinusY;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_PlusZ;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_MinusZ;

        // Corner chunks (XZ)
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_PlusXZ;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_MinusXZ;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_MinusXPlusZ;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Block> _blocks_PlusXMinusZ;

        // Corner chunks (XY)
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_PlusXY;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_MinusXY;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_MinusXPlusY;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_PlusXMinusY;

        // Corner chunks (YZ)
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_PlusYZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_MinusYZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_MinusYPlusZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_PlusYMinusZ;

        // Corner chunks (XYZ)
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_PlusY_PlusXZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_PlusY_MinusXZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_PlusY_MinusX_PlusZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_PlusY_PlusX_MinusZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_MinusY_PlusXZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_MinusY_MinusXZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_MinusY_MinusX_PlusZ;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Block> _blocks_MinusY_PlusX_MinusZ;
        
        // +X-Z                +X                 +X+Z  Corner blocks are needed only for marching cubes.
        //      ┌─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┐       For regular voxels they are redundant, as you only have to check +X, -X, +Y, -Y, +Z, and -Z faces.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       With marching cubes, you actually have to check for every neighbour block.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Otherwise you'll end up with holes in your chunk.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤ 
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Every surrounding chunk is converted into NativeArray<Block> and then fed into resized chunk.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Resized chunk is [ChunkSize + 1] and called MCS (Marching Cubes Scaled)
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Then the surrounding chunk's neighbour blocks to our worker chunk are getting copied into MCS.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       
        //   -Z ├─┼─┼─┼─┼─┼─┼ 16 ^3 ┼─┼─┼─┼─┼─┼─┤ +Z    Then each block is analyzed for marching cubes algorithm.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Iterating goes from (0) to (ChunkSize) for X and Z, and from (-1) to (ChunkSize - 1)
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Why not everything from (-1) to (ChunkSize)? Because marching cubes meshes would overlap.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Not that it's a problem (because it wouldn't be) other than more tris on scene.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Just because of that, we are skipping the iterations.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       Also Y goes from (-1) to (ChunkSize - 1) because the ceiling is broken. I have to fix that.
        //      ├─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┼─┤       
        //      └─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┘       Anyways, algorithm is now working properly, but it STILL generates weird artifacts, i might now know what causes it
        // -X-Z                -X                 -X+Z  but i can't seem to find the issue in the code, it's probably something deeper than i think it is.

        // Greedy mesh flag for every direction of block
        [DeallocateOnJobCompletion] public NativeArray<bool> _blocks_greedy_U;
        [DeallocateOnJobCompletion] public NativeArray<bool> _blocks_greedy_D;
        [DeallocateOnJobCompletion] public NativeArray<bool> _blocks_greedy_N;
        [DeallocateOnJobCompletion] public NativeArray<bool> _blocks_greedy_S;
        [DeallocateOnJobCompletion] public NativeArray<bool> _blocks_greedy_E;
        [DeallocateOnJobCompletion] public NativeArray<bool> _blocks_greedy_W;

        // RENDERING OPTIONS
        [ReadOnly] public bool UseGreedyMeshing; //TODO: Fix texturing (it's all stretched now) - Idea: Divide the mesh into submeshes and apply tri-planar shader for texturing.
        [ReadOnly] public bool UseMarchingCubes; //TODO: Fix the +X -X corners, it's making weird artifacts now. Also allow to divide mesh into submeshes, so you can have independent blocks
                                                 //      marched together

        // MARCHING CUBES, Define this ONLY if you want to perform marching, otherwise, don't.
        [DeallocateOnJobCompletion] public NativeArray<float> MarchedBlocks;

        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<int> Table_EdgeConnection;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<float> Table_EdgeDirection;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<int> Table_CubeEdgeFlags;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<int> Table_TriangleConnection;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<int> Table_VertexOffset;

        public static int GetAddress(int x, int y, int z, int size = 16)
        {
            return (x + y * size + z * size * size);
        }

        public void Execute()
        {
            Block tbmx, tbpx, tbmy, tbpy, tbmz, tbpz, tb;
            int MCS = chunkSize + 1;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        if (_blocks[GetAddress(x,y,z)].GetID != 0) {
                            if (_blocks[x + y * 16 + z * 256].GetID == 1) {
                                // TERRAIN ( Marching Cubes )
                                if (UseMarchingCubes)
                                {
                                    // TODO: Think of changing the 1f value to random OR average of neighbour 3-4 blocks
                                    MarchedBlocks[GetAddress(x + 1, y + 1, z + 1, MCS)] = 1f;
                                }
                            } else if (_blocks[GetAddress(x, y, z)].GetID == 2) {
                                // WATER
                                if (UseMarchingCubes)
                                {
                                    //MarchedBlocks_Water[x + y * 16 + z * 256] = 1f;
                                }
                            } else if (_blocks[GetAddress(x, y, z)].GetID > 10) {
                                if (x == 0) tbmx = _blocks_MinusX[GetAddress(chunkSize - 1, y, z)]; else tbmx = _blocks[GetAddress(x - 1, y, z)];
                                if (x == chunkSize - 1) tbpx = _blocks_PlusX[GetAddress(0, y, z)]; else tbpx = _blocks[GetAddress(x + 1, y, z)];
                                if (y == 0) tbmy = _blocks_MinusY[GetAddress(x, chunkSize - 1, z)]; else tbmy = _blocks[GetAddress(x, y - 1, z)];
                                if (y == chunkSize - 1) tbpy = _blocks_PlusY[GetAddress(x, 0, z)]; else tbpy = _blocks[GetAddress(x, y + 1, z)];
                                if (z == 0) tbmz = _blocks_MinusZ[GetAddress(x, y, chunkSize - 1)]; else tbmz = _blocks[GetAddress(x, y, z - 1)];
                                if (z == chunkSize - 1) tbpz = _blocks_PlusZ[GetAddress(x, y, 0)]; else tbpz = _blocks[GetAddress(x, y, z + 1)];

                                tb = _blocks[GetAddress(x, y, z)];


                                if (UseGreedyMeshing)
                                {
                                    // XZ - Up
                                    // Check if tb (thisblock) is solid, tbpy (temporary block plus Y) is solid and if this block is not already marked as greedy.
                                    if (tb.Solid && !tbpy.Solid && !_blocks_greedy_U[x + y * 16 + z * 256])
                                    {
                                        // Declare maximum value for Z, just for first iteration it has to be ChunkSize-1
                                        int max_z = 15;
                                        // Temporary maximum value for Z, gets bigger only at first iteration and then moves into max_z
                                        int temp_max_z = 15;
                                        // greed_x is just a value to store vertex's max X position
                                        int greed_x = x;
                                        // and last but not least, bcoken. This is needed to break out of both loops (X and Z)
                                        bool broken = false;

                                        // Iterate X -> ChunkSize
                                        for (int x_greedy = x; x_greedy < chunkSize; x_greedy++)
                                        {
                                            // Check if current's iteration X is bigger than starting X and if first block in this iteration is the same, if not - break.
                                            if (x_greedy > x && _blocks[x_greedy + y * 16 + z * 256].GetID != tb.GetID) break;
                                            // Iterate Z -> Max_Z (ChunkSize - 1 for first iteration)
                                            for (int z_greedy = z; z_greedy <= max_z; z_greedy++)
                                            {
                                                // Macro to Y + 1 block to see if it's solid. If current Y == 15, get Y = 0 at Y + 1 chunk
                                                bool PlusY;
                                                if (y == 15) PlusY = _blocks_PlusY[x_greedy + z_greedy * 256].Solid;
                                                else PlusY = _blocks[x_greedy + (y + 1) * 16 + z_greedy * 256].Solid;

                                                // Check if this block is marked as greedy, compare blockID with starting block and check if PlusY is solid.
                                                if (!_blocks_greedy_U[x_greedy + y * 16 + z_greedy * 256] && tb.GetID == _blocks[x_greedy + y * 16 + z_greedy * 256].GetID && !PlusY)
                                                {
                                                    // Set the temporary value of Max_Z to current iteration of Z
                                                    if (x_greedy == x) temp_max_z = z_greedy;
                                                    // Mark the current block as greedy
                                                    _blocks_greedy_U[x_greedy + y * 16 + z_greedy * 256] = true;
                                                }
                                                else
                                                {
                                                    // If block in current iteration was different or already greedy, break
                                                    // Then, reverse last iteration to non-greedy state.
                                                    if (z_greedy <= max_z && x_greedy > x)
                                                    {
                                                        for (int z1 = z; z1 < z_greedy; z1++)
                                                        {
                                                            // Reverse the greedy to false
                                                            _blocks_greedy_U[x_greedy + y * 16 + z1 * 256] = false;
                                                        }
                                                        // Break out of both loops
                                                        broken = true;
                                                    }
                                                    break;
                                                }
                                            }
                                            // Next, after an iterations is done (or broken), move the temporary value of Max_Z to non-temporary
                                            max_z = temp_max_z;
                                            if (broken) break;
                                            // If both loops weren't broken, set vertex's max X value to current X iteration
                                            greed_x = x_greedy;
                                        }

                                        // Create the vertices
                                        vertices.Add(new Vector3(x - 0.5f, y + 0.5f, max_z + 0.5f));
                                        vertices.Add(new Vector3(greed_x + 0.5f, y + 0.5f, max_z + 0.5f));
                                        vertices.Add(new Vector3(greed_x + 0.5f, y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
                                        // This is basically "AddQuadTriangles", but you can't refference MeshData inside a Job, so i had to copy this over from
                                        // MeshData.cs
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        // And lastly, set the textures onto those triangles.
                                        
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Up.x + tileSize - 0.001f, tileSize * tb.Texture_Up.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Up.x + tileSize - 0.001f, tileSize * tb.Texture_Up.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Up.x + 0.001f, tileSize * tb.Texture_Up.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Up.x + 0.001f, tileSize * tb.Texture_Up.y + 0.001f));
                                    }

                                    // XZ - Down
                                    if (tb.Solid && !tbmy.Solid && !_blocks_greedy_D[x + y * 16 + z * 256])
                                    {
                                        int max_z = 15;
                                        int temp_max_z = 15;
                                        int greed_x = x;
                                        bool broken = false;

                                        for (int x_greedy = x; x_greedy < chunkSize; x_greedy++)
                                        {
                                            if (x_greedy > x && _blocks[x_greedy + y * 16 + z * 256].GetID != tb.GetID) break;

                                            for (int z_greedy = z; z_greedy <= max_z; z_greedy++)
                                            {
                                                bool MinusY;
                                                if (y == 0) MinusY = _blocks_MinusY[x_greedy + (15 * 16) + z_greedy * 256].Solid;
                                                else MinusY = _blocks[x_greedy + (y - 1) * 16 + z_greedy * 256].Solid;

                                                if (!_blocks_greedy_D[x_greedy + y * 16 + z_greedy * 256] && tb.GetID == _blocks[x_greedy + y * 16 + z_greedy * 256].GetID && !MinusY)
                                                {
                                                    if (x_greedy == x) temp_max_z = z_greedy;
                                                    _blocks_greedy_D[x_greedy + y * 16 + z_greedy * 256] = true;

                                                }
                                                else
                                                {
                                                    if (z_greedy <= max_z && x_greedy > x)
                                                    {
                                                        for (int z1 = z; z1 < z_greedy; z1++)
                                                        {
                                                            _blocks_greedy_D[x_greedy + y * 16 + z1 * 256] = false;
                                                        }
                                                        broken = true;
                                                    }
                                                    break;
                                                }
                                            }
                                            max_z = temp_max_z;
                                            if (broken) break;
                                            greed_x = x_greedy;
                                        }

                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(greed_x + 0.5f, y - 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(greed_x + 0.5f, y - 0.5f, max_z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, max_z + 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Down.x + tileSize - 0.001f, tileSize * tb.Texture_Down.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Down.x + tileSize - 0.001f, tileSize * tb.Texture_Down.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Down.x + 0.001f, tileSize * tb.Texture_Down.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Down.x + 0.001f, tileSize * tb.Texture_Down.y + 0.001f));
                                    }

                                    // XY - East
                                    if (tb.Solid && !tbpz.Solid && !_blocks_greedy_W[x + y * 16 + z * 256])
                                    {
                                        int max_y = 15;
                                        int temp_max_y = 15;
                                        int greed_x = x;
                                        bool broken = false;

                                        for (int x_greedy = x; x_greedy < chunkSize; x_greedy++)
                                        {
                                            if (x_greedy > x && _blocks[x_greedy + y * 16 + z * 256].GetID != tb.GetID) break;

                                            for (int y_greedy = y; y_greedy <= max_y; y_greedy++)
                                            {
                                                bool PlusZ;
                                                if (z == 15) PlusZ = _blocks_PlusZ[x_greedy + y_greedy * 16].Solid;
                                                else PlusZ = _blocks[x_greedy + y_greedy * 16 + (z + 1) * 256].Solid;

                                                if (!_blocks_greedy_W[x_greedy + y_greedy * 16 + z * 256] && tb.GetID == _blocks[x_greedy + y_greedy * 16 + z * 256].GetID && !PlusZ)
                                                {
                                                    if (x_greedy == x) temp_max_y = y_greedy;
                                                    _blocks_greedy_W[x_greedy + y_greedy * 16 + z * 256] = true;

                                                }
                                                else
                                                {
                                                    if (y_greedy <= max_y && x_greedy > x)
                                                    {
                                                        for (int y1 = z; y1 < y_greedy; y1++)
                                                        {
                                                            _blocks_greedy_W[x_greedy + y1 * 16 + z * 256] = false;
                                                        }
                                                        broken = true;
                                                    }
                                                    break;
                                                }
                                            }
                                            max_y = temp_max_y;
                                            if (broken) break;
                                            greed_x = x_greedy;
                                        }

                                        vertices.Add(new Vector3(greed_x + 0.5f, y - 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(greed_x + 0.5f, max_y + 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, max_y + 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                    }

                                    // XY - West
                                    if (tb.Solid && !tbmz.Solid && !_blocks_greedy_E[x + y * 16 + z * 256])
                                    {
                                        int max_y = 15;
                                        int temp_max_y = 15;
                                        int greed_x = x;
                                        bool broken = false;

                                        for (int x_greedy = x; x_greedy < chunkSize; x_greedy++)
                                        {
                                            if (x_greedy > x && _blocks[x_greedy + y * 16 + z * 256].GetID != tb.GetID) break;

                                            for (int y_greedy = y; y_greedy <= max_y; y_greedy++)
                                            {
                                                bool MinusZ;
                                                if (z == 0) MinusZ = _blocks_MinusZ[x_greedy + y_greedy * 16 + 15 * 256].Solid;
                                                else MinusZ = _blocks[x_greedy + y_greedy * 16 + (z - 1) * 256].Solid;

                                                if (!_blocks_greedy_E[x_greedy + y_greedy * 16 + z * 256] && tb.GetID == _blocks[x_greedy + y_greedy * 16 + z * 256].GetID && !MinusZ)
                                                {
                                                    if (x_greedy == x) temp_max_y = y_greedy;
                                                    _blocks_greedy_E[x_greedy + y_greedy * 16 + z * 256] = true;

                                                }
                                                else
                                                {
                                                    if (y_greedy <= max_y && x_greedy > x)
                                                    {
                                                        for (int y1 = z; y1 < y_greedy; y1++)
                                                        {
                                                            _blocks_greedy_E[x_greedy + y1 * 16 + z * 256] = false;
                                                        }
                                                        broken = true;
                                                    }
                                                    break;
                                                }
                                            }
                                            max_y = temp_max_y;
                                            if (broken) break;
                                            greed_x = x_greedy;
                                        }
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, max_y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(greed_x + 0.5f, max_y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(greed_x + 0.5f, y - 0.5f, z - 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                    }

                                    // YZ - North
                                    if (tb.Solid && !tbpx.Solid && !_blocks_greedy_N[x + y * 16 + z * 256])
                                    {
                                        int max_z = 15;
                                        int temp_max_z = 15;
                                        int greed_y = y;
                                        bool broken = false;

                                        for (int y_greedy = y; y_greedy < chunkSize; y_greedy++)
                                        {
                                            if (y_greedy > y && _blocks[x + y_greedy * 16 + z * 256].GetID != tb.GetID) break;

                                            for (int z_greedy = z; z_greedy <= max_z; z_greedy++)
                                            {
                                                bool PlusX;
                                                if (x == 15) PlusX = _blocks_PlusX[y_greedy * 16 + z_greedy * 256].Solid;
                                                else PlusX = _blocks[(x + 1) + y_greedy * 16 + z_greedy * 256].Solid;

                                                if (!_blocks_greedy_N[x + y_greedy * 16 + z_greedy * 256] && tb.GetID == _blocks[x + y_greedy * 16 + z_greedy * 256].GetID && !PlusX)
                                                {
                                                    if (y_greedy == y) temp_max_z = z_greedy;
                                                    _blocks_greedy_N[x + y_greedy * 16 + z_greedy * 256] = true;

                                                }
                                                else
                                                {
                                                    if (z_greedy <= max_z && y_greedy > y)
                                                    {
                                                        for (int z1 = z; z1 < z_greedy; z1++)
                                                        {
                                                            _blocks_greedy_N[x + y_greedy * 16 + z1 * 256] = false;
                                                        }
                                                        broken = true;
                                                    }
                                                    break;
                                                }
                                            }
                                            max_z = temp_max_z;
                                            if (broken) break;
                                            greed_y = y_greedy;
                                        }

                                        vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, greed_y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, greed_y + 0.5f, max_z + 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y - 0.5f, max_z + 0.5f));

                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                    }

                                    // YZ - South
                                    if (tb.Solid && !tbmx.Solid && !_blocks_greedy_S[x + y * 16 + z * 256])
                                    {
                                        int max_z = 15;
                                        int temp_max_z = 15;
                                        int greed_y = y;
                                        bool broken = false;

                                        for (int y_greedy = y; y_greedy < chunkSize; y_greedy++)
                                        {
                                            if (y_greedy > y && _blocks[x + y_greedy * 16 + z * 256].GetID != tb.GetID) break;

                                            for (int z_greedy = z; z_greedy <= max_z; z_greedy++)
                                            {
                                                bool MinusX;
                                                if (x == 0) MinusX = _blocks_MinusX[15 + y_greedy * 16 + z_greedy * 256].Solid;
                                                else MinusX = _blocks[(x - 1) + y_greedy * 16 + z_greedy * 256].Solid;

                                                if (!_blocks_greedy_S[x + y_greedy * 16 + z_greedy * 256] && tb.GetID == _blocks[x + y_greedy * 16 + z_greedy * 256].GetID && !MinusX)
                                                {
                                                    if (y_greedy == y) temp_max_z = z_greedy;
                                                    _blocks_greedy_S[x + y_greedy * 16 + z_greedy * 256] = true;

                                                }
                                                else
                                                {
                                                    if (z_greedy <= max_z && y_greedy > y)
                                                    {
                                                        for (int z1 = z; z1 < z_greedy; z1++)
                                                        {
                                                            _blocks_greedy_S[x + y_greedy * 16 + z1 * 256] = false;
                                                        }
                                                        broken = true;
                                                    }
                                                    break;
                                                }
                                            }
                                            max_z = temp_max_z;
                                            if (broken) break;
                                            greed_y = y_greedy;
                                        }

                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, max_z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, greed_y + 0.5f, max_z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, greed_y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                    }

                                } else {

                                    // Standart culled meshing
                                    // No algorithm needed.

                                    if (tb.Solid && !tbpy.Solid && ((tbpy.GetID == 0) || ((tbpy.GetID > 1) && (tbpy.GetID < 11))))
                                    {
                                        vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Up.x + tileSize - 0.001f, tileSize * tb.Texture_Up.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Up.x + tileSize - 0.001f, tileSize * tb.Texture_Up.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Up.x + 0.001f, tileSize * tb.Texture_Up.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Up.x + 0.001f, tileSize * tb.Texture_Up.y + 0.001f));
                                    }

                                    if (tb.Solid && !tbmy.Solid && ((tbmy.GetID == 0) || ((tbmy.GetID > 1) && (tbmy.GetID < 11))))
                                    {
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Down.x + tileSize - 0.001f, tileSize * tb.Texture_Down.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Down.x + tileSize - 0.001f, tileSize * tb.Texture_Down.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Down.x + 0.001f, tileSize * tb.Texture_Down.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_Down.x + 0.001f, tileSize * tb.Texture_Down.y + 0.001f));
                                    }

                                    if (tb.Solid && !tbpz.Solid && ((tbpz.GetID == 0) || ((tbpz.GetID > 1) && (tbpz.GetID < 11))))
                                    {
                                        vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + tileSize - 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_North.x + 0.001f, tileSize * tb.Texture_North.y + 0.001f));
                                    }

                                    if (tb.Solid && !tbmz.Solid && ((tbmz.GetID == 0) || ((tbmz.GetID > 1) && (tbmz.GetID < 11))))
                                    {
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_South.x + tileSize - 0.001f, tileSize * tb.Texture_South.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_South.x + tileSize - 0.001f, tileSize * tb.Texture_South.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_South.x + 0.001f, tileSize * tb.Texture_South.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_South.x + 0.001f, tileSize * tb.Texture_South.y + 0.001f));
                                    }

                                    if (tb.Solid && !tbpx.Solid && ((tbpx.GetID == 0) || ((tbpx.GetID > 1) && (tbpx.GetID < 11))))
                                    {
                                        vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_East.x + tileSize - 0.001f, tileSize * tb.Texture_East.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_East.x + tileSize - 0.001f, tileSize * tb.Texture_East.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_East.x + 0.001f, tileSize * tb.Texture_East.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_East.x + 0.001f, tileSize * tb.Texture_East.y + 0.001f));
                                    }

                                    if (tb.Solid && !tbmx.Solid && ((tbmx.GetID == 0) || ((tbmx.GetID > 1) && (tbmx.GetID < 11))))
                                    {
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
                                        vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 3);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 4);
                                        triangles.Add(vertices.Length - 2);
                                        triangles.Add(vertices.Length - 1);
                                        uvs.Add(new Vector2(tileSize * tb.Texture_West.x + tileSize - 0.001f, tileSize * tb.Texture_West.y + 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_West.x + tileSize - 0.001f, tileSize * tb.Texture_West.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_West.x + 0.001f, tileSize * tb.Texture_West.y + tileSize - 0.001f));
                                        uvs.Add(new Vector2(tileSize * tb.Texture_West.x + 0.001f, tileSize * tb.Texture_West.y + 0.001f));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // MARCHING CUBES HERE
            if (UseMarchingCubes)
            {
                int ix, iy, iz, vert, idx;
                
                // FILL THE CORNER BLOCKS
                for (int i = 0; i < chunkSize; i++) {
                    for (int i2 = 0; i2 < chunkSize; i2++) {
                        if      (_blocks_MinusX[GetAddress(chunkSize - 1,             i,            i2)].GetID == 1) MarchedBlocks[GetAddress(0,      i + 1, i2 + 1, MCS)] = 1f;
                        if       (_blocks_PlusX[GetAddress(            0,             i,            i2)].GetID == 1) MarchedBlocks[GetAddress(MCS,    i + 1, i2 + 1, MCS)] = 1f;
                        if      (_blocks_MinusY[GetAddress(            i, chunkSize - 1,            i2)].GetID == 1) MarchedBlocks[GetAddress(i + 1,      0, i2 + 1, MCS)] = 1f;
                        if       (_blocks_PlusY[GetAddress(            i,             0,            i2)].GetID == 1) MarchedBlocks[GetAddress(i + 1,    MCS, i2 + 1, MCS)] = 1f;
                        if      (_blocks_MinusZ[GetAddress(            i,            i2, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(i + 1, i2 + 1,      0, MCS)] = 1f;
                        if       (_blocks_PlusZ[GetAddress(            i,            i2,             0)].GetID == 1) MarchedBlocks[GetAddress(i + 1, i2 + 1,    MCS, MCS)] = 1f;
                    }
                }
                for (int i = 0; i < chunkSize; i++) {
                    // XZ
                    if         (_blocks_MinusXZ[GetAddress(chunkSize - 1,             i, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(0,      i + 1,      0, MCS)] = 1f;
                    if          (_blocks_PlusXZ[GetAddress(            0,             i,             0)].GetID == 1) MarchedBlocks[GetAddress(MCS,    i + 1,    MCS, MCS)] = 1f;
                    if     (_blocks_MinusXPlusZ[GetAddress(chunkSize - 1,             i,             0)].GetID == 1) MarchedBlocks[GetAddress(0,      i + 1,    MCS, MCS)] = 1f;
                    if     (_blocks_PlusXMinusZ[GetAddress(            0,             i, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(MCS,    i + 1,      0, MCS)] = 1f;
                    // XY
                    if         (_blocks_MinusXY[GetAddress(chunkSize - 1, chunkSize - 1,             i)].GetID == 1) MarchedBlocks[GetAddress(0,          0,  i + 1, MCS)] = 1f;
                    if          (_blocks_PlusXY[GetAddress(            0,             0,             i)].GetID == 1) MarchedBlocks[GetAddress(MCS,      MCS,  i + 1, MCS)] = 1f;
                    if     (_blocks_MinusXPlusY[GetAddress(chunkSize - 1,             0,             i)].GetID == 1) MarchedBlocks[GetAddress(0,        MCS,  i + 1, MCS)] = 1f;
                    if     (_blocks_PlusXMinusY[GetAddress(            0, chunkSize - 1,             i)].GetID == 1) MarchedBlocks[GetAddress(MCS,        0,  i + 1, MCS)] = 1f;
                    // YZ
                    if         (_blocks_MinusYZ[GetAddress(            i, chunkSize - 1, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(i + 1,      0,      0, MCS)] = 1f;
                    if          (_blocks_PlusYZ[GetAddress(            i,             0,             0)].GetID == 1) MarchedBlocks[GetAddress(i + 1,    MCS,    MCS, MCS)] = 1f;
                    if     (_blocks_MinusYPlusZ[GetAddress(            i, chunkSize - 1,             0)].GetID == 1) MarchedBlocks[GetAddress(i + 1,      0,    MCS, MCS)] = 1f;
                    if     (_blocks_PlusYMinusZ[GetAddress(            i,             0, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(i + 1,    MCS,      0, MCS)] = 1f;
                }
                // Corners (XYZ)
                if        (_blocks_PlusY_PlusXZ[GetAddress(            0,             0,             0)].GetID == 1) MarchedBlocks[GetAddress(MCS,      MCS,    MCS, MCS)] = 1f;
                if       (_blocks_MinusY_PlusXZ[GetAddress(            0, chunkSize - 1,             0)].GetID == 1) MarchedBlocks[GetAddress(MCS,        0,    MCS, MCS)] = 1f;
                if      (_blocks_MinusY_MinusXZ[GetAddress(chunkSize - 1, chunkSize - 1, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(0,          0,      0, MCS)] = 1f;
                if       (_blocks_PlusY_MinusXZ[GetAddress(chunkSize - 1,             0, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(0,        MCS,      0, MCS)] = 1f;
                if  (_blocks_PlusY_MinusX_PlusZ[GetAddress(chunkSize - 1,             0,             0)].GetID == 1) MarchedBlocks[GetAddress(0,        MCS,    MCS, MCS)] = 1f;
                if (_blocks_MinusY_MinusX_PlusZ[GetAddress(chunkSize - 1, chunkSize - 1,             0)].GetID == 1) MarchedBlocks[GetAddress(0,          0,    MCS, MCS)] = 1f;
                if  (_blocks_PlusY_PlusX_MinusZ[GetAddress(            0,             0, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(MCS,      MCS,      0, MCS)] = 1f;
                if (_blocks_MinusY_PlusX_MinusZ[GetAddress(            0, chunkSize - 1, chunkSize - 1)].GetID == 1) MarchedBlocks[GetAddress(MCS,        0,      0, MCS)] = 1f;
                // END OF FILLING CORNER BLOCKS


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

                //TODO: Fix the weird slope on +X and -X corner (+X+Y, -X+Y)
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int y = -1; y < chunkSize - 1; y++)
                    {
                        for (int z = 0; z < chunkSize; z++)
                        {
                            
                                
                            //Get the values in the 8 neighbours which make up a cube
                            for (int i = 0; i < 8; i++)
                            {
                                ix = x+1 + Table_VertexOffset[i*3 + 0];
                                iy = y+1 + Table_VertexOffset[i*3 + 1];
                                iz = z+1 + Table_VertexOffset[i*3 + 2];

                                Cube[i] = MarchedBlocks[ix + iy * MCS + iz * MCS * MCS];
                            }

                            //Perform algorithm
                            NativeArray<float3> EdgeVertex = new NativeArray<float3>(12, Allocator.Temp);
                            //Vector3[] EdgeVertex = new Vector3[12];
                            
                            int flagIndex = 0;
                            float offset = 0.0f;

                            //Find which vertices are inside of the surface and which are outside
                            for (int i2 = 0; i2 < 8; i2++) if (Cube[i2] <= Surface) flagIndex |= 1 << i2;

                            //Find which edges are intersected by the surface
                            int edgeFlags = Table_CubeEdgeFlags[flagIndex];

                            //If the cube is entirely inside or outside of the surface, then there will be no intersections
                            if (edgeFlags != 0)
                            {

                                //Find the point of intersection of the surface with each edge
                                for (int i2 = 0; i2 < 12; i2++)
                                {
                                    //if there is an intersection on this edge
                                    if ((edgeFlags & (1 << i2)) != 0)
                                    {
                                        float delta = Cube[Table_EdgeConnection[i2 * 2 + 1]] - Cube[Table_EdgeConnection[i2 * 2 + 0]];

                                        offset = (delta == 0.0f) ? Surface : (Surface - Cube[Table_EdgeConnection[i2 * 2 + 0]]) / delta;
                                        float3 EV = new float3(x + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 0] + offset * Table_EdgeDirection[i2 * 3 + 0]),
                                            y + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 1] + offset * Table_EdgeDirection[i2 * 3 + 1]),
                                            z + (Table_VertexOffset[Table_EdgeConnection[i2 * 2 + 0] * 3 + 2] + offset * Table_EdgeDirection[i2 * 3 + 2]));
                                        EdgeVertex[i2] = EV;
                                    }
                                }

                                //Save the triangles that were found. There can be up to five per cube
                                for (int i2 = 0; i2 < 5; i2++)
                                {
                                    if (Table_TriangleConnection[flagIndex * 16 + (3 * i2)] < 0) break;

                                    idx = vertices.Length;

                                    for (int j = 0; j < 3; j++)
                                    {
                                        vert = Table_TriangleConnection[flagIndex * 16 + (3 * i2 + j)];
                                        Marching_triangles.Add(idx + WindingOrder[j]);
                                        vertices.Add(EdgeVertex[vert]);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            // END OF MARCHING CUBES
        }
    }

    #endregion

    #region "Job System/Unity ECS - Chunk Generation"

    public void GenerateChunk()
    {
        if (MapGen_JobHandle.IsCompleted && !Native_blocks.IsCreated)
        {
            Native_blocks = new NativeArray<Block>(4096, Allocator.TempJob);
            
            _counter = new NativeArray<float>(30, Allocator.TempJob);
            float3 _ChunkCoordinates = new float3(pos.x, pos.y, pos.z);
            float Height_Dunes_ = (16) * 0.52f;
            float Height_Sand_ = (16) * 0.52f;
            float Height_Water_ = (16) * 0.5f;
            float Height_Dirt_ = (16) * 0.3f;
            float Height_Mountain_ = (16) * 0.7f;

            var job = new WorldGen()
            {
                random = new Unity.Mathematics.Random(0x6E624EB7u),
                _blocksNew = Native_blocks,
                Counter = _counter,
                ChunkCoordinates = new Vector3(pos.x, pos.y, pos.z),
                World_Size = new int3(16, 16, 16),
                Height_Dunes = Height_Dunes_,
                Height_Sand = Height_Sand_,
                Height_Water = Height_Water_,
                Height_Dirt = Height_Dirt_,
                Height_Mountain = Height_Mountain_,
                blocktype = blocktypes // BlockData values
            };

            MapGen_JobHandle = job.Schedule();
            IsGenerating = true;
            //MapGen_JobHandle.Complete();
            //blocks = new Block[Native_blocks.Length];
            //Native_blocks.CopyTo(blocks);
            //Native_blocks.Dispose();

            //Debug.Log("Mapgen done. " + this.gameObject.name);
        }
    }

    [BurstCompile]
    private struct WorldGen : IJob
    {
        [DeallocateOnJobCompletion][ReadOnly] public Unity.Mathematics.Random random;

        [DeallocateOnJobCompletion][ReadOnly] public float3 ChunkCoordinates;
        
        [DeallocateOnJobCompletion] public NativeArray<float> Counter;

        [DeallocateOnJobCompletion][ReadOnly] public int3 World_Size;

        [DeallocateOnJobCompletion][ReadOnly] public float Height_Dunes;
        [DeallocateOnJobCompletion][ReadOnly] public float Height_Sand;
        [DeallocateOnJobCompletion][ReadOnly] public float Height_Water;

        [DeallocateOnJobCompletion][ReadOnly] public float Height_Dirt;
        [DeallocateOnJobCompletion][ReadOnly] public float Height_Mountain;

        [ReadOnly] public NativeArray<Block> blocktype;

        public NativeArray<Block> _blocksNew;
        
        public void Execute()
        {
            /*byte iterations = 3;
            int size = 2;

            //_blocks[x + y * 16 + z * 256] = 1;
            if (World_Size.x <= 512 && World_Size.z >= 512)
                iterations = 8;
            else if (World_Size.x <= 256 && World_Size.z >= 256)
                iterations = 7;
            else if (World_Size.x <= 128 && World_Size.z >= 128)
                iterations = 6;
            else if (World_Size.x <= 64 && World_Size.z >= 64)
                iterations = 5;
            else if (World_Size.x <= 32 && World_Size.z >= 32)
                iterations = 4;
            else if (World_Size.x <= 16 && World_Size.z >= 16)
                iterations = 3;
            
            for (int ix = 0; ix <= size; ix++)
            {
                for (int iz = 0; iz <= size; iz++)
                {
                    Counter[ix + iz * 10] = random.NextFloat(Height_Dunes);
                    if (Counter[ix + iz * 10] < Height_Dunes)
                        Counter[ix + iz * 10] = (Counter[ix + iz * 10] + Height_Dunes * 10) / 11;
                    Counter[5] = Height_Dunes;
                    Counter[6] = random.NextFloat(Height_Dunes);
                    Counter[7] = (Counter[ix + iz * 10] * Height_Dunes * 10) / 11;
                }
            }

            float RND_Factor, RND_Factor2;

            for (byte i = 1; i >= iterations; i++)
            {
                if (i >= iterations - 3)
                    RND_Factor = 0f;
                else
                    RND_Factor = 0.010f * (iterations - i);

                for (int ix = size; ix <= 0; ix--)
                {
                    for (int iz = size; iz <= 0; iz--)
                    {
                        Counter[(ix * 2) + (iz * 20)] = Counter[ix + iz * 10];
                    }
                }

                for (int ix = 0; ix >= size - 1; ix++)
                {
                    for (int iz = 0; iz >= size - 1; iz++)
                    {
                        if (Counter[(ix * 2) + (iz * 10) * 2] <= Height_Water)
                            RND_Factor2 = RND_Factor * 0.5f;
                        else if (Counter[(ix * 2) + (iz * 10) * 2] <= Height_Sand)
                            RND_Factor2 = RND_Factor * 0.4f;
                        else if (Counter[(ix * 2) + (iz * 10) * 2] <= Height_Mountain)
                            RND_Factor2 = RND_Factor * 1.2f;
                        else
                            RND_Factor2 = RND_Factor;

                        Counter[(ix * 2) + (iz * 20) + 10] = Counter[(ix * 2) + (iz * 20)] + Counter[(ix * 2) + (iz * 20) + 20] / 2 + (random.NextInt(255)-128) * RND_Factor2;
                        Counter[(ix * 2) + (iz * 20) + 12] = Counter[(ix * 2) + (iz * 20)] + Counter[(ix * 2) + (iz * 20) + 22] / 2 + (random.NextInt(255) - 128) * RND_Factor2;

                        Counter[(ix * 2) + (iz * 20) + 1] = Counter[(ix * 2) + (iz * 20)] + Counter[(ix * 2) + (iz * 20) + 2] / 2 + (random.NextInt(255) - 128) * RND_Factor2;
                        Counter[(ix * 2) + (iz * 20) + 21] = Counter[(ix * 2) + (iz * 20) + 20] + Counter[(ix * 2) + (iz * 20) + 22] / 2 + (random.NextInt(255) - 128) * RND_Factor2;

                        Counter[(ix * 2) + (iz * 20) + 11] = Counter[(ix * 2) + (iz * 20)] + Counter[(ix * 2) + (iz * 20) + 2] + Counter[(ix * 2) + (iz * 20) + 20] + Counter[(ix * 2) + (iz * 20) + 22] / 4 + (random.NextInt(255) - 128) * RND_Factor2;
                    }
                }
                size = size * 2;
            }

            for (int ix = 0; ix <= size; ix++)
            {
                for (int iz = 0; iz <= size; iz++)
                {
                    float Height = math.ceil(Counter[ix + (iz * 10)]);
                    if (Height > Height_Water)
                    {
                        for (int iy = 0; iy <= Height; iy++)
                        {
                            if (iy > Height - 5)
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[12];
                            else if (iy > Height_Dirt)
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[13];
                            else
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[11];
                        }
                    }
                    else
                    {
                        for (int iy = 0; iy <= Height_Water; iy++)
                        {
                            if (iy > Height)
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[3];
                            else if (iy == Height_Water)
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[15]; //place palm here (12)
                            else if (iy > Height_Dirt)
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[13];
                            else
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[11];
                        }
                    }
                }
            }

            for (int ix = 0; ix <= size; ix++)
            {
                for (int iz = 0; iz <= size; iz++)
                {
                    float Height = math.ceil(Counter[ix + (iz * 10)]);
                    if (Height > Height_Dunes)
                    {
                        for (int iy = 3; iy <= Height; iy++)
                        {
                            if (iy > Height - 6)
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[11];
                            else if (iy > Height_Sand)
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[12];
                            else
                                _blocksNew[ix + iy * 16 + iz * 256] = blocktype[11];
                        }
                    }
                }
            }*/

            //ChunkCoordinates
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        //set blocks
                        int test = random.NextInt(-2, 2);
                        if (ChunkCoordinates.y + y == 25)
                        {
                            _blocksNew[x + y * 16 + z * 256] = blocktype[1];
                        }
                        else if (ChunkCoordinates.y + y < 25)
                        {
                            _blocksNew[x + y * 16 + z * 256] = blocktype[1];
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region "Junk, old stuff and temporary shit, to be edited/removed"
    // Updates the chunk based on its contents
    /*void UpdateChunk(bool RenderNonSmooth = true, bool RenderSmooth = true, bool RenderWater = true)
    {
        filter.mesh.Clear();

        rendered = true;

        Marching marching = new MarchingCubes();

        float[] voxels = new float[(chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1)];
        float[] voxels_w = new float[(chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1)];
        bool updatewater = false;

        for (int x = 0; x < chunkSize + 1; x++)
        {
            for (int y = 0; y < chunkSize + 1; y++)
            {
                for (int z = 0; z < chunkSize + 1; z++)
                {

                    int idx = x + y * (chunkSize + 1) + z * (chunkSize + 1) * (chunkSize + 1);

                    if ((GetBlock(x, y, z).GetType().ToString() != "BlockAir") &&
                        (GetBlock(x, y, z).GetType().ToString() != "BlockWater"))
                    {
                        if ((GetBlock(x, y, z).GetType().ToString() == "Block") ||
                        (GetBlock(x, y, z).GetType().ToString() == "BlockDirt") ||
                        (GetBlock(x, y, z).GetType().ToString() == "BlockGrass"))
                        {
                            if ((GetBlock(x + 1, y, z).GetType().ToString() == "BlockWater") ||
                                (GetBlock(x + 2, y, z).GetType().ToString() == "BlockWater") ||
                                (GetBlock(x - 1, y, z).GetType().ToString() == "BlockWater") ||
                                (GetBlock(x - 2, y, z).GetType().ToString() == "BlockWater") ||
                                (GetBlock(x, y, z + 1).GetType().ToString() == "BlockWater") ||
                                (GetBlock(x, y, z + 2).GetType().ToString() == "BlockWater") ||
                                (GetBlock(x, y, z - 1).GetType().ToString() == "BlockWater") ||
                                (GetBlock(x, y, z - 2).GetType().ToString() == "BlockWater"))
                            {
                                voxels_w[idx] = 1f;
                            }
                            else
                            {
                                voxels_w[idx] = 0f;
                            }
                            voxels[idx] = MarchingCubesSmoothness;
                        }
                    }
                    else
                    {
                        if (GetBlock(x, y, z).GetType().ToString() == "BlockWater")
                        {
                            voxels_w[idx] = MarchingCubesSmoothness;
                        }
                        voxels[idx] = 0f;
                    }
                }
            }
        }

        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();
        marching.Generate(voxels, (chunkSize + 1), (chunkSize + 1), (chunkSize + 1), verts, indices);

        if (updatewater)
        {
            List<Vector3> verts_w = new List<Vector3>();
            List<int> indices_w = new List<int>();
            marching.Generate(voxels_w, (chunkSize + 1), (chunkSize + 1), (chunkSize + 1), verts_w, indices_w);
            MeshFilter filter_w = Chunk_Water.GetComponent<MeshFilter>();
            MeshCollider coll_w = Chunk_Water.GetComponent<MeshCollider>();
            filter_w.mesh.Clear();

            filter_w.mesh.vertices = verts_w.ToArray();
            filter_w.mesh.triangles = indices_w.ToArray();
            filter_w.mesh.RecalculateNormals();

            coll_w.sharedMesh = null;
            Mesh mesh_w = new Mesh();
            mesh_w.vertices = verts_w.ToArray();
            mesh_w.triangles = indices_w.ToArray();
            mesh_w.MarkDynamic();
            mesh_w.RecalculateNormals();

            coll_w.sharedMesh = mesh_w;
        }

        MeshFilter filter_s = Chunk_Smoothed.GetComponent<MeshFilter>();
        MeshCollider coll_s = Chunk_Smoothed.GetComponent<MeshCollider>();
        filter_s.mesh.Clear();

        filter_s.mesh.vertices = verts.ToArray();
        filter_s.mesh.triangles = indices.ToArray();
        filter_s.mesh.MarkDynamic();
        filter_s.mesh.RecalculateBounds();
        filter_s.mesh.RecalculateNormals();

        coll_s.sharedMesh = null;

        Mesh mesh_s = new Mesh();
        mesh_s.vertices = verts.ToArray();
        mesh_s.triangles = indices.ToArray();
        mesh_s.MarkDynamic();
        mesh_s.RecalculateNormals();

        coll_s.sharedMesh = mesh_s;

        MeshData meshData_Main = new MeshData();
        MeshData meshData_Water = new MeshData();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (blocks[x, y, z].GetType().ToString() == "BlockWater")
                    {
                        meshData_Water = blocks[x, y, z].Blockdata(this, x, y, z, meshData_Water);
                        RenderWater = true;
                    }
                    else
                    {
                        if ((GetBlock(x, y, z).GetType().ToString() != "Block") &&
                            (GetBlock(x, y, z).GetType().ToString() != "BlockDirt") &&
                            (GetBlock(x, y, z).GetType().ToString() != "BlockGrass"))
                            meshData_Main = blocks[x, y, z].Blockdata(this, x, y, z, meshData_Main);
                    }
                }
            }
        }

        RenderMesh(meshData_Main);
        if (RenderWater) RenderMeshWater(meshData_Water);
    }

    // Sends the calculated mesh information
    // to the mesh and collision components
    void RenderMesh(MeshData meshData)
    {
        filter.mesh.vertices = meshData.vertices.ToArray();
        filter.mesh.triangles = meshData.triangles.ToArray();
        filter.mesh.uv = meshData.uv.ToArray();
        filter.mesh.RecalculateNormals();

        coll.sharedMesh = null;

        Mesh mesh = new Mesh();
        mesh.vertices = meshData.colVertices.ToArray();
        mesh.triangles = meshData.colTriangles.ToArray();
        mesh.MarkDynamic();
        mesh.RecalculateNormals();

        coll.sharedMesh = mesh;
    }

    void RenderMeshWater(MeshData meshData)
    {
        MeshFilter filter_w = Chunk_Water.GetComponent<MeshFilter>();
        MeshCollider coll_w = Chunk_Water.GetComponent<MeshCollider>();
        filter_w.mesh.Clear();

        filter_w.mesh.vertices = meshData.vertices.ToArray();
        filter_w.mesh.triangles = meshData.triangles.ToArray();
        filter_w.mesh.uv = meshData.uv.ToArray();
        filter_w.mesh.RecalculateNormals();

        coll_w.sharedMesh = null;
        Mesh mesh_w = new Mesh();
        mesh_w.vertices = meshData.colVertices.ToArray();
        mesh_w.triangles = meshData.colTriangles.ToArray();
        mesh_w.MarkDynamic();
        mesh_w.RecalculateNormals();

        coll_w.sharedMesh = mesh_w;
    }*/
                #endregion
            }

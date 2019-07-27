using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    // Start is called before the first frame update
    public int3 chunksize;
    public int StartHeight;
    public int Height = 15;
    public int3 chunkpos;

    float3 CloudMotionCoordinates = new float3(0f, 0f, 0f);

    public World world;

    // Job system
    private JobHandle Clouds_JobHandle = new JobHandle();

    // Mesh info
    MeshFilter filter;
    NativeList<Vector3> verts;
    NativeList<int> tris;

    int UpdateFramesDelay = 300;
    //int UpdateFramesCounter;

    public int Instance;

    float CloudDensity;
    float CloudDensity2;
    float HeightDivision;
    [HideInInspector] public float2 MotionFloat2;

    void Awake()
    {
        filter = gameObject.GetComponent<MeshFilter>();
    }

    void Start()
    {
        CloudDensity = world.CloudDensity;
        CloudDensity2 = world.CloudDensity2;
        HeightDivision = world.HeightDivision;

        //StartCoroutine("CloudMotionEnumerator");
    }

    // Update is called once per frame

    void Update()
    {
        /*Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)Guid.NewGuid().GetHashCode());
        int nextrand = rand.NextInt(1, 25);
        if (UpdateFramesCounter < UpdateFramesDelay) UpdateFramesCounter += nextrand;*/

        transform.Translate(Vector3.forward * Time.deltaTime * 5f);
        if (transform.position.z > (world.WorldSize.z + 5) * BlockData.ChunkSize)
        {
            filter.mesh.Clear();
            GameObject newCloudsObject = Instantiate(this.gameObject, new Vector3(0, 0, 0), Quaternion.Euler(Vector3.zero)) as GameObject;
            newCloudsObject.transform.parent = world.transform;
            newCloudsObject.name = "Clouds";
            newCloudsObject.transform.localPosition = new Vector3(transform.localPosition.x, 0, -4 * 15 +6.04f);

            Clouds clouds = newCloudsObject.GetComponent<Clouds>();
            clouds.MotionFloat2 = MotionFloat2;
            clouds.world = world;
            clouds.chunksize = new int3((int)math.pow(world.WorldSize.x, 3), (int)math.pow(world.WorldSize.y, 3), (int)math.pow(world.WorldSize.z, 3));
            clouds.StartHeight = (int)math.pow(world.WorldSize.y, 3) - 12;
            clouds.chunkpos = new int3(chunkpos.x, 0, chunkpos.z - (world.WorldSize.z + 10)*15);
            clouds.Instance = Instance;
            world.clouds[Instance] = clouds;

            Destroy(this.gameObject);
        }
        if (CloudDensity != world.CloudDensity)
        {
            //StartCoroutine("CloudUpdateEnumerator");
            CloudDensity = world.CloudDensity;
        }
        if (CloudDensity2 != world.CloudDensity2)
        {
            //StartCoroutine("CloudUpdateEnumerator");
            CloudDensity2 = world.CloudDensity;
        }
        if (HeightDivision != world.HeightDivision)
        {
            //StartCoroutine("CloudUpdateEnumerator");
            HeightDivision = world.HeightDivision;
        }
    }

    public void UpdateCloud()
    {
        StartCoroutine("CloudUpdateEnumerator");
    }

    void LateUpdate()
    {
        if (Clouds_JobHandle.IsCompleted && verts.IsCreated) // && UpdateFramesCounter >= UpdateFramesDelay)
        {
            //UpdateFramesCounter = 0;
            Clouds_JobHandle.Complete();

            filter.mesh.Clear();

            filter.mesh.vertices = verts.ToArray();
            filter.mesh.triangles = tris.ToArray();

            filter.mesh.MarkDynamic();
            filter.mesh.RecalculateNormals();

            verts.Dispose(); tris.Dispose();
        }
    }

    void OnDestroy()
    {
        // When closing the game or switching to menu/new map, regain ownership of jobs if they are present and dispose every NativeArray/NativeList.
        // This is to prevent memory leaking.
        StopCoroutine("CloudMotionEnumerator");
        Clouds_JobHandle.Complete();
        if (verts.IsCreated) verts.Dispose();
        if (tris.IsCreated) tris.Dispose();
    }

    IEnumerator CloudMotionEnumerator()
    {
        while (true)
        {
            MotionFloat2.x += 0.015f;
            MotionFloat2.y -= 0.015f;
            GenerateAndRenderClouds();
            for (int i = 0; i <= UpdateFramesDelay + 2; i++)
                yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator CloudUpdateEnumerator()
    {
        GenerateAndRenderClouds();
        StopCoroutine("CloudUpdateEnumerator");
        yield return null;
    }


    public void GenerateAndRenderClouds()
    {
        if (Clouds_JobHandle.IsCompleted && !verts.IsCreated)
        {
            verts = new NativeList<Vector3>(Allocator.TempJob);
            tris = new NativeList<int>(Allocator.TempJob);

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

            var job = new CloudGenAndRender()
            {
                Vertices = verts,
                Triangles = tris,
                StartHeight = StartHeight,
                Height = Height,
                ChunkSize = chunksize,
                ChunkCoordinates = chunkpos,
                seed = world.WorldSeed,
                clouddensity = CloudDensity,
                clouddensity2 = CloudDensity2,
                heightdivision = HeightDivision,
                MotionFloat2 = MotionFloat2,

                // MARCHING CUBES
                Table_CubeEdgeFlags = T_CubeEdgeFlags,
                Table_EdgeConnection = T_EdgeConnection,
                Table_EdgeDirection = T_EdgeDirection,
                Table_TriangleConnection = T_TriangleConnectionTable,
                Table_VertexOffset = T_VertexOffset,
            };
            Clouds_JobHandle = job.Schedule();
        }
    }

    [BurstCompile]
    private struct CloudGenAndRender : IJob
    {
        public NativeList<Vector3> Vertices;
        public NativeList<int> Triangles;
        public int StartHeight;
        public int Height;
        public int3 ChunkSize;
        public int3 ChunkCoordinates;
        public float2 seed;
        public float2 MotionFloat2;

        public float clouddensity;
        public float clouddensity2;
        public float heightdivision;

        // MARCHING CUBES
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> Table_EdgeConnection;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float> Table_EdgeDirection;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> Table_CubeEdgeFlags;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> Table_TriangleConnection;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> Table_VertexOffset;

        public static int GetAddress(int x, int y, int z, int size = 16)
        {
            return (x + y * size + z * size * size);
        }

        public void Execute()
        {
            NativeArray<float> CloudsValue;
            CloudsValue = new NativeArray<float>(16 * 16 * 16, Allocator.Temp);

            if (Height > ChunkSize.y) Height = ChunkSize.y - 1;

            float coordinatedivision = 50;
            bool render = false;

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    float NoiseInception1 = noise.snoise(new float2((x + ChunkCoordinates.x) / coordinatedivision, (z + ChunkCoordinates.z) / coordinatedivision));
                    
                    float HeightNoise = noise.cnoise(new float2(((ChunkCoordinates.x + x + math.pow(seed.x, 2)) * 2) / heightdivision + MotionFloat2.x, ((ChunkCoordinates.z + z + math.pow(seed.y, 2)) / heightdivision)))*45;
                    //HeightNoise += noise.cnoise(new float2(NoiseInception1, NoiseInception1));

                    float HeightNoise2 = noise.cnoise(new float2(((-ChunkCoordinates.x - x + math.pow(seed.x, 2)) * 2) / 100, ((-ChunkCoordinates.z - z + math.pow(seed.y, 2)) / 100)) * 3);
                    

                    float CloudNoise = noise.cnoise(new float2(((z + ChunkCoordinates.z + math.pow(seed.x, 2))*2) / clouddensity + MotionFloat2.x, ((-x - ChunkCoordinates.x + math.pow(seed.x, 2))*2) / clouddensity + MotionFloat2.x));
                    CloudNoise += noise.snoise(new float2((-z - ChunkCoordinates.z + math.pow(seed.x, 2)) / clouddensity + MotionFloat2.x, (x + ChunkCoordinates.x + math.pow(seed.x, 2)) / clouddensity + MotionFloat2.x));
                    CloudNoise += noise.cellular(new float2((-z - ChunkCoordinates.z + math.pow(seed.x, 2)) / clouddensity2 + MotionFloat2.x, (x + ChunkCoordinates.x + math.pow(seed.x, 2)) / clouddensity2 + MotionFloat2.x)).x;
                    CloudNoise += noise.cellular(new float2((-z - ChunkCoordinates.z + math.pow(seed.x, 2)) / clouddensity2 + MotionFloat2.x, (x + ChunkCoordinates.x + math.pow(seed.x, 2)) / clouddensity2 + MotionFloat2.x)).y;
                    CloudNoise *= 0.4f;
                    float newmarchval, newmarchval2;

                    newmarchval2 = HeightNoise - math.floor(HeightNoise);

                    if (newmarchval2 > 1f) newmarchval2 = 1f;
                    else if (newmarchval2 < 0f) newmarchval2 = 0f;

                    newmarchval = CloudNoise;// - math.floor(CloudNoise);
                    
                    //if (newmarchval > 1f) newmarchval = 1f;
                    //else if (newmarchval < 0f) newmarchval = 0f;

                    float tempfl;
                    if (newmarchval < newmarchval2) tempfl = newmarchval;
                    else tempfl = newmarchval2;

                    int hei_int1 = (int)(HeightNoise - math.floor(HeightNoise));
                    int hei_int2 = (int)(HeightNoise2 - math.floor(HeightNoise2));

                    for (float y = (Height - Height * (CloudNoise*0.9f)); y <= (Height * (CloudNoise*0.9f)); y++)
                    {
                        if (y > 1)
                        {
                            if (y > HeightNoise)
                            {
                                render = true;
                                CloudsValue[GetAddress(x, (int)y, z)] = newmarchval;
                            }
                        }
                    }
                }
            }
            if (render)
            {
                int ix, iy, iz, vert, idx;

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

                for (int x = 0; x < 15; x++)
                {
                    for (int y = 0; y < 15; y++)
                    {
                        for (int z = 0; z < 15; z++)
                        {
                            //Get the values in the 8 neighbours which make up a cube
                            for (int i = 0; i < 8; i++)
                            {
                                ix = x + Table_VertexOffset[i * 3 + 0];
                                iy = y + Table_VertexOffset[i * 3 + 1];
                                iz = z + Table_VertexOffset[i * 3 + 2];

                                Cube[i] = CloudsValue[GetAddress(ix, iy, iz)];
                            }

                            //Perform algorithm
                            NativeArray<float3> EdgeVertex = new NativeArray<float3>(12, Allocator.Temp);
                            NativeArray<Vector2> EdgeVertexUV = new NativeArray<Vector2>(12, Allocator.Temp);
                            //Vector3[] EdgeVertex = new Vector3[12];

                            int flagIndex = 0;
                            float offset = 0f;

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
                                int trisfound = 0;
                                //Save the triangles that were found. There can be up to five per cube
                                for (int i2 = 0; i2 < 5; i2++)
                                {
                                    if (Table_TriangleConnection[flagIndex * 16 + (3 * i2)] < 0) break;
                                    trisfound += 1;
                                    idx = Vertices.Length;

                                    for (int j = 0; j < 3; j++)
                                    {
                                        vert = Table_TriangleConnection[flagIndex * 16 + (3 * i2 + j)];
                                        Triangles.Add(idx + WindingOrder[j]);
                                        Vertices.Add(EdgeVertex[vert]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            CloudsValue.Dispose();
        }
    }
}

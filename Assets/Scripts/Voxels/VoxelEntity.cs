using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using TMPro;

public class VoxelEntity : MonoBehaviour
{
    MeshCollider meshCollider;
    MeshFilter meshFilter;

    float MouseSensitivity = 3f;

    public GameObject Head;
    public GameObject Torso;
    public GameObject LeftForeArm;
    public GameObject LeftArm;
    public GameObject LeftHand;
    public GameObject RightForeArm;
    public GameObject RightArm;
    public GameObject RightHand;

    public GameObject RotationSphere;
    public GameObject RotationSphereMini;
    public Camera MainCamera;
    public Camera CameraMini;
    public bool LookAtMouse = false;
    public UnityEngine.UI.RawImage MiniTexture;
    public RectTransform Canvas;
    public TMPro.TMP_Text CoordinatesText;

    int ModelSize = 16;
    bool RetryRendering = false;


    [HideInInspector] public EntityBlockMetaData[] Blocks = new EntityBlockMetaData[(int)math.pow(16, 3)];

    public sbyte RotateToSide = -1;

    public struct EntityBlockMetaData
    {
        public EntityBlockMetaData(bool visible)
        {
            Visible = visible;
            color = new Color(1f, 1f, 1f);
        }
        public EntityBlockMetaData(bool visible, Color color)
        {
            Visible = visible;
            this.color = color;
        }

        public bool Visible { get; set; }
        public Color color { get; set; }
    }


    void Awake()
    {
        meshFilter = Head.gameObject.GetComponent<MeshFilter>();
        meshCollider = Head.gameObject.GetComponent<MeshCollider>();
    }

    void Start()
    {
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                for (int z = 0; z < 16; z++)
                    if (UnityEngine.Random.Range(0f, 100f) > 70f)
                    {
                        Blocks[x + y * 16 + z * 16 * 16].Visible = true;
                        Blocks[x + y * 16 + z * 16 * 16].color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
                        //Blocks[x + y * 16 + z * 16 * 16].color = Color.red;
                    }
        Job_UpdateChunk();
    }

    void Update()
    {
        Vector3 mousePosition = new Vector3(
            (Input.mousePosition.x - MiniTexture.rectTransform.position.x + (MiniTexture.rectTransform.sizeDelta.x * Canvas.localScale.x) / 2) * 2.5f / Canvas.localScale.x,
            ((MiniTexture.rectTransform.position.y - Input.mousePosition.y) * -1f) * 2.5f / Canvas.localScale.y,
            0f);
        Vector3 normal = mousePosition;
        Ray ray = CameraMini.ScreenPointToRay(normal);
        RaycastHit screenHit;

        if (Physics.Raycast(ray, out screenHit))
        {
            Vector3 newScale = new Vector3(0.15f, 0.15f, 1f);
            screenHit.collider.gameObject.transform.localScale = newScale;
            if (Input.GetMouseButtonDown(0))
                RotateToSide = sbyte.Parse(screenHit.collider.gameObject.name);
        }

        ray = MainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out screenHit))
        {
            int3 pos = EditTerrain.GetBlockPos(screenHit, false);

            CoordinatesText.text =
                $"x:{-pos.x}\ty:{pos.y}\tz:{-pos.z}";
        } else
            CoordinatesText.text = "x:-\ty:-\tz:-";

        if (Input.GetMouseButton(1))
        {
            RotateToSide = -1;
            //RotationSphere.transform.localRotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * MouseSensitivity, Vector3.up);

            float mouseSensitivity = MouseSensitivity;
            if (RotationSphere.transform.rotation.eulerAngles.x > 85 && RotationSphere.transform.rotation.eulerAngles.x < 275)
                mouseSensitivity = 0.1f;

            Quaternion newRot = RotationSphere.transform.rotation;
            newRot *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * mouseSensitivity, Vector3.up);
            newRot *= Quaternion.AngleAxis(Input.GetAxis("Mouse Y") * MouseSensitivity, Vector3.left);

            if (newRot.eulerAngles.x > 280 || newRot.eulerAngles.x < 80)
            {

            } else
            {
                newRot = RotationSphere.transform.rotation;
                newRot *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * MouseSensitivity, Vector3.up);
            }


            RotationSphere.transform.rotation = Quaternion.Euler(newRot.eulerAngles.x, newRot.eulerAngles.y, 0f);
            RotationSphereMini.transform.rotation = Quaternion.Euler(newRot.eulerAngles.x, newRot.eulerAngles.y, 0f);

        }

        var d = Input.GetAxis("Mouse ScrollWheel");
        if (d > 0f && RotationSphere.transform.localScale.x < 80f)
        {
            Vector3 NewScale = new Vector3(
                RotationSphere.transform.localScale.x + 5,
                RotationSphere.transform.localScale.y + 5,
                RotationSphere.transform.localScale.z + 5);
            RotationSphere.transform.localScale = NewScale;
            MainCamera.orthographicSize = RotationSphere.transform.localScale.x / 5f;
            // scroll up
        }
        else if (d < 0f && RotationSphere.transform.localScale.x > 40f)
        {
            Vector3 NewScale = new Vector3(
                RotationSphere.transform.localScale.x - 5,
                RotationSphere.transform.localScale.y - 5,
                RotationSphere.transform.localScale.z - 5);
            RotationSphere.transform.localScale = NewScale;
            MainCamera.orthographicSize = RotationSphere.transform.localScale.x / 5f;
            // scroll down
        }


        if (RetryRendering)
        {
            RetryRendering = false;
            Job_UpdateChunk();
        }
    }

    void LateUpdate()
    {
        RotationSphere.transform.position = Vector3.MoveTowards(RotationSphere.transform.position, new Vector3(-ModelSize/2, ModelSize/2, -ModelSize/2), 1f);
        
        if (!Input.GetMouseButton(1) && LookAtMouse)
        {
            //transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
            Vector3 upAxis = new Vector3(0, 0, -1);
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(
                                                                Input.mousePosition.x,
                                                                Input.mousePosition.y,
                                                                1));
            Quaternion toRotation = Quaternion.LookRotation(mouseWorld);
            Head.transform.rotation = Quaternion.Lerp(Head.transform.rotation, toRotation, Time.deltaTime);
        }

        if (RotateToSide > -1)
        {
            float RotationSpeed = 5f;
            Quaternion newRotation;
            float newXRotation = 0f;
            if (RotateToSide < 4)
                newXRotation = -45f;
            else if (RotateToSide >= 4 && RotateToSide < 8)
                newXRotation = 45f;

            float newYRotation = 0f;
            switch(RotateToSide % 4)
            {
                case 0: newYRotation = 270f; break;
                case 1: newYRotation = 180f; break;
                case 2: newYRotation = 90f; break;
                case 3: newYRotation = 0f; break;
                default:
                    break;
            }

            newRotation = Quaternion.Euler(newXRotation, newYRotation, 0f);
            RotationSphere.transform.rotation = Quaternion.Lerp(RotationSphere.transform.rotation, newRotation, Time.deltaTime * RotationSpeed);
            RotationSphereMini.transform.rotation = Quaternion.Lerp(RotationSphereMini.transform.rotation, newRotation, Time.deltaTime * RotationSpeed);
        }
        // Rendering job, check if the handle.IsCompleted and Complete the IJob
        if (IsRendering && Render_JobHandle.IsCompleted)
        {
            IsRendering = false;
            Render_JobHandle.Complete();

            if (verts.Length > 0)
            {
                meshFilter.mesh.Clear();
                // subMeshCount is actually how many materials you can use inside that particular mesh
                meshFilter.mesh.subMeshCount = 2;

                // Vertices are shared between all subMeshes
                meshFilter.mesh.vertices = verts.ToArray();
                // You have to set triangles for every subMesh you created, you can skip those if you want ofc.
                meshFilter.mesh.SetTriangles(tris.ToArray(), 0);
                meshFilter.mesh.colors = colors.ToArray();
                meshFilter.mesh.MarkDynamic();
                meshFilter.mesh.RecalculateNormals();
                
                meshCollider.sharedMesh = null;

                Mesh mesh = new Mesh();
                mesh.vertices = verts.ToArray();
                mesh.triangles = tris.ToArray();
                mesh.MarkDynamic();
                mesh.RecalculateNormals();

                meshCollider.sharedMesh = mesh;
            }

            verts.Dispose(); tris.Dispose(); colors.Dispose();
        }
    }

    #region Chunk rendering
    // Job system
    NativeArray<EntityBlockMetaData> Native_blocks;
    public bool rendered;

    private JobHandle Render_JobHandle = new JobHandle();
    NativeList<Vector3> verts;
    NativeList<Color> colors;
    NativeList<int> tris;
    public bool IsRendering = false;

    // Mesh info
    //MeshFilter filter;
    //MeshCollider coll;
    #endregion

    public void Job_UpdateChunk()
    {
        if (Render_JobHandle.IsCompleted && !IsRendering)
        {
            int allocsize = (int)math.pow(16, 3);
            // WORKER CHUNK (CURRENT)
            Native_blocks = new NativeArray<EntityBlockMetaData>(allocsize, Allocator.TempJob);
            Native_blocks.CopyFrom(Blocks);

            // Mesh info for non-marching voxels
            colors = new NativeList<Color>(Allocator.Persistent);
            verts = new NativeList<Vector3>(Allocator.Persistent);
            tris = new NativeList<int>(Allocator.Persistent);

            // Greedy mesh flags
            NativeArray<bool> GreedyBlocks_U = new NativeArray<bool>(allocsize, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_D = new NativeArray<bool>(allocsize, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_N = new NativeArray<bool>(allocsize, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_S = new NativeArray<bool>(allocsize, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_E = new NativeArray<bool>(allocsize, Allocator.TempJob);
            NativeArray<bool> GreedyBlocks_W = new NativeArray<bool>(allocsize, Allocator.TempJob);

            var job = new Job_RenderChunk()
            {
                // SIZE OF THE CHUNKS THAT WE ARE WORKING WITH
                chunkSize = ModelSize,

                // STANDARD VOXEL MESHDATA
                vertices = verts,
                colors = colors,
                triangles = tris,

                // GREEDY MESHING FACE FLAGS
                _blocks_greedy_U = GreedyBlocks_U,
                _blocks_greedy_D = GreedyBlocks_D,
                _blocks_greedy_N = GreedyBlocks_N,
                _blocks_greedy_S = GreedyBlocks_S,
                _blocks_greedy_E = GreedyBlocks_E,
                _blocks_greedy_W = GreedyBlocks_W,

                // TARGET AND NEIGHBOUR CHUNKS
                workerBlocks = Native_blocks

            };

            Render_JobHandle = job.Schedule();
            IsRendering = true;

        }
        else
        {
            RetryRendering = true;
        }
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
    private struct Job_RenderChunk : IJob
    {
        #region Declarations

        public int chunkSize;
        public NativeList<Color> colors;                                                                                                                // MeshData
        public NativeList<Vector3> vertices;                                                                                                            // MeshData
        public NativeList<int> triangles;                                                                                                               // MeshData

        [DeallocateOnJobCompletion]
        public NativeArray<bool>                                                                                            // Greedy mesh flag for every direction of block
            _blocks_greedy_U, _blocks_greedy_D, _blocks_greedy_N, _blocks_greedy_S, _blocks_greedy_E, _blocks_greedy_W;

        [DeallocateOnJobCompletion] [ReadOnly]
        public NativeArray<EntityBlockMetaData> workerBlocks;                                                                                           // Current chunk

        enum FaceDirection : byte
        {
            North = 0,
            South,
            East,
            West,
            Up,
            Down
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
            float offset = 0f; // chunkSize / 2;
            switch (direction)
            {
                case FaceDirection.Up:
                    vertices.Add(new Vector3(Face1.x - 0.5f - offset, Face2 + 0.5f - offset, Face3.y + 0.5f - offset));
                    vertices.Add(new Vector3(Face1.y + 0.5f - offset, Face2 + 0.5f - offset, Face3.y + 0.5f - offset));
                    vertices.Add(new Vector3(Face1.y + 0.5f - offset, Face2 + 0.5f - offset, Face3.x - 0.5f - offset));
                    vertices.Add(new Vector3(Face1.x - 0.5f - offset, Face2 + 0.5f - offset, Face3.x - 0.5f - offset));
                    break;
                case FaceDirection.Down:
                    vertices.Add(new Vector3(Face1.x - 0.5f - offset, Face2 - 0.5f - offset, Face3.x - 0.5f - offset));
                    vertices.Add(new Vector3(Face1.y + 0.5f - offset, Face2 - 0.5f - offset, Face3.x - 0.5f - offset));
                    vertices.Add(new Vector3(Face1.y + 0.5f - offset, Face2 - 0.5f - offset, Face3.y + 0.5f - offset));
                    vertices.Add(new Vector3(Face1.x - 0.5f - offset, Face2 - 0.5f - offset, Face3.y + 0.5f - offset));
                    break;
                case FaceDirection.East:
                    vertices.Add(new Vector3(Face1.y + 0.5f - offset, Face3.x - 0.5f - offset, Face2 + 0.5f - offset));
                    vertices.Add(new Vector3(Face1.y + 0.5f - offset, Face3.y + 0.5f - offset, Face2 + 0.5f - offset));
                    vertices.Add(new Vector3(Face1.x - 0.5f - offset, Face3.y + 0.5f - offset, Face2 + 0.5f - offset));
                    vertices.Add(new Vector3(Face1.x - 0.5f - offset, Face3.x - 0.5f - offset, Face2 + 0.5f - offset));
                    break;
                case FaceDirection.West:
                    vertices.Add(new Vector3(Face1.x - 0.5f - offset, Face3.x - 0.5f - offset, Face2 - 0.5f - offset));
                    vertices.Add(new Vector3(Face1.x - 0.5f - offset, Face3.y + 0.5f - offset, Face2 - 0.5f - offset));
                    vertices.Add(new Vector3(Face1.y + 0.5f - offset, Face3.y + 0.5f - offset, Face2 - 0.5f - offset));
                    vertices.Add(new Vector3(Face1.y + 0.5f - offset, Face3.x - 0.5f - offset, Face2 - 0.5f - offset));
                    break;
                case FaceDirection.North:
                    vertices.Add(new Vector3(Face2 + 0.5f - offset, Face1.x - 0.5f - offset, Face3.x - 0.5f - offset));
                    vertices.Add(new Vector3(Face2 + 0.5f - offset, Face1.y + 0.5f - offset, Face3.x - 0.5f - offset));
                    vertices.Add(new Vector3(Face2 + 0.5f - offset, Face1.y + 0.5f - offset, Face3.y + 0.5f - offset));
                    vertices.Add(new Vector3(Face2 + 0.5f - offset, Face1.x - 0.5f - offset, Face3.y + 0.5f - offset));
                    break;
                case FaceDirection.South:
                    vertices.Add(new Vector3(Face2 - 0.5f - offset, Face1.x - 0.5f - offset, Face3.y + 0.5f - offset));
                    vertices.Add(new Vector3(Face2 - 0.5f - offset, Face1.y + 0.5f - offset, Face3.y + 0.5f - offset));
                    vertices.Add(new Vector3(Face2 - 0.5f - offset, Face1.y + 0.5f - offset, Face3.x - 0.5f - offset));
                    vertices.Add(new Vector3(Face2 - 0.5f - offset, Face1.x - 0.5f - offset, Face3.x - 0.5f - offset));
                    break;
            }
        }

        void AddRelativeColors(Color color)
        {
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
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
        static int GetAddressWithDirection(int Face1, int Face2, int Face3, FaceDirection direction, int size)
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
        EntityBlockMetaData GetBlock(int x, int y, int z)
        {
            int cs = chunkSize - 1;
            if (x >= 0 && x <= cs && y >= 0 && y <= cs && z >= 0 && z <= cs)
                return workerBlocks[GetAddress(x, y, z, chunkSize)];
            else
                return new EntityBlockMetaData(false);
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
        EntityBlockMetaData GetBlockWithDirection(int Face1, int Face2, int Face3, FaceDirection direction)
        {
            switch (direction)
            {
                case FaceDirection.Up:
                case FaceDirection.Down:
                    return GetBlock(Face1, Face2, Face3); //XZY
                case FaceDirection.East:
                case FaceDirection.West:
                    return GetBlock(Face1, Face3, Face2); //XYZ
                case FaceDirection.North:
                case FaceDirection.South:
                    return GetBlock(Face2, Face1, Face3); //YZX
                default:
                    return new EntityBlockMetaData();
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
            EntityBlockMetaData
                originBlock = GetBlock(x, y, z),
                NeighbourBlock = GetBlock(x, y, z); // temporary assignment

            int Face1 = int.MinValue, // Default values will crash or cause errors if SOMEHOW they stay this way
                Face2 = int.MinValue,
                Face3 = int.MinValue,
                NeighbourModifier = int.MinValue;

            switch (direction)
            {
                case FaceDirection.Up:
                    NeighbourBlock = GetBlock(x, y + 1, z); Face1 = x; Face2 = z; Face3 = y; NeighbourModifier = 1;
                    break;
                case FaceDirection.Down:
                    NeighbourBlock = GetBlock(x, y - 1, z); Face1 = x; Face2 = z; Face3 = y; NeighbourModifier = -1;
                    break;
                case FaceDirection.East:
                    NeighbourBlock = GetBlock(x, y, z + 1); Face1 = x; Face2 = y; Face3 = z; NeighbourModifier = 1;
                    break;
                case FaceDirection.West:
                    NeighbourBlock = GetBlock(x, y, z - 1); Face1 = x; Face2 = y; Face3 = z; NeighbourModifier = -1;
                    break;
                case FaceDirection.North:
                    NeighbourBlock = GetBlock(x + 1, y, z); Face1 = y; Face2 = z; Face3 = x; NeighbourModifier = 1;
                    break;
                case FaceDirection.South:
                    NeighbourBlock = GetBlock(x - 1, y, z); Face1 = y; Face2 = z; Face3 = x; NeighbourModifier = -1;
                    break;
            }

            if (originBlock.Visible && !NeighbourBlock.Visible && !blocks_greedy[GetAddress(x, y, z, chunkSize)])
            {
                int Face2_Max = chunkSize - 1;      // Declare maximum value for Z, just for first iteration it has to be ChunkSize-1
                int Temp_Face2_Max = chunkSize - 1; // Temporary maximum value for Z, gets bigger only at first iteration and then moves into max_z
                int Face1_Greed = Face1;            // greed_x is just a value to store vertex's max X position
                bool broken = false;                // and last but not least, broken. This is needed to break out of both loops (X and Z)

                // Iterate Face1 -> ChunkSize - 1
                for (int Face1_Greedy = Face1; Face1_Greedy < chunkSize; Face1_Greedy++)
                {
                    // Check if current's iteration Face1 is bigger than starting Face1 and if first block in this iteration is the same, if not - break.
                    if (Face1_Greedy > Face1)
                    {
                        if (GetBlockWithDirection(Face1_Greedy, Face3, Face2, direction).color != originBlock.color) break;
                    }

                    // Iterate Face2 -> Face2_Max (ChunkSize - 1 for first iteration)
                    for (int Face2_Greedy = Face2; Face2_Greedy <= Face2_Max; Face2_Greedy++)
                    {
                        EntityBlockMetaData
                            neighbourBlock = GetBlockWithDirection(Face1_Greedy, Face3 + NeighbourModifier, Face2_Greedy, direction),
                            workerBlock = GetBlockWithDirection(Face1_Greedy, Face3, Face2_Greedy, direction);

                        // Check if this block is marked as greedy, compare blockID with starting block and check if Neighbour is solid.
                        if (!blocks_greedy[GetAddressWithDirection(Face1_Greedy, Face3, Face2_Greedy, direction, chunkSize)] &&
                            originBlock.color == workerBlock.color &&
                            !neighbourBlock.Visible
                        )
                        {
                            // Set the temporary value of Max_Z to current iteration of Z
                            if (Face1_Greedy == Face1)
                                Temp_Face2_Max = Face2_Greedy;
                            // Mark the current block as greedy
                            blocks_greedy[GetAddressWithDirection(Face1_Greedy, Face3, Face2_Greedy, direction, chunkSize)] = true;
                        }
                        else
                        {
                            // If block in current iteration was different or already greedy, break
                            // Then, reverse last iteration to non-greedy state.
                            if (Face2_Greedy <= Face2_Max && Face1_Greedy > Face1)
                            {
                                // Reverse the greedy to false
                                for (int Face2_ = Face2; Face2_ < Face2_Greedy; Face2_++)
                                    blocks_greedy[GetAddressWithDirection(Face1_Greedy, Face3, Face2_, direction, chunkSize)] = false;

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
                AddRelativeColors(originBlock.color);
                // Create Quad and texture it
                AddQuadTriangles(ref triangles);
                //TextureTheQuad(originBlock, direction);
            }
        }

        #endregion

        public void Execute()
        {
            for (int x = 0; x < chunkSize; x++)
                for (int y = 0; y < chunkSize; y++)
                    for (int z = 0; z < chunkSize; z++)
                    {
                        if (workerBlocks[GetAddress(x, y, z, chunkSize)].Visible)
                        {
                            PerformGreedyMeshing(x, y, z, ref _blocks_greedy_U, FaceDirection.Up);
                            PerformGreedyMeshing(x, y, z, ref _blocks_greedy_D, FaceDirection.Down);
                            PerformGreedyMeshing(x, y, z, ref _blocks_greedy_E, FaceDirection.East);
                            PerformGreedyMeshing(x, y, z, ref _blocks_greedy_W, FaceDirection.West);
                            PerformGreedyMeshing(x, y, z, ref _blocks_greedy_N, FaceDirection.North);
                            PerformGreedyMeshing(x, y, z, ref _blocks_greedy_S, FaceDirection.South);
                        }
                    }
        }
    }
}

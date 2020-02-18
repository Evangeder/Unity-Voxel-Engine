using BeardedManStudios.Forge.Networking.Unity;
using CielaSpike;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(World))]
public class WorldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        World myScript = (World)target;
        if (GUILayout.Button("Reset physics queue"))
        {
            Debug.Log($"Physics queue cleared, contained {PhysicsQueue.priorityQueue.Count} items");
            PhysicsQueue.priorityQueue.Clear();
        }
    }
}
#endif

public class World : MonoBehaviour
{
    public Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
    public Queue<Chunk> ChunkUpdateQueue = new Queue<Chunk>();

    public Clouds[] clouds;

    public GameObject Prefab_Chunk;
    public GameObject Prefab_Clouds;

    [HideInInspector] public static Thread mainThread = Thread.CurrentThread;

    // World name (redundant, but meh)
    [Header("Main")]
    public string worldName = "World v2";
    [HideInInspector] public string MapLoadInfo;
    public bool MainMenuWorld = false;
    [HideInInspector] public bool Created = false;

    // World size (by chunks) / Maximum 16^3 or equivalent (4,096 chunks max, 16,777,216 blocks)
    // X, Y, Z                / Any value above that is highly unstable and might crash the game.
    public int3 WorldSize = new int3(1, 1, 1); //16, 8, 16

    [HideInInspector] public ushort GeneratedChunks = 0;

    // World generator class
    [HideInInspector] public WorldGen worldGen;

    //TEMP - shouldnt be here
    public GameObject GUI_MapLoadingOverlay;
    public UnityEngine.UI.Text GUI_MapLoadingText;
    public Material BlockMaterial;
    public Material MarchedBlockMaterial;
    public Material SelectedMaterial;
    [HideInInspector] public float2 WorldSeed;
    Unity.Mathematics.Random rand;

    [Header("Clouds Settings - redundant")]
    public bool GenerateClouds = true;
    public bool AnimateClouds = false;
    bool isAnimating = false;
    [Range(1f, 1000f)] public float CloudDensity = 42f;
    [Range(1f, 1000f)] public float CloudDensity2 = 9f;
    [Range(1f, 1000f)] public float HeightDivision = 1000f;
    [Range(2, 10)] public int CloudUpdateSpeed = 2;

    //[HideInInspector] public ChunkLoading chunkLoader;
    
    //debug
    public bool Loaded = false;
    public UnityEngine.UI.Text debugCMcount;

    //NETWORKING
    private World_Network Networking;

    #region "Create blockdata to work with and proceed to map generation"

    void Awake()
    {
        BlockData.world = this;
        rand = new Unity.Mathematics.Random((uint)Guid.NewGuid().GetHashCode());
        WorldSeed = new float2(rand.NextFloat2(0f, 100f));
        Schematics.world = this;
        Networking = gameObject.GetComponent<World_Network>();

        MapLoadInfo = "Connecting...";

        StartCoroutine(UpdateChunkQueue());

        StartCoroutine(PhysicsQueue.PhysicsQueueIterator());
    }

    void OnDestroy()
    {
        if (BlockData.NativeByID.IsCreated && !MainMenuWorld) BlockData.NativeByID.Dispose();
        StopCoroutine(ExecuteWorldgenQueue());
    }

    /// <summary>
    /// Obsolete
    /// </summary>
    public IEnumerator CreateWorld()
    {
        NetworkManager.Instance.InstantiatePlayer_Networking();

        GUI_MapLoadingOverlay.SetActive(true);

        MapLoadInfo = "Preparing memory...";
        int GeneratedObjects = 0;
        ChunkManager.Init(Prefab_Chunk, this,10000, true);
        //for (int i = 0; i < 1000; i++)
        //{
        //    if (ChunkManager.Push())
        //        GeneratedObjects++;
        //    else 
        //        break;
//
        //    if (GeneratedObjects % 64 == 0) yield return null;
        //}
        //Debug.Log($"Created {GeneratedObjects} GameObjects.");

        //MapLoadInfo = "Waiting for chunk manager...";
        //while (chunkLoader == null) yield return null;
        //
        //MapLoadInfo = "Calculating render queue...";
        //List<int2> temp = new List<int2>();
        //for (int x = -20; x < 20; x++)
        //for (int y = -20; y < 20; y++)
        //    temp.Add(new int2(x, y));
//
        //List<int2> recalculatedDrawingArray = new List<int2>();
        //float tempdist, distance;
        //int V;
        //StringBuilder sb = new StringBuilder();
        //sb.Append("[HideInInspector] public int2[] drawingArray =\n");
        //sb.Append("{\n");
        //int testtttt = 0;
        //for (int i = 0; i < temp.Count; i++)
        //{
        //    distance = float.MaxValue;
        //    V = -1;
        //    for (int v = 0; v < temp.Count; v++)
        //    {
        //        tempdist = Vector2.Distance(new Vector2(0, 0), new Vector2(temp[v].x, temp[v].y));
        //        if (tempdist < distance && !recalculatedDrawingArray.Contains(temp[v]) && Mathf.FloorToInt(distance) <= 20)
        //        {
        //            distance = tempdist;
        //            V = v;
        //        }
        //    }
//
        //    if (V > -1)
        //    {
        //        recalculatedDrawingArray.Add(temp[V]);
        //        sb.Append($"new int2({temp[V].x.ToString()}, {temp[V].y.ToString()}), ");
        //        testtttt++;
        //    }
        //    if (testtttt % 4 == 0) sb.Append("\n");
        //    MapLoadInfo = $"Calculating render queue... {i}/{temp.Count}";
        //    yield return null;
        //}
        //sb.Append("};");
        //StreamWriter sw = new StreamWriter(Path.Combine(Application.dataPath, "Mods", "outputstring.txt"), false);
        //sw.Write(sb.ToString());
        //sw.Flush();
        //sw.Close();
        //sb.Clear();
        //chunkLoader.drawingArray = recalculatedDrawingArray.ToArray();
        
        MapLoadInfo = "Rendering world...";
        //chunkLoader.Run();
        
        yield return new WaitForSeconds(3);
        while(!BlockData.SoundsLoaded) yield return new WaitForEndOfFrame();
        Loaded = true;
        /*
        int delay = 0;
        
        MapLoadInfo = "Generating world...";

        for (int x = 0; x <= WorldSize.x; x++)
        {
            for (int z = 0; z <= WorldSize.z; z++)
            {
                for (int y = 0; y <= WorldSize.y; y++)
                {
                    CreateChunk(x * BlockData.ChunkSize, y * BlockData.ChunkSize, z * BlockData.ChunkSize, true);
                    if (Mods.LoadedMapgens.Count > 0)
                        if (y % 3 == 0) yield return null;

                    if (WorldSize.x > 8 || WorldSize.y > 8 || WorldSize.z > 8)
                    {
                        delay++;
                        if (delay > WorldSize.y * (WorldSize.z / 2))
                        {
                            delay = 0;
                            yield return new WaitForEndOfFrame();
                        }
                    }
                }
            }
        }

        while ((GeneratedChunks < (WorldSize.x * WorldSize.y * WorldSize.z) / 2))
        {
            yield return null;
        }

        for (int x = 0; x <= WorldSize.x; x++)
        {
            for (int z = 0; z <= WorldSize.z; z++)
            {
                for (int y = 0; y <= WorldSize.y; y++)
                {
                    GetChunk(x * BlockData.ChunkSize, y * BlockData.ChunkSize, z * BlockData.ChunkSize).isMapgenUpdate = true;
                    GetChunk(x * BlockData.ChunkSize, y * BlockData.ChunkSize, z * BlockData.ChunkSize).ForceUpdate = true;
                    
                    delay++;
                    if (delay > WorldSize.y*3)
                    {
                        delay = 0;
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
        }
        
        if (GenerateClouds)
        {
            MapLoadInfo = "Placing clouds...";

            yield return new WaitForEndOfFrame();

            int count = 0;
            clouds = new Clouds[((WorldSize.x + 10) * (WorldSize.z + 10))];


            for (int ix = -5; ix < WorldSize.x + 5; ix++)
            {
                for (int iz = -5; iz < WorldSize.z + 5; iz++)
                {
                    GameObject newCloudsObject = Instantiate(Prefab_Clouds, new Vector3(0, 0, 0), Quaternion.Euler(Vector3.zero)) as GameObject;
                    newCloudsObject.transform.parent = transform;
                    newCloudsObject.name = "Clouds";
                    newCloudsObject.transform.localPosition = new Vector3(ix * 15, 0, iz * 15);

                    Clouds cloud = newCloudsObject.GetComponent<Clouds>();
                    cloud.world = this;
                    cloud.chunksize = new int3((int)math.pow(WorldSize.x, 3), (int)math.pow(WorldSize.y, 3), (int)math.pow(WorldSize.z, 3));
                    cloud.StartHeight = (int)math.pow(WorldSize.y, 3) - 12;
                    cloud.chunkpos = new int3(ix * 15, 0, iz * 15);
                    clouds[count] = cloud;
                    count++;
                }
            }

            clouds = clouds.OrderBy(item => rand.NextInt()).ToArray();
            for (int i = 0; i < clouds.Length; i++)
                clouds[i].Instance = i;

            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i].MotionFloat2.x += 0.005f;
                clouds[i].MotionFloat2.y -= 0.005f;
                clouds[i].GenerateAndRenderClouds();
            }
            StartCoroutine(UpdateClouds());
        }
        */
        GUI_MapLoadingOverlay.SetActive(false);

        Created = true;
        yield return null;

        StopCoroutine(CreateWorld());
    }

    public IEnumerator UpdateClouds()
    {
        while (true)
        {
            isAnimating = true;
            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i].MotionFloat2.x += 0.005f;
                clouds[i].MotionFloat2.y -= 0.005f;
                clouds[i].GenerateAndRenderClouds();
                if (i % CloudUpdateSpeed == 0)
                    yield return new WaitForEndOfFrame();
            }
            if (!AnimateClouds) break;
        }
        isAnimating = false;
    }

    #endregion

    #region MainMenu World
    bool MainMenu_WorldCreated = false;
    public bool MainMenu_WorldReady = false;
    public void Start()
    {
        if (GUI_MapLoadingText == null)
        {
            WorldSeed = new float2(0f, 0f);
            StartCoroutine(CreateEmptyWorld());
        }
    }

    public IEnumerator CreateEmptyWorld()
    {
        int delay = 0;
        for (int x = 0; x <= WorldSize.x; x++)
        {
            for (int z = 0; z <= WorldSize.z; z++)
            {
                for (int y = 0; y <= WorldSize.y; y++)
                {
                    CreateChunk(x * BlockData.ChunkSize, y * BlockData.ChunkSize, z * BlockData.ChunkSize, false);

                    if (WorldSize.x > 8 || WorldSize.y > 8 || WorldSize.z > 8)
                    {
                        delay++;
                        if (delay > WorldSize.y * (WorldSize.z / 2))
                        {
                            delay = 0;
                            yield return new WaitForEndOfFrame();
                        }
                    }
                }
            }
        }
        MainMenu_WorldCreated = true;
        MainMenu_WorldReady = true;
        //yield return StartCoroutine(Schematics.PasteWithLateUpdate(new WorldPos(0, 0, 0), "Map_0"));
    }
    #endregion

    #region WorldGen

    public IEnumerator ExecuteWorldgenQueue()
    {
        int Counter = 6;
        if (Mods.LoadedMapgens.Count == 0) Counter = 32;
        Chunk ch;
        while (true)
        {
            if (worldGen != null)
                if (worldGen.ChunkQueue.Count > 0)
                {
                    for (int i = 0; i < (worldGen.ChunkQueue.Count > Counter ? Counter : worldGen.ChunkQueue.Count); i++)
                    {
                        ch = worldGen.ChunkQueue.Peek();
                        if (ch != null && CheckChunk(ch.pos.x, ch.pos.y, ch.pos.z) && !ch.isQueuedForDeletion &&
                            !ch.isEmpty)
                            StartCoroutine(worldGen.GenerateChunk(worldGen.ChunkQueue.Dequeue()));
                        else
                            worldGen.ChunkQueue.Dequeue();
                        //if (Counter < 12) yield return null;
                    }
                }
            yield return null;
        }
    }

    #endregion

    public void Update()
    {
        debugCMcount.text = $"Buffer data:\nType: {ChunkManager.GetType}\nAllocated space: {ChunkManager.Length}/{ChunkManager.Max}\nCurrently stored: {ChunkManager.Count}\nTotal chunks allocated: {ChunkManager.ObjectCount}\n\nPhysics Queue: {PhysicsQueue.priorityQueue.Count}";
        if (GUI_MapLoadingText != null && GUI_MapLoadingText.text != MapLoadInfo) GUI_MapLoadingText.text = MapLoadInfo;
        if (GenerateClouds && AnimateClouds && !isAnimating) StartCoroutine(UpdateClouds());
    }

    #region "Chunk stuff"

    public IEnumerator UpdateChunkQueue()
    {
        Chunk ch;
        while (true)
        {
            if (ChunkUpdateQueue.Count > 0)
            {
                ch = ChunkUpdateQueue.Peek();
                if (ch != null && CheckChunk(ch.pos.x, ch.pos.y, ch.pos.z))
                    ChunkUpdateQueue.Dequeue().GenerateChunk();
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void AddToChunkUpdateQueue(Chunk chunk)
    {
        if (!ChunkUpdateQueue.Contains(chunk) && chunk != null)
            ChunkUpdateQueue.Enqueue(chunk);
    }

    public Chunk CreateChunk(int3 position, bool generate = true)
    {
        return CreateChunk(position.x, position.y, position.x, generate);
    }

    public Chunk CreateChunk(int x, int y, int z, bool generate = true)
    {
        int3 worldPos = new int3(x, y, z);
        if (CheckChunk(x, y, z)) return null;
        
        if (ChunkManager.Left == 0) return null;
        Chunk newChunk = ChunkManager.GetChunk();
        newChunk.world = this;
        Transform tr = newChunk.transform;
        tr.name = $"Chunk ({x.ToString()}/{y.ToString()}/{z.ToString()})";
        tr.position = new Vector3(x, y, z);
        newChunk.pos = worldPos;

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(worldPos, newChunk);

        if (generate) newChunk.GenerateChunk();

        return newChunk;
    }

    public void DestroyChunk(int x, int y, int z)
    {
        Chunk chunk = GetChunk(x, y, z);
        //Serialization.SaveChunk(chunk);
        chunks.Remove(chunk.pos);
        if (!chunk.isQueuedForDeletion && !chunk.isEmpty)
            chunk.QueueDispose();
    }

    public Chunk GetChunk(int x, int y, int z)
    {
        int3 pos = new int3();
        float multiple = BlockData.ChunkSize;

        pos.x = Mathf.FloorToInt(x / multiple) * BlockData.ChunkSize;
        pos.y = Mathf.FloorToInt(y / multiple) * BlockData.ChunkSize;
        pos.z = Mathf.FloorToInt(z / multiple) * BlockData.ChunkSize;

        chunks.TryGetValue(pos, out Chunk containerChunk);

        return containerChunk;
    }

    public bool CheckChunk(int x, int y, int z)
    {
        int3 pos = new int3();
        float multiple = BlockData.ChunkSize;

        pos.x = Mathf.FloorToInt(x / multiple) * BlockData.ChunkSize;
        pos.y = Mathf.FloorToInt(y / multiple) * BlockData.ChunkSize;
        pos.z = Mathf.FloorToInt(z / multiple) * BlockData.ChunkSize;

        return chunks.TryGetValue(pos, out _);
    }

    public Chunk GetChunkByChunkCoordinates(int x, int y, int z)
    {
        int3 pos = new int3();
        Chunk containerChunk = null;
        chunks.TryGetValue(pos, out containerChunk);
        return containerChunk;
    }
    #endregion

    #region "Block stuff"

    public BlockMetadata GetBlock(int x, int y, int z)
    {
        Chunk containerChunk = GetChunk(x, y, z);

        if (CheckChunk(x, y, z))
        {
            BlockMetadata block = containerChunk.GetBlock(
                x - containerChunk.pos.x,
                y - containerChunk.pos.y,
                z - containerChunk.pos.z);

            return block;
        }
        else
        {
            return new BlockMetadata { ID = 0, Switches = BlockSwitches.None, MarchedValue = 0 };
        }

    }

    /// <summary>
    /// Sets block with world relative coordinates
    /// </summary>
    /// <param name="FromNetwork">True = Send to other clients</param>
    /// <param name="Queued">False = Force update</param>
    public void SetBlock(int x, int y, int z, BlockMetadata blockMetadata, bool FromNetwork = false, BlockUpdateMode UpdateMode = BlockUpdateMode.ForceUpdate)
    {
        Chunk chunk = GetChunk(x, y, z);

        // Check if the block was placed via network to prevent echoing
        if (!FromNetwork) Networking.SetBlock_Caller(x, y, z, blockMetadata, UpdateMode);

        if (chunk != null)
        {
            chunk.SetBlock(x - chunk.pos.x, y - chunk.pos.y, z - chunk.pos.z, blockMetadata, FromNetwork, UpdateMode);

            //if (UpdateMode == BlockUpdateMode.ForceUpdate)
            //    chunk.UpdateChunk(ChunkUpdateMode.ForceSingle);
            //else if (UpdateMode == BlockUpdateMode.Queue)
            //    AddToChunkUpdateQueue(chunk);

            //for (int ix = -1; ix < 2; ix++)
            //{
            //    for (int iy = -1; iy < 2; iy++)
            //    {
            //        for (int iz = -1; iz < 2; iz++)
            //        {
            //            Chunk tempchunk = GetChunk(x + ix, y + iy, z + iz);
            //            if (tempchunk != chunk && tempchunk != null)
            //                if (UpdateMode == BlockUpdateMode.Queue)
            //                    AddToChunkUpdateQueue(chunk);
            //                else
            //                    tempchunk.UpdateChunk(ChunkUpdateMode.ForceSingle);
            //        }
            //    }
            //}
        }
    }
    #endregion
}

public enum BlockUpdateMode : byte
{
    ForceUpdate = 0,
    Queue,
    Silent,
    None
}
using BeardedManStudios.Forge.Networking.Unity;
using CielaSpike;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class World : MonoBehaviour
{
    public Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
    public List<Chunk> ChunkUpdateQueue = new List<Chunk>();

    public Clouds[] clouds;

    public GameObject Prefab_Chunk;
    public GameObject Prefab_Clouds;

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
    [HideInInspector] public dynamic worldGen;

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

    //NETWORKING
    private World_Network Networking;

    #region "Create blockdata to work with and proceed to map generation"

    void Awake()
    {
        rand = new Unity.Mathematics.Random((uint)Guid.NewGuid().GetHashCode());
        WorldSeed = new float2(rand.NextFloat2(0f, 100f));
        Schematics.world = this;
        Networking = gameObject.GetComponent<World_Network>();

        MapLoadInfo = "Connecting...";

        StartCoroutine(UpdateChunkQueue());
    }

    void OnDestroy()
    {
        StopCoroutine(ExecuteWorldgenQueue());
    }

    /// <summary>
    /// Obsolete
    /// </summary>
    public IEnumerator CreateWorld()
    {
        NetworkManager.Instance.InstantiatePlayer_Networking();

        GUI_MapLoadingOverlay.SetActive(true);

        if (chunks.Count() > 0)
        {
            foreach (KeyValuePair<int3, Chunk> chunk in chunks)
                Destroy(chunk.Value.gameObject);

            foreach (Clouds cld in clouds)
                Destroy(cld.gameObject);

            chunks.Clear();
            clouds = new Clouds[0];
            Debug.Log("Cleared old chunks and clouds.");
        }
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
        if (Mods.LoadedMapgens.Count == 0) Counter = 12;

        while (true)
        {
            if (worldGen != null)
                if (worldGen.ChunkQueue.Count > 0)
                {
                    for (int i = 0; i < (worldGen.ChunkQueue.Count > Counter ? Counter : worldGen.ChunkQueue.Count); i++)
                    {
                        StartCoroutine(worldGen.GenerateChunk(worldGen.ChunkQueue[0]));
                        worldGen.ChunkQueue.RemoveAt(0);
                        if (Counter < 12) yield return null;
                    }
                }
            yield return null;
        }
    }

    #endregion

    public void Update()
    {
        if (GUI_MapLoadingText != null && GUI_MapLoadingText.text != MapLoadInfo) GUI_MapLoadingText.text = MapLoadInfo;
        if (GenerateClouds && AnimateClouds && !isAnimating) StartCoroutine(UpdateClouds());
    }

    #region "Chunk stuff"

    public IEnumerator UpdateChunkQueue()
    {
        while (true)
        {
            if (ChunkUpdateQueue.Count > 0)
            {
                ChunkUpdateQueue[0].UpdateChunk(ChunkUpdateMode.ForceSingle);
                ChunkUpdateQueue.RemoveAt(0);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void AddToChunkUpdateQueue(Chunk chunk)
    {
        if (!ChunkUpdateQueue.Contains(chunk))
            ChunkUpdateQueue.Add(chunk);
    }

    public Chunk CreateChunk(int x, int y, int z, bool generate = true)
    {
        int3 worldPos = new int3(x, y, z);
        //Safety check
        Chunk newChunk = GetChunk(x, y, z);
        if (newChunk != null) return null;

        //Instantiate the chunk at the coordinates using the chunk prefab
        GameObject newChunkObject = Instantiate(Prefab_Chunk, new Vector3(x, y, z), Quaternion.Euler(Vector3.zero)) as GameObject;
        newChunkObject.transform.parent = transform;
        newChunkObject.name = $"Chunk ({x.ToString()}/{y.ToString()}/{z.ToString()})";

        newChunk = newChunkObject.GetComponent<Chunk>();
        newChunk.pos = worldPos;
        newChunk.world = this;

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(worldPos, newChunk);

        if (generate) newChunk.GenerateChunk();

        return newChunk;
    }

    public void DestroyChunk(int x, int y, int z)
    {
        Chunk chunk = null;
        if (chunks.TryGetValue(new int3(x, y, z), out chunk))
        {
            //Serialization.SaveChunk(chunk);
            Destroy(chunk.gameObject);
            chunks.Remove(new int3(x, y, z));
        }
    }

    public Chunk GetChunk(int x, int y, int z)
    {
        int3 pos = new int3();
        float multiple = BlockData.ChunkSize;
        pos.x = (int)System.Math.Floor(x / multiple) * BlockData.ChunkSize;
        pos.y = (int)System.Math.Floor(y / multiple) * BlockData.ChunkSize;
        pos.z = (int)System.Math.Floor(z / multiple) * BlockData.ChunkSize;

        chunks.TryGetValue(pos, out Chunk containerChunk);

        return containerChunk;
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

        if (containerChunk != null)
        {
            BlockMetadata block = containerChunk.GetBlock(
                x - containerChunk.pos.x,
                y - containerChunk.pos.y,
                z - containerChunk.pos.z);

            return block;
        }
        else
        {
            return new BlockMetadata { ID = 0, Marched = false, MarchedValue = 0 };
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
        if (!FromNetwork) Networking.SetBlock_Caller(x, y, z, blockMetadata);

        if (chunk != null)
        {
            chunk.SetBlock(x - chunk.pos.x, y - chunk.pos.y, z - chunk.pos.z, blockMetadata, FromNetwork, UpdateMode);

            if (UpdateMode == BlockUpdateMode.ForceUpdate)
                chunk.UpdateChunk(ChunkUpdateMode.ForceSingle);
            else if (UpdateMode == BlockUpdateMode.Queue)
                AddToChunkUpdateQueue(chunk);

            for (int ix = -1; ix < 2; ix++)
            {
                for (int iy = -1; iy < 2; iy++)
                {
                    for (int iz = -1; iz < 2; iz++)
                    {
                        Chunk tempchunk = GetChunk(x + ix, y + iy, z + iz);
                        if (tempchunk != chunk && tempchunk != null)
                            if (UpdateMode == BlockUpdateMode.Queue)
                                AddToChunkUpdateQueue(chunk);
                            else
                                tempchunk.UpdateChunk(ChunkUpdateMode.ForceSingle);
                    }
                }
            }

            UpdateIfEqual(x - chunk.pos.x, 0, new int3(x - 1, y, z));
            UpdateIfEqual(x - chunk.pos.x, BlockData.ChunkSize - 1, new int3(x + 1, y, z));
            UpdateIfEqual(y - chunk.pos.y, 0, new int3(x, y - 1, z));
            UpdateIfEqual(y - chunk.pos.y, BlockData.ChunkSize - 1, new int3(x, y + 1, z));
            UpdateIfEqual(z - chunk.pos.z, 0, new int3(x, y, z - 1));
            UpdateIfEqual(z - chunk.pos.z, BlockData.ChunkSize - 1, new int3(x, y, z + 1));
        }
    }

    void UpdateIfEqual(int value1, int value2, int3 pos)
    {
        if (value1 == value2)
        {
            Chunk chunk = GetChunk(pos.x, pos.y, pos.z);
            if (chunk != null)
                chunk.UpdateChunk(ChunkUpdateMode.ForceSingle);
        }
    }
    #endregion
}

public enum BlockUpdateMode
{
    ForceUpdate = 0,
    Queue,
    None
}
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
    public Dictionary<WorldPos, Chunk> chunks = new Dictionary<WorldPos, Chunk>();
    public Clouds[] clouds;

    public GameObject Prefab_Chunk;
    public GameObject Prefab_Clouds;

    // World name (redundant, but meh)
    public string worldName = "World v2";
    string MapLoadInfo = "Generating world";
    private bool Created = false;

    // World size (by chunks) / Maximum 16^3 or equivalent (4,096 chunks max, 16,777,216 blocks)
    // X, Y, Z                / Any value above that is highly unstable and might crash the game.
    public int3 WorldSize = new int3(1, 1, 1); //16, 8, 16

    [HideInInspector] public ushort GeneratedChunks = 0;

    //TEMP
    public GameObject GUI_MapLoadingOverlay;
    public UnityEngine.UI.Text GUI_MapLoadingText;

    public Material BlockMaterial;
    public Material MarchedBlockMaterial;
    public Material SelectedMaterial;

    public float2 WorldSeed;

    Unity.Mathematics.Random rand;

    [Header("Clouds Settings")]
    public bool AnimateClouds = false;
    bool isAnimating = false;
    [Range(1f, 1000f)] public float CloudDensity = 42f;
    [Range(1f, 1000f)] public float CloudDensity2 = 9f;
    [Range(1f, 1000f)] public float HeightDivision = 1000f;
    [Range(2, 10)] public int CloudUpdateSpeed = 2;

    #region "Create blockdata to work with and proceed to map generation"

    public void Awake()
    {
        rand = new Unity.Mathematics.Random((uint)Guid.NewGuid().GetHashCode());
        WorldSeed = new float2(rand.NextFloat2(0f, 100f));

        BlockData.InitalizeBlocks();

        string[] PropertyNames = BlockMaterial.GetTexturePropertyNames();

        BlockMaterial.SetFloat("Vector1_430CB87B", BlockData.BlockTileSize);
        BlockMaterial.SetFloat("Vector1_7C9B6D59", BlockData.TextureSize);
        BlockMaterial.SetTexture(PropertyNames[0], BlockData.BlockTexture);
        BlockMaterial.GetTexture(PropertyNames[0]).filterMode = FilterMode.Point;

        PropertyNames = MarchedBlockMaterial.GetTexturePropertyNames();
        MarchedBlockMaterial.SetFloat("Vector1_430CB87B", BlockData.BlockTileSize);
        MarchedBlockMaterial.SetFloat("Vector1_7C9B6D59", BlockData.TextureSize);
        MarchedBlockMaterial.SetTexture(PropertyNames[0], BlockData.BlockTexture);
        MarchedBlockMaterial.GetTexture(PropertyNames[0]).filterMode = FilterMode.Point;

        SelectedMaterial.SetTextureOffset("_UnlitColorMap", new Vector2(BlockData.byID[1].Texture_Up.x * BlockData.BlockTileSize, BlockData.byID[1].Texture_Up.y * BlockData.BlockTileSize));
        SelectedMaterial.SetTextureScale("_UnlitColorMap", new Vector2(BlockData.BlockTileSize, BlockData.BlockTileSize));
        SelectedMaterial.SetTexture("_UnlitColorMap", BlockData.BlockTexture);
        SelectedMaterial.GetTexture("_UnlitColorMap").filterMode = FilterMode.Point;

        StartCoroutine(CreateWorld());
    }

    public IEnumerator CreateWorld()
    {
        List<WorldPos> Chunk_InstantiateList = new List<WorldPos>();
        List<WorldPos> Chunk_GenerateList = new List<WorldPos>();
        List<WorldPos> buildList2 = new List<WorldPos>();
        List<WorldPos> saveList = new List<WorldPos>();

        Created = true;
        int delay = 0;

        for (int x = 0; x <= WorldSize.x; x++)
        {
            for (int z = 0; z <= WorldSize.z; z++)
            {
                for (int y = 0; y <= WorldSize.y; y++)
                {
                    CreateChunk(x * Chunk.chunkSize, y * Chunk.chunkSize, z * Chunk.chunkSize, true);

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

        while (GeneratedChunks < (WorldSize.x * WorldSize.y * WorldSize.z)/2)
        {
            yield return null;
        }

        MapLoadInfo = "Rendering world...";

        for (int x = 0; x <= WorldSize.x; x++)
        {
            for (int z = 0; z <= WorldSize.z; z++)
            {
                for (int y = 0; y <= WorldSize.y; y++)
                {
                    GetChunk(x * Chunk.chunkSize, y * Chunk.chunkSize, z * Chunk.chunkSize).update = true;
                    
                    delay++;
                    if (delay > WorldSize.y*3)
                    {
                        delay = 0;
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
        }

        MapLoadInfo = "Placing clouds...";

        yield return new WaitForEndOfFrame();

        int count = 0;
        clouds = new Clouds[((WorldSize.x+10) * (WorldSize.z+10))];


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

        GUI_MapLoadingOverlay.SetActive(false);

        StartCoroutine(UpdateClouds());
        StopCoroutine(CreateWorld());
        yield return null;
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

    public void Update()
    {
        if (AnimateClouds && !isAnimating) StartCoroutine(UpdateClouds());
        if (GUI_MapLoadingText.isActiveAndEnabled)
        {
            if (GeneratedChunks < ((WorldSize.x) * (WorldSize.y) * (WorldSize.z)))
            {
                float onepercent = (WorldSize.x * WorldSize.y * WorldSize.z) / 100f;
                GUI_MapLoadingText.text = MapLoadInfo + " " + (Mathf.FloorToInt(GeneratedChunks / onepercent)) + "%";
            } else {
                GUI_MapLoadingText.text = MapLoadInfo;
            }
        }
    }

    #region "Chunk stuff"
        
    public void CreateChunk(int x, int y, int z, bool generate = true)
    {
        WorldPos worldPos = new WorldPos(x, y, z);

        //Instantiate the chunk at the coordinates using the chunk prefab
        GameObject newChunkObject = Instantiate(Prefab_Chunk, new Vector3(x, y, z), Quaternion.Euler(Vector3.zero)) as GameObject;
        newChunkObject.transform.parent = transform;
        newChunkObject.name = "Chunk (" + x + "/" + y + "/" + z + ")";

        Chunk newChunk = newChunkObject.GetComponent<Chunk>();
        newChunk.pos = worldPos;
        newChunk.world = this;

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(worldPos, newChunk);

        if (generate) newChunk.GenerateChunk();
    }

    public void DestroyChunk(int x, int y, int z)
    {
        Chunk chunk = null;
        if (chunks.TryGetValue(new WorldPos(x, y, z), out chunk))
        {
            //Serialization.SaveChunk(chunk);
            Destroy(chunk.gameObject);
            chunks.Remove(new WorldPos(x, y, z));
        }
    }

    public Chunk GetChunk(int x, int y, int z)
    {
        WorldPos pos = new WorldPos();
        float multiple = Chunk.chunkSize;
        pos.x = (int)System.Math.Floor(x / multiple) * Chunk.chunkSize;
        pos.y = (int)System.Math.Floor(y / multiple) * Chunk.chunkSize;
        pos.z = (int)System.Math.Floor(z / multiple) * Chunk.chunkSize;

        Chunk containerChunk = null;

        chunks.TryGetValue(pos, out containerChunk);

        return containerChunk;
    }

    public Chunk GetChunkByChunkCoordinates(int x, int y, int z)
    {
        WorldPos pos = new WorldPos();
        Chunk containerChunk = null;
        chunks.TryGetValue(pos, out containerChunk);
        return containerChunk;
    }
    #endregion

    #region "Block stuff"

    public Block GetBlock(int x, int y, int z)
    {
        Chunk containerChunk = GetChunk(x, y, z);

        if (containerChunk != null)
        {
            Block block = containerChunk.GetBlock(
                x - containerChunk.pos.x,
                y - containerChunk.pos.y,
                z - containerChunk.pos.z);

            return block;
        }
        else
        {
            return BlockData.byID[0];
        }

    }

    public void SetBlock(int x, int y, int z, Block block, byte modifier = 0, bool UsePhysics = false, bool PlacedByPhysics = false)
    {
        Chunk chunk = GetChunk(x, y, z);

        if (chunk != null)
        {
            chunk.SetBlock(x - chunk.pos.x, y - chunk.pos.y, z - chunk.pos.z, block, UsePhysics);
            chunk.update = true;

            for (int ix = -1; ix < 2; ix++)
            {
                for (int iy = -1; iy < 2; iy++)
                {
                    for (int iz = -1; iz < 2; iz++)
                    {
                        Chunk tempchunk = GetChunk(x + ix, y + iy, z + iz);
                        if (tempchunk != chunk && tempchunk != null)
                            tempchunk.update = true;
                    }
                }
            }

            UpdateIfEqual(x - chunk.pos.x, 0, new WorldPos(x - 1, y, z));
            UpdateIfEqual(x - chunk.pos.x, Chunk.chunkSize - 1, new WorldPos(x + 1, y, z));
            UpdateIfEqual(y - chunk.pos.y, 0, new WorldPos(x, y - 1, z));
            UpdateIfEqual(y - chunk.pos.y, Chunk.chunkSize - 1, new WorldPos(x, y + 1, z));
            UpdateIfEqual(z - chunk.pos.z, 0, new WorldPos(x, y, z - 1));
            UpdateIfEqual(z - chunk.pos.z, Chunk.chunkSize - 1, new WorldPos(x, y, z + 1));
        }
    }

    void UpdateIfEqual(int value1, int value2, WorldPos pos)
    {
        if (value1 == value2)
        {
            Chunk chunk = GetChunk(pos.x, pos.y, pos.z);
            if (chunk != null)
                chunk.update = true;
        }
    }
    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class World : MonoBehaviour
{
    public Dictionary<WorldPos, Chunk> chunks = new Dictionary<WorldPos, Chunk>();

    public GameObject Prefab_Chunk;
    
    // World name (redundant, but meh)
    public string worldName = "World v2";
    private bool Created = false;

    // World size (by chunks) / Maximum 16^3 or equivalent (4,096 chunks max, 16,777,216 blocks)
    // X, Y, Z                / Any value above that is highly unstable and might crash the game.
    int3 WorldSize = new int3(4, 4, 4);

    [HideInInspector] public ushort GeneratedChunks = 0;
    
    //TEMP
    public GameObject GUI_MapLoadingOverlay;
    public UnityEngine.UI.Text GUI_MapLoadingText;

    public Material BlockMaterial;
    public Material MarchedBlockMaterial;
    public Material SelectedMaterial;


    #region "Create blockdata to work with and proceed to map generation"

    public void Awake()
    {
        Application.targetFrameRate = 300;
        QualitySettings.vSyncCount = 1;
        BlockData.InitalizeBlocks();

        string[] PropertyNames = BlockMaterial.GetTexturePropertyNames();

        BlockMaterial.SetFloat("Vector1_430CB87B", BlockData.BlockTileSize);
        BlockMaterial.SetTexture(PropertyNames[0], BlockData.BlockTexture);
        BlockMaterial.GetTexture(PropertyNames[0]).filterMode = FilterMode.Point;

        PropertyNames = MarchedBlockMaterial.GetTexturePropertyNames();
        MarchedBlockMaterial.SetFloat("Vector1_430CB87B", BlockData.BlockTileSize);
        MarchedBlockMaterial.SetTexture(PropertyNames[0], BlockData.BlockTexture);
        MarchedBlockMaterial.GetTexture(PropertyNames[0]).filterMode = FilterMode.Point;

        SelectedMaterial.SetTextureOffset("_BaseColorMap", new Vector2(BlockData.byID[1].Texture_Up.x * BlockData.BlockTileSize, BlockData.byID[1].Texture_Up.y * BlockData.BlockTileSize));
        SelectedMaterial.SetTextureScale("_BaseColorMap", new Vector2(BlockData.BlockTileSize, BlockData.BlockTileSize));
        SelectedMaterial.SetTexture("_BaseColorMap", BlockData.BlockTexture);
        SelectedMaterial.GetTexture("_BaseColorMap").filterMode = FilterMode.Point;

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

        while (GeneratedChunks < (WorldSize.x * WorldSize.y * WorldSize.z))
        {
            yield return null;
        }

        GUI_MapLoadingOverlay.SetActive(false);

        for (int x = 0; x <= WorldSize.x; x++)
        {
            for (int z = 0; z <= WorldSize.z; z++)
            {
                for (int y = 0; y <= WorldSize.y; y++)
                {
                    GetChunk(x * Chunk.chunkSize, y * Chunk.chunkSize, z * Chunk.chunkSize).update = true;
                    
                    delay++;
                    if (delay > WorldSize.y)
                    {
                        delay = 0;
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
        }
        StopCoroutine(CreateWorld());
        yield return null;
    }

    #endregion

    public void Update()
    {

        if (GeneratedChunks < ((WorldSize.x) * (WorldSize.y) * (WorldSize.z)))
        {
            float onepercent = (WorldSize.x * WorldSize.y * WorldSize.z) / 100f;
            GUI_MapLoadingText.text = "Generating world... " + (Mathf.FloorToInt(GeneratedChunks / onepercent)) + "%\n"
                + "Mapsize: " + WorldSize.x + "/" + WorldSize.y + "/" + WorldSize.z + "\n"
                + "Blocks: " + (WorldSize.x * WorldSize.y * WorldSize.z)*16*16*16;
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

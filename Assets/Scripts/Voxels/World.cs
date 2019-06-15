using MarchingCubesProject;
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

    public GameObject Prefab_Chunk_NonSmoothed;
    public GameObject Prefab_Chunk_Smoothed;
    public GameObject Prefab_Chunk_Water;
    
    // [Chunk blocks]
    // Calculation for coordinates:
    //          [x + y * 16 + z * 256] = array index
    //
    // Reverse:
    //          z = (blocks / 256);
    //          y = (blocks / 16) - (z * 16);
    //          x = blocks - y * 16 - z * 256;
    //
    // Warning: Dispose this on GameObject.Destroy!
    // public NativeArray<ushort> NA_SingleChunk;
    
    // World name (redundant, but meh)
    public string worldName = "World v2";

    private bool Created = false;

    // World size (by chunks)
    // MUST BE DIVIDABLE BY TWO (>= 2)
    // Each chunk contains 16^3 blocks
    // 
    // Maximum 16^3 or equivalent (4,096 chunks max, 16,777,216 blocks)
    // Any value above that is highly unstable and might crash the game.
    // Just remember, every block >= 2 tris, that gives us from 33,554,432 to 201,326,592 triangles.
    // This is to be optimized via greedy meshing
    // 
    // X, Y, Z
    int3 WorldSize = new int3(16, 4, 16);

    // For loading purposes of old IJob code, each IJob increases value of this ushort by 1
    // When this gets around 70% of WorldSize X^Y^Z, proceed to rendering
    [HideInInspector] public ushort GeneratedChunks = 0;

    public UnityEngine.UI.Text DebugText;

    // Used for debug to calculate time, to be removed
    //private float CurrentTime;

    //TEMP
    public GameObject GUI_MapLoadingOverlay;
    public UnityEngine.UI.Text GUI_MapLoadingText;

    #region "Create blockdata to work with"

    public void Awake()
    {
        BlockData.InitalizeBlocks();

        StartCoroutine(CreateWorld());
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
                    //if (WorldSize.x > 8 || WorldSize.y > 8 || WorldSize.z > 8)
                    //{
                    delay++;
                    if (delay > WorldSize.y*4)
                    {
                        delay = 0;
                        yield return new WaitForEndOfFrame();
                    }
                    //}
                    //GetChunk(x * Chunk.chunkSize, y * Chunk.chunkSize, z * Chunk.chunkSize).GenerateChunk();
                    //yield return new WaitForEndOfFrame();
                }
            }
        }

        while (GeneratedChunks < (WorldSize.x * WorldSize.y * WorldSize.z)/2)
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
                    //if (WorldSize.x > 8 || WorldSize.y > 8 || WorldSize.z > 8)
                    //{
                    delay++;
                    if (delay > WorldSize.y)
                    {
                        delay = 0;
                        yield return new WaitForEndOfFrame();
                    }
                    //}
                    //yield return new WaitForEndOfFrame();
                }
            }
        }

        StopCoroutine(CreateWorld());
        yield return null;
    }

    #region "Physics stuff, to be removed later on"
    /*
    // ############################################################################
    public struct PUQStruct
    {
        public int x, y, z;
        public float LastQueued;
    }
    public List<PUQStruct> PhysicsUpdateQueueList = new List<PUQStruct>();

    public bool PhysicsPaused = false;
    public bool RefreshMe = false;

    int PreviousValue = 0; int CurrentVal = 0;
    int bud_ = 0; int previousbud_ = 0;

    void Update()
    {
        if (RefreshMe)
        {
            RefreshMe = false;
            ForceUpdateMapPhysics("BlockWater");
        }
        CurrentTime = Time.timeSinceLevelLoad;

        DebugText.text = "Block updates queue: " + PhysicsUpdateQueueList.Count + "\nUpdated in single frame: " + bud_ + " (previous: " + previousbud_ + ")";
        if (bud_ > 0) previousbud_ = bud_;
        bud_ = 0;

        if (PhysicsUpdateQueueList.Count > 0)
        {
            //List<PUQStruct> templist = PhysicsUpdateQueueList.OrderBy(sel => sel.LastQueued).ToList();
            //PhysicsUpdateQueueList = templist;
            if (PreviousValue != 0)
                CurrentVal = PhysicsUpdateQueueList.Count - PreviousValue;

            PreviousValue = PhysicsUpdateQueueList.Count;

        }
    }

    public void ForceUpdateMapPhysics(string blocktype = "BlockAir")
    {
        if (blocktype != "BlockAir")
        {
            for (int x1 = 0; x1 <= WorldSize.x * 16; x1++)
            {
                for (int y1 = -(WorldSize.y / 2) * 16; y1 <= (WorldSize.y / 2) * 16; y1++)
                {
                    for (int z1 = 0; z1 <= WorldSize.z * 16; z1++)
                    {
                        if (GetBlock(x1, y1, z1).GetType().ToString() == blocktype)
                        {
                            SetBlock(x1, y1, z1, new BlockWater(), 0, true, true);
                        }
                    }
                }
            }
        }
    }

    IEnumerator BlockPhysicsQueue()
    {
        //yield return Ninja.JumpBack;
        while (!PhysicsPaused)
        {
            if (PhysicsUpdateQueueList.Count > 0)
            {
                for (int i = 0; i < PhysicsUpdateQueueList.Count; i++)
                {
                    if (PhysicsUpdateQueueList[0].LastQueued + GetBlock(PhysicsUpdateQueueList[0].x, PhysicsUpdateQueueList[0].y, PhysicsUpdateQueueList[0].z).GetPhysicsTime(this, PhysicsUpdateQueueList[0].x, PhysicsUpdateQueueList[0].y, PhysicsUpdateQueueList[0].z) < CurrentTime)
                    {
                        if (GetBlock(PhysicsUpdateQueueList[0].x, PhysicsUpdateQueueList[0].y, PhysicsUpdateQueueList[0].z).UpdateBlock(this, PhysicsUpdateQueueList[0].x, PhysicsUpdateQueueList[0].y, PhysicsUpdateQueueList[0].z))
                        {
                            for (int x1 = PhysicsUpdateQueueList[0].x - 1; x1 < PhysicsUpdateQueueList[0].x + 2; x1++)
                            {
                                for (int y1 = PhysicsUpdateQueueList[0].y - 1; y1 < PhysicsUpdateQueueList[0].y + 2; y1++)
                                {
                                    for (int z1 = PhysicsUpdateQueueList[0].z - 1; z1 < PhysicsUpdateQueueList[0].z + 2; z1++)
                                    {
                                        PUQStruct tempPUQ;
                                        tempPUQ.x = x1;
                                        tempPUQ.y = y1;
                                        tempPUQ.z = z1;
                                        tempPUQ.LastQueued = CurrentTime + GetBlock(x1, y1, z1).GetPhysicsTime(this, x1, y1, z1);
                                        if (!(x1 == -1 && y1 == -1 && z1 == -1) &&
                                            !(x1 == 1 && y1 == 1 && z1 == -1) &&
                                            !(x1 == -1 && y1 == 1 && z1 == -1) &&
                                            !(x1 == 1 && y1 == -1 && z1 == -1) &&
                                            !(x1 == -1 && y1 == -1 && z1 == 1) &&
                                            !(x1 == 1 && y1 == 1 && z1 == 1) &&
                                            !(x1 == -1 && y1 == 1 && z1 == 1) &&
                                            !(x1 == 1 && y1 == -1 && z1 == 1) &&
                                            !(x1 == 0 && y1 == 0 && z1 == 0) &&
                                            (x1 > 0) &&
                                            (x1 < (WorldSize.x * 16) - 1) &&
                                            (y1 > -((WorldSize.y / 2) * 16) - 1) &&
                                            (y1 < ((WorldSize.y / 2) * 16) - 1) &&
                                            (z1 > 0) &&
                                            (z1 < (WorldSize.z * 16) - 1))

                                        {
                                            PhysicsUpdateQueueList.Add(tempPUQ);
                                            bud_++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        PhysicsUpdateQueueList.Add(PhysicsUpdateQueueList[0]);
                    }
                    PhysicsUpdateQueueList.RemoveAt(0);
                }
            }
        }
        yield return null;
    }
    // ############################################################################
    */
    #endregion

    #region "Chunk stuff"
        
    public void CreateChunk(int x, int y, int z, bool generate = true)
    {
        WorldPos worldPos = new WorldPos(x, y, z);

        //Instantiate the chunk at the coordinates using the chunk prefab
        GameObject newChunkObject = Instantiate(
                        Prefab_Chunk_NonSmoothed, new Vector3(x, y, z),
                        Quaternion.Euler(Vector3.zero)
                    ) as GameObject;
        
        newChunkObject.transform.parent = transform;
        newChunkObject.name = "Chunk (" + x + "/" + y + "/" + z + ")";

        Chunk newChunk = newChunkObject.GetComponent<Chunk>();
        //newChunk.Chunk_Water = newChunkObject_water;
        //newChunk.Chunk_Smoothed = newChunkObject_smoothed;

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

        //pos.x = Mathf.FloorToInt(x / multiple) * Chunk.chunkSize;
        //pos.y = Mathf.FloorToInt(y / multiple) * Chunk.chunkSize;
        //pos.z = Mathf.FloorToInt(z / multiple) * Chunk.chunkSize;

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

            // ############################################################
            // REMOVE THIS LATER
            // ############################################################
            /*if (!PlacedByPhysics)
            {
                for (int x1 = x - 1; x1 < x + 2; x1++)
                {
                    for (int y1 = y - 1; y1 < y + 2; y1++)
                    {
                        for (int z1 = z - 1; z1 < z + 2; z1++)
                        {
                            if (!(x1 == -1 && y1 == -1 && z1 == -1) &&
                                !(x1 == 1 && y1 == 1 && z1 == -1) &&
                                !(x1 == -1 && y1 == 1 && z1 == -1) &&
                                !(x1 == 1 && y1 == -1 && z1 == -1) &&
                                !(x1 == -1 && y1 == -1 && z1 == 1) &&
                                !(x1 == 1 && y1 == 1 && z1 == 1) &&
                                !(x1 == -1 && y1 == 1 && z1 == 1) &&
                                !(x1 == 1 && y1 == -1 && z1 == 1) &&
                                !(x1 == 0 && y1 == 0 && z1 == 0) &&
                                !(x1 == -1 && y1 == 1 && z1 == 1) &&
                                !(x1 == 1 && y1 == 1 && z1 == 1) &&
                                !(x1 == -1 && y1 == -1 && z1 == 1) &&
                                !(x1 == 1 && y1 == -1 && z1 == 1) &&
                                !(x1 == -1 && y1 == 1 && z1 == -1) &&
                                !(x1 == 1 && y1 == 1 && z1 == -1) &&
                                !(x1 == -1 && y1 == -1 && z1 == -1) &&
                                !(x1 == 1 && y1 == -1 && z1 == -1) &&
                                (x1 > 0) &&
                                (x1 < (WorldSize.x * 16) - 1) &&
                                (y1 > -((WorldSize.y / 2) * 16) - 1) &&
                                (y1 < ((WorldSize.y / 2) * 16) - 1) &&
                                (z1 > 0) &&
                                (z1 < (WorldSize.z * 16) - 1))
                            {
                                if (GetBlock(x1, y1, z1).GetType().ToString() == "BlockWater")
                                {
                                    if (chunk != GetChunk(x1, y1, z1))
                                    {
                                        chunk.forceupdate = true;
                                    }
                                    chunk = GetChunk(x1, y1, z1);
                                    chunk.SetBlock(x1 - chunk.pos.x, y1 - chunk.pos.y, z1 - chunk.pos.z, new BlockWater(), UsePhysics);
                                }
                            }
                        }
                    }
                }
            }*/
            // ############################################################
            // REMOVE THIS LATER
            // ############################################################

            UpdateIfEqual(x - chunk.pos.x, 0, new WorldPos(x - 1, y, z));
            UpdateIfEqual(x - chunk.pos.x, Chunk.chunkSize - 1, new WorldPos(x + 1, y, z));
            UpdateIfEqual(y - chunk.pos.y, 0, new WorldPos(x, y - 1, z));
            UpdateIfEqual(y - chunk.pos.y, Chunk.chunkSize - 1, new WorldPos(x, y + 1, z));
            UpdateIfEqual(z - chunk.pos.z, 0, new WorldPos(x, y, z - 1));
            UpdateIfEqual(z - chunk.pos.z, Chunk.chunkSize - 1, new WorldPos(x, y, z + 1));


            /*if (UsePhysics && !PlacedByPhysics)
            {
                for (int x1 = x - 1; x1 < x + 1; x1++)
                {
                    for (int y1 = y - 1; y1 < y + 1; y1++)
                    {
                        for (int z1 = z - 1; z1 < z + 1; z1++)
                        {
                            PUQStruct tempPUQ;
                            tempPUQ.x = x1;
                            tempPUQ.y = y1;
                            tempPUQ.z = z1;
                            tempPUQ.LastQueued = CurrentTime + block.GetPhysicsTime(this, x1, y1, z1);
                            PhysicsUpdateQueueList.Add(tempPUQ);
                            //PhysicsUpdateQueueList.OrderBy(a => a.LastQueued);
                        }
                    }
                }
            }*/

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

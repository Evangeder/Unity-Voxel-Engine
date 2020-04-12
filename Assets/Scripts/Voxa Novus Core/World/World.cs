using BeardedManStudios.Forge.Networking.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;

namespace VoxaNovus
{
    public class World : MonoBehaviour
    {
        public Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
        public Queue<Chunk> ChunkUpdateQueue = new Queue<Chunk>();

        public GameObject Prefab_Chunk;
        public GameObject PlayerChar;

        [HideInInspector] public static Thread mainThread = Thread.CurrentThread;

        [Header("Main")]
        public string worldName = "World v2";
        public bool MainMenuWorld = false;
        [HideInInspector] public bool Created = false;

        // World size (by chunks) / Maximum 16^3 or equivalent (4,096 chunks max, 16,777,216 blocks)
        // X, Y, Z                / Any value above that is highly unstable and might crash the game.
        public int3 WorldSize = new int3(1, 1, 1); //16, 8, 16

        [HideInInspector] public ushort GeneratedChunks = 0;

        // World generator class
        [HideInInspector] public WorldGen.WorldGen worldGen;

        //TEMP - shouldnt be here
        public GameObject GUI_MapLoadingOverlay;
        public Material BlockMaterial;
        public Material MarchedBlockMaterial;
        public Material SelectedMaterial;
        [HideInInspector] public float2 WorldSeed;
        Unity.Mathematics.Random rand;

        //debug
        public bool Loaded = false;
        public UnityEngine.UI.Text debugCMcount;
        public UnityEngine.UI.Text debugCMcount2;

        //NETWORKING
        private World_Network Networking;

        #region "Create blockdata to work with and proceed to map generation"

        [SerializeField] UnityEngine.UI.Text debugLog;
        public void AppendLog(string text)
        {
            debugLog.text = text;
            //if (debugLog.text.Length > 0)
            //{
            //    string[] split = debugLog.text.Split('\n');
            //    if (split.Length >= 10)
            //    {
            //        debugLog.text = "";
            //        for (int i = split.Length - 9; i < split.Length; i++)
            //            debugLog.text += $"{split[i]}\n";
            //        debugLog.text += $"{text}\n";
            //    }
            //    else
            //    {
            //        debugLog.text += $"{text}\n";
            //    }
            //}
            //debugLog.text += $"{text}\n";
        }

        void Awake()
        {
            CalculatedExplosion.Recalculate();
            BlockSettings.world = this;
            Schematics.world = this;
            BlockRaycast.world = this;
            rand = new Unity.Mathematics.Random((uint)Guid.NewGuid().GetHashCode());
            WorldSeed = new float2(rand.NextFloat2(0f, 100f));
            Networking = gameObject.GetComponent<World_Network>();

            StartCoroutine(UpdateChunkQueue());
            StartCoroutine(BlockUpdateQueue.CoroutineIterateThroughQueue());

            StartCoroutine(PhysicsQueue.PhysicsQueueIterator());
        }

        void OnDestroy()
        {
            if (BlockSettings.NativeByID.IsCreated && !MainMenuWorld) BlockSettings.NativeByID.Dispose();
            StopCoroutine(ExecuteWorldgenQueue());
        }

        public IEnumerator CreateWorld()
        {
            GUI_MapLoadingOverlay.SetActive(true);
            NetworkManager.Instance.InstantiatePlayer_Networking();
            ChunkManager.Init(Prefab_Chunk, this, 100000, true);

            yield return new WaitForSeconds(5);

            //while (!BlockSettings.SoundsLoaded)
            //    yield return new WaitForEndOfFrame();

            Loaded = true;
            Created = true;

            GUI_MapLoadingOverlay.SetActive(false);

            yield return null;
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
            debugCMcount.text = $"Buffer data:\nType: {ChunkManager.GetType}\nAllocated space: {ChunkManager.Length}/{ChunkManager.Max}\nCurrently stored: {ChunkManager.Count}\nTotal chunks allocated: {ChunkManager.ObjectCount}\n\nPhysics Queue: {PhysicsQueue.priorityQueue.Count}\n{(PlayerChar != null ? $"Pos: {PlayerChar.transform.position}" : "")}";
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
                        worldGen.QueueChunk(ChunkUpdateQueue.Dequeue());
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

            if (chunks.ContainsKey(worldPos)
                || CheckChunk(x, y, z)
                || GetChunk(x, y, z))
                return null;

            if (ChunkManager.Left == 0) return null;
            Chunk newChunk = ChunkManager.GetChunk();
            newChunk.world = this;
            Transform tr = newChunk.transform;
            tr.name = $"Chunk ({x.ToString()}/{y.ToString()}/{z.ToString()})";
            tr.position = new Vector3(x * BlockSettings.ChunkScale, y * BlockSettings.ChunkScale, z * BlockSettings.ChunkScale);
            newChunk.transformPosition = new int3((int)tr.position.x, (int)tr.position.y, (int)tr.position.z);
            newChunk.pos = worldPos;

            //Add it to the chunks dictionary with the position as the key
            chunks.Add(worldPos, newChunk);

            if (generate) worldGen.QueueChunk(newChunk);

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
            float multiple = BlockSettings.ChunkSize;

            pos.x = Mathf.FloorToInt(x / multiple) * BlockSettings.ChunkSize;
            pos.y = Mathf.FloorToInt(y / multiple) * BlockSettings.ChunkSize;
            pos.z = Mathf.FloorToInt(z / multiple) * BlockSettings.ChunkSize;

            chunks.TryGetValue(pos, out Chunk containerChunk);

            return containerChunk;
        }

        public bool CheckChunk(int3 position)
        {
            return CheckChunk(position.x, position.y, position.z);
        }

        public bool CheckChunk(int x, int y, int z)
        {
            int3 pos = new int3();
            float multiple = BlockSettings.ChunkSize;

            pos.x = Mathf.FloorToInt(x / multiple) * BlockSettings.ChunkSize;
            pos.y = Mathf.FloorToInt(y / multiple) * BlockSettings.ChunkSize;
            pos.z = Mathf.FloorToInt(z / multiple) * BlockSettings.ChunkSize;

            return chunks.TryGetValue(pos, out _);
        }

        public bool CheckChunk(out Chunk chunk, int x, int y, int z)
        {
            int3 pos = new int3();
            float multiple = BlockSettings.ChunkSize;

            pos.x = Mathf.FloorToInt(x / multiple) * BlockSettings.ChunkSize;
            pos.y = Mathf.FloorToInt(y / multiple) * BlockSettings.ChunkSize;
            pos.z = Mathf.FloorToInt(z / multiple) * BlockSettings.ChunkSize;

            return chunks.TryGetValue(pos, out chunk);
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

        public BlockMetadata GetBlock(int3 pos)
        {
            return GetBlock(pos.x, pos.y, pos.z);
        }

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

        public void SetBlock(int3 pos, BlockMetadata blockMetadata, bool FromNetwork = false, BlockUpdateMode UpdateMode = BlockUpdateMode.ForceUpdate)
        {
            SetBlock(pos.x, pos.y, pos.z, blockMetadata, FromNetwork, UpdateMode);
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
}
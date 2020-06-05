//using CielaSpike;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using VoxaNovus.Rendering;

namespace VoxaNovus
{
    public static class ChunkLOD
    {
        public static int3 PlayerPosition_int;
        public static Vector3 PlayerPosition, CameraNormalized;

        public static float CalculateImportance(int3 chunkPosition)
        {
            Vector3 posv = new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z);
            float distanceSqr = (posv - PlayerPosition).sqrMagnitude;
            return Vector3.Dot(posv - PlayerPosition, CameraNormalized) / (float)math.pow(distanceSqr, 0.7);
        }
        public static float Distance(int3 chunkPosition)
        {
            return math.abs(Vector3.Distance(new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z), PlayerPosition));
        }

        public static float Distance(Vector3 chunkPosition)
        {
            return math.abs(Vector3.Distance(chunkPosition, PlayerPosition));
        }

        public static float RealDistance(int3 chunkPosition)
        {
            return math.abs(Vector3.Distance(new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z), new Vector3(PlayerPosition_int.x, PlayerPosition_int.y, PlayerPosition_int.z)));
        }
    }

    public class ChunkLoading : MonoBehaviour
    {
        ConcurrentDictionary<int3, Chunk> updateDict = new ConcurrentDictionary<int3, Chunk>();
        World world;

        WaitForSeconds WaitOneSecond = new WaitForSeconds(1f);

        Thread tFindChunksToDelete, tRenderQueuedChunks;
        private bool runThreads = true;

        internal void Stop()
        {
            runThreads = false;
            if (tFindChunksToDelete.IsAlive)
                tFindChunksToDelete.Abort();
            if (tRenderQueuedChunks.IsAlive)
                tRenderQueuedChunks.Abort();
        }

        void Start()
        {
            world = GameObject.Find("World").GetComponent<World>();
            //StartCoroutine(UpdateChunkQueue());
            //this.StartCoroutineAsync(FindChunksToDeleteCoroutine());

            //this.StartCoroutineAsync(RenderQueuedChunks());

            tFindChunksToDelete = new Thread(() =>
            {
                try
                {
                    while (runThreads)
                    {
                        if (updateDict.Count > 0)
                            foreach (var chunk in updateDict)
                                if (ChunkLOD.Distance(chunk.Value.transformPosition) > BlockSettings.ChunkSize * BlockSettings.ChunkScale * (PlayerSettings.Chunk_DrawDistance + 1))
                                    chunksToDelete.Enqueue(chunk.Key);

                        foreach (var chunk in world.chunks)
                        {
                            if (ChunkLOD.Distance(chunk.Value.transformPosition) > BlockSettings.ChunkSize * BlockSettings.ChunkScale * (PlayerSettings.Chunk_DrawDistance + 1))
                                chunksToDelete.Enqueue(chunk.Key);
                        }
                        Thread.Sleep(500);
                    }
                } catch (Exception)
                {
                    Application.Quit(1);
                }
            });
            tFindChunksToDelete.Start();

            StartCoroutine(ChunkDeleter());
            StartCoroutine(FindChunksToLoadCoroutine());

            tRenderQueuedChunks = new Thread(() =>
            {
                Chunk tempCurrentChunk;
                bool tempChunkBool;

                bool Loop(KeyValuePair<int3, Chunk> item)
                {
                    Chunk tempNeighbourChunk;
                    for (int ix = -1; ix <= 1; ix++)
                        for (int iy = -1; iy <= 1; iy++)
                            for (int iz = -1; iz <= 1; iz++)
                                if (ix != 0 && iy != 0 && iz != 0)
                                {
                                    int3 tempChPos = new int3(
                                        item.Key.x + ix * BlockSettings.ChunkSize,
                                        item.Key.y + iy * BlockSettings.ChunkSize,
                                        item.Key.z + iz * BlockSettings.ChunkSize);

                                    tempNeighbourChunk = world.GetChunk(
                                        tempChPos.x,
                                        tempChPos.y,
                                        tempChPos.z);

                                    if (!world.CheckChunk(
                                            tempChPos.x,
                                            tempChPos.y,
                                            tempChPos.z)
                                        || tempNeighbourChunk == null
                                        || tempNeighbourChunk.BlocksN.Length != (int)math.pow(BlockSettings.ChunkSize, 3)
                                        || tempNeighbourChunk.isGenerating
                                        || !tempNeighbourChunk.generated
                                        || tempNeighbourChunk.isWriting
                                        || tempNeighbourChunk.isEmpty
                                        || tempNeighbourChunk.ioRenderValue > 0
                                        || chunksToDelete.Contains(tempChPos)
                                        )
                                        return false;
                                }
                    return true;
                }

                try
                {
                    while (runThreads)
                    {
                        if (updateDict.Count > 0)
                            foreach (var item in updateDict)
                            {
                                tempCurrentChunk = world.GetChunk(item.Key.x, item.Key.y, item.Key.z);
                                tempChunkBool = world.CheckChunk(item.Key.x, item.Key.y, item.Key.z);
                                if ((tempChunkBool && (tempCurrentChunk.isQueuedForDeletion || tempCurrentChunk.isEmpty || tempCurrentChunk.rendered)) || !tempChunkBool)
                                {
                                    updateDict.Remove(item.Key);
                                    continue;
                                }

                                if (Loop(item))
                                {
                                    tempCurrentChunk = world.GetChunk(item.Key.x, item.Key.y, item.Key.z);
                                    if (tempCurrentChunk != null)
                                        tempCurrentChunk.UpdateChunk(ChunkUpdateMode.ForceSingle);
                                    updateDict.Remove(item.Key);
                                }
                            }
                        Thread.Sleep(10);
                    }
                } 
                catch (Exception)
                {
                    Application.Quit(2);
                }
            });
            tRenderQueuedChunks.Start();
        }

        void OnDestroy()
        {
            Stop();
        }

        /*
        /// <summary>
        /// In case of needing to redraw the render queue array
        /// </summary>
        public void RecalculateDrawingArray()
        {
            List<int2> temp = new List<int2>();
            for (int x = -30; x < 30; x++)
                for (int y = -30; y < 30; y++)
                    temp.Add(new int2(x, y));

            List<int2> recalculatedDrawingArray = new List<int2>();
            for (int i = 0; i < temp.Count; i++)
            {
                float distance = float.MaxValue;
                int V = -1;
                for (int v = 0; v < temp.Count; v++)
                {
                    float tempdist = Vector2.Distance(new Vector2(0, 0), new Vector2(temp[v].x, temp[v].y));
                    if (tempdist < distance && !recalculatedDrawingArray.Contains(temp[v]))
                    {
                        distance = tempdist;
                        V = v;
                    }
                }
                if (V > -1) recalculatedDrawingArray.Add(temp[V]);
            }
            _drawingArray = recalculatedDrawingArray.ToArray();

            temp.Clear();
            recalculatedDrawingArray.Clear();
        }
        */

        void Update()
        {
            Vector3 position = transform.position;

            ChunkLOD.PlayerPosition_int = new int3(
                Mathf.FloorToInt(position.x / (BlockSettings.ChunkSize * BlockSettings.ChunkScale)) * BlockSettings.ChunkSize,
                Mathf.FloorToInt(position.y / (BlockSettings.ChunkSize * BlockSettings.ChunkScale)) * BlockSettings.ChunkSize,
                Mathf.FloorToInt(position.z / (BlockSettings.ChunkSize * BlockSettings.ChunkScale)) * BlockSettings.ChunkSize);

            ChunkLOD.PlayerPosition = position;
            ChunkLOD.CameraNormalized = transform.rotation.normalized * Vector3.forward;
        }

        //IEnumerator UpdateChunkQueue()
        //{
        //    while (true)
        //    {
        //        foreach (Chunk chunk in world.chunks.Values)
        //        {
        //            if (chunk.BlockchangeQueue.Count > 0 && chunk.isReading == 0 && !chunk.isGenerating && chunk.rendered && !chunk.isRenderQueued)
        //            {
        //                Tuple<int, BlockMetadata> tempBlockData = chunk.BlockchangeQueue.Dequeue();
        //                //chunk.SetBlock(tempBlockData.Item1, tempBlockData.Item2, tempBlockData.Item3);
        //            }
        //        }
        //        yield return new WaitForEndOfFrame();
        //    }
        //}

        IEnumerator FindChunksToLoadCoroutine()
        {
            yield return WaitOneSecond;
            int3 newChunkPos;
            Chunk tempChunk;

            while (true)
            {
                int Waiter = 0;
                for (int coord = 0; coord < Tables.DrawingTable.Length; coord++)
                {
                    newChunkPos = new int3(
                        Tables.DrawingTable[coord].x * BlockSettings.ChunkSize + ChunkLOD.PlayerPosition_int.x,
                        ChunkLOD.PlayerPosition_int.y,
                        Tables.DrawingTable[coord].y * BlockSettings.ChunkSize + ChunkLOD.PlayerPosition_int.z);

                    if (ChunkLOD.RealDistance(newChunkPos) > PlayerSettings.Chunk_DrawDistance * BlockSettings.ChunkSize) continue;

                    bool shouldSpawn = false;
                    for (int i = -4; i < 4; i++)
                        if (!updateDict.ContainsKey(newChunkPos) && !world.CheckChunk(newChunkPos.x, newChunkPos.y + i * BlockSettings.ChunkSize, newChunkPos.z))
                            shouldSpawn = true;
                    if (!shouldSpawn) continue;

                    if (PlayerSettings.Chunk_LoadingSpeed <= 100)
                    {
                        if (Waiter > PlayerSettings.Chunk_LoadingSpeed)
                        {
                            Waiter = 0;
                            yield return null;
                        }
                        else
                            Waiter++;
                    }

                    for (int y = (int)(-4 / BlockSettings.ChunkScale); y < (int)(4 / BlockSettings.ChunkScale); y++)
                    {
                        int3 pos = new int3(newChunkPos.x, y * BlockSettings.ChunkSize + ChunkLOD.PlayerPosition_int.y, newChunkPos.z);
                        if (!updateDict.ContainsKey(pos))
                        {
                            tempChunk = world.CreateChunk(pos.x, pos.y, pos.z, true);
                            if (tempChunk != null)
                                updateDict.TryAdd(pos, tempChunk);
                        }
                    }
                    //break;
                }
                yield return null;
            }
        }

        IEnumerator RenderQueuedChunks()
        {
            //yield return Ninja.JumpBack;
            Chunk tempCurrentChunk;
            bool tempChunkBool;

            bool Loop(KeyValuePair<int3, Chunk> item)
            {
                Chunk tempNeighbourChunk;
                for (int ix = -1; ix <= 1; ix++)
                    for (int iy = -1; iy <= 1; iy++)
                        for (int iz = -1; iz <= 1; iz++)
                            if (ix != 0 && iy != 0 && iz != 0)
                            {
                                int3 tempChPos = new int3(
                                    item.Key.x + ix * BlockSettings.ChunkSize,
                                    item.Key.y + iy * BlockSettings.ChunkSize,
                                    item.Key.z + iz * BlockSettings.ChunkSize);

                                tempNeighbourChunk = world.GetChunk(
                                    tempChPos.x,
                                    tempChPos.y,
                                    tempChPos.z);

                                if (!world.CheckChunk(
                                        tempChPos.x,
                                        tempChPos.y,
                                        tempChPos.z)
                                    || tempNeighbourChunk == null
                                    || tempNeighbourChunk.BlocksN.Length != (int)math.pow(BlockSettings.ChunkSize, 3)
                                    || tempNeighbourChunk.isGenerating
                                    || !tempNeighbourChunk.generated
                                    || tempNeighbourChunk.isWriting
                                    || tempNeighbourChunk.isEmpty
                                    || tempNeighbourChunk.ioRenderValue > 0
                                    || chunksToDelete.Contains(tempChPos)
                                    )
                                    return false;
                            }
                return true;
            }

            while (true)
            {
                if (updateDict.Count > 0)
                    foreach (var item in updateDict)
                    {
                        tempCurrentChunk = world.GetChunk(item.Key.x, item.Key.y, item.Key.z);
                        tempChunkBool = world.CheckChunk(item.Key.x, item.Key.y, item.Key.z);
                        if ((tempChunkBool && (tempCurrentChunk.isQueuedForDeletion || tempCurrentChunk.isEmpty || tempCurrentChunk.rendered)) || !tempChunkBool)
                        {
                            updateDict.Remove(item.Key);
                            continue;
                        }

                        if (Loop(item))
                        {
                            tempCurrentChunk = world.GetChunk(item.Key.x, item.Key.y, item.Key.z);
                            if (tempCurrentChunk != null)
                                tempCurrentChunk.UpdateChunk(ChunkUpdateMode.ForceSingle);
                            updateDict.Remove(item.Key);
                        }
                    }
                yield return null;
            }
        }

        Queue<int3> chunksToDelete = new Queue<int3>();
        IEnumerator FindChunksToDeleteCoroutine()
        {
            //yield return Ninja.JumpBack;
            while (true)
            {
                foreach (var chunk in world.chunks)
                {
                    if (ChunkLOD.Distance(chunk.Value.transformPosition) > BlockSettings.ChunkSize * BlockSettings.ChunkScale * (PlayerSettings.Chunk_DrawDistance + 1))
                        chunksToDelete.Enqueue(chunk.Key);
                }

                for (int i = 0; i < 10; i++)
                    yield return Macros.Coroutine.WaitFor_EndOfFrame;
            }
        }

        IEnumerator ChunkDeleter()
        {
            while (true)
            {
                if (chunksToDelete.Count > 0)
                    for (int i = 0; i < chunksToDelete.Count; i++)
                    {
                        int3 chunk = chunksToDelete.Dequeue();
                        if (!updateDict.ContainsKey(chunk))
                            updateDict.Remove(chunk);

                        if (world.CheckChunk(chunk.x, chunk.y, chunk.z))
                            world.DestroyChunk(chunk.x, chunk.y, chunk.z);
                    }
                yield return Macros.Coroutine.WaitFor_EndOfFrame;
            }

        }
    }
}
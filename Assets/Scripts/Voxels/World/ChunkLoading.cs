using CielaSpike;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;

public static class ChunkLOD {
    public static int3 playerPositionint;
    public static Vector3 playerPosition;
}

public class ChunkLoading : MonoBehaviour
{
    private int previousDrawDistance = 30;

    int2[] drawingArray;
    Dictionary<int3, Chunk> updateDict = new Dictionary<int3, Chunk>();
    private AutoResetEvent autoResetEvent = new AutoResetEvent(true);
    World world;

    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        drawingArray = SpiralCoords.GenerateOutTo(PlayerSettings.Chunk_DrawDistance).ToArray();
        StartCoroutine(UpdatePlayerPos());
        StartCoroutine(FindChunksToDeleteCoroutine());
        StartCoroutine(FindChunksToLoadCoroutine());
        //this.StartCoroutineAsync(FindAndSpawnChunks());
    }

    void LateUpdate()
    {
        if (previousDrawDistance != PlayerSettings.Chunk_DrawDistance)
        {
            previousDrawDistance = PlayerSettings.Chunk_DrawDistance;
            drawingArray = SpiralCoords.GenerateOutTo(PlayerSettings.Chunk_DrawDistance).ToArray();
            Debug.Log("Recalculated Draw Distance.");
        }
    }

    IEnumerator UpdatePlayerPos()
    {
        while (true)
        {
            ChunkLOD.playerPosition = new Vector3(
                Mathf.FloorToInt(transform.position.x / BlockData.ChunkSize) * BlockData.ChunkSize,
                Mathf.FloorToInt(transform.position.y / BlockData.ChunkSize) * BlockData.ChunkSize,
                Mathf.FloorToInt(transform.position.z / BlockData.ChunkSize) * BlockData.ChunkSize);
            ChunkLOD.playerPositionint = new int3(
                Mathf.FloorToInt(transform.position.x / BlockData.ChunkSize) * BlockData.ChunkSize,
                Mathf.FloorToInt(transform.position.y / BlockData.ChunkSize) * BlockData.ChunkSize,
                Mathf.FloorToInt(transform.position.z / BlockData.ChunkSize) * BlockData.ChunkSize);
            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator FindChunksToLoadCoroutine()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            int Waiter = 0;
            for (int coord = 0; coord < drawingArray.Length; coord++)
            {
                int newX = drawingArray[coord].x;
                int newZ = drawingArray[coord].y;

                int3 newChunkPos = new int3(newX * BlockData.ChunkSize + ChunkLOD.playerPositionint.x, ChunkLOD.playerPositionint.y, newZ * BlockData.ChunkSize + ChunkLOD.playerPositionint.z);
                autoResetEvent.WaitOne();
                Chunk newChunk = world.GetChunk(newChunkPos.x, newChunkPos.y, newChunkPos.z);
                autoResetEvent.Set();

                if (PlayerSettings.Chunk_LoadingSpeed <= 100)
                {
                    if (Waiter > PlayerSettings.Chunk_LoadingSpeed)
                    { Waiter = 0; yield return null; }
                    else
                        Waiter++;
                }

                if (newChunk != null && (newChunk.rendered || updateDict.ContainsKey(newChunkPos)))
                    continue;

                for (int y = -4; y < 4; y++)
                {
                    int3 pos = new int3(newChunkPos.x, y * BlockData.ChunkSize + ChunkLOD.playerPositionint.y, newChunkPos.z);
                    if (!updateDict.ContainsKey(pos))
                    {
                        Chunk tempChunk = world.CreateChunk(pos.x, pos.y, pos.z, true);
                        if (tempChunk != null)
                        {
                            autoResetEvent.WaitOne();
                            updateDict.Add(pos, tempChunk);
                            autoResetEvent.Set();
                        }
                    }
                }
                break;
            }
            

            if (updateDict.Count > 0)
            {
                List<int3> keysToRemove = new List<int3>();
                foreach (var item in updateDict)
                {
                    bool render = true;
                    for (int ix = -1; ix <= 1; ix++)
                        for (int iy = -1; iy <= 1; iy++)
                            for (int iz = -1; iz <= 1; iz++)
                                if (ix != 0 && iy != 0 && iz != 0)
                                {
                                    Chunk tempchunk = world.GetChunk(item.Value.pos.x + ix * 16, item.Value.pos.y + iy * 16, item.Value.pos.z + iz * 16);
                                    if (tempchunk == null || !tempchunk.generated)
                                        render = false;
                                }
                    if (render)
                    {
                        item.Value.UpdateChunk(ChunkUpdateMode.ForceSingle);
                        item.Value.rendered = true;
                        keysToRemove.Add(item.Key);
                    }
                }
                if (keysToRemove.Count > 0)
                {
                    foreach (int3 pos in keysToRemove)
                        updateDict.Remove(pos);
                    keysToRemove.Clear();
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator FindChunksToDeleteCoroutine()
    {
        while (true)
        {
            yield return Ninja.JumpBack;
            List<int3> chunksToDelete = new List<int3>();
            foreach (var chunk in world.chunks)
            {
                float distance = Vector3.Distance(
                    new Vector3(chunk.Value.pos.x, chunk.Value.pos.y, chunk.Value.pos.z),
                    ChunkLOD.playerPosition);
                if (distance > BlockData.ChunkSize * (PlayerSettings.Chunk_DrawDistance + 1))
                    chunksToDelete.Add(chunk.Key);
                else
                {
                    if (distance > BlockData.ChunkSize * 10)
                        chunk.Value.LOD = 2;
                    else if (distance > BlockData.ChunkSize * 12)
                        chunk.Value.LOD = 4;
                    else if (distance > BlockData.ChunkSize * 14)
                        chunk.Value.LOD = 8;
                    else
                        chunk.Value.LOD = 1;
                }
            }

            yield return Ninja.JumpToUnity;
            foreach (var chunk in chunksToDelete)
            {
                int3 pos = new int3(chunk.x, chunk.y, chunk.z);
                if (!updateDict.ContainsKey(pos))
                    updateDict.Remove(pos);
                world.DestroyChunk(chunk.x, chunk.y, chunk.z);
            }
            yield return Ninja.JumpBack;
            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
        }
    }
}

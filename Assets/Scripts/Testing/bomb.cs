using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using VoxaNovus;
using CielaSpike;

public static class CalculatedExplosion
{
    public static List<int3> positions = new List<int3>();

    public static void Recalculate()
    {
        positions.Clear();
        float R = math.sqrt(math.pow(3, 2) + math.pow(3, 2) + math.pow(3, 2));
        for (float ix = -R; ix <= R; ix++)
            for (float iy = -R; iy <= R; iy++)
                for (float iz = -R; iz <= R; iz++)
                    if ((math.pow(ix, 2) + math.pow(iy, 2) + math.pow(iz, 2)) < (math.pow(R, 2)))
                        positions.Add(new int3((int)ix, (int)iy, (int)iz));
        Debug.Log($"bomb.cs -> Explosion: Calculated {positions.Count} blocks.");
    }
}

public class bomb : MonoBehaviour
{
    // Start is called before the first frame update
    public World world;
    bool ready = true;

    public GameObject bombExplosion;


    void Start()
    {
        Physics.IgnoreLayerCollision(12, 10, true);
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.name.Contains("Chunk") && ready)
        {
            ready = false;
            int3 coords = new int3(
                Mathf.FloorToInt(transform.localPosition.x / BlockSettings.ChunkScale), 
                Mathf.FloorToInt(transform.localPosition.y / BlockSettings.ChunkScale), 
                Mathf.FloorToInt(transform.localPosition.z / BlockSettings.ChunkScale));
            
            Destroy(GetComponent<Rigidbody>());
            Destroy(GetComponent<SphereCollider>());
            Destroy(GetComponent<MeshFilter>());

            GameObject newBomb = Instantiate(bombExplosion);
            newBomb.transform.localPosition = transform.localPosition;
            newBomb.transform.position = transform.position;

            StartCoroutine(calculateDamage(coords));
        }
    }

    IEnumerator calculateDamage(int3 coords)
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        yield return Ninja.JumpBack;

        List<Chunk> chunksToUpdate = new List<Chunk>();
        int iterator = 0;

        foreach (var pos in CalculatedExplosion.positions)
        {
            if (iterator++ % 512 == 0) yield return null;
            if (!world.CheckChunk(out Chunk chunk, coords.x + pos.x, coords.y + pos.y, coords.z + pos.z)) continue;

            int3 localPosition = new int3(coords.x + pos.x - chunk.pos.x, coords.y + pos.y - chunk.pos.y, coords.z + pos.z - chunk.pos.z);
            chunk.SetBlock(localPosition.x, localPosition.y, localPosition.z, BlockMetadata.EmptyPhysicsTrigger(), false, BlockUpdateMode.None);
            //chunk.BlockchangeQueue.Enqueue(new QueuedBlock(localPosition, new BlockMetadata()));
            //chunk.SetBlock(
            //    coords.x + pos.x - chunk.pos.x,
            //    coords.y + pos.y - chunk.pos.y,
            //    coords.z + pos.z - chunk.pos.z, 
            //    new BlockMetadata(), 
            //    false, 
            //    BlockUpdateMode.None
            //    );
            //
            if (!chunksToUpdate.Contains(chunk))             
                chunksToUpdate.Add(chunk);
        }

        yield return Macros.Coroutine.WaitFor_EndOfFrame;
        yield return Ninja.JumpToUnity;

        foreach (var ch in chunksToUpdate)
            //BlockUpdateQueue.Push(ch);
            //ch.UpdateChunk(ChunkUpdateMode.ForceNeighbours);
            ch.UpdateChunk(ChunkUpdateMode.ForceNeighbours);

        sw.Stop();
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.localPosition.y < -20f)
            Destroy(gameObject);
    }
}

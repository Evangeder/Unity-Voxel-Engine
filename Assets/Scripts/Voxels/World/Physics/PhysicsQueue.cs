using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Priority_Queue;

//Example of how to create a priority queue node class

public static class PhysicsQueue
{
    public class PhysicsQueueNode// : FastPriorityQueueNode
    {
        public PhysicsQueueNode(BlockMetadata metaData, int X, int Y, int Z)
        {
            metadata = metaData;
            x = X;
            y = Y;
            z = Z;
        }
        public BlockMetadata metadata { get; }
        public int x { get; }
        public int y { get; }
        public int z { get; }
    }

    //const int MAX_BLOCKS_IN_QUEUE = 10000;
    public static Dictionary<int3, ushort> StoredBlocks = new Dictionary<int3, ushort>();
    public static SimplePriorityQueue<PhysicsQueueNode> priorityQueue = new SimplePriorityQueue<PhysicsQueueNode>();
    static List<Chunk> ChunksToUpdate = new List<Chunk>();
    static System.Diagnostics.Stopwatch swtime = new System.Diagnostics.Stopwatch();

    public static void Push(BlockMetadata block, int3 position)
    {
        if (block.Switches.Get(BlockSwitches.Static)) return;
        //if (block.Switches.Get(BlockSwitches.Physics_Finished)) return;
        block.Switches &= BlockSwitches.Marched;

        if (block.ID == 0 || !BlockData.PhysicsBound[block.ID]) return;

        PhysicsQueueNode queuedData = new PhysicsQueueNode(block, position.x, position.y, position.z);
        if (!StoredBlocks.ContainsKey(position))
        {
            StoredBlocks.Add(position, block.ID);
            priorityQueue.Enqueue(queuedData, Time.realtimeSinceStartup + BlockData.byID[block.ID].PhysicsTime);
        }
    }

    public static void Push(BlockMetadata block, int x, int y, int z)
    {
        PhysicsQueueNode queuedData = new PhysicsQueueNode(block, x, y, z);
        if (!priorityQueue.Contains(queuedData))
            priorityQueue.Enqueue(queuedData, Time.realtimeSinceStartup + BlockData.byID[block.ID].PhysicsTime);
    }

    public static IEnumerator PhysicsQueueIterator()
    {
        Debug.Log("Physics Queue started.");
        int iterator = 0;
        while (true)
        {
            iterator++;
            swtime.Restart();
            if (priorityQueue.Count > 0 && priorityQueue.GetPriority(priorityQueue.First) < Time.realtimeSinceStartup)
            {
                PhysicsQueueNode node = priorityQueue.Dequeue();
                StoredBlocks.Remove(new int3(node.x, node.y, node.z));
                if (BlockData.world.CheckChunk(node.x, node.y, node.z))
                {
                    Chunk ch = BlockData.world.GetChunk(node.x, node.y, node.z);
                    if (BlockData.world.GetBlock(node.x, node.y, node.z).ID == node.metadata.ID && BlockData.PhysicsBound[node.metadata.ID])
                        BlockData.PhysicsFunctions[node.metadata.ID].Tick(node.metadata, node.x, node.y, node.z);

                    if (!ChunksToUpdate.Contains(ch))
                        ChunksToUpdate.Add(ch);
                }
            }
                
            swtime.Stop();
            if (swtime.Elapsed.Ticks > 1000000)
                Debug.Log($"Physics tick took: {swtime.Elapsed.ToString()}");
            if (iterator > 500)
            {
                iterator = 0;
                foreach (Chunk ch in ChunksToUpdate)
                    ch.UpdateChunk();
                ChunksToUpdate.Clear();
                yield return null;
            }
        }
    }

}

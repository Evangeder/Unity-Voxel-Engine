using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Threading;

namespace VoxaNovus
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        #region Declarations

        #region General chunk structure
        [HideInInspector] public World world;
        [HideInInspector] public int3 pos, transformPosition;
        [HideInInspector] public int chunkManagerIndex = -1;

        [HideInInspector] public NativeArray<BlockMetadata> BlocksN;

        [HideInInspector] public Queue<QueuedBlock> BlockchangeQueue;

        // Physics/Blockupdate queue
        //[HideInInspector] public Queue<Tuple<int, BlockMetadata>> BlockchangeQueue = new Queue<Tuple<int, BlockMetadata>>();
        #endregion

        #region Chunk generation
        [HideInInspector] public JobHandle WorldGen_JobHandle = new JobHandle();
        [HideInInspector] public JobHandle BlockClear_JobHandle = new JobHandle();
        #endregion

        #region Safety checks

        public bool
            generated = false,
            isGenerating = false,
            isQueuedForDeletion = false,
            isRenderQueued = false,
            isWriting = false,
            isEmpty = true;
        public byte ioRenderValue = 0;

        #endregion


        #region Chunk rendering
        // Level of Detail for rendering far chunks (the higher number, the less detail), default 1.
        [HideInInspector] public byte LOD = 1;
        private byte PreviousLOD = 1;
        public bool rendered;
        internal bool IsRendering = false;

        #endregion

        #endregion

        IEnumerator DisposeCoroutine()
        {
            while (ioRenderValue > 0) yield return null;

            isQueuedForDeletion = true;
            WorldGen_JobHandle.Complete();
            isGenerating = false;
            IsRendering = false;
            isWriting = false;
            isRenderQueued = false;
            generated = false;
            rendered = false;

            GetComponent<MeshFilter>().mesh.Clear();
            GetComponent<MeshCollider>().sharedMesh = null;

            pos = int3.zero;
            transformPosition = int3.zero;

            for (int i = 0; i < 3; i++)
                yield return Macros.Coroutine.WaitFor_EndOfFrame;

            if (BlocksN.IsCreated) BlocksN.Dispose();
            BlocksN = new NativeArray<BlockMetadata>((int)math.pow(BlockSettings.ChunkSize, 3), Allocator.Persistent);

            ioRenderValue = 0;

            this.Dispose();
        }

        public void QueueDispose()
        {
            StartCoroutine(DisposeCoroutine());
        }

        void Awake()
        {
            BlocksN = new NativeArray<BlockMetadata>((int)math.pow(BlockSettings.ChunkSize, 3), Allocator.Persistent);
            BlockchangeQueue = new Queue<QueuedBlock>();
            LOD = 1;
        }

        #region "Block Functions"

        public BlockMetadata GetBlock(int ArrayPosition)
        {
            return BlocksN[ArrayPosition];
        }

        public BlockMetadata GetBlock(int x, int y, int z)
        {
            if (InRange(x) && InRange(y) && InRange(z))
                return BlocksN[x + y * BlockSettings.ChunkSize + z * (int)math.pow(BlockSettings.ChunkSize, 2)];
            return world.GetBlock(pos.x + x, pos.y + y, pos.z + z);
        }

        public static bool InRange(int index)
        {
            if (index < 0 || index >= BlockSettings.ChunkSize)
                return false;

            return true;
        }

        public void SetBlock(Tuple<int, BlockMetadata> blockData)
        {
            SetBlock(blockData.Item1, blockData.Item2, BlockUpdateMode.None);
        }

        public void SetBlock(int ArrayPosition, BlockMetadata metadata, BlockUpdateMode UpdateMode = BlockUpdateMode.ForceUpdate)
        {
            bool placedByPhysics = metadata.Switches.Get(BlockSwitches.PhysicsTrigger);

            if (ioRenderValue == 0 && !IsRendering && generated
                && (UpdateMode == BlockUpdateMode.ForceUpdate || UpdateMode == BlockUpdateMode.Silent))
            {
                int x = ArrayPosition % 16, y = (ArrayPosition / 16) % 16, z = (ArrayPosition / 256) % 16;

                BlockMetadata ReplacementData = metadata;
                ReplacementData.Switches &= ~BlockSwitches.PhysicsTrigger;

                if (GetBlock(x, y, z) == ReplacementData) return;

                BlocksN[ArrayPosition] = ReplacementData;

                if (UpdateMode == BlockUpdateMode.ForceUpdate)
                {
                    if (!isRenderQueued)
                        UpdateChunk();
                    foreach (Chunk chunk in GetNeighboursToUpdate(x, y, z))
                        chunk.UpdateChunk();
                }

                for (int ix = -1; ix < 2; ix++)
                    for (int iy = -1; iy < 2; iy++)
                        for (int iz = -1; iz < 2; iz++)
                            if (((ix == 0 && iy == 0) || (iy == 0 && iz == 0) || (iz == 0 && ix == 0)) && !(ix == 0 && iy == 0 && iz == 0))
                            {
                                BlockMetadata b = GetBlock(x + ix, y + iy, z + iz);
                                if (BlockSettings.PhysicsBound[b.ID]
                                        && !b.Switches.Get(BlockSwitches.DontTriggerPhysics)
                                        && b.ID != 0)
                                    PhysicsQueue.Push(
                                        b,
                                        new int3(x + pos.x + ix,
                                            y + pos.y + iy,
                                            z + pos.z + iz)
                                        );
                            }

                if (BlockSettings.PhysicsBound[metadata.ID]
                        && !metadata.Switches.Get(BlockSwitches.DontTriggerPhysics)
                        && !placedByPhysics
                        && metadata.ID != 0)
                    PhysicsQueue.Push(
                        ReplacementData,
                        new int3(x + pos.x, y + pos.y, z + pos.z)
                    );
            }
            else if (ioRenderValue == 0 && !IsRendering && generated && UpdateMode == BlockUpdateMode.None)
            {
                int x = ArrayPosition % 16, y = (ArrayPosition / 16) % 16, z = (ArrayPosition / 256) % 16;
                if (placedByPhysics)
                    for (int ix = -1; ix < 2; ix++)
                        for (int iy = -1; iy < 2; iy++)
                            for (int iz = -1; iz < 2; iz++)
                                if (((ix == 0 && iy == 0) || (iy == 0 && iz == 0) || (iz == 0 && ix == 0)) && !(ix == 0 && iy == 0 && iz == 0))
                                {
                                    BlockMetadata b = GetBlock(x + ix, y + iy, z + iz);
                                    if (BlockSettings.PhysicsBound[b.ID]
                                            && !b.Switches.Get(BlockSwitches.DontTriggerPhysics)
                                            && b.ID != 0)
                                        PhysicsQueue.Push(
                                            b,
                                            new int3(x + pos.x + ix,
                                                y + pos.y + iy,
                                                z + pos.z + iz)
                                            );
                                }
                metadata.Switches &= ~BlockSwitches.PhysicsTrigger;
                BlocksN[ArrayPosition] = metadata;
            }
            else
            {
                //BlockchangeQueue.Enqueue(new Tuple<int, BlockMetadata>(ArrayPosition, metadata));
                BlockchangeQueue.Enqueue(new QueuedBlock(ArrayPosition, metadata));
                BlockUpdateQueue.Push(this);
            }
        }

        public void SetBlock(int x, int y, int z, BlockMetadata metadata, bool FromNetwork = false, BlockUpdateMode UpdateMode = BlockUpdateMode.ForceUpdate)
        {
            bool placedByPhysics = metadata.Switches.Get(BlockSwitches.PhysicsTrigger);

            if (InRange(x) && InRange(y) && InRange(z))
                if (ioRenderValue == 0 && !IsRendering && generated
                    && (UpdateMode == BlockUpdateMode.ForceUpdate || UpdateMode == BlockUpdateMode.Silent))
                {
                    BlockMetadata ReplacementData = metadata;
                    ReplacementData.Switches &= ~BlockSwitches.PhysicsTrigger;

                    BlocksN[x + y * BlockSettings.ChunkSize + z * (int)math.pow(BlockSettings.ChunkSize, 2)] = ReplacementData;
                    if (UpdateMode == BlockUpdateMode.ForceUpdate)
                    {
                        if (!isRenderQueued)
                            UpdateChunk();

                        foreach (Chunk chunk in GetNeighboursToUpdate(x, y, z))
                            chunk.UpdateChunk();
                    }

                    for (int ix = -1; ix < 2; ix++)
                        for (int iy = -1; iy < 2; iy++)
                            for (int iz = -1; iz < 2; iz++)
                                if (((ix == 0 && iy == 0) || (iy == 0 && iz == 0) || (iz == 0 && ix == 0)) && !(ix == 0 && iy == 0 && iz == 0))
                                {
                                    BlockMetadata b = GetBlock(x + ix, y + iy, z + iz);
                                    if (BlockSettings.PhysicsBound[b.ID]
                                            && !b.Switches.Get(BlockSwitches.DontTriggerPhysics)
                                            && b.ID != 0)
                                        PhysicsQueue.Push(
                                            b,
                                            new int3(x + pos.x + ix,
                                                y + pos.y + iy,
                                                z + pos.z + iz)
                                            );
                                }

                    if (BlockSettings.PhysicsBound[metadata.ID]
                        && !metadata.Switches.Get(BlockSwitches.DontTriggerPhysics)
                        && metadata.ID != 0)
                        PhysicsQueue.Push(
                            metadata,
                            new int3(x + pos.x, y + pos.y, z + pos.z)
                        );
                }
                else if (ioRenderValue == 0 && !IsRendering && generated && UpdateMode == BlockUpdateMode.None)
                {
                    if (placedByPhysics)
                        for (int ix = -1; ix < 2; ix++)
                            for (int iy = -1; iy < 2; iy++)
                                for (int iz = -1; iz < 2; iz++)
                                    if (((ix == 0 && iy == 0) || (iy == 0 && iz == 0) || (iz == 0 && ix == 0)) && !(ix == 0 && iy == 0 && iz == 0))
                                    {
                                        BlockMetadata b = GetBlock(x + ix, y + iy, z + iz);
                                        if (BlockSettings.PhysicsBound[b.ID]
                                                && !b.Switches.Get(BlockSwitches.DontTriggerPhysics)
                                                && b.ID != 0)
                                            PhysicsQueue.Push(
                                                b,
                                                new int3(x + pos.x + ix,
                                                    y + pos.y + iy,
                                                    z + pos.z + iz)
                                                );
                                    }
                    BlockMetadata ReplacementData = metadata;
                    ReplacementData.Switches &= ~BlockSwitches.PhysicsTrigger;
                    BlocksN[x + y * BlockSettings.ChunkSize + z * (int)math.pow(BlockSettings.ChunkSize, 2)] = metadata;
                }
                else
                {
                    metadata.Switches &= ~BlockSwitches.PhysicsTrigger;
                    //BlockchangeQueue.Enqueue(new Tuple<int, BlockMetadata>(x + y * BlockSettings.ChunkSize + z * (int)math.pow(BlockSettings.ChunkSize, 2), metadata));
                    BlockchangeQueue.Enqueue(new QueuedBlock(new int3(x, y, z), metadata));
                    BlockUpdateQueue.Push(this);
                }
            else
                world.SetBlock(pos.x + x, pos.y + y, pos.z + z, metadata, FromNetwork, UpdateMode);
        }

        Chunk[] GetNeighboursToUpdate(int x, int y, int z)
        {
            List<Chunk> chunks = new List<Chunk>();
            for (int x1 = -1; x1 < 2; x1++)
                for (int y1 = -1; y1 < 2; y1++)
                    for (int z1 = -1; z1 < 2; z1++)
                        if ((!InRange(x + x1) || !InRange(y + y1) || !InRange(z + z1)) && !(x1 == 0 && y1 == 0 && z1 == 0))
                        {
                            Chunk tempChunk = world.GetChunk(
                                !InRange(x + x1) ? pos.x + (x1 * BlockSettings.ChunkSize) : pos.x,
                                !InRange(y + y1) ? pos.y + (y1 * BlockSettings.ChunkSize) : pos.y,
                                !InRange(z + z1) ? pos.z + (z1 * BlockSettings.ChunkSize) : pos.z);
                            if (!chunks.Contains(tempChunk)) chunks.Add(tempChunk);
                        }
            return chunks.ToArray();
        }

        #endregion

        #region "Chunk Update Function"

        IEnumerator ForceUpdateChunk()
        {
            isRenderQueued = true;
            while (IsRendering || BlocksN.Length != (int)math.pow(BlockSettings.ChunkSize, 3) || isGenerating || !generated || ioRenderValue != 0 || isWriting)
            {
                if (isQueuedForDeletion || isEmpty) {
                    isRenderQueued = false;
                    yield break;
                }
                yield return null;
            }
            if (!isQueuedForDeletion && !isEmpty)
                StartCoroutine(ChunkRenderer.RenderChunk(this));

            yield return null;
        }

        public void UpdateChunk()
        {
            UpdateChunk(ChunkUpdateMode.ForceSingle);
        }

        public void UpdateChunk(ChunkUpdateMode UM)
        {
            switch (UM)
            {
                case ChunkUpdateMode.ForceNeighbours:
                    for (int ix = -1; ix <= 1; ix++)
                        for (int iy = -1; iy <= 1; iy++)
                            for (int iz = -1; iz <= 1; iz++)
                            {
                                Chunk ch = world.GetChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize);
                                if (world.CheckChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize) && !ch.isRenderQueued && !world.ChunkUpdateQueue.Contains(ch))
                                    ch.UpdateChunk(ChunkUpdateMode.ForceSingle);
                            }
                    break;
                case ChunkUpdateMode.ForceSingle:
                    if (!isRenderQueued)
                    {
                        if (Thread.CurrentThread == World.mainThread)
                            StartCoroutine(ForceUpdateChunk());
                        else
                            UnityMainThreadDispatcher.Instance().Enqueue(ForceUpdateChunk());
                    }
                    break;
                case ChunkUpdateMode.QueueNeighbours:
                    for (int ix = -1; ix <= 1; ix++)
                        for (int iy = -1; iy <= 1; iy++)
                            for (int iz = -1; iz <= 1; iz++)
                            {
                                Chunk ch = world.GetChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize);
                                if (world.CheckChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize))
                                    world.AddToChunkUpdateQueue(ch);
                            }
                    break;
                case ChunkUpdateMode.QueueSingle:
                    if (!isRenderQueued)
                        world.AddToChunkUpdateQueue(this);
                    break;

                case ChunkUpdateMode.QueueNeighboursForceSingle:
                    for (int ix = -1; ix <= 1; ix++)
                        for (int iy = -1; iy <= 1; iy++)
                            for (int iz = -1; iz <= 1; iz++)
                            {
                                Chunk ch = world.GetChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize);
                                if (world.CheckChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize))
                                    if (ch == this && !isRenderQueued)
                                    {
                                        if (!ch.isRenderQueued && !world.ChunkUpdateQueue.Contains(ch))
                                        {
                                            isRenderQueued = true;
                                            if (Thread.CurrentThread == World.mainThread)
                                                StartCoroutine(ForceUpdateChunk());
                                            else
                                                UnityMainThreadDispatcher.Instance().Enqueue(ForceUpdateChunk());
                                        }
                                    }
                                    else
                                        world.AddToChunkUpdateQueue(ch);
                            }
                    break;
                case ChunkUpdateMode.QueueMarchingCubesFix:
                    for (int ix = -1; ix <= 0; ix++)
                        for (int iy = -1; iy <= 0; iy++)
                            for (int iz = -1; iz <= 0; iz++)
                            {
                                Chunk ch = world.GetChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize);
                                if (world.CheckChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize) && ch != this)
                                    world.AddToChunkUpdateQueue(ch);
                            }
                    break;
                case ChunkUpdateMode.ForceMarchingCubesFix:
                    for (int ix = -1; ix <= 0; ix++)
                        for (int iy = -1; iy <= 0; iy++)
                            for (int iz = -1; iz <= 0; iz++)
                            {
                                Chunk ch = world.GetChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize);
                                if (world.CheckChunk(pos.x + ix * BlockSettings.ChunkSize, pos.y + iy * BlockSettings.ChunkSize, pos.z + iz * BlockSettings.ChunkSize) && ch != this && !ch.isRenderQueued)
                                {
                                    ch.isRenderQueued = true;
                                    ch.UpdateChunk(ChunkUpdateMode.ForceSingle);
                                }
                            }
                    break;
            }
        }
        #endregion
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;
using BeardedManStudios.Forge.Networking;
using UnityEngine.SceneManagement;
using System;
using Unity.Collections;

public class World_Network : WorldNetworkingBehavior
{
    World world;

    protected override void NetworkStart()
    {
        base.NetworkStart();

        if (NetworkManager.Instance.Networker is IServer)
        {
            //here you can also do some server specific code
        }
        else
        {
            //setup the disconnected event
            NetworkManager.Instance.Networker.disconnected += DisconnectedFromServer;

        }

        if (!NetworkManager.Instance.IsServer) return;

        world.MapLoadInfo = "Loading server settings...";
        BlockData.InitializeBlocks();
        world.MapLoadInfo = "Setting up server...";

        // Send handshake packet to let player know that the connection was estabilished.
        networkObject.SendRpc(RPC_HANDSHAKE, Receivers.OthersBuffered, (byte)0);


        // Send texturepack (works, but bugged af, todo)
        /*
        networkObject.SendRpc(RPC_SEND_TEXTURE_PACK, Receivers.OthersBuffered,
            BlockData.BlockTexture.GetRawTextureData(),
            BlockData.BlockTileSize,
            BlockData.TextureSize);
        */

        // Send world info (seed, size, etc)
        networkObject.SendRpc(RPC_BLOCK_INIT, Receivers.OthersBuffered,
            BlockData.byID.ObjectToByteArray(),
            BlockData.BlockNames.ObjectToByteArray());

        if (Mods.LoadedMapgens.Count > 0)
        {
            Debug.Log("Found custom mapgen");
            world.worldGen = Activator.CreateInstance(Mods.LoadedMapgens[0]);
        }
        else
        {
            Debug.Log("Using default mapgen");
            world.worldGen = new WorldGen();
        }

        world.worldGen.PrepareBlockInfo();
        StartCoroutine(world.ExecuteWorldgenQueue());

        if (Mods.LoadedMapgens.Count > 0)
            networkObject.SendRpc(RPC_SEND_MAPGEN, Receivers.OthersBuffered, System.Text.Encoding.UTF8.GetBytes(Mods.LoadedMapgens_Code[0]), System.Text.Encoding.UTF8.GetBytes(Mods.LoadedMapgens_Name[0]));

        networkObject.SendRpc(RPC_CREATE_WORLD, Receivers.AllBuffered, new Vector2(world.WorldSeed.x, world.WorldSeed.y), (byte)world.WorldSize.x, (byte)world.WorldSize.y, (byte)world.WorldSize.z);
    }

    /// <summary>
    /// Called when a player disconnects
    /// </summary>
    /// <param name="sender"></param>
    private void DisconnectedFromServer(NetWorker sender)
    {
        NetworkManager.Instance.Networker.disconnected -= DisconnectedFromServer;

        MainThreadManager.Run(() =>
        {
            //Loop through the network objects to see if the disconnected player is the host
            foreach (var no in sender.NetworkObjectList)
            {
                if (no.Owner.IsHost)
                {
                    BMSLogger.Instance.Log("Server disconnected");
                    //Should probably make some kind of "You disconnected" screen. ah well
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                }
            }

            NetworkManager.Instance.Disconnect();
        });
    }


    public void Awake()
    {
        world = gameObject.GetComponent<World>();
    }

    public override void Handshake(RpcArgs args)
    {
        byte received = args.GetNext<byte>();

        world.MapLoadInfo = "Fetching world data...";
    }

    public override void SendTexturePack(RpcArgs args)
    {
        byte[] receivedBytes = args.GetNext<byte[]>();
        BlockData.BlockTexture = new Texture2D(128, 128);
        BlockData.BlockTexture.LoadRawTextureData(receivedBytes);
        BlockData.BlockTexture.Apply();

        BlockData.BlockTileSize = args.GetNext<float>();

        BlockData.TextureSize = args.GetNext<float>();
    }

    public override void BlockInit(RpcArgs args)
    {
        BlockData.InitalizeClient();

        byte[] receivedBytes = args.GetNext<byte[]>();
        BlockData.byID.Clear();
        BlockData.byID = receivedBytes.ByteArrayToObject<List<BlockTypes>>();
        receivedBytes = args.GetNext<byte[]>();
        BlockData.BlockNames = receivedBytes.ByteArrayToObject<string[]>();
    }

    public override void CreateWorld(RpcArgs args)
    {
        world.MapLoadInfo = "Preparing map...";

        string[] PropertyNames = world.BlockMaterial.GetTexturePropertyNames();

        world.BlockMaterial.SetFloat("Vector1_430CB87B", BlockData.BlockTileSize);
        world.BlockMaterial.SetFloat("Vector1_7C9B6D59", BlockData.TextureSize);
        world.BlockMaterial.SetTexture(PropertyNames[0], BlockData.BlockTexture);
        world.BlockMaterial.GetTexture(PropertyNames[0]).filterMode = FilterMode.Point;

        PropertyNames = world.MarchedBlockMaterial.GetTexturePropertyNames();
        world.MarchedBlockMaterial.SetFloat("Vector1_430CB87B", BlockData.BlockTileSize);
        world.MarchedBlockMaterial.SetFloat("Vector1_7C9B6D59", BlockData.TextureSize);
        world.MarchedBlockMaterial.SetTexture(PropertyNames[0], BlockData.BlockTexture);
        world.MarchedBlockMaterial.GetTexture(PropertyNames[0]).filterMode = FilterMode.Point;

        world.SelectedMaterial.SetTextureOffset("_UnlitColorMap", new Vector2(BlockData.byID[1].Texture_Up.x * BlockData.BlockTileSize, BlockData.byID[1].Texture_Up.y * BlockData.BlockTileSize));
        world.SelectedMaterial.SetTextureScale("_UnlitColorMap", new Vector2(BlockData.BlockTileSize, BlockData.BlockTileSize));
        world.SelectedMaterial.SetTexture("_UnlitColorMap", BlockData.BlockTexture);
        world.SelectedMaterial.GetTexture("_UnlitColorMap").filterMode = FilterMode.Point;

        Vector2 tempv2 = args.GetNext<Vector2>();
        world.WorldSeed = new Unity.Mathematics.float2(tempv2.x, tempv2.y);
        world.WorldSize = new Unity.Mathematics.int3((int)args.GetNext<byte>(), (int)args.GetNext<byte>(), (int)args.GetNext<byte>());
        world.StartCoroutine(world.CreateWorld());
    }

    public override void SendChunk(RpcArgs args)
    {
        throw new System.NotImplementedException();
    }

    public void SetBlock_Caller(int x, int y, int z, BlockMetadata metadata)
    {
        networkObject.SendRpc(RPC_SET_BLOCK, Receivers.AllBuffered, x, y, z, metadata.ObjectToByteArray());
    }

    public override void SetBlock(RpcArgs args)
    {
        world.SetBlock(args.GetNext<int>(), args.GetNext<int>(), args.GetNext<int>(), args.GetNext<byte[]>().ByteArrayToObject<BlockMetadata>(), true);
    }

    public override void SendMapgen(RpcArgs args)
    {
        byte[] MapgenCode = args.GetNext<byte[]>();
        byte[] MapgenName = args.GetNext<byte[]>();
        string sMapgenName = System.Text.Encoding.UTF8.GetString(MapgenName, 0, MapgenName.Length);

        Mods.CompileMod(System.Text.Encoding.UTF8.GetString(MapgenCode, 0, MapgenCode.Length), sMapgenName);
        world.worldGen = Activator.CreateInstance(Mods.GetMapgen(sMapgenName));
        world.worldGen.PrepareBlockInfo();
        StartCoroutine(world.ExecuteWorldgenQueue());
    }

    public override void GetChunk(RpcArgs args)
    {
        throw new System.NotImplementedException();
    }

    public override void SendBroadcast(RpcArgs args)
    {
        throw new System.NotImplementedException();
    }
}

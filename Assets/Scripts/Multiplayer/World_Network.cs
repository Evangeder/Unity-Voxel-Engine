using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;
using BeardedManStudios.Forge.Networking;
using UnityEngine.SceneManagement;
using System;
using Unity.Collections;
using VoxaNovus;
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

        //MarchingCubesTables.ConvertToNative();
        //BlockConfigurationReader.InitializeBlocks();
        
        //StartCoroutine(BlockConfigurationReader.InitializeSounds());

        // Send handshake packet to let player know that the connection was estabilished.
        networkObject.SendRpc(RPC_HANDSHAKE, Receivers.OthersBuffered, (byte)0);


        // Send texturepack (works, but bugged af, todo)
        /*
        networkObject.SendRpc(RPC_SEND_TEXTURE_PACK, Receivers.OthersBuffered,
            BlockSettings.BlockTexture.GetRawTextureData(),
            BlockSettings.BlockTileSize,
            BlockSettings.TextureSize);
        */

        // Send world info (seed, size, etc)

        networkObject.SendRpc(RPC_BLOCK_INIT, Receivers.OthersBuffered,
            BlockSettings.byID.ObjectToByteArray(),
            BlockSettings.BlockNames.ObjectToByteArray());

        switch (BlockSettings.worldGen)
        {
            default:
            case 0: 
                world.worldGen = new VoxaNovus.WorldGen.FlatgrassWithChunkBorder();
                Debug.Log("FlatgrassWithChunkBorder"); 
                break;
            case 1: 
                world.worldGen = new VoxaNovus.WorldGen.Flatgrass(); 
                Debug.Log("Flatgrass"); 
                break;
            case 2: 
                world.worldGen = new VoxaNovus.WorldGen.FloatingIslands(); 
                Debug.Log("FloatingIslands"); 
                break;
        }

        //if (Mods.LoadedMapgens.Count > 0)
        //{
        //    Debug.Log("Found custom mapgen");
        //    world.worldGen = new VoxaNovus.WorldGen.Flatgrass(); //Activator.CreateInstance(Mods.LoadedMapgens[0]);
        //}
        //else
        //{
        //    Debug.Log("Using default mapgen");
        //    world.worldGen = new VoxaNovus.WorldGen.Flatgrass();
        //}

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
    }

    public override void SendTexturePack(RpcArgs args)
    {
        byte[] receivedBytes = args.GetNext<byte[]>();
        BlockSettings.BlockTexture = new Texture2D(128, 128);
        BlockSettings.BlockTexture.LoadRawTextureData(receivedBytes);
        BlockSettings.BlockTexture.Apply();

        BlockSettings.BlockTileSize = args.GetNext<float>();

        BlockSettings.TextureSize = args.GetNext<float>();
    }

    public override void BlockInit(RpcArgs args)
    {
        BlockConfigurationReader.InitalizeClient();

        byte[] receivedBytes = args.GetNext<byte[]>();
        BlockSettings.byID.Clear();
        BlockSettings.byID = receivedBytes.ByteArrayToObject<List<BlockData>>();
        receivedBytes = args.GetNext<byte[]>();
        BlockSettings.BlockNames = receivedBytes.ByteArrayToObject<string[]>();
    }

    public override void CreateWorld(RpcArgs args)
    {
        string[] PropertyNames = world.BlockMaterial.GetTexturePropertyNames();

        world.BlockMaterial.SetFloat("Vector1_430CB87B", BlockSettings.BlockTileSize);
        world.BlockMaterial.SetFloat("Vector1_7C9B6D59", BlockSettings.TextureSize);
        world.BlockMaterial.SetTexture(PropertyNames[0], BlockSettings.BlockTexture);
        world.BlockMaterial.GetTexture(PropertyNames[0]).filterMode = FilterMode.Point;

        PropertyNames = world.MarchedBlockMaterial.GetTexturePropertyNames();
        world.MarchedBlockMaterial.SetFloat("Vector1_430CB87B", BlockSettings.BlockTileSize);
        world.MarchedBlockMaterial.SetFloat("Vector1_7C9B6D59", BlockSettings.TextureSize);
        world.MarchedBlockMaterial.SetTexture(PropertyNames[0], BlockSettings.BlockTexture);
        world.MarchedBlockMaterial.GetTexture(PropertyNames[0]).filterMode = FilterMode.Point;

        world.SelectedMaterial.SetTextureOffset("_UnlitColorMap", new Vector2(BlockSettings.byID[1].Texture_Up.x * BlockSettings.BlockTileSize, BlockSettings.byID[1].Texture_Up.y * BlockSettings.BlockTileSize));
        world.SelectedMaterial.SetTextureScale("_UnlitColorMap", new Vector2(BlockSettings.BlockTileSize, BlockSettings.BlockTileSize));
        world.SelectedMaterial.SetTexture("_UnlitColorMap", BlockSettings.BlockTexture);
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

    public void SetBlock_Caller(int x, int y, int z, BlockMetadata metadata, BlockUpdateMode blockUpdateMode)
    {
        networkObject.SendRpc(RPC_SET_BLOCK, Receivers.AllBuffered, x, y, z, metadata.ObjectToByteArray(), (byte)blockUpdateMode);
    }

    public override void SetBlock(RpcArgs args)
    {
        world.SetBlock(args.GetNext<int>(), args.GetNext<int>(), args.GetNext<int>(), args.GetNext<byte[]>().ByteArrayToObject<BlockMetadata>(), true, (BlockUpdateMode)args.GetNext<byte>());
    }

    public override void SendMapgen(RpcArgs args)
    {
        byte[] MapgenCode = args.GetNext<byte[]>();
        byte[] MapgenName = args.GetNext<byte[]>();
        string sMapgenName = System.Text.Encoding.UTF8.GetString(MapgenName, 0, MapgenName.Length);

        Mods.CompileMod(System.Text.Encoding.UTF8.GetString(MapgenCode, 0, MapgenCode.Length), sMapgenName);
        //world.worldGen = Activator.CreateInstance(Mods.GetMapgen(sMapgenName));

        switch (BlockSettings.worldGen)
        {
            default:
            case 0: 
                world.worldGen = new VoxaNovus.WorldGen.FlatgrassWithChunkBorder();
                Debug.Log("FlatgrassWithChunkBorder"); 
                break;
            case 1: 
                world.worldGen = new VoxaNovus.WorldGen.Flatgrass(); 
                Debug.Log("Flatgrass"); 
                break;
            case 2: 
                world.worldGen = new VoxaNovus.WorldGen.FloatingIslands(); 
                Debug.Log("FloatingIslands"); 
                break;
        }
        //world.worldGen.PrepareBlockInfo();
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

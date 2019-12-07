using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;
using UnityEngine;

namespace BeardedManStudios.Forge.Networking.Generated
{
	[GeneratedRPC("{\"types\":[[\"byte\"][\"byte[]\", \"float\", \"float\"][\"byte[]\", \"byte[]\"][\"byte[]\", \"byte[]\"][\"Vector2\", \"byte\", \"byte\", \"byte\"][\"int\", \"int\", \"int\", \"byte[]\"][\"int\", \"int\", \"int\"][\"int\", \"int\", \"int\", \"byte[]\"][\"byte[]\"]]")]
	[GeneratedRPCVariableNames("{\"types\":[[\"Byte\"][\"TexturePack\", \"BlockTileSize\", \"TextureSize\"][\"MapgenCode\", \"MapgenName\"][\"BlockData\", \"BlockNames\"][\"Seed\", \"SizeX\", \"SizeY\", \"SizeZ\"][\"X\", \"Y\", \"Z\", \"Block\"][\"\", \"\", \"\"][\"X\", \"Y\", \"Z\", \"Chunk\"][\"\"]]")]
	public abstract partial class WorldNetworkingBehavior : NetworkBehavior
	{
		public const byte RPC_HANDSHAKE = 0 + 5;
		public const byte RPC_SEND_TEXTURE_PACK = 1 + 5;
		public const byte RPC_SEND_MAPGEN = 2 + 5;
		public const byte RPC_BLOCK_INIT = 3 + 5;
		public const byte RPC_CREATE_WORLD = 4 + 5;
		public const byte RPC_SET_BLOCK = 5 + 5;
		public const byte RPC_GET_CHUNK = 6 + 5;
		public const byte RPC_SEND_CHUNK = 7 + 5;
		public const byte RPC_SEND_BROADCAST = 8 + 5;
		
		public WorldNetworkingNetworkObject networkObject = null;

		public override void Initialize(NetworkObject obj)
		{
			// We have already initialized this object
			if (networkObject != null && networkObject.AttachedBehavior != null)
				return;
			
			networkObject = (WorldNetworkingNetworkObject)obj;
			networkObject.AttachedBehavior = this;

			base.SetupHelperRpcs(networkObject);
			networkObject.RegisterRpc("Handshake", Handshake, typeof(byte));
			networkObject.RegisterRpc("SendTexturePack", SendTexturePack, typeof(byte[]), typeof(float), typeof(float));
			networkObject.RegisterRpc("SendMapgen", SendMapgen, typeof(byte[]), typeof(byte[]));
			networkObject.RegisterRpc("BlockInit", BlockInit, typeof(byte[]), typeof(byte[]));
			networkObject.RegisterRpc("CreateWorld", CreateWorld, typeof(Vector2), typeof(byte), typeof(byte), typeof(byte));
			networkObject.RegisterRpc("SetBlock", SetBlock, typeof(int), typeof(int), typeof(int), typeof(byte[]));
			networkObject.RegisterRpc("GetChunk", GetChunk, typeof(int), typeof(int), typeof(int));
			networkObject.RegisterRpc("SendChunk", SendChunk, typeof(int), typeof(int), typeof(int), typeof(byte[]));
			networkObject.RegisterRpc("SendBroadcast", SendBroadcast, typeof(byte[]));

			networkObject.onDestroy += DestroyGameObject;

			if (!obj.IsOwner)
			{
				if (!skipAttachIds.ContainsKey(obj.NetworkId)){
					uint newId = obj.NetworkId + 1;
					ProcessOthers(gameObject.transform, ref newId);
				}
				else
					skipAttachIds.Remove(obj.NetworkId);
			}

			if (obj.Metadata != null)
			{
				byte transformFlags = obj.Metadata[0];

				if (transformFlags != 0)
				{
					BMSByte metadataTransform = new BMSByte();
					metadataTransform.Clone(obj.Metadata);
					metadataTransform.MoveStartIndex(1);

					if ((transformFlags & 0x01) != 0 && (transformFlags & 0x02) != 0)
					{
						MainThreadManager.Run(() =>
						{
							transform.position = ObjectMapper.Instance.Map<Vector3>(metadataTransform);
							transform.rotation = ObjectMapper.Instance.Map<Quaternion>(metadataTransform);
						});
					}
					else if ((transformFlags & 0x01) != 0)
					{
						MainThreadManager.Run(() => { transform.position = ObjectMapper.Instance.Map<Vector3>(metadataTransform); });
					}
					else if ((transformFlags & 0x02) != 0)
					{
						MainThreadManager.Run(() => { transform.rotation = ObjectMapper.Instance.Map<Quaternion>(metadataTransform); });
					}
				}
			}

			MainThreadManager.Run(() =>
			{
				NetworkStart();
				networkObject.Networker.FlushCreateActions(networkObject);
			});
		}

		protected override void CompleteRegistration()
		{
			base.CompleteRegistration();
			networkObject.ReleaseCreateBuffer();
		}

		public override void Initialize(NetWorker networker, byte[] metadata = null)
		{
			Initialize(new WorldNetworkingNetworkObject(networker, createCode: TempAttachCode, metadata: metadata));
		}

		private void DestroyGameObject(NetWorker sender)
		{
			MainThreadManager.Run(() => { try { Destroy(gameObject); } catch { } });
			networkObject.onDestroy -= DestroyGameObject;
		}

		public override NetworkObject CreateNetworkObject(NetWorker networker, int createCode, byte[] metadata = null)
		{
			return new WorldNetworkingNetworkObject(networker, this, createCode, metadata);
		}

		protected override void InitializedTransform()
		{
			networkObject.SnapInterpolations();
		}

		/// <summary>
		/// Arguments:
		/// byte Byte
		/// </summary>
		public abstract void Handshake(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] TexturePack
		/// float BlockTileSize
		/// float TextureSize
		/// </summary>
		public abstract void SendTexturePack(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] MapgenCode
		/// byte[] MapgenName
		/// </summary>
		public abstract void SendMapgen(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] BlockData
		/// byte[] BlockNames
		/// </summary>
		public abstract void BlockInit(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// Vector2 Seed
		/// byte SizeX
		/// byte SizeY
		/// byte SizeZ
		/// </summary>
		public abstract void CreateWorld(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// int X
		/// int Y
		/// int Z
		/// byte[] Block
		/// </summary>
		public abstract void SetBlock(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// int 
		/// int 
		/// int
		/// </summary>
		public abstract void GetChunk(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// int X
		/// int Y
		/// int Z
		/// byte[] Chunk
		/// </summary>
		public abstract void SendChunk(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[]
		/// </summary>
		public abstract void SendBroadcast(RpcArgs args);

		// DO NOT TOUCH, THIS GETS GENERATED PLEASE EXTEND THIS CLASS IF YOU WISH TO HAVE CUSTOM CODE ADDITIONS
	}
}
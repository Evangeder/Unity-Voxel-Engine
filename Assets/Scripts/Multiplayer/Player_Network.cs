using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;
using BeardedManStudios.Forge.Networking;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using VoxaNovus;

public class Player_Network : Player_NetworkingBehavior
{
    [SerializeField] GameObject LocalPlayerObject;
    [SerializeField] GameObject NetworkPlayerObject;
    World world;

    protected override void NetworkStart()
    {
        base.NetworkStart();

        if (!networkObject.IsOwner)
        {
            Destroy(LocalPlayerObject);
            NetworkPlayerObject.SetActive(true);
        } else {
            LocalPlayerObject.SetActive(true);
            Destroy(NetworkPlayerObject);
        }

        networkObject.onDestroy += OnDestroyEvent;
    }

    public void Awake()
    {
        world = GameObject.Find("World").GetComponent<World>();
    }

    private void OnDestroyEvent(NetWorker sender)
    {

    }

    private void Update()
    {
        // If unity's Update() runs, before the object is
        // instantiated in the network, then simply don't
        // continue, otherwise a bug/error will happen.
        // 
        // Unity's Update() running, before this object is instantiated
        // on the network is **very** rare, but better be safe 100%
        if (networkObject == null)
            return;

        if (networkObject.Owner.Disconnected)
        {
            networkObject.Destroy();
        }

        // If we are not the owner of this network object then we should
        // move this cube to the position/rotation dictated by the owner
        if (!networkObject.IsOwner)
        {
            transform.position = networkObject.Position;
            return;
        }

        // Let the owner move the cube around with the arrow keys
        //transform.position += new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized * 5f * Time.deltaTime;

        // If we are the owner of the object we should send the new position
        // and rotation across the network for receivers to move to in the above code

        networkObject.Position = transform.position;

        // Note: Forge Networking takes care of only sending the delta, so there
        // is no need for you to do that manually
    }
}

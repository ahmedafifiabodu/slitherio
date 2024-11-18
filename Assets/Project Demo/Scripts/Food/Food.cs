using Unity.Netcode;
using UnityEngine;

public class Food : NetworkBehaviour
{
    internal GameObject _prefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (collision.TryGetComponent(out PlayerLength playerLength))
            playerLength.AddLength();
        else if (collision.TryGetComponent(out Tail tail))
            tail._networkOwner.GetComponent<PlayerLength>().AddLength();

        NetworkObject.Despawn(true);
        NetworkObjectPool.Singleton.ReturnNetworkObject(NetworkObject, _prefab);
    }
}
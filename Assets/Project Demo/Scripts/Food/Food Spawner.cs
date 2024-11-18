using Unity.Netcode;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _foodPrefab;

    private const int MaxPrefabCount = 50;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SpawnFoodStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnFoodStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void SpawnFoodStarted()
    {
        NetworkObjectPool.Singleton.OnNetworkSpawn();

        for (int i = 0; i < 20; ++i)
            SpawnFood();

        StartCoroutine(SpawnFoodOverTime());
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 1)
            StartCoroutine(SpawnFoodOverTime());
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 0)
            StopCoroutine(SpawnFoodOverTime());
    }

    private void SpawnFood()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Logging.LogWarning("Attempted to spawn food on a client. Only the server can spawn food.");
            return;
        }

        NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(_foodPrefab, GetRandomPositionOnMap(), Quaternion.identity);
        obj.GetComponent<Food>()._prefab = _foodPrefab;
        if (!obj.IsSpawned) obj.Spawn(true);
    }

    private Vector3 GetRandomPositionOnMap() => new(Random.Range(-9, 9), Random.Range(-5, 5), 0);

    private System.Collections.IEnumerator SpawnFoodOverTime()
    {
        while (NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            yield return new WaitForSeconds(2f);

            if (NetworkManager.Singleton.IsServer && NetworkObjectPool.Singleton.GetCurrentPrefabCount(_foodPrefab) < MaxPrefabCount)
                SpawnFood();
        }
    }
}
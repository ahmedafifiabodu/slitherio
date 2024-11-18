using Unity.Netcode;
using UnityEngine;

public class NetworkApprovalHandler : MonoBehaviour
{
    public static int _maxPlayer = 10;

    private void Start() => NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count >= _maxPlayer)
        {
            response.Approved = false;
            response.Reason = "Server is full!";
        }
        else
        {
            response.Approved = true;
            response.CreatePlayerObject = true;

            // Ensure the player prefab is registered and use its hash
            var playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            if (playerPrefab != null)
            {
                response.PlayerPrefabHash = playerPrefab.GetComponent<NetworkObject>().PrefabIdHash;
            }
            else
            {
                Debug.LogError("Player prefab is not registered in the NetworkManager.");
                response.Approved = false;
                response.Reason = "Player prefab is not registered.";
            }
        }

        response.Pending = false;
    }
}
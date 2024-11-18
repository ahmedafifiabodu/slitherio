using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _playerLengthText;
    [SerializeField] private Canvas _gameOverCanvas;

    private void OnEnable()
    {
        PlayerLength.OnLengthChanged += OnLengthChanged;
        PlayerController.OnGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        PlayerLength.OnLengthChanged -= OnLengthChanged;
        PlayerController.OnGameOver -= OnGameOver;
    }

    public void StartServer() => NetworkManager.Singleton.StartServer();

    public void StartClient() => NetworkManager.Singleton.StartClient();

    public void StartHost() => NetworkManager.Singleton.StartHost();

    private void OnLengthChanged(ushort length) => _playerLengthText.text = $"Player Length: {length}";

    public void OnGameOver() => _gameOverCanvas.enabled = true;
}
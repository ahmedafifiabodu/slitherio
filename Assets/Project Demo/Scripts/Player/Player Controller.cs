using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;

    private Camera _mainCamera;
    private InputManager _inputManager;

    private bool _canCollide = true;

    private Vector2 _mousePosition;
    private Vector3 _targetPosition;
    private LayerMask _playerLayerMask;

    private PlayerLength _playerLength;
    private readonly ulong[] _targetClientsArray = new ulong[1];

    public static event System.Action OnGameOver;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _inputManager = ServiceLocator.Instance.GetService<InputManager>();
        _mainCamera = Camera.main;

        _inputManager._playerActions.MoveByPress.performed += OnMovePerformed;

        _playerLayerMask = 1 << gameObject.layer;

        if (TryGetComponent(out PlayerLength playerLength))
            _playerLength = playerLength;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _inputManager._playerActions.MoveByPress.performed -= OnMovePerformed;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.control.device is Pointer pointer)
        {
            _mousePosition = pointer.position.ReadValue();
            Vector3 mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint(new Vector3(_mousePosition.x, _mousePosition.y, _mainCamera.nearClipPlane));
            mouseWorldCoordinates.z = 0;

            // Send the input to the server
            SetTargetPositionServerRpc(mouseWorldCoordinates);
        }
    }

    [ServerRpc]
    private void SetTargetPositionServerRpc(Vector3 targetPosition)
    {
        _targetPosition = targetPosition;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !Application.isFocused)
            return;

        MovePlayerServerRpc();
    }

    [ServerRpc]
    private void MovePlayerServerRpc()
    {
        // Move the player towards the target position
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, Time.deltaTime * _speed);

        // Rotation
        if (_targetPosition != transform.position)
        {
            Vector3 direction = _targetPosition - transform.position;
            transform.up = direction;
        }
    }

    [ServerRpc]
    private void DetermineCollisionWinnerServerRpc(PlayerData _player1, PlayerData _player2)
    {
        if (_player1._length > _player2._length)
        {
            WinInformation(_player1._id, _player2._id);
        }
        else
        {
            WinInformation(_player2._id, _player1._id);
        }
    }

    private void WinInformation(ulong _winner, ulong _loser)
    {
        // Send the win message to the winner
        _targetClientsArray[0] = _winner;
        ClientRpcParams clientRpcAttribute = new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = _targetClientsArray
            }
        };
        AtePlayerClientRpc(clientRpcAttribute);

        // Send the game over message to the loser
        _targetClientsArray[0] = _loser;
        clientRpcAttribute.Send.TargetClientIds = _targetClientsArray;
        GameOverClientRpc(clientRpcAttribute);
    }

    [ClientRpc]
    private void AtePlayerClientRpc(ClientRpcParams _ = default)
    {
        if (!IsOwner) return;

        Logging.Log("Ate player");
    }

    [ClientRpc]
    private void GameOverClientRpc(ClientRpcParams _ = default)
    {
        if (!IsOwner) return;

        Logging.Log("Game over");
        OnGameOver?.Invoke();
        NetworkManager.Singleton.Shutdown();
    }

    private IEnumerator CollisionCheckCoroutine()
    {
        _canCollide = false;
        yield return new WaitForSeconds(0.5f);
        _canCollide = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsOwner) return;

        // Check if the collided object is in the player layer
        if (((1 << collision.gameObject.layer) & _playerLayerMask) == 0) return;

        if (!_canCollide) return;
        StartCoroutine(CollisionCheckCoroutine());

        if (collision.gameObject.TryGetComponent(out PlayerLength _playerCollideLength))
        {
            var _player1 = new PlayerData
            {
                _id = OwnerClientId,
                _length = _playerLength.length.Value
            };

            var _player2 = new PlayerData
            {
                _id = _playerCollideLength.OwnerClientId,
                _length = _playerCollideLength.length.Value
            };

            DetermineCollisionWinnerServerRpc(_player1, _player2);
        }
        else if (collision.gameObject.TryGetComponent(out Tail _tail))
        {
            var _playerLength = _tail._networkOwner.GetComponent<PlayerLength>();

            var _player1 = new PlayerData
            {
                _id = OwnerClientId,
                _length = _playerLength.length.Value
            };

            var _player2 = new PlayerData
            {
                _id = _playerLength.OwnerClientId,
                _length = _playerLength.length.Value
            };

            DetermineCollisionWinnerServerRpc(_player1, _player2);
        }
    }
}

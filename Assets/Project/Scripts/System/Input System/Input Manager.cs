using UnityEngine;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions _inputSystem;
    internal InputSystem_Actions.PlayerActions _playerActions;

    private ServiceLocator _serviceLocator;

    private void Awake()
    {
        _serviceLocator = ServiceLocator.Instance;
        _serviceLocator.RegisterService(this, true);

        _inputSystem = new InputSystem_Actions();

        _playerActions = _inputSystem.Player;
    }

    private void OnEnable() => _inputSystem.Enable();

    private void OnDisable() => _inputSystem?.Disable();
}
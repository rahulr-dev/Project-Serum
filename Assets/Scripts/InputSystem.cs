using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    public static InputSystem Instance;

    [SerializeField] private InputActionAsset inputActionAsset;
    private InputActionMap _playerMap;
    private InputAction _moveAction;
    private InputAction _crouchAction;
    private InputAction _jumpAction;
    private InputAction _interactAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _playerMap = inputActionAsset.FindActionMap("Player");
        _moveAction = _playerMap.FindAction("Move");
        _crouchAction = _playerMap.FindAction("Crouch");
        _jumpAction = _playerMap.FindAction("Jump");
        _interactAction = _playerMap.FindAction("Interact");
    }

    private void OnEnable()
    {
        _playerMap?.Enable();
    }

    private void OnDisable()
    {
        _playerMap?.Disable();
    }

    public float Move() => _moveAction?.ReadValue<float>() ?? 0f;

    public bool Crouch() => _crouchAction?.inProgress ?? false;

    public bool JumpPressed() => _jumpAction?.WasPressedThisFrame() ?? false;

    public bool JumpHeld() => _jumpAction?.inProgress ?? false;

    public bool InteractPressed() => _interactAction?.WasPressedThisFrame() ?? false;
}
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    public static InputSystem Instance;

    [SerializeField] private InputActionAsset inputActionAsset;
    private InputActionMap _playerMap;

    private InputAction _moveAction;

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
    }

    private void Start()
    {

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
}

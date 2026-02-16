using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterControl : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [Tooltip("Time in seconds to go from zero to full speed (and vice versa). 0 = instant.")]
    [SerializeField] float timeToFullSpeed = 0.2f;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] InputActionAsset inputActions;

    InputAction _moveAction;
    InputAction _interactAction;
    float _moveInput;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (inputActions == null) return;
        var playerMap = inputActions.FindActionMap("Player");
        _moveAction = playerMap.FindAction("Move");
        _interactAction = playerMap.FindAction("Interact");
        _moveAction?.Enable();
        _interactAction?.Enable();
        if (_interactAction != null)
            _interactAction.performed += OnInteractPerformed;
    }

    void OnDisable()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteractPerformed;
        _moveAction?.Disable();
        _interactAction?.Disable();
    }

    void Update()
    {
        _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>().x : 0f;
    }

    void FixedUpdate()
    {
        float targetVelX = _moveInput * moveSpeed;
        float currentVelX = rb.linearVelocity.x;
        float newVelX = timeToFullSpeed <= 0f
            ? targetVelX
            : Mathf.MoveTowards(currentVelX, targetVelX, (moveSpeed / timeToFullSpeed) * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
    }

    void OnInteractPerformed(InputAction.CallbackContext _)
    {
        Interact();
    }

    void Interact()
    {
        // Override or hook up your interaction logic (e.g. raycast, overlap for interactables)
        Debug.Log("Interact pressed");
    }
}

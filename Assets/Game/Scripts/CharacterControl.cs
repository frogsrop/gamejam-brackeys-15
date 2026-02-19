using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterControl : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [Tooltip("Time in seconds to go from zero to full speed (and vice versa). 0 = instant.")]
    [SerializeField] float timeToFullSpeed = 0.2f;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] InputActionAsset inputActions;

    [Header("Hint Panel")]
    [SerializeField] private GameObject hintPanel;
    [Tooltip("World-space offset from the character (e.g. 0, 1.5, 0 = above).")]
    [SerializeField] private Vector3 hintPanelOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private TMP_Text hintTextComponent;
    [SerializeField] private string defaultHintText = "Press E to interact";
    [SerializeField] private Transform body;

    InputAction _moveAction;
    InputAction _interactAction;
    float _moveInput;
    IInteractable _currentInteractable;
    GameObject _currentInteractableObject;
    Animator animator;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
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
        if (hintPanel != null && hintPanel.activeSelf)
            hintPanel.transform.position = transform.position + hintPanelOffset;

        animator.SetFloat("WalkSpeed", Mathf.Abs(_moveInput));
        Vector3 s = gameObject.transform.localScale;
        var newScale = new Vector3(Mathf.Sign(-_moveInput), s.y, s.z);
        if (Mathf.Abs(_moveInput) > 0) {
            gameObject.transform.localScale = newScale;
        }
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
        if (_currentInteractable != null)
        {
            _currentInteractable.Interact(gameObject);
            return;
        }
        Debug.Log("Interact pressed");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var interactable = other.GetComponent<IInteractable>()
            ?? other.GetComponentInParent<IInteractable>()
            ?? other.GetComponentInChildren<IInteractable>();
        if (interactable != null)
        {
            _currentInteractable = interactable;
            _currentInteractableObject = other.gameObject;
        }

        if (hintPanel == null) return;

        hintPanel.transform.position = transform.position + hintPanelOffset;
        hintPanel.SetActive(true);

        string text = defaultHintText;
        var hintTrigger = other.GetComponent<HintTrigger>();
        if (hintTrigger != null)
            text = hintTrigger.HintText;

        if (hintTextComponent != null)
            hintTextComponent.text = text;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == _currentInteractableObject)
        {
            _currentInteractable = null;
            _currentInteractableObject = null;
        }
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }
}

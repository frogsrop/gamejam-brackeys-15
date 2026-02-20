using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GhostReveal : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Visibility")]
    [Tooltip("How long the ghost stays visible after the action is triggered.")]
    [SerializeField] private float revealDuration = 2f;

    [Header("Audio")]
    [Tooltip("Optional sound to play on reveal.")]
    [SerializeField] private AudioClip revealSound;

    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private InputAction _revealAction;
    private Coroutine _revealCoroutine;

    void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = false;
    }

    void OnEnable()
    {
        if (inputActions == null) return;
        var playerMap = inputActions.FindActionMap("Player");
        _revealAction = playerMap?.FindAction("Jump");
        _revealAction?.Enable();
        if (_revealAction != null)
            _revealAction.performed += OnRevealPerformed;
    }

    void OnDisable()
    {
        if (_revealAction != null)
            _revealAction.performed -= OnRevealPerformed;
        _revealAction?.Disable();
    }

    void OnRevealPerformed(InputAction.CallbackContext _)
    {
        Reveal();
    }

    public void Reveal()
    {
        if (_revealCoroutine != null)
            StopCoroutine(_revealCoroutine);

        _revealCoroutine = StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;

        if (revealSound != null && _audioSource != null)
            _audioSource.PlayOneShot(revealSound);

        yield return new WaitForSeconds(revealDuration);

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = false;

        _revealCoroutine = null;
    }
}

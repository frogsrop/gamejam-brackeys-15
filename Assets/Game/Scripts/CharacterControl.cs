using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

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
    [Tooltip("If set, hint panel follows this pivot; otherwise uses offset from character.")]
    [SerializeField] private Transform hintPivot;
    [Tooltip("World-space offset from the character when hintPivot is not set (e.g. 0, 1.5, 0 = above).")]
    [SerializeField] private Vector3 hintPanelOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private TMP_Text hintTextComponent;
    [SerializeField] private string defaultHintText = "Press E to interact";
    [Tooltip("Radius for trigger detection when no collider overlap (fallback).")]
    [SerializeField] private float triggerRefreshRadius = 2f;
    [Tooltip("Poll triggers every N seconds. 0 = every frame (default, negligible cost).")]
    [SerializeField] private float triggerPollInterval = 0f;
    [SerializeField] private Transform body;
    float _nextTriggerPollTime;

    InputAction _moveAction;
    InputAction _interactAction;
    InputAction _reportAction;
    float _moveInput;
    IInteractable _currentInteractable;
    GameObject _currentInteractableObject;
    IReportGhostActivity _currentReportable;
    GameObject _currentReportableObject;
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
        _reportAction = playerMap.FindAction("Report");
        _moveAction?.Enable();
        _interactAction?.Enable();
        _reportAction?.Enable();
        if (_interactAction != null)
            _interactAction.performed += OnInteractPerformed;
        if (_reportAction != null)
            _reportAction.performed += OnReportPerformed;
    }

    void OnDisable()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteractPerformed;
        if (_reportAction != null)
            _reportAction.performed -= OnReportPerformed;
        _moveAction?.Disable();
        _interactAction?.Disable();
        _reportAction?.Disable();
    }

    void Update()
    {
        _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>().x : 0f;
        PollTriggers();
        if (hintPanel != null && hintPanel.activeSelf)
            hintPanel.transform.position = hintPivot != null ? hintPivot.position : transform.position + hintPanelOffset;
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

    void OnReportPerformed(InputAction.CallbackContext _)
    {
        if (_currentReportable != null)
            _currentReportable.ReportGhostActivity(gameObject);
    }

    void PollTriggers()
    {
        if (triggerPollInterval > 0f && Time.time < _nextTriggerPollTime)
            return;
        _nextTriggerPollTime = Time.time + triggerPollInterval;
        _currentInteractable = null;
        _currentInteractableObject = null;
        _currentReportable = null;
        _currentReportableObject = null;
        var col = GetComponent<Collider2D>();
        var hits = new List<Collider2D>();
        if (col != null)
        {
            var filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = false;
            Physics2D.OverlapCollider(col, filter, hits);
        }
        if (hits.Count == 0)
        {
            var circleHits = Physics2D.OverlapCircleAll(transform.position, triggerRefreshRadius);
            foreach (var c in circleHits) if (c != null && c.isTrigger) hits.Add(c);
        }
        foreach (var c in hits)
        {
            if (c == null || c.gameObject == gameObject || !c.isTrigger) continue;
            if (_currentInteractable == null)
            {
                var i = c.GetComponent<IInteractable>() ?? c.GetComponentInParent<IInteractable>() ?? c.GetComponentInChildren<IInteractable>();
                if (i != null) { _currentInteractable = i; _currentInteractableObject = c.gameObject; }
            }
            if (_currentReportable == null)
            {
                var r = c.GetComponent<IReportGhostActivity>() ?? c.GetComponentInParent<IReportGhostActivity>() ?? c.GetComponentInChildren<IReportGhostActivity>();
                if (r != null) { _currentReportable = r; _currentReportableObject = c.gameObject; }
            }
        }
        if (hintPanel == null) return;
        if (_currentInteractable == null && _currentReportable == null)
        {
            hintPanel.SetActive(false);
            return;
        }
        hintPanel.transform.position = hintPivot != null ? hintPivot.position : transform.position + hintPanelOffset;
        hintPanel.SetActive(true);
        var lines = new List<string>();
        if (_currentInteractable != null && _currentInteractableObject != null)
        {
            var hintTrigger = _currentInteractableObject.GetComponent<HintTrigger>() ?? _currentInteractableObject.GetComponentInParent<HintTrigger>() ?? _currentInteractableObject.GetComponentInChildren<HintTrigger>();
            lines.Add(hintTrigger != null ? hintTrigger.HintText : defaultHintText);
        }
        if (_currentReportable != null && _currentReportableObject != null)
        {
            var reportTrigger = _currentReportableObject.GetComponent<ReportGhostActivityTrigger>() ?? _currentReportableObject.GetComponentInParent<ReportGhostActivityTrigger>() ?? _currentReportableObject.GetComponentInChildren<ReportGhostActivityTrigger>();
            if (reportTrigger != null) lines.Add(reportTrigger.HintText);
        }
        if (hintTextComponent != null)
            hintTextComponent.text = lines.Count > 0 ? string.Join("\n", lines) : defaultHintText;
    }
}

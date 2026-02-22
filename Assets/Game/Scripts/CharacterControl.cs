using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

    [Header("Hints")]
    [Tooltip("Hints container. Top-right of Hints is placed at top-right of camera (camera is in world space).")]
    [SerializeField] private RectTransform hintsRoot;
    [Tooltip("Padding from top-right corner (world units). X = from right, Y = from top.")]
    [SerializeField] private Vector2 hintsPaddingTopRight = Vector2.zero;
    [SerializeField] private TMP_Text hintsQ;
    [SerializeField] private TMP_Text hintsE;
    [SerializeField] private TMP_Text hintsF;
    [SerializeField] private string defaultHintText = "Press E to interact";
    [Tooltip("Radius for trigger detection when no collider overlap (fallback).")]
    [SerializeField] private float triggerRefreshRadius = 2f;
    [Tooltip("Poll triggers every N seconds. 0 = every frame (default, negligible cost).")]
    [SerializeField] private float triggerPollInterval = 0f;
    [SerializeField] private Transform body;
    [Header("Ghost Report Screenshot")]
    [Tooltip("RawImage to show screenshot when user reports a ghost-activated node. Centered on screen.")]
    [SerializeField] private RawImage screenshotRawImage;
    [Tooltip("Orthographic size for capture (world units around object).")]
    [SerializeField] private float captureRadius = 5f;
    [Tooltip("RenderTexture resolution for the capture.")]
    [SerializeField] private int captureResolution = 256;
    [Tooltip("For World Space Canvas: scale of the screenshot in world units (e.g. 0.01 = small).")]
    [SerializeField] private float screenshotWorldScale = 0.01f;
    float _nextTriggerPollTime;

    InputAction _moveAction;
    InputAction _interactAction;
    InputAction _reportAction;
    InputAction _restoreAction;
    float _moveInput;
    IInteractable _currentInteractable;
    GameObject _currentInteractableObject;
    IReportGhostActivity _currentReportable;
    GameObject _currentReportableObject;
    IRestorable _currentRestorable;
    GameObject _currentRestorableObject;
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
        ReportGhostActivityTrigger.OnGhostReported += OnGhostReported;
        if (inputActions == null) return;
        var playerMap = inputActions.FindActionMap("Player");
        _moveAction = playerMap.FindAction("Move");
        _interactAction = playerMap.FindAction("Interact");
        _reportAction = playerMap.FindAction("Report");
        _restoreAction = playerMap.FindAction("Restore");
        _moveAction?.Enable();
        _interactAction?.Enable();
        _reportAction?.Enable();
        _restoreAction?.Enable();
        if (_interactAction != null)
            _interactAction.performed += OnInteractPerformed;
        if (_reportAction != null)
            _reportAction.performed += OnReportPerformed;
        if (_restoreAction != null)
            _restoreAction.performed += OnRestorePerformed;
    }

    void OnGhostReported(Transform target)
    {
        Debug.Log($"[Screenshot] OnGhostReported for {target?.name}, RawImage={(screenshotRawImage != null ? "set" : "null")}");
        if (screenshotRawImage != null)
            CaptureAndShowScreenshot(target);
    }

    void OnDisable()
    {
        ReportGhostActivityTrigger.OnGhostReported -= OnGhostReported;
        if (_interactAction != null)
            _interactAction.performed -= OnInteractPerformed;
        if (_reportAction != null)
            _reportAction.performed -= OnReportPerformed;
        if (_restoreAction != null)
            _restoreAction.performed -= OnRestorePerformed;
        _moveAction?.Disable();
        _interactAction?.Disable();
        _reportAction?.Disable();
        _restoreAction?.Disable();
    }

    void Update()
    {
        _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>().x : 0f;
        PollTriggers();
        if (hintsRoot != null)
            PositionHints();
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

    void CaptureAndShowScreenshot(Transform target)
    {
        var cam = Camera.main;
        if (cam == null) { Debug.Log("[Screenshot] Aborted - Camera.main null"); return; }
        if (screenshotRawImage == null) { Debug.Log("[Screenshot] Aborted - RawImage null"); return; }

        Debug.Log($"[Screenshot] Making capture for {target?.name}");
        var ghostObjects = new System.Collections.Generic.List<(GameObject go, bool wasActive)>();
        foreach (Transform t in target.GetComponentsInChildren<Transform>(true))
        {
            if (t.CompareTag("Ghost"))
            {
                ghostObjects.Add((t.gameObject, t.gameObject.activeSelf));
                t.gameObject.SetActive(true);
            }
        }
        var rt = new RenderTexture(captureResolution, captureResolution, 24);
        rt.Create();
        var captureCam = new GameObject("ScreenshotCamera").AddComponent<Camera>();
        captureCam.CopyFrom(cam);
        var excludeMask = (1 << gameObject.layer);
        var uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0) excludeMask |= (1 << uiLayer);
        captureCam.cullingMask = cam.cullingMask & ~excludeMask;
        captureCam.clearFlags = CameraClearFlags.SolidColor;
        captureCam.backgroundColor = cam.backgroundColor;
        captureCam.targetTexture = rt;
        captureCam.orthographic = true;
        captureCam.orthographicSize = captureRadius;
        var pos = target.position;
        captureCam.transform.position = new Vector3(pos.x, pos.y, cam.transform.position.z);
        captureCam.transform.rotation = cam.transform.rotation;
        captureCam.Render();
        captureCam.targetTexture = null;
        Destroy(captureCam.gameObject);
        foreach (var (go, wasActive) in ghostObjects)
        {
            if (go != null) go.SetActive(wasActive);
        }

        Debug.Log("[Screenshot] Done - set to RawImage");
        if (screenshotRawImage.texture is RenderTexture prev && prev != rt)
            prev.Release();
        screenshotRawImage.texture = rt;
        screenshotRawImage.uvRect = new Rect(0, 0, 1, 1);
        var parent = screenshotRawImage.rectTransform.parent as RectTransform;
        var photoRoot = parent != null ? parent : screenshotRawImage.rectTransform;
        photoRoot.gameObject.SetActive(true);
        if (screenshotRawImage.canvas != null && screenshotRawImage.canvas.renderMode == RenderMode.WorldSpace)
        {
            var canvas = screenshotRawImage.canvas;
            var canvasRect = canvas.GetComponent<RectTransform>();
            var dist = canvasRect != null ? Vector3.Dot(canvasRect.position - cam.transform.position, cam.transform.forward) : 5f;
            var cornerPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, dist));
            var keepZ = canvasRect != null ? canvasRect.position.z : photoRoot.position.z;
            photoRoot.position = new Vector3(cornerPos.x, cornerPos.y, keepZ);
            photoRoot.localScale = Vector3.one * screenshotWorldScale;
        }
    }

    void OnRestorePerformed(InputAction.CallbackContext _)
    {
        if (_currentRestorable != null)
            _currentRestorable.Restore(gameObject);
    }

    // Camera is in world space - Hints top-right aligns with camera viewport top-right.
    void PositionHints()
    {
        if (hintsRoot == null) return;
        var cam = Camera.main;
        if (cam == null) return;
        var canvas = hintsRoot.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var canvasRect = canvas.GetComponent<RectTransform>();
        var dist = canvasRect != null ? Vector3.Dot(canvasRect.position - cam.transform.position, cam.transform.forward) : 5f;
        var pos = cam.ViewportToWorldPoint(new Vector3(1f, 1f, dist));
        var keepZ = canvasRect != null ? canvasRect.position.z : hintsRoot.position.z;
        hintsRoot.pivot = new Vector2(1f, 1f);
        hintsRoot.position = new Vector3(pos.x - hintsPaddingTopRight.x, pos.y - hintsPaddingTopRight.y, keepZ);
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
        _currentRestorable = null;
        _currentRestorableObject = null;
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
            if (_currentRestorable == null)
            {
                var rest = c.GetComponent<IRestorable>() ?? c.GetComponentInParent<IRestorable>() ?? c.GetComponentInChildren<IRestorable>();
                if (rest != null) { _currentRestorable = rest; _currentRestorableObject = c.gameObject; }
            }
        }
        if (hintsRoot == null) return;
        var reportTrigger = _currentReportableObject != null
            ? (_currentReportableObject.GetComponent<ReportGhostActivityTrigger>() ?? _currentReportableObject.GetComponentInParent<ReportGhostActivityTrigger>() ?? _currentReportableObject.GetComponentInChildren<ReportGhostActivityTrigger>())
            : null;
        var restoreTrigger = _currentRestorableObject != null
            ? (_currentRestorableObject.GetComponent<ReportGhostActivityTrigger>() ?? _currentRestorableObject.GetComponentInParent<ReportGhostActivityTrigger>() ?? _currentRestorableObject.GetComponentInChildren<ReportGhostActivityTrigger>())
            : null;
        var showE = _currentInteractable != null && _currentInteractableObject != null;
        var showF = reportTrigger != null && reportTrigger.Reportable;
        var showQ = restoreTrigger != null && restoreTrigger.HasRestore;

        var eText = showE && _currentInteractableObject != null
            ? ((_currentInteractableObject.GetComponent<HintTrigger>() ?? _currentInteractableObject.GetComponentInParent<HintTrigger>() ?? _currentInteractableObject.GetComponentInChildren<HintTrigger>())?.HintText ?? defaultHintText)
            : defaultHintText;
        if (hintsE != null) { hintsE.gameObject.SetActive(showE); if (showE) hintsE.text = eText; }
        if (hintsF != null) { hintsF.gameObject.SetActive(showF); if (showF) hintsF.text = reportTrigger?.HintText ?? "Press F"; }
        if (hintsQ != null) { hintsQ.gameObject.SetActive(showQ); if (showQ) hintsQ.text = restoreTrigger?.RestoreHintText ?? "Press Q"; }
        hintsRoot.gameObject.SetActive(showE || showF || showQ);
    }
}

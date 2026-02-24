using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GhostReveal : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Visibility")]
    [Tooltip("How long the photo / ghost reveal is shown.")]
    [SerializeField] private float revealDuration = 2f;

    [Header("Photo")]
    [Tooltip("Optional: picture of the player's room is shown here. Ghost appears in the picture only if he is in the same room.")]
    [SerializeField] private RawImage photoDisplay;
    [Tooltip("Parent of photoDisplay to show/hide (e.g. full-screen panel). If null, only photoDisplay is toggled.")]
    [SerializeField] private GameObject photoPanelRoot;
    [Tooltip("Camera for centering the photo panel in World Space. If unset, uses Camera.main.")]
    [SerializeField] private Camera worldSpaceCamera;

    [Header("Audio")]
    [Tooltip("Optional sound to play on reveal.")]
    [SerializeField] private AudioClip revealSound;

    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private InputAction _revealAction;
    private Coroutine _revealCoroutine;
    private RenderTexture _captureRt;
    private RenderTexture _previousTarget;

    /// <summary>True while the screenshot preview is visible. Other scripts (e.g. CharacterControl) can check this to block input.</summary>
    public static bool IsPreviewingPhoto { get; private set; }

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

    void LateUpdate()
    {
        if (photoPanelRoot != null && photoPanelRoot.activeSelf)
            CenterPhotoPanelInView();
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
        bool sameRoom = false;
        if (GameManager.Instance != null)
        {
            Vector2 ghostPos = transform.position;
            var player = FindFirstObjectByType<CharacterControl>();
            if (player != null)
            {
                Vector2 playerPos = player.transform.position;
                int ghostRoom = GameManager.Instance.GetRoomIdAtPosition(ghostPos);
                int playerRoom = GameManager.Instance.GetRoomIdAtPosition(playerPos);
                sameRoom = ghostRoom == playerRoom && ghostRoom != 0;
                Debug.Log($"[GhostReveal] Ghost room={ghostRoom}, Player room={playerRoom}, same panel={sameRoom}");
            }
            else
                Debug.Log("[GhostReveal] Player (CharacterControl) not found");
        }

        // Show ghost in the world only if he is in the same room (so the capture will include him)
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = sameRoom;

        if (revealSound != null && _audioSource != null)
            _audioSource.PlayOneShot(revealSound);

        // Capture the room; when sameRoom (player won) we don't show the in-game preview
        DoCapture();
        if (GameManager.Instance != null)
        {
            var player = FindFirstObjectByType<CharacterControl>();
            if (player != null)
            {
                if (sameRoom)
                {
                    GameManager.SetScreenshotFromRenderTexture(_captureRt);
                    GameManager.Instance.OnCorrectGuess();
                }
                else
                {
                    GameManager.SetScreenshotFromRenderTexture(_captureRt);
                    GameManager.Instance.OnWrongGuess();
                }
            }
        }

        if (sameRoom)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = false;
            _revealCoroutine = null;
            yield break;
        }

        ShowPhotoPanel();
        yield return new WaitForSeconds(revealDuration);

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = false;
        HidePhoto();

        _revealCoroutine = null;
    }

    /// <summary>Captures the current room into _captureRt. Does not show the photo panel.</summary>
    void DoCapture()
    {
        var cam = Camera.main;
        if (cam == null) return;

        int side = Mathf.Max(64, Mathf.Max(Screen.width, Screen.height));
        if (_captureRt == null || _captureRt.width != side || _captureRt.height != side)
        {
            if (_captureRt != null) _captureRt.Release();
            _captureRt = new RenderTexture(side, side, 24);
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        int excludeMask = (uiLayer >= 0) ? (1 << uiLayer) : 0;
        int cullingMask = cam.cullingMask & ~excludeMask;

        bool usedRoomCapture = false;
        if (GameManager.Instance != null)
        {
            var player = FindFirstObjectByType<CharacterControl>();
            if (player != null)
            {
                int playerRoom = GameManager.Instance.GetRoomIdAtPosition(player.transform.position);
                Rect roomRect = GameManager.Instance.GetRoomRect(playerRoom);
                if (playerRoom >= 1 && roomRect.width > 0.01f && roomRect.height > 0.01f)
                {
                    var roomCam = new GameObject("RoomCaptureCamera").AddComponent<Camera>();
                    roomCam.CopyFrom(cam);
                    roomCam.cullingMask = cullingMask;
                    roomCam.clearFlags = CameraClearFlags.SolidColor;
                    roomCam.backgroundColor = Color.white;
                    roomCam.orthographic = true;
                    float roomSize = Mathf.Max(roomRect.width, roomRect.height);
                    roomCam.orthographicSize = roomSize * 0.5f;
                    Vector3 center = new Vector3(roomRect.x + roomRect.width * 0.5f, roomRect.y + roomRect.height * 0.5f, cam.transform.position.z);
                    roomCam.transform.position = center;
                    roomCam.transform.rotation = cam.transform.rotation;
                    roomCam.aspect = 1f;
                    roomCam.targetTexture = _captureRt;
                    roomCam.Render();
                    roomCam.targetTexture = null;
                    Destroy(roomCam.gameObject);
                    usedRoomCapture = true;
                }
            }
        }

        if (!usedRoomCapture)
        {
            float originalAspect = cam.aspect;
            var originalClearFlags = cam.clearFlags;
            var originalBackgroundColor = cam.backgroundColor;
            int originalCullingMask = cam.cullingMask;

            cam.cullingMask = cullingMask;
            cam.aspect = 1f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;

            _previousTarget = cam.targetTexture;
            cam.targetTexture = _captureRt;
            cam.Render();
            cam.targetTexture = _previousTarget;
            cam.cullingMask = originalCullingMask;
            cam.aspect = originalAspect;
            cam.clearFlags = originalClearFlags;
            cam.backgroundColor = originalBackgroundColor;
        }
    }

    void ShowPhotoPanel()
    {
        if (photoDisplay != null)
        {
            if (photoDisplay.texture is RenderTexture prev && prev != _captureRt)
                prev.Release();
            photoDisplay.texture = _captureRt;
            photoDisplay.gameObject.SetActive(true);
            FitPhotoInPanel();
        }
        if (photoPanelRoot != null)
        {
            photoPanelRoot.SetActive(true);
            CenterPhotoPanelInView();
        }
        IsPreviewingPhoto = true;
        inputActions?.Disable();
    }

    void CenterPhotoPanelInView()
    {
        if (photoPanelRoot == null) return;
        var canvas = photoPanelRoot.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace) return;

        Camera cam = worldSpaceCamera != null ? worldSpaceCamera : Camera.main;
        if (cam == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        float depth = Vector3.Dot(canvasRect.position - cam.transform.position, cam.transform.forward);
        Vector3 worldCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
        photoPanelRoot.transform.position = worldCenter;
    }

    void FitPhotoInPanel()
    {
        if (photoDisplay == null || _captureRt == null) return;

        RectTransform panelRect = photoPanelRoot != null ? photoPanelRoot.transform as RectTransform : photoDisplay.rectTransform.parent as RectTransform;
        if (panelRect == null) return;

        float panelW = panelRect.rect.width;
        float panelH = panelRect.rect.height;
        if (panelW <= 0 || panelH <= 0) return;

        float texAspect = (float)_captureRt.width / _captureRt.height;
        float panelAspect = panelW / panelH;

        float fitW, fitH;
        if (texAspect > panelAspect)
        {
            fitW = panelW;
            fitH = panelW / texAspect;
        }
        else
        {
            fitH = panelH;
            fitW = panelH * texAspect;
        }

        RectTransform rawRect = photoDisplay.rectTransform;
        rawRect.anchorMin = new Vector2(0.5f, 0.5f);
        rawRect.anchorMax = new Vector2(0.5f, 0.5f);
        rawRect.pivot = new Vector2(0.5f, 0.5f);
        rawRect.sizeDelta = new Vector2(fitW, fitH);
        rawRect.anchoredPosition = Vector2.zero;
    }

    void HidePhoto()
    {
        if (photoPanelRoot != null)
            photoPanelRoot.SetActive(false);
        if (photoDisplay != null)
            photoDisplay.gameObject.SetActive(false);
        IsPreviewingPhoto = false;
        inputActions?.Enable();
    }
}

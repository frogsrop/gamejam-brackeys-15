using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<GameObject> visionPanels = new List<GameObject>();

    [Header("HP")]
    [SerializeField] private int hp = 3;
    [Tooltip("Assign the HP UI element (RectTransform) from Canvas; it will be placed at the top-left of the screen.")]
    [SerializeField] private RectTransform hpPanel;
    [SerializeField] private TMP_Text hpAmountText;
    [Tooltip("Offset from the top-left corner (pixels for Overlay/Canvas, local units for World Space).")]
    [SerializeField] private Vector2 topLeftPadding = new Vector2(20f, 20f);
    [Tooltip("Camera used for World Space placement. If unset, uses Camera.main.")]
    [SerializeField] private Camera worldSpaceCamera;

    public int Hp => hp;

    [Header("Win / Lose")]
    [Tooltip("Scene to load on Win or Lose. Result is stored in GameManager.LastGameWon (true = win, false = lose).")]
    [SerializeField] private string winLoseSceneName = "WinLoseScene";

    /// <summary>Set before loading WinLose scene. Read in that scene's Start to know if the player won or lost.</summary>
    public static bool LastGameWon { get; private set; }

    /// <summary>Screenshot passed to WinLose scene (e.g. from GhostReveal). May be null if none was set.</summary>
    public static Texture2D LastScreenshot { get; private set; }

    /// <summary>Copy a render texture to LastScreenshot so the WinLose scene can display it. Call before Win()/Lose() loads the scene.</summary>
    public static void SetScreenshotFromRenderTexture(RenderTexture rt)
    {
        if (LastScreenshot != null)
        {
            Destroy(LastScreenshot);
            LastScreenshot = null;
        }
        if (rt == null || !rt.IsCreated()) return;

        LastScreenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        LastScreenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        LastScreenshot.Apply();
        RenderTexture.active = prev;
    }

    [Header("Debug")]
    [SerializeField] private bool logRoomDetection = true;

    void Awake()
    {
        Instance = this;
        BuildRoomRectsFromPanels();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        ActivateRoom(5);
        PlaceHpAtTopLeft();
        RefreshHpDisplay();
    }

    void LateUpdate()
    {
        if (hpPanel == null) return;
        var canvas = hpPanel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            PlaceHpAtTopLeft();
    }

    void PlaceHpAtTopLeft()
    {
        if (hpPanel == null) return;

        var canvas = hpPanel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            Camera cam = worldSpaceCamera != null ? worldSpaceCamera : Camera.main;
            if (cam == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            // Visible top-left: viewport (0,1) at the canvas plane
            float depth = Vector3.Dot(canvasRect.position - cam.transform.position, cam.transform.forward);
            Vector3 worldTopLeft = cam.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
            // Work in canvas local space so padding and placement match the visible rect
            Vector3 canvasLocalTopLeft = canvasRect.InverseTransformPoint(worldTopLeft);
            Vector3 targetLocal = canvasLocalTopLeft + new Vector3(topLeftPadding.x, -topLeftPadding.y, 0f);
            Vector3 worldTarget = canvasRect.TransformPoint(targetLocal);

            hpPanel.anchorMin = new Vector2(0f, 1f);
            hpPanel.anchorMax = new Vector2(0f, 1f);
            hpPanel.pivot = new Vector2(0f, 1f);
            hpPanel.position = worldTarget;
        }
        else
        {
            hpPanel.anchorMin = new Vector2(0f, 1f);
            hpPanel.anchorMax = new Vector2(0f, 1f);
            hpPanel.pivot = new Vector2(0f, 1f);
            hpPanel.anchoredPosition = new Vector2(topLeftPadding.x, -topLeftPadding.y);
        }
    }

    void RefreshHpDisplay()
    {
        if (hpAmountText != null)
            hpAmountText.text = "<voffset=0.09em>x</voffset>" + hp.ToString();
    }

    /// <summary>Called when the player reports and it was a correct guess (ghost). Override or subscribe from code.</summary>
    public virtual void OnCorrectGuess()
    {
        Win();
    }

    /// <summary>Called when the player reports and it was a wrong guess (non-ghost). Override or subscribe from code.</summary>
    public virtual void OnWrongGuess()
    {        hp = Mathf.Max(0, hp - 1);
        RefreshHpDisplay();
        if (hp == 0)
            Lose();
    }

    /// <summary>Called when the player wins (e.g. correct ghost reveal). Override to show victory, load scene, etc.</summary>
    public virtual void Win()
    {
        LastGameWon = true;
        if (!string.IsNullOrEmpty(winLoseSceneName))
            SceneManager.LoadScene(winLoseSceneName);
    }

    /// <summary>Called when HP reaches zero. Override to show game over, load scene, etc.</summary>
    public virtual void Lose()
    {
        LastGameWon = false;
        if (!string.IsNullOrEmpty(winLoseSceneName))
            SceneManager.LoadScene(winLoseSceneName);
    }

    public void ActivateRoom(int roomId)
    {
        if (roomId > 0 && roomId <= visionPanels.Count)
        {
            var panel = visionPanels[roomId - 1];
            foreach (var p in visionPanels)
            {
                p.SetActive(p != panel);
            }
        }
        else
        {
            Debug.LogError($"Vision panel with index {roomId} not found");
        }
    }

    /// <summary>Room bounds in world space (min.x, min.y, max.x, max.y). Derived from vision panels at Start.</summary>
    public IReadOnlyList<Rect> RoomRects => _roomRects;

    List<Rect> _roomRects = new List<Rect>();

    /// <summary>Build room rectangles from vision panels: each panel's world position and size define that room's bounds.</summary>
    void BuildRoomRectsFromPanels()
    {
        _roomRects.Clear();
        if (visionPanels == null)
        {
            if (logRoomDetection) Debug.Log("[RoomDetection] visionPanels is null");
            return;
        }

        if (logRoomDetection) Debug.Log($"[RoomDetection] Building rects from {visionPanels.Count} panels");

        for (int i = 0; i < visionPanels.Count; i++)
        {
            var panel = visionPanels[i];
            if (panel == null)
            {
                _roomRects.Add(new Rect(0, 0, 0, 0));
                if (logRoomDetection) Debug.Log($"[RoomDetection] Panel {i + 1}: null");
                continue;
            }

            Rect r = GetPanelWorldRect(panel, out string source);
            _roomRects.Add(r);
            if (logRoomDetection)
                Debug.Log($"[RoomDetection] Panel {i + 1} '{panel.name}': rect=({r.x:F2},{r.y:F2}) size=({r.width:F2},{r.height:F2}) from={source}");
        }
    }

    /// <summary>World-space rect for a panel: from SpriteRenderer bounds if present, else from Transform (position ± half scale).</summary>
    static Rect GetPanelWorldRect(GameObject panel, out string source)
    {
        var sr = panel.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            var b = sr.bounds;
            if (b.extents.x > 0.01f && b.extents.y > 0.01f)
            {
                source = "SpriteRenderer";
                return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            }
        }

        var t = panel.transform;
        Vector3 pos = t.position;
        Vector3 scale = t.lossyScale;
        float halfX = scale.x * 0.5f;
        float halfY = scale.y * 0.5f;
        source = "Transform";
        return new Rect(pos.x - halfX, pos.y - halfY, scale.x, scale.y);
    }

    /// <summary>Returns which room (1 to N) contains the world position, or 0 if none. Uses room coordinates from vision panels.</summary>
    public int GetRoomIdAtPosition(Vector2 worldPosition)
    {
        if (_roomRects == null || _roomRects.Count == 0)
        {
            if (logRoomDetection) Debug.Log("[RoomDetection] GetRoomIdAtPosition: no rects built");
            return Node.Outside;
        }

        for (int i = 0; i < _roomRects.Count; i++)
        {
            if (_roomRects[i].Contains(worldPosition))
                return i + 1;
        }
        return Node.Outside;
    }

    /// <summary>True if the world position is inside any vision panel's bounds.</summary>
    public bool IsPositionInPanel(Vector2 worldPosition) => GetRoomIdAtPosition(worldPosition) != Node.Outside;

    /// <summary>True if the transform's position is inside any vision panel's bounds.</summary>
    public bool IsObjectInPanel(Transform t)
    {
        if (t == null) return false;
        return IsPositionInPanel(new Vector2(t.position.x, t.position.y));
    }

    /// <summary>True if the GameObject's position is inside any vision panel's bounds.</summary>
    public bool IsObjectInPanel(GameObject go) => go != null && IsObjectInPanel(go.transform);
}

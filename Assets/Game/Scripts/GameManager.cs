using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> visionPanels = new List<GameObject>();
    [Header("Debug")]
    [SerializeField] private bool logRoomDetection = true;

    void Start() {
        ActivateRoom(5);
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

    void Awake()
    {
        BuildRoomRectsFromPanels();
    }

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
}

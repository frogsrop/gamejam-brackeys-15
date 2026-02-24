using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Node : MonoBehaviour
{
    /// <summary>RoomId value meaning "outside" (not in any room).</summary>
    public const int Outside = 0;

    public List<Node> nodeChildren = new List<Node>();
    public abstract void UpdateNode();
    public abstract void ActivateNode();
    public abstract void RestoreNode();
    public bool activated = false;
    /// <summary>True if this node was activated by ghost event (not normal parent-child flow).</summary>
    public bool activatedByGhost = false;

    [Tooltip("If false, ghost cannot activate this node. True by default.")]
    [SerializeField] private bool ghostActivatable = true;

    /// <summary>If false, ghost cannot activate this node.</summary>
    public bool GhostActivatable => ghostActivatable;

    [Tooltip("Seconds before this node can be activated again. 0 = no cooldown.")]
    [SerializeField] private float cooldownSeconds = 0f;

    float _lastActivatedTime = -9999f;

    /// <summary>True if cooldown has finished (or is 0) and node can be activated.</summary>
    public bool IsCooldownComplete => cooldownSeconds <= 0f || (Time.time - _lastActivatedTime) >= cooldownSeconds;

    /// <summary>Call to activate; records time for cooldown. Root uses this instead of ActivateNode directly.</summary>
    public void Activate()
    {
        _lastActivatedTime = Time.time;
        ActivateNode();
    }

    [Tooltip("0 = outside, 1-6 = room index (matches GameManager vision panels). Used when Auto-detect Room By Panel is off.")]
    [SerializeField] private int roomId = Outside;

    [Tooltip("Optional: multiple room IDs (e.g. for doors spanning two rooms). If set, overrides single roomId for IsInRoom checks.")]
    [SerializeField] private int[] roomIds;

    [Tooltip("If true, room is detected at Start by testing this object's position against GameManager vision panels (Collider2D, SpriteRenderer bounds, or RectTransform).")]
    [SerializeField] private bool autoDetectRoomByPanel = true;

    private int _cachedRoomId = -1;

    /// <summary>Primary room index (0 = outside, 1-6). When roomIds is set, returns first entry.</summary>
    public int RoomId
    {
        get
        {
            if (roomIds != null && roomIds.Length > 0) return roomIds[0];
            return (autoDetectRoomByPanel && _cachedRoomId >= 0) ? _cachedRoomId : roomId;
        }
    }
    /// <summary>True if this node is not assigned to any room (roomId == 0).</summary>
    public bool IsOutside => RoomId == Outside;
    /// <summary>True if this node is in the given room (id 1-6). Uses roomIds when set, else single roomId.</summary>
    public bool IsInRoom(int id)
    {
        if (id <= 0) return false;
        if (roomIds != null && roomIds.Length > 0)
        {
            foreach (int r in roomIds) if (r == id) return true;
            return false;
        }
        return id == ((autoDetectRoomByPanel && _cachedRoomId >= 0) ? _cachedRoomId : roomId);
    }

    protected virtual void Awake()
    {
        if (autoDetectRoomByPanel)
            RefreshRoomFromPanels();
    }

    /// <summary>Re-run room detection from GameManager (room coordinates from vision panels).</summary>
    public void RefreshRoomFromPanels()
    {
        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning($"[RoomDetection] Node '{name}': GameManager not found");
            return;
        }

        var pos = transform.position;
        Vector2 worldPos = new Vector2(pos.x, pos.y);
        _cachedRoomId = gm.GetRoomIdAtPosition(worldPos);
        roomId = _cachedRoomId;
        Debug.Log($"[RoomDetection] Node '{name}' at ({worldPos.x:F2}, {worldPos.y:F2}) -> room {_cachedRoomId}");
    }
}

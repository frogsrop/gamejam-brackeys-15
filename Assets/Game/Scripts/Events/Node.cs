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

    [Tooltip("0 = outside, 1-6 = room index (matches GameManager vision panels). Used when Auto-detect Room By Panel is off.")]
    [SerializeField] private int roomId = Outside;

    [Tooltip("If true, room is detected at Start by testing this object's position against GameManager vision panels (Collider2D, SpriteRenderer bounds, or RectTransform).")]
    [SerializeField] private bool autoDetectRoomByPanel = true;

    private int _cachedRoomId = -1;

    /// <summary>Room index (0 = outside, 1-6 = room). Matches GameManager.ActivateRoom(roomId).</summary>
    public int RoomId => (autoDetectRoomByPanel && _cachedRoomId >= 0) ? _cachedRoomId : roomId;
    /// <summary>True if this node is not assigned to any room (roomId == 0).</summary>
    public bool IsOutside => RoomId == Outside;
    /// <summary>True if this node is in the given room (id 1-6).</summary>
    public bool IsInRoom(int id) => id > 0 && id == RoomId;

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

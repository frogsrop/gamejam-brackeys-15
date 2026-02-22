using UnityEngine;

/// <summary>
/// Implement on objects that can be restored (Q key when in range).
/// Add a trigger collider so the character can enter range.
/// </summary>
public interface IRestorable
{
    void Restore(GameObject restorer);
}

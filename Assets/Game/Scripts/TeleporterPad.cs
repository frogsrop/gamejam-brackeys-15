using UnityEngine;

/// <summary>
/// Attach to a teleporter pad (trigger child). Implements IInteractable so the character
/// can activate this pad when pressing E. Requires a Teleporter on a parent object.
/// </summary>
public class TeleporterPad : MonoBehaviour, IInteractable
{
    public void Interact(GameObject interactor)
    {
        var teleporter = GetComponentInParent<Teleporter>();
        if (teleporter != null)
            teleporter.Teleport(gameObject, interactor);
    }
}

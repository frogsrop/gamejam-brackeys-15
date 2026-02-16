using UnityEngine;

/// <summary>
/// Implement on any object that can be interacted with (E key when in range).
/// Add a trigger collider so the character can enter range and see the hint.
/// </summary>
public interface IInteractable
{
    void Interact(GameObject interactor);
}

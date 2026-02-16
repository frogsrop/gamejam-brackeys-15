using UnityEngine;

/// <summary>
/// Optional component on a trigger collider. When the character enters, the hint panel shows this text instead of the default.
/// </summary>
public class HintTrigger : MonoBehaviour
{
    [SerializeField] private string hintText = "Press E to interact";

    public string HintText => hintText;
}

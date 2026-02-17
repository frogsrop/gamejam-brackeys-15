using UnityEngine;
using UnityEngine.Events;

public class Probability : MonoBehaviour
{
    [Tooltip("Probability (0-1) that the output event fires when triggered.")]
    [Range(0f, 1f)]
    [SerializeField] private float probability = 1f;

    [Tooltip("Fired when the probability check passes.")]
    [SerializeField] private UnityEvent onOutput;

    /// <summary>
    /// Call this from any UnityEvent. The output fires only if
    /// the random check passes.
    /// </summary>
    public void Trigger()
    {
        if (Random.value <= probability)
        {
            onOutput?.Invoke();
        }
    }
}

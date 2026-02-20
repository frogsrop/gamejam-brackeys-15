using UnityEngine;
using UnityEngine.Events;

public class Delay : MonoBehaviour
{
    [Tooltip("Delay in seconds before the output event fires.")]
    [SerializeField] private float delay = 1f;

    [Tooltip("Fired after the delay elapses.")]
    [SerializeField] private UnityEvent onOutput;

    /// <summary>
    /// Call this from another component's UnityEvent (e.g. Oscillator.OnSignal,
    /// OscillatorListener.OnTriggered) to start the delayed trigger.
    /// </summary>
    public void Trigger()
    {
        StartCoroutine(DelayedInvoke());
    }

    private System.Collections.IEnumerator DelayedInvoke()
    {
        yield return new WaitForSeconds(delay);
        onOutput?.Invoke();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ListenerEntry
{
    [Tooltip("Probability (0-1) that this event fires when triggered.")]
    [Range(0f, 1f)]
    public float probability = 1f;

    [Tooltip("Delay in seconds before the event fires (0 = immediate).")]
    public float delay = 0f;

    [Tooltip("The event to fire.")]
    public UnityEvent onOutput;
}

public class Listener : MonoBehaviour
{
    [Tooltip("Each entry fires independently with its own probability and delay.")]
    [SerializeField] private List<ListenerEntry> entries = new List<ListenerEntry>();

    /// <summary>
    /// Call this from an Oscillator's OnSignal event (or from another
    /// component's UnityEvent to create a chain).
    /// Each entry is evaluated independently for probability and delay.
    /// </summary>
    public void Trigger()
    {
        foreach (var entry in entries)
        {
            if (entry.onOutput == null) continue;

            if (Random.value <= entry.probability)
            {
                if (entry.delay > 0f)
                    StartCoroutine(DelayedInvoke(entry));
                else
                    entry.onOutput.Invoke();
            }
        }
    }

    private IEnumerator DelayedInvoke(ListenerEntry entry)
    {
        yield return new WaitForSeconds(entry.delay);
        entry.onOutput?.Invoke();
    }
}

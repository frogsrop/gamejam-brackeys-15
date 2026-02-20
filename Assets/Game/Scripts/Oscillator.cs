using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Oscillator : MonoBehaviour
{
    [Tooltip("Time in seconds between each signal.")]
    [SerializeField] private float period = 1f;

    [Tooltip("Random spread in seconds. Each tick's interval is offset by a random value in [-spread, +spread].")]
    [SerializeField] private float spread = 0f;

    [Tooltip("Each entry fires independently with its own probability and delay on every tick.")]
    [SerializeField] private List<ListenerEntry> entries = new List<ListenerEntry>();

    private float timer;
    private float currentInterval;

    void OnEnable()
    {
        currentInterval = GetRandomizedInterval();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= currentInterval)
        {
            timer -= currentInterval;
            currentInterval = GetRandomizedInterval();
            Debug.Log($"[Oscillator] '{gameObject.name}' emitted signal");
            FireEntries();
        }
    }

    /// <summary>
    /// Call from external code to force an immediate signal.
    /// </summary>
    public void ForceSignal()
    {
        timer = 0f;
        currentInterval = GetRandomizedInterval();
        Debug.Log($"[Oscillator] '{gameObject.name}' emitted signal (forced)");
        FireEntries();
    }

    private void FireEntries()
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

    private float GetRandomizedInterval()
    {
        return Mathf.Max(0f, period + Random.Range(-spread, spread));
    }
}

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add to a node (or any object) with a trigger collider to allow reporting ghost activity with F.
/// </summary>
public class ReportGhostActivityTrigger : MonoBehaviour, IReportGhostActivity
{
    [Tooltip("Shown in the hint panel when in range.")]
    [SerializeField] private string hintText = "Press F to report ghost activity";
    public string HintText => hintText;

    [Tooltip("Invoked when the player reports ghost activity here (F key).")]
    public UnityEvent<GameObject> onReported;

    public void ReportGhostActivity(GameObject reporter)
    {
        Debug.Log($"[Report] Ghost activity reported at {gameObject.name} by {reporter.name}");
        onReported?.Invoke(reporter);
    }
}

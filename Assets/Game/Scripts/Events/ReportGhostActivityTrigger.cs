using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add to a node (or any object) with a trigger collider.
/// F = report ghost activity, Q = restore linked node.
/// </summary>
public class ReportGhostActivityTrigger : MonoBehaviour, IReportGhostActivity, IRestorable
{
    /// <summary>Invoked when user reports. Args: node transform, wasGhost, feedback text (ReportedGhostText or ReportedWrongText).</summary>
    public static event System.Action<Transform, bool, string> OnReported;
    [Tooltip("Shown in the hint panel when in range.")]
    [SerializeField] private string hintText = "Press F to report ghost activity";
    public string HintText => hintText;

    [Tooltip("Shown when this trigger also supports restore (Q).")]
    [SerializeField] private string restoreHintText = "Press Q to restore";

    [Tooltip("Text for when user reported but node was not ghost-activated.")]
    [SerializeField] private string reportedWrongText = "";

    [Tooltip("Text for when user reported a ghost-activated node.")]
    [SerializeField] private string reportedGhostText = "";

    [Tooltip("Node for both Report (F) and Restore (Q). If empty, auto-finds Node on this object or parent.")]
    [SerializeField] private Node node;

    [Tooltip("If false, report (F) is disabled and hint is hidden.")]
    [SerializeField] private bool reportable = true;

    [Tooltip("Invoked when the player reports ghost activity here (F key).")]
    public UnityEvent<GameObject> onReported;

    public bool Reportable => reportable && ResolvedNode != null && ResolvedNode.activated;

    Node ResolvedNode => node != null ? node : (node = GetComponent<Node>() ?? GetComponentInParent<Node>() ?? GetComponentInChildren<Node>());
    public Node ReportedNode => ResolvedNode;
    public string RestoreHintText => restoreHintText;
    public string ReportedWrongText => reportedWrongText;
    public string ReportedGhostText => reportedGhostText;
    public bool HasRestore => ResolvedNode != null && ResolvedNode.activated;

    public void ReportGhostActivity(GameObject reporter)
    {
        if (!Reportable) return;
        var n = ResolvedNode;
        var wasGhost = n != null && n.activatedByGhost;
        Debug.Log($"[Report] Ghost activity reported at {gameObject.name} by {reporter.name}, wasGhost={wasGhost}, node={n?.name}");
        if (n != null)
        {
            var hasListeners = OnReported != null;
            Debug.Log($"[Report] Invoking OnReported for node {n.name}, wasGhost={wasGhost}, hasListeners={hasListeners}");
            var feedbackText = wasGhost ? ReportedGhostText : ReportedWrongText;
            OnReported?.Invoke(n.transform, wasGhost, feedbackText);
        }
        onReported?.Invoke(reporter);
    }

    public void Restore(GameObject restorer)
    {
        var n = ResolvedNode;
        if (n != null && n.activated)
        {
            n.RestoreNode();
            Debug.Log($"[Restore] {n.name} restored by {restorer.name}");
        }
    }
}

using UnityEngine;

/// <summary>
/// Implement on objects that can be used to report ghost activity (F key when in range).
/// Add a trigger collider so the character can enter range.
/// </summary>
public interface IReportGhostActivity
{
    void ReportGhostActivity(GameObject reporter);
}

using UnityEngine;

public class AlwaysActiveNode : Node
{
    void Awake() {}

    public override void ActivateNode() { }
    public override void RestoreNode() { activatedByGhost = false; }
    public override void UpdateNode() { }
}

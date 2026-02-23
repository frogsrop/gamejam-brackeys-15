using UnityEngine;

public class AlwaysActiveNode : Node
{
    protected override void Awake() { base.Awake(); }

    public override void ActivateNode() { }
    public override void RestoreNode() { activatedByGhost = false; }
    public override void UpdateNode() { }
}

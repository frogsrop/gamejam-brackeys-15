using UnityEngine;

public class AlwaysActiveNode : Node
{
    void Awake() {}

    public override void ActivateNode() { }
    public override void RestoreNode() { }
    public override void UpdateNode() { }
}

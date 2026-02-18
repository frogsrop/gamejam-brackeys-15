using UnityEngine;

public class Root : Node
{
    public override void RunNode() {}

    void Start() {
        foreach (var child in children) {
            child.RunNode();
        }
    }
}

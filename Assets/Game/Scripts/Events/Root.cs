using UnityEngine;
using System;
using System.Collections.Generic;
#nullable enable

public class Root : Node
{
    public override void ActivateNode() {}
    public override void RestoreNode() {}
    public override void UpdateNode() {}

    readonly List<Node> _activatedNodes = new List<Node>();
    readonly List<Node> _childrenOfActivated = new List<Node>();

    public IReadOnlyList<Node> activatedNodes => _activatedNodes;

    public IReadOnlyList<Node> childrenOfActivatedNodes => _childrenOfActivated;

    public Func<IReadOnlyList<Node>, Root, Node?> selectionStrategy = (candidates, _) => {
            if (candidates == null || candidates.Count == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        };

    [Header("Debug")]
    [Tooltip("Draw node tree in Scene view (Gizmos).")]
    public bool drawTreeGizmos = true;
    [HideInInspector] public Color lineColorActivatedToActivated = Color.green;
    [HideInInspector] public Color lineColorActivatedToInactive = Color.yellow;
    [HideInInspector] public Color lineColorInactiveToInactive = Color.red;
    [HideInInspector] public Color lineColorActive = new Color(0.2f, 1f, 0.3f, 1f);
    [HideInInspector] public Color lineColorInactive = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    public float nodeSphereRadius = 0.15f;

    [Header("Tick")]
    [Tooltip("Seconds between each tree tick (RunSelector, etc.).")]
    public float tickIntervalSeconds = 1f;
    [Tooltip("Seconds before first tick.")]
    public float tickDelaySeconds = 0f;

    void Start() {
        InvokeRepeating(nameof(Tick), tickDelaySeconds, tickIntervalSeconds);
    }

    void Tick() {
        WalkTreeAndUpdateNodes(this);
        RunSelector();
        SelectFromChildrenOfActivated()?.ActivateNode();
    }

    void WalkTreeAndUpdateNodes(Node node) {
        if (node != this) node.UpdateNode();
        foreach (var child in node.nodeChildren)
            WalkTreeAndUpdateNodes(child);
    }

    void RunSelector() {
        _activatedNodes.Clear();
        _childrenOfActivated.Clear();
        CollectActivated(this);
        BuildChildrenOfActivated();
        Debug.Log($"[Root] Activated: {string.Join(", ", System.Linq.Enumerable.Select(_activatedNodes, n => n.gameObject.name))}");
    }

    void CollectActivated(Node node) {
        if (node != this && node.activated)
            _activatedNodes.Add(node);
        foreach (var child in node.nodeChildren)
            CollectActivated(child);
    }

    void BuildChildrenOfActivated() {
        foreach (var node in _activatedNodes) {
            foreach (var child in node.nodeChildren) {
                if (!child.activated && !_childrenOfActivated.Contains(child))
                    _childrenOfActivated.Add(child);
            }
        }
    }
    public Node? SelectFromChildrenOfActivated() {
        if (_childrenOfActivated.Count == 0) return null;
        if (selectionStrategy != null) return selectionStrategy(_childrenOfActivated, this);
        return null;
    }

    void OnDrawGizmos() {
        if (!drawTreeGizmos) return;
        DrawTree(this, null);
    }

    void DrawTree(Node node, Node? parent) {
        if (node == null) return;
        if (node == this) {
            foreach (var child in node.nodeChildren) {
                if (child != null) DrawTree(child, node);
            }
            return;
        }
        Vector3 pos = node.transform.position;
        if (parent != null && parent != this) {
            Gizmos.color = parent.activated && node.activated ? lineColorActivatedToActivated
                : parent.activated && !node.activated ? lineColorActivatedToInactive
                : !parent.activated && !node.activated ? lineColorInactiveToInactive
                : lineColorActivatedToInactive; // inactive -> activated: use yellow
            Gizmos.DrawLine(parent.transform.position, pos);
        }
        bool hasActivatedParent = parent != null && parent != this && parent.activated;
        Gizmos.color = node.activated ? Color.green : hasActivatedParent ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(pos, nodeSphereRadius);
        foreach (var child in node.nodeChildren) {
            if (child != null) DrawTree(child, node);
        }
    }
}

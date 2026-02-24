using UnityEngine;
using System;
using System.Collections.Generic;
#nullable enable

public class Root : Node
{
    public override void ActivateNode() {}
    public override void RestoreNode() { activatedByGhost = false; }
    public override void UpdateNode() {}

    readonly List<Node> _activatedNodes = new List<Node>();
    readonly List<Node> _childrenOfActivated = new List<Node>();
    readonly List<Node> _nodesWithNoActiveParent = new List<Node>();
    readonly Dictionary<Node, float> _ghostNormalizeTimers = new Dictionary<Node, float>();

    public IReadOnlyList<Node> activatedNodes => _activatedNodes;

    public IReadOnlyList<Node> childrenOfActivatedNodes => _childrenOfActivated;

    public Func<IReadOnlyList<Node>, Root, Node?> selectionStrategy = (candidates, _) => {
            if (candidates == null || candidates.Count == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        };

    public Func<IReadOnlyList<Node>, Root, Node?> ghostSelectionStrategy = (candidates, _) => {
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
    [Range(0f, 1f)]
    [Tooltip("Chance per tick to run Ghost event (activate node with no active parent). 0 = normal only, 1 = ghost only.")]
    public float ghostEventChance = 0f;
    [Tooltip("Seconds until ghost nodes become normal when they have an active non-ghost parent. -1 = never.")]
    public float ghostToNormalSeconds = -1f;

    [Header("Non-Ghost Selection Strategy")]
    [Tooltip("Probability: prefer node that has a child activated by ghost. Can move Player and Random.")]
    [Range(0f, 1f)] [SerializeField] private float probHasGhostChild = 0.33f;
    [Tooltip("Probability: prefer node (or node with child) in player's room. Can only move Random.")]
    [Range(0f, 1f)] [SerializeField] private float probInPlayerRoom = 0.33f;
    [Tooltip("Random (remainder). Read-only, computed from above.")]
    [SerializeField] [ReadOnly] private float probRandom = 0.34f;

    float ProbRandom => probRandom;

    void OnValidate()
    {
        probInPlayerRoom = Mathf.Clamp(probInPlayerRoom, 0f, 1f - probHasGhostChild);
        probRandom = Mathf.Max(0f, 1f - probHasGhostChild - probInPlayerRoom);
    }

    void Start()
    {
        InvokeRepeating(nameof(Tick), tickDelaySeconds, tickIntervalSeconds);
    }

    void Update() {
        var toRemove = new List<Node>();
        var snapshot = new List<KeyValuePair<Node, float>>(_ghostNormalizeTimers);
        foreach (var kv in snapshot) {
             var node = kv.Key;
            if (!node.activated) { toRemove.Add(node); continue; }
            var remaining = kv.Value - Time.deltaTime;
            if (remaining <= 0f) {
                node.activatedByGhost = false;
                toRemove.Add(node);
            } else {
                _ghostNormalizeTimers[node] = remaining;
            }
        }
        foreach (var n in toRemove) _ghostNormalizeTimers.Remove(n);
    }

    void Tick() {
        WalkTreeAndUpdateNodes(this);
        RunSelector();
        BuildNodesWithNoActiveParent();
        var ghost = UnityEngine.Random.value < ghostEventChance;
        if (ghost) {
            var n = SelectFromNodesWithNoActiveParent();
            if (n != null) { n.activatedByGhost = true; n.ActivateNode(); }
        } else {
            var n = SelectFromChildrenOfActivated();
            if (n != null) { n.activatedByGhost = false; n.ActivateNode(); }
        }
        StartGhostNormalizeTimers();
    }

    void StartGhostNormalizeTimers() {
        void Walk(Node node, Node? parent) {
            if (node == this) {
                foreach (var c in node.nodeChildren) Walk(c, this);
                return;
            }
            var directParentIsActiveNonGhost = parent != null && parent != this && parent.activated && !parent.activatedByGhost;
            if (node.activatedByGhost && node.activated && ghostToNormalSeconds >= 0f && directParentIsActiveNonGhost) {
                if (!_ghostNormalizeTimers.ContainsKey(node))
                    _ghostNormalizeTimers[node] = ghostToNormalSeconds;
            }
            foreach (var c in node.nodeChildren)
                Walk(c, node);
        }
        Walk(this, null);
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
        var candidates = PickCandidatesByStrategy();
        if (candidates.Count == 0) return null;
        if (selectionStrategy != null) return selectionStrategy(candidates, this);
        return null;
    }

    List<Node> PickCandidatesByStrategy() {
        var list1 = BuildNodesWithGhostActivatedChild();
        var list2 = BuildNodesInPlayerRoom();
        float p1 = probHasGhostChild;
        float p2 = probInPlayerRoom;
        float p3 = ProbRandom;
        float r = UnityEngine.Random.value;
        if (r < p1 && list1.Count > 0) return list1;
        if (r < p1 + p2 && list2.Count > 0) return list2;
        return _childrenOfActivated;
    }

    List<Node> BuildNodesWithGhostActivatedChild() {
        var result = new List<Node>();
        foreach (var node in _childrenOfActivated) {
            foreach (var child in node.nodeChildren) {
                if (child.activatedByGhost) {
                    result.Add(node);
                    break;
                }
            }
        }
        return result;
    }

    List<Node> BuildNodesInPlayerRoom() {
        var result = new List<Node>();
        var player = FindFirstObjectByType<CharacterControl>();
        if (player == null || GameManager.Instance == null) return result;
        int playerRoom = GameManager.Instance.GetRoomIdAtPosition(player.transform.position);
        if (playerRoom == 0) return result;
        foreach (var node in _childrenOfActivated) {
            if (NodeOrDescendantInRoom(node, playerRoom))
                result.Add(node);
        }
        return result;
    }

    bool NodeOrDescendantInRoom(Node node, int room) {
        if (node.IsInRoom(room)) return true;
        foreach (var child in node.nodeChildren) {
            if (NodeOrDescendantInRoom(child, room)) return true;
        }
        return false;
    }

    void BuildNodesWithNoActiveParent() {
        _nodesWithNoActiveParent.Clear();
        CollectNodesWithNoActiveParent(this, null);
    }

    void CollectNodesWithNoActiveParent(Node node, Node? parent) {
        if (node == this) {
            foreach (var child in node.nodeChildren)
                CollectNodesWithNoActiveParent(child, this);
            return;
        }
        if (!node.activated) {
            var parentIsActive = parent != null && parent != this && parent.activated;
            if (!parentIsActive)
                _nodesWithNoActiveParent.Add(node);
        }
        foreach (var child in node.nodeChildren)
            CollectNodesWithNoActiveParent(child, node);
    }

    public Node? SelectFromNodesWithNoActiveParent() {
        if (_nodesWithNoActiveParent.Count == 0) return null;
        var candidates = FilterOutNodesInPlayerRoom(_nodesWithNoActiveParent);
        if (candidates.Count == 0) return null;
        if (ghostSelectionStrategy != null) return ghostSelectionStrategy(candidates, this);
        return null;
    }

    /// <summary>Excludes nodes that are in the same room as the player. Ghost should not activate in player's room.</summary>
    List<Node> FilterOutNodesInPlayerRoom(List<Node> nodes) {
        if (nodes == null || nodes.Count == 0) return nodes;
        var player = FindFirstObjectByType<CharacterControl>();
        if (player == null || GameManager.Instance == null) return nodes;
        int playerRoom = GameManager.Instance.GetRoomIdAtPosition(player.transform.position);
        if (playerRoom == 0) return nodes;
        var filtered = new List<Node>();
        foreach (var n in nodes) {
            if (!n.IsInRoom(playerRoom))
                filtered.Add(n);
        }
        return filtered;
    }

    public void RestoreActivatedNodesNear(Vector3 position, float radius) {
        float rSq = radius * radius;
        foreach (var node in _activatedNodes) {
            if ((node.transform.position - position).sqrMagnitude <= rSq)
                node.RestoreNode();
        }
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
        Gizmos.color = node.activatedByGhost ? Color.white : node.activated ? Color.green : hasActivatedParent ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(pos, nodeSphereRadius);
        foreach (var child in node.nodeChildren) {
            if (child != null) DrawTree(child, node);
        }
    }
}

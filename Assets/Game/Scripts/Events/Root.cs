using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
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
    [Tooltip("Log detailed tick flow to diagnose why a node never activates. Set debugNodeName to focus on a specific node (e.g. train).")]
    [SerializeField] private bool logTickDetails = false;
    [Tooltip("Optional: only log when this node name is relevant. Case-insensitive. E.g. train.")]
    [SerializeField] private string debugNodeName = "";
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

    bool ShouldLog(Node? n) => !string.IsNullOrEmpty(debugNodeName) && n != null &&
        string.Equals(n.gameObject.name, debugNodeName, System.StringComparison.OrdinalIgnoreCase);

    bool ShouldLogAny(System.Collections.Generic.IEnumerable<Node> nodes) {
        if (string.IsNullOrEmpty(debugNodeName)) return false;
        foreach (var n in nodes)
            if (n != null && string.Equals(n.gameObject.name, debugNodeName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    void Tick() {
        WalkTreeAndUpdateNodes(this);
        RunSelector();
        BuildNodesWithNoActiveParent();
        var ghost = UnityEngine.Random.value < ghostEventChance;
        if (logTickDetails) {
            Debug.Log($"[Root] Tick: ghost={ghost}, activated=[{string.Join(", ", System.Linq.Enumerable.Select(_activatedNodes, x => x.gameObject.name))}], " +
                $"childrenOfActivated=[{string.Join(", ", System.Linq.Enumerable.Select(_childrenOfActivated, x => x.gameObject.name))}], " +
                $"nodesWithNoActiveParent=[{string.Join(", ", System.Linq.Enumerable.Select(_nodesWithNoActiveParent, x => x.gameObject.name))}]");
        }
        if (ghost) {
            var n = SelectFromNodesWithNoActiveParent();
            if (logTickDetails || ShouldLog(n))
                Debug.Log($"[Root] Ghost selected: {(n != null ? n.gameObject.name : "null")}");
            if (n != null) { n.activatedByGhost = true; n.Activate(); }
        } else {
            var n = SelectFromChildrenOfActivated();
            if (logTickDetails || ShouldLog(n))
                Debug.Log($"[Root] Normal selected: {(n != null ? n.gameObject.name : "null")}");
            if (n != null) { n.activatedByGhost = false; n.Activate(); }
        }
        StartGhostNormalizeTimers();
    }

    void StartGhostNormalizeTimers() {
        void Walk(Node node, Node? parent) {
            if (node == null) return;
            if (node == this) {
                if (node.nodeChildren != null) {
                    foreach (var c in node.nodeChildren) {
                        if (c != null) Walk(c, this);
                    }
                }
                return;
            }
            var directParentIsActiveNonGhost = parent != null && parent != this && parent.activated && !parent.activatedByGhost;
            if (node.activatedByGhost && node.activated && ghostToNormalSeconds >= 0f && directParentIsActiveNonGhost) {
                if (!_ghostNormalizeTimers.ContainsKey(node))
                    _ghostNormalizeTimers[node] = ghostToNormalSeconds;
            }
            if (node.nodeChildren != null) {
                foreach (var c in node.nodeChildren) {
                    if (c != null) Walk(c, node);
                }
            }
        }
        Walk(this, null);
    }

    void WalkTreeAndUpdateNodes(Node node) {
        if (node == null) return;
        if (node != this) node.UpdateNode();
        if (node.nodeChildren == null) return;
        foreach (var child in node.nodeChildren) {
            if (child != null)
                WalkTreeAndUpdateNodes(child);
        }
    }

    void RunSelector() {
        _activatedNodes.Clear();
        _childrenOfActivated.Clear();
        CollectActivated(this);
        BuildChildrenOfActivated();
        Debug.Log($"[Root] Activated: {string.Join(", ", System.Linq.Enumerable.Select(_activatedNodes, n => n.gameObject.name))}");
    }

    void CollectActivated(Node node) {
        if (node == null) return;
        if (node != this && node.activated)
            _activatedNodes.Add(node);
        if (node.nodeChildren == null) return;
        foreach (var child in node.nodeChildren) {
            if (child != null) CollectActivated(child);
        }
    }

    void BuildChildrenOfActivated() {
        // Root counts as active: its direct children are candidates for normal flow
        if (nodeChildren != null) {
            foreach (var child in nodeChildren) {
                if (child != null && !child.activated && !_childrenOfActivated.Contains(child))
                    _childrenOfActivated.Add(child);
            }
        }
        foreach (var node in _activatedNodes) {
            if (node?.nodeChildren == null) continue;
            foreach (var child in node.nodeChildren) {
                if (child != null && !child.activated && !_childrenOfActivated.Contains(child))
                    _childrenOfActivated.Add(child);
            }
        }
    }
    public Node? SelectFromChildrenOfActivated() {
        if (_childrenOfActivated.Count == 0) return null;
        var candidates = PickCandidatesByStrategy();
        candidates = FilterByCooldown(candidates);
        if (candidates.Count == 0) return null;
        if (!string.IsNullOrEmpty(debugNodeName)) {
            bool inChildren = _childrenOfActivated.Any(c => c != null && string.Equals(c.gameObject.name, debugNodeName, System.StringComparison.OrdinalIgnoreCase));
            bool inCandidates = candidates.Any(c => c != null && string.Equals(c.gameObject.name, debugNodeName, System.StringComparison.OrdinalIgnoreCase));
            if (!inChildren)
                Debug.Log($"[Root] Normal: '{debugNodeName}' not in childrenOfActivated (need parent activated first). Activated={string.Join(", ", _activatedNodes.Select(x => x.gameObject.name))}");
            else if (!inCandidates)
                Debug.Log($"[Root] Normal: '{debugNodeName}' in childrenOfActivated but filtered by strategy. Candidates={string.Join(", ", candidates.Select(x => x.gameObject.name))}");
        }
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
            if (node?.nodeChildren == null) continue;
            foreach (var child in node.nodeChildren) {
                if (child != null && child.activatedByGhost) {
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
        if (node == null) return false;
        if (node.IsInRoom(room)) return true;
        if (node.nodeChildren == null) return false;
        foreach (var child in node.nodeChildren) {
            if (child != null && NodeOrDescendantInRoom(child, room)) return true;
        }
        return false;
    }

    void BuildNodesWithNoActiveParent() {
        _nodesWithNoActiveParent.Clear();
        CollectNodesWithNoActiveParent(this, null);
    }

    void CollectNodesWithNoActiveParent(Node node, Node? parent) {
        if (node == null) return;
        if (node == this) {
            if (node.nodeChildren != null) {
                foreach (var child in node.nodeChildren) {
                    if (child != null) CollectNodesWithNoActiveParent(child, this);
                }
            }
            return;
        }
        if (!node.activated) {
            var parentIsActive = parent != null && parent != this && parent.activated;
            if (!parentIsActive)
                _nodesWithNoActiveParent.Add(node);
        }
        if (node.nodeChildren != null) {
            foreach (var child in node.nodeChildren) {
                if (child != null) CollectNodesWithNoActiveParent(child, node);
            }
        }
    }

    public Node? SelectFromNodesWithNoActiveParent() {
        if (_nodesWithNoActiveParent.Count == 0) return null;
        var afterRoom = FilterOutNodesInPlayerRoom(_nodesWithNoActiveParent);
        var afterGhost = FilterGhostActivatable(afterRoom);
        var candidates = FilterByCooldown(afterGhost);
        if (!string.IsNullOrEmpty(debugNodeName)) {
            foreach (var n in _nodesWithNoActiveParent) {
                if (n == null || !string.Equals(n.gameObject.name, debugNodeName, System.StringComparison.OrdinalIgnoreCase)) continue;
                bool excludedByRoom = !afterRoom.Contains(n);
                bool excludedByGhost = afterRoom.Contains(n) && !afterGhost.Contains(n);
                bool excludedByCooldown = afterGhost.Contains(n) && !candidates.Contains(n);
                Debug.Log($"[Root] Ghost candidate '{n.gameObject.name}': room={(excludedByRoom ? "excluded" : "kept")}, ghostActivatable={(excludedByGhost ? "excluded" : "kept")}, cooldown={(excludedByCooldown ? "excluded" : "kept")}, RoomId={n.RoomId}");
            }
        }
        if (candidates.Count == 0) return null;
        if (ghostSelectionStrategy != null) return ghostSelectionStrategy(candidates, this);
        return null;
    }

    List<Node> FilterGhostActivatable(List<Node> nodes) {
        if (nodes == null || nodes.Count == 0) return nodes;
        var filtered = new List<Node>();
        foreach (var n in nodes) {
            if (n != null && n.GhostActivatable)
                filtered.Add(n);
        }
        return filtered;
    }

    List<Node> FilterByCooldown(List<Node> nodes) {
        if (nodes == null || nodes.Count == 0) return nodes;
        var filtered = new List<Node>();
        foreach (var n in nodes) {
            if (n != null && n.IsCooldownComplete)
                filtered.Add(n);
        }
        return filtered;
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
            if (node.nodeChildren != null) {
                foreach (var child in node.nodeChildren) {
                    if (child != null) DrawTree(child, node);
                }
            }
            return;
        }
        Vector3 pos = node.transform.position;
        if (parent != null && parent != this) {
            bool parentActive = parent.activated;
            Gizmos.color = parentActive && node.activated ? lineColorActivatedToActivated
                : parentActive && !node.activated ? lineColorActivatedToInactive
                : !parentActive && !node.activated ? lineColorInactiveToInactive
                : lineColorActivatedToInactive; // inactive -> activated: use yellow
            Gizmos.DrawLine(parent.transform.position, pos);
        }
        bool hasActivatedParent = parent != null && (parent == this || parent.activated);
        Gizmos.color = node.activatedByGhost ? Color.white : node.activated ? Color.green : hasActivatedParent ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(pos, nodeSphereRadius);
        if (node.nodeChildren != null) {
            foreach (var child in node.nodeChildren) {
                if (child != null) DrawTree(child, node);
            }
        }
    }
}

using UnityEngine;

public class AlwaysActiveNode : Node
{
    [Tooltip("If true, audio plays in a loop while node is active. If false, plays once.")]
    [SerializeField] private bool audioLoop = false;
    [Tooltip("AudioSource to play when node activates. Can be on this object or a child.")]
    [SerializeField] private AudioSource audioSource;

    protected override void Awake() { base.Awake(); }

    public override void UpdateNode()
    {
        if (audioSource == null || !activated) return;
        if (audioLoop && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log($"[AlwaysActiveNode] Audio started (loop) on {gameObject.name}");
        }
    }

    public override void ActivateNode()
    {
        if (audioSource != null)
        {
            audioSource.loop = audioLoop;
            audioSource.Play();
            Debug.Log($"[AlwaysActiveNode] Audio started on {gameObject.name}");
        }
        activated = true;
    }

    public override void RestoreNode()
    {
        activatedByGhost = false;
        if (audioSource != null)
            audioSource.Stop();
        activated = false;
    }
}

using UnityEngine;

#nullable enable
public class AudioAnimationNode : Node
{
    const string MainAnimationName = "MainAnimation";

    [SerializeField] private bool loop = false;
    [Tooltip("If true, audio plays in a loop while node is active. If false, plays once.")]
    [SerializeField] private bool audioLoop = false;
    [Tooltip("Seconds until auto-restore. -1 = never.")]
    [SerializeField] private float deactivationTimer = -1f;
    [Tooltip("If true, restore rewinds to start instead of playing backwards (or jumping to end).")]
    [SerializeField] private bool restoreByRewind = false;
    [Tooltip("AudioSource to play when node activates. Can be on this object or a child.")]
    [SerializeField] private AudioSource audioSource;
    private Animation? animationComponent;
    private Animator? animator;

    protected override void Awake()
    {
        base.Awake();
        animationComponent = GetComponent<Animation>();
        animator = GetComponent<Animator>();
    }

    public override void UpdateNode()
    {
        if (audioSource == null || !activated) return;
        if (audioLoop && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log($"[AudioAnimationNode] Audio started (loop) on {gameObject.name}");
        }
    }

    public override void ActivateNode()
    {
        CancelInvoke(nameof(RestoreNode));
        if (audioSource != null)
        {
            audioSource.loop = audioLoop;
            audioSource.Play();
            Debug.Log($"[AudioAnimationNode] Audio started on {gameObject.name}");
        }
        if (animator != null)
        {
            animator.Play(MainAnimationName, 0, 0f);
        }
        else if (animationComponent != null && animationComponent.clip != null)
        {
            AnimationState state = animationComponent[animationComponent.clip.name];
            state.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            state.speed = 1f;
            animationComponent.Play();
        }
        activated = true;
        if (deactivationTimer >= 0f)
            Invoke(nameof(RestoreNode), deactivationTimer);
    }

    public override void RestoreNode()
    {
        activatedByGhost = false;
        CancelInvoke(nameof(RestoreNode));
        if (audioSource != null)
            audioSource.Stop();
        if (animator != null)
        {
            if (restoreByRewind)
                animator.Rebind();
            else
                animator.Play(MainAnimationName, 0, 1f);
        }
        else if (animationComponent != null && animationComponent.clip != null)
        {
            AnimationState state = animationComponent[animationComponent.clip.name];
            state.wrapMode = WrapMode.Once;
            if (restoreByRewind)
            {
                animationComponent.Stop();
                state.time = 0f;
            }
            else
            {
                state.time = state.length;
                state.speed = -1f;
                animationComponent.Play();
            }
        }
        activated = false;
    }
}

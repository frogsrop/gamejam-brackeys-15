using UnityEngine;

#nullable enable
[RequireComponent(typeof(Animation))]
public class AudioAnimationNode : Node
{    
    [SerializeField] private bool loop = false;
    [Tooltip("Seconds until auto-restore. -1 = never.")]
    [SerializeField] private float deactivationTimer = -1f;
    private Animation animationComponent;
    private AudioSource? audioSource = null;

    void Awake() {
        if (animationComponent == null) 
            animationComponent = GetComponent<Animation>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void UpdateNode() {
        if (audioSource != null && audioSource?.isPlaying == false)
            audioSource?.Play();
    }

    public override void ActivateNode() {
        CancelInvoke(nameof(RestoreNode));
        AnimationState state = animationComponent[animationComponent.clip.name];
        state.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
        state.speed = 1f;
        animationComponent.Play();
        activated = true;
        if (deactivationTimer >= 0f)
            Invoke(nameof(RestoreNode), deactivationTimer);
    }

    public override void RestoreNode()
    {
        activatedByGhost = false;
        CancelInvoke(nameof(RestoreNode));
        AnimationState state = animationComponent[animationComponent.clip.name];
        state.wrapMode = WrapMode.Once;
        state.time = state.length;
        state.speed = -1f;
        animationComponent.Play();
        activated = false;
    }
}

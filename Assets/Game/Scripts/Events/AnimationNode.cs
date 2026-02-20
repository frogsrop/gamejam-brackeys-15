using UnityEngine;

#nullable enable
public class AudioAnimationNode : Node
{    
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
        AnimationState state = animationComponent[animationComponent.clip.name];
        state.speed = 1f;
        animationComponent.Play();
        activated = true;
    }

    public override void RestoreNode()
    {
        AnimationState state = animationComponent[animationComponent.clip.name];
        state.time = state.length;
        state.speed = -1f;
        animationComponent.Play();
        activated = false;
    }
}

using UnityEngine;

public class Rain : Node
{
    AudioSource audioSource;

    void Awake() {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void RunNode()
    { 
        if (!audioSource.isPlaying)
            audioSource.Play();
    } 
}

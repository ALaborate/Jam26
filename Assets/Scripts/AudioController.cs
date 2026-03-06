using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] PlayerView playerView;

    AudioSource audioSource;
    bool inited;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        inited = false;
    }

    private void Update()
    {
        if (!inited && !playerView.Peek.HasValue)
        {
            audioSource.Play();
            inited = true;
            enabled = false;
        }
    }
}

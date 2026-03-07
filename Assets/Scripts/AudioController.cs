using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] PlayerView playerView;

    AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerView.onFinishingQueue.AddListener(StartPlaying);
    }

    void StartPlaying()
    {
        audioSource.Play();
        playerView.onFinishingQueue.RemoveListener(StartPlaying);
    }
}

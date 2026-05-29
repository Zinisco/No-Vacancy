using UnityEngine;

public class StarSoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip starCompleteSound;

    public void PlayStarCompleteSound()
    {
        if (audioSource != null && starCompleteSound != null)
        {
            audioSource.PlayOneShot(starCompleteSound);
        }
    }
}
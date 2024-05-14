using UnityEngine;
using UnityEngine.UI;

public class SoundButton : MonoBehaviour
{
    public Button button;
    public AudioSource audioSource;
    public AudioClip soundClip;
    private bool isPaused = true; // Start as paused

    void Start()
    {
        // Get reference to button component
        button = GetComponent<Button>();

        // Get or add Audio Source component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Assign the audio clip to the Audio Source
        audioSource.clip = soundClip;

        // Add button click listener
        button.onClick.AddListener(ToggleSound);
    }

    void ToggleSound()
    {
        if (!audioSource.isPlaying)
        {
            // If not playing, play the audio clip
            audioSource.Play();
            isPaused = true;
        }
        else
        {
            Debug.Log("Audio is already playing.");
        }
    }
}
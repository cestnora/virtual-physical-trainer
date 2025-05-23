using UnityEngine;

public class DemoBGM : MonoBehaviour
{
    [SerializeField] private AudioSource bgmSource; // BGM AudioSource of Demo scene

    void Awake()
    {
        // Load the saved music volume from PlayerPrefs (or use 1 if not present)
        float vol = PlayerPrefs.GetFloat("musicVolume", 1f);
        if (bgmSource != null)
            bgmSource.volume = vol;
    }
}

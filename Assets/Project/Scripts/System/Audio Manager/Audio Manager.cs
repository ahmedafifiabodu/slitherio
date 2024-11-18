using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private List<AudioClip> audioClips;

    private Dictionary<string, AudioClip> audioClipDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioClips();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioClips()
    {
        audioClipDictionary = new Dictionary<string, AudioClip>();
        foreach (var clip in audioClips)
        {
            audioClipDictionary[clip.name] = clip;
        }
    }

    public void PlayMusic(string clipName)
    {
        if (audioClipDictionary.TryGetValue(clipName, out var clip))
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
        else
        {
            Logging.LogWarning($"Audio clip {clipName} not found!");
        }
    }

    public void PlaySFX(string clipName)
    {
        if (audioClipDictionary.TryGetValue(clipName, out var clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Logging.LogWarning($"Audio clip {clipName} not found!");
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}

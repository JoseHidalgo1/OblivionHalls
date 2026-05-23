using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class MusicTrack
{
    public string name;
    public AudioClip clip;
    public float volume = 1f;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music Tracks")]
    public MusicTrack mainMenuTrack;
    public MusicTrack explorationTrack;
    public MusicTrack bossTrack;
    public MusicTrack winTrack;
    public MusicTrack deathTrack;
    public MusicTrack loadingTrack;

    [Header("Audio Sources")]
    private AudioSource musicSource;
    private Dictionary<string, float> trackVolumes = new Dictionary<string, float>();
    private Dictionary<string, MusicTrack> trackLookup = new Dictionary<string, MusicTrack>(StringComparer.OrdinalIgnoreCase);

    private string currentTrack = "";
    // Track fading state removed because it was assigned but never read.
    private float fadeDuration = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        // Create audio source if it doesn't exist
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        BuildTrackLookup();
        ValidateTrackAssignments();

        // Load saved volumes
        LoadVolumes();
    }

    public static AudioManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        if (!Application.isPlaying)
        {
            return null;
        }

        AudioManager existing = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        Debug.LogWarning("AudioManager.GetOrCreate: No existing AudioManager found in the scene. Creating a temporary fallback instance.");

        GameObject audioObject = new GameObject("AudioManager");
        Instance = audioObject.AddComponent<AudioManager>();
        return Instance;
    }

    /// <summary>
    /// Play a specific music track by name
    /// </summary>
    public void PlayTrack(string trackName)
    {
        if (currentTrack == trackName && musicSource.isPlaying)
            return;

        MusicTrack track = GetTrackByName(trackName);
        if (track == null || track.clip == null)
        {
            Debug.LogWarning($"Track '{trackName}' not found or has no clip assigned");
            return;
        }

        currentTrack = trackName;
        musicSource.clip = track.clip;
        musicSource.volume = GetTrackVolume(trackName);
        musicSource.Play();
    }

    /// <summary>
    /// Stop music with fade out effect
    /// </summary>
    public void StopMusic(float fadeDuration = 1f)
    {
        if (!musicSource.isPlaying)
            return;

        this.fadeDuration = fadeDuration;
        StartCoroutine(FadeOutCoroutine(fadeDuration));
    }

    /// <summary>
    /// Fade from one track to another
    /// </summary>
    public void CrossfadeTrack(string newTrackName, float fadeDuration = 1.5f)
    {
        if (currentTrack == newTrackName && musicSource.isPlaying)
            return;

        this.fadeDuration = fadeDuration;
        StartCoroutine(CrossfadeCoroutine(newTrackName, fadeDuration));
    }

    /// <summary>
    /// Set volume for a specific track (0-1)
    /// </summary>
    public void SetTrackVolume(string trackName, float volume)
    {
        volume = Mathf.Clamp01(volume);
        trackVolumes[trackName] = volume;

        if (currentTrack == trackName && musicSource.isPlaying)
        {
            musicSource.volume = volume;
        }

        SaveVolumes();
    }

    /// <summary>
    /// Get volume for a specific track
    /// </summary>
    public float GetTrackVolume(string trackName)
    {
        if (trackVolumes.ContainsKey(trackName))
            return trackVolumes[trackName];
        return 1f;
    }

    /// <summary>
    /// Get all track names and their volumes
    /// </summary>
    public Dictionary<string, float> GetAllTrackVolumes()
    {
        return new Dictionary<string, float>(trackVolumes);
    }

    private MusicTrack GetTrackByName(string trackName)
    {
        if (string.IsNullOrWhiteSpace(trackName))
            return null;

        if (trackLookup.TryGetValue(trackName, out MusicTrack track))
            return track;

        return null;
    }

    private void BuildTrackLookup()
    {
        trackLookup.Clear();
        AddTrackLookup("MainMenu", mainMenuTrack);
        AddTrackLookup("Exploration", explorationTrack);
        AddTrackLookup("Boss", bossTrack);
        AddTrackLookup("Win", winTrack);
        AddTrackLookup("Death", deathTrack);
        AddTrackLookup("Loading", loadingTrack);
    }

    private void AddTrackLookup(string key, MusicTrack track)
    {
        if (track == null)
            return;

        if (!trackLookup.ContainsKey(key))
            trackLookup.Add(key, track);
    }

    private void ValidateTrackAssignments()
    {
        foreach (var kvp in trackLookup)
        {
            if (kvp.Value.clip == null)
            {
                Debug.LogWarning($"AudioManager: Track '{kvp.Key}' has no AudioClip assigned.");
            }
        }
    }

    private void SaveVolumes()
    {
        foreach (var kvp in trackVolumes)
        {
            PlayerPrefs.SetFloat($"MusicVolume_{kvp.Key}", kvp.Value);
        }
        PlayerPrefs.Save();
    }

    private void LoadVolumes()
    {
        trackVolumes.Clear();
        trackVolumes["MainMenu"] = PlayerPrefs.GetFloat("MusicVolume_MainMenu", 1f);
        trackVolumes["Exploration"] = PlayerPrefs.GetFloat("MusicVolume_Exploration", 1f);
        trackVolumes["Boss"] = PlayerPrefs.GetFloat("MusicVolume_Boss", 0.8f);
        trackVolumes["Win"] = PlayerPrefs.GetFloat("MusicVolume_Win", 1f);
        trackVolumes["Death"] = PlayerPrefs.GetFloat("MusicVolume_Death", 0.9f);
        trackVolumes["Loading"] = PlayerPrefs.GetFloat("MusicVolume_Loading", 1f);
    }

    private System.Collections.IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.Stop();
    }

    private System.Collections.IEnumerator CrossfadeCoroutine(string newTrackName, float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        // Fade out current track
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        // Switch to new track
        PlayTrack(newTrackName);

        // Fade in new track
        float targetVolume = GetTrackVolume(newTrackName);
        elapsed = 0f;
        musicSource.volume = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public bool IsPlaying(string trackName)
    {
        return currentTrack == trackName && musicSource.isPlaying;
    }

    public string GetCurrentTrack()
    {
        return currentTrack;
    }
}

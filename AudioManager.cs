using UnityEngine;

/// <summary>
/// 🎧 Централизованный аудиоменеджер игры.
/// Три дорожки: Master / Music / SFX (разделён на UI и Environment).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Источники звука")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSfxSource;   // 💥 короткие клики
    [SerializeField] private AudioSource envSfxSource;  // 🌆 фоновые шумы (офис, стройка)

    [Header("Громкости")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- Создание источников ---
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (uiSfxSource == null)
        {
            uiSfxSource = gameObject.AddComponent<AudioSource>();
            uiSfxSource.loop = false;
            uiSfxSource.playOnAwake = false;
        }

        if (envSfxSource == null)
        {
            envSfxSource = gameObject.AddComponent<AudioSource>();
            envSfxSource.loop = true; // фоновые шумы — цикличные
            envSfxSource.playOnAwake = false;
        }

        LoadVolumes();
        ApplyVolumes();
    }

    // --- Применение громкости ---
    public void ApplyVolumes()
    {
        AudioListener.volume = masterVolume;

        if (musicSource != null)
            musicSource.volume = musicVolume;

        if (uiSfxSource != null)
            uiSfxSource.volume = sfxVolume;

        if (envSfxSource != null)
            envSfxSource.volume = sfxVolume * 0.9f; // чуть тише фоновых шумов
    }

    // --- Сеттеры ---
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        ApplyVolumes();
    }

    // --- Воспроизведение ---
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic() => musicSource?.Stop();

    /// <summary> 🎛 Короткие звуки интерфейса </summary>
    public void PlayUISFX(AudioClip clip)
    {
        if (clip == null || uiSfxSource == null) return;
        uiSfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary> 🌆 Фоновые шумы (в петле) </summary>
    public void PlayEnvironmentSFX(AudioClip clip)
    {
        if (clip == null || envSfxSource == null) return;
        envSfxSource.clip = clip;
        envSfxSource.loop = true;
        envSfxSource.volume = sfxVolume * 0.9f;
        envSfxSource.Play();
    }

    public void StopEnvironmentSFX() => envSfxSource?.Stop();

    private void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 중앙 오디오 싱글턴 — 모노 2D, 풀 16, 우선순위/쿨다운, BGM 크로스페이드, WebGL 첫 입력 게이트, PlayerPrefs 볼륨 저장.
/// </summary>
public sealed class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    private const int DefaultSfxPoolSize = 16;
    private const float DefaultBgmFadeDuration = 0.3f;
    private const string PrefsMaster = "Audio/Master";
    private const string PrefsBgm = "Audio/BGM";
    private const string PrefsSfx = "Audio/SFX";
    private const string PrefsUi = "Audio/UI";

    public enum Priority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    [Header("설정")]
    [Tooltip("중앙 오디오 설정 SO — 단일 진실 원천")]
    [SerializeField]
    private AudioConfig config;

    [Tooltip("SFX 풀 크기 — WebGL RealVoiceCount 32 이하 권장")]
    [SerializeField, Min(1)]
    private int sfxPoolSize = DefaultSfxPoolSize;

    [Tooltip("BGM 크로스페이드 기본 시간(초)")]
    [SerializeField, Min(0f)]
    private float bgmFadeDuration = DefaultBgmFadeDuration;

    [Header("볼륨 — 인스펙터에서 조절, PlayerPrefs에 저장")]
    [SerializeField, Range(0f, 1f)]
    private float masterVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float bgmVolume = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float sfxVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float uiVolume = 1f;

    private AudioSource bgmSource;
    private readonly List<AudioSource> sfxPool = new List<AudioSource>();
    private int sfxPoolIndex;
    private readonly Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();
    private Coroutine bgmFadeCoroutine;
    private bool audioUnlocked;
    private bool volumesLoaded;

    public AudioConfig Config => config;
    public float MasterVolume => masterVolume;
    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;
    public float UiVolume => uiVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 무조건 Resources/DefaultAudioConfig를 사용 — 인스펙터 할당 무시
        config = Resources.Load<AudioConfig>("DefaultAudioConfig");
#if UNITY_EDITOR
        if (config == null) config = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Resources/DefaultAudioConfig.asset");
#endif
        if (config == null)
        {
            Debug.LogError("AudioService: Resources/DefaultAudioConfig.asset을 찾을 수 없습니다.", this);
        }

        LoadVolumes();
        EnsureAudioSources();
        ApplyVolumes();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("AudioService");
        go.AddComponent<AudioService>();
    }

    public static bool TryGetInstance(out AudioService instance)
    {
        if (Instance != null)
        {
            instance = Instance;
            return true;
        }
        // Lazy creation fallback — Resources 로드 전이라도 생성
        EnsureInstance();
        instance = Instance;
        return instance != null;
    }

    private void OnEnable()
    {
        // 씬에 이미 있는 AudioListener와 중복 방지 — Main Camera 1개 유지
    }

    private void Update()
    {
        if (!audioUnlocked)
        {
            TryUnlockAudio();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && audioUnlocked)
        {
            AudioListener.pause = false;
        }
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        bgmVolume = Mathf.Clamp01(bgmVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        uiVolume = Mathf.Clamp01(uiVolume);
        if (Application.isPlaying)
        {
            ApplyVolumes();
        }
    }

    private void EnsureAudioSources()
    {
        if (bgmSource == null)
        {
            GameObject bgmGo = new GameObject("BGM");
            bgmGo.transform.SetParent(transform, false);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.dopplerLevel = 0f;
            bgmSource.priority = 0;
        }

        sfxPool.Clear();
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject go = new GameObject($"SFX_{i:D2}");
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f;
            src.priority = 128;
            sfxPool.Add(src);
        }
        sfxPoolIndex = 0;
    }

    private void LoadVolumes()
    {
        if (volumesLoaded) return;
        if (PlayerPrefs.HasKey(PrefsMaster)) masterVolume = PlayerPrefs.GetFloat(PrefsMaster, masterVolume);
        if (PlayerPrefs.HasKey(PrefsBgm)) bgmVolume = PlayerPrefs.GetFloat(PrefsBgm, bgmVolume);
        if (PlayerPrefs.HasKey(PrefsSfx)) sfxVolume = PlayerPrefs.GetFloat(PrefsSfx, sfxVolume);
        if (PlayerPrefs.HasKey(PrefsUi)) uiVolume = PlayerPrefs.GetFloat(PrefsUi, uiVolume);

        // Config 초기값으로 덮어쓰기 — Config가 있으면 그 값을 우선하되, 저장값이 있으면 저장값 유지
        if (config != null && !PlayerPrefs.HasKey(PrefsMaster))
        {
            masterVolume = config.MasterVolume;
            bgmVolume = config.BgmVolume;
            sfxVolume = config.SfxVolume;
            uiVolume = config.UiVolume;
        }
        volumesLoaded = true;
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = masterVolume * bgmVolume;
        }

        float sfxVol = masterVolume * sfxVolume;
        foreach (var src in sfxPool)
        {
            if (src != null) src.volume = sfxVol;
        }
    }

    private void SaveVolumes()
    {
        PlayerPrefs.SetFloat(PrefsMaster, masterVolume);
        PlayerPrefs.SetFloat(PrefsBgm, bgmVolume);
        PlayerPrefs.SetFloat(PrefsSfx, sfxVolume);
        PlayerPrefs.SetFloat(PrefsUi, uiVolume);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        SaveVolumes();
    }

    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        SaveVolumes();
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        SaveVolumes();
    }

    public void SetUiVolume(float v)
    {
        uiVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        SaveVolumes();
    }

    private void TryUnlockAudio()
    {
        // WebGL AudioContext는 사용자 제스처 후 resume 필요 — 키보드/마우스만 (Input System 전용)
        bool pressed = false;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            pressed = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
        }

        if (!pressed) return;

        AudioListener.pause = false;
        if (bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
        audioUnlocked = true;
    }

    /// <summary>
    /// SFX 재생 — 풀 재사용, 2D 모노, 쿨다운/우선순위 지원.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f, Priority priority = Priority.Medium, float cooldown = 0f, string cooldownKey = null)
    {
        if (clip == null) return;

        // WebGL 첫 입력 전 pause 상태에서도 재생되도록 강제 해제 — Play 시점이 Update 순서보다 먼저여도 보장
        if (!audioUnlocked) TryUnlockAudio();
        if (AudioListener.pause) AudioListener.pause = false;

        if (cooldown > 0f && !string.IsNullOrEmpty(cooldownKey))
        {
            float last;
            if (lastPlayTime.TryGetValue(cooldownKey, out last) && Time.unscaledTime - last < cooldown)
            {
                return;
            }
            lastPlayTime[cooldownKey] = Time.unscaledTime;
        }

        // High 우선순위가 아니면 풀 고갈 시 스킵 — WebGL voice 보호
        AudioSource src = GetPooledSource(priority);
        if (src == null) return;

        src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        src.volume = masterVolume * sfxVolume * Mathf.Clamp01(volumeScale);
        src.spatialBlend = 0f;
        src.dopplerLevel = 0f;
        src.clip = clip;
        src.Play();
    }

    public void PlaySFX(AudioClip clip, string cooldownKey, float cooldown, Priority priority = Priority.Medium)
    {
        PlaySFX(clip, 1f, 1f, priority, cooldown, cooldownKey);
    }

    private AudioSource GetPooledSource(Priority priority)
    {
        if (sfxPool.Count == 0) return null;

        // High는 항상 재생, Low는 재생 중인 소스가 많으면 드랍
        int playingCount = 0;
        foreach (var s in sfxPool) if (s.isPlaying) playingCount++;

        if (priority == Priority.Low && playingCount >= sfxPool.Count - 2)
        {
            return null;
        }
        if (priority == Priority.Medium && playingCount >= sfxPool.Count - 1)
        {
            return null;
        }

        // 라운드로빈 — 가장 오래된 소스 재사용
        for (int i = 0; i < sfxPool.Count; i++)
        {
            int idx = (sfxPoolIndex + i) % sfxPool.Count;
            if (!sfxPool[idx].isPlaying)
            {
                sfxPoolIndex = (idx + 1) % sfxPool.Count;
                return sfxPool[idx];
            }
        }

        // 모두 재생 중이면 High만 강제로 끊고 재사용
        if (priority == Priority.High)
        {
            AudioSource src = sfxPool[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Count;
            src.Stop();
            return src;
        }

        return null;
    }

    public void StopAllSFX()
    {
        foreach (var src in sfxPool)
        {
            if (src != null) src.Stop();
        }
    }

    public void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = -1f)
    {
        if (clip == null) return;
        if (!audioUnlocked) TryUnlockAudio();
        if (AudioListener.pause) AudioListener.pause = false;
        if (bgmSource == null) EnsureAudioSources();

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        float duration = fadeDuration < 0f ? bgmFadeDuration : fadeDuration;
        bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(clip, loop, duration));
    }

    public void StopBGM(float fadeDuration = -1f)
    {
        if (bgmSource == null || !bgmSource.isPlaying) return;
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        float duration = fadeDuration < 0f ? bgmFadeDuration : fadeDuration;
        bgmFadeCoroutine = StartCoroutine(FadeOutBGM(duration));
    }

    private IEnumerator CrossfadeBGM(AudioClip next, bool loop, float duration)
    {
        float startVol = bgmSource.volume;
        float targetVol = masterVolume * bgmVolume;

        if (bgmSource.isPlaying && duration > 0f)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = next;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        if (duration > 0f)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVol, t / duration);
                yield return null;
            }
        }
        bgmSource.volume = targetVol;
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        float startVol = bgmSource.volume;
        if (duration > 0f)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
        }
        bgmSource.Stop();
        bgmSource.volume = masterVolume * bgmVolume;
        bgmFadeCoroutine = null;
    }

    // Inspector에서 볼륨 슬라이더가 바뀌면 즉시 반영
    public void ApplyConfigVolumes()
    {
        if (config != null)
        {
            masterVolume = config.MasterVolume;
            bgmVolume = config.BgmVolume;
            sfxVolume = config.SfxVolume;
            uiVolume = config.UiVolume;
        }
        ApplyVolumes();
    }
}

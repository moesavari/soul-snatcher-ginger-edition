using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoSingleton<AudioManager>
{
    [SerializeField] private Transform _poolRoot;

    [Header("Mixer and Groups")]
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private AudioMixerGroup _musicGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private AudioMixerGroup _voiceGroup;
    [SerializeField] private AudioMixerGroup _ambientGroup;
    [SerializeField] private AudioMixerGroup _uiGroup;
    [SerializeField] private AudioMixerGroup _footstepsGroup;

    [Header("Mixer Exposed Params (dB)")]
    [SerializeField] private string _masterParam = "MasterVol";
    [SerializeField] private string _musicParam = "MusicVol";
    [SerializeField] private string _sfxParam = "SFXVol";
    [SerializeField] private string _voiceParam = "VoiceVol";
    [SerializeField] private string _ambientParam = "AmbientVol";
    [SerializeField] private string _uiParam = "UIVol";
    [SerializeField] private string _footstepsParam = "FootstepsVol";

    [Header("Pools")]
    [SerializeField] private int _sfxPoolSize = 16;
    [SerializeField] private int _voicePoolSize = 8;
    [SerializeField] private int _ambientPoolSize = 8;
    [SerializeField] private int _uiPoolSize = 8;
    [SerializeField] private int _footstepsPoolSize = 8;

    [Header("Music")]
    [SerializeField] private int _musicSources = 2; //for crossfades
    [SerializeField] private float _defaultMusicFade = 0.8f;

    [Header("Ducking")]
    [SerializeField] private bool _duckMusicOnVoice = true;
    [SerializeField] private float _duckAmountDb = -10f;
    [SerializeField] private float _duckFadeSeconds = 0.15f;

    private readonly Dictionary<AudioChannel, List<AudioSource>> _pools = new();
    private readonly Dictionary<AudioChannel, int> _poolIndices = new();

    private AudioSource[] _musicSrc;
    private int _activeMusicIndex;
    private Coroutine _musicFadeRoutine;
    private int _activeVoices;

    private readonly Dictionary<string, float> _cooldownUntilTime = new();

    private const string _ppMaster = "vol_master";
    private const string _ppMusic = "vol_music";
    private const string _ppSFX = "vol_sfx";
    private const string _ppVoice = "vol_voice";
    private const string _ppAmbient = "vol_ambient";
    private const string _ppUI = "vol_ui";
    private const string _ppFoot = "vol_foot";

    #region Unity
    protected override void Awake()
    {
        base.Awake();

        InitializePools();
        InitializeMusic();
        LoadMixerVolumes();

        if (_poolRoot == null)
        {
            var go = new GameObject("AudioPool");
            go.transform.SetParent(transform);
            _poolRoot = go.transform;
        }
    }
    #endregion

    #region Initalization
    private void InitializePools()
    {
        InitPool(AudioChannel.SFX, _sfxPoolSize, _sfxGroup);
        InitPool(AudioChannel.Voice, _voicePoolSize, _voiceGroup);
        InitPool(AudioChannel.Ambient, _ambientPoolSize, _ambientGroup);
        InitPool(AudioChannel.UI, _uiPoolSize, _uiGroup);
        InitPool(AudioChannel.Footsteps, _footstepsPoolSize, _footstepsGroup);
    }

    private void InitPool(AudioChannel channel, int size, AudioMixerGroup group)
    {
        if (!_pools.ContainsKey(channel)) _pools[channel] = new List<AudioSource>(size);
        _poolIndices[channel] = 0;

        for (int i = 0; i < size; i++)
        {
            GameObject go = new GameObject($"Audio_{channel}_{i}");
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = ResolveGroup(channel);
            src.spatialBlend = 0f;
            _pools[channel].Add(src);
        }
    }

    private void InitializeMusic()
    {
        _musicSrc = new AudioSource[_musicSources];

        for (int i = 0; i < _musicSources; i++)
        {
            GameObject go = new GameObject($"Music_{i}");
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.outputAudioMixerGroup = _musicGroup;
            src.spatialBlend = 0f;
            _musicSrc[i] = src;
        }

        _activeMusicIndex = 0;
    }

    private void TrackNonLooping(AudioSource src)
    {
        if (src && !src.loop) StartCoroutine(ReturnToPool(src));
    }

    private IEnumerator ReturnToPool(AudioSource src)
    {
        yield return new WaitWhile(() => src && src.isPlaying);

        if (!src) yield break;

        src.Stop();
        src.clip = null;
        src.transform.SetParent(_poolRoot, false);
        src.gameObject.SetActive(false);
    }
    #endregion

    #region Public Volume API (dB)
    public void SetMasterVolumeDb(float db) => SetMixerDb(_masterParam, db, _ppMaster);
    public void SetMusicVolumeDb(float db) => SetMixerDb(_musicParam, db, _ppMusic);
    public void SetSFXVolumeDb(float db) => SetMixerDb(_sfxParam, db, _ppSFX);
    public void SetVoiceVolumeDb(float db) => SetMixerDb(_voiceParam, db, _ppVoice);
    public void SetAmbientVolumeDb(float db) => SetMixerDb(_ambientParam, db, _ppAmbient);
    public void SetUIVolumeDb(float db) => SetMixerDb(_uiParam, db, _ppUI);
    public void SetFootstepsVolumeDb(float db) => SetMixerDb(_footstepsParam, db, _ppFoot);

    public float GetMixerVolumeDb(string param)
    {
        if (_mixer == null) return 0f;
        if(_mixer.GetFloat(param, out float v)) return v;

        Debug.LogWarning($"[AudioManager] Mixer param '{param}' not found");
        return 0f;
    }
    #endregion

    #region Music Controls
    public void PlayMusic(AudioClip clip, float fadeSeconds = -1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayMusic called with null clip.");
            return;
        }

        float dur = fadeSeconds >= 0f ? fadeSeconds : _defaultMusicFade;

        int next = 1 - _activeMusicIndex;
        var a = _musicSrc[_activeMusicIndex];
        var b = _musicSrc[next];

        b.clip = clip;
        b.time = 0f;
        b.volume = 0f;
        b.Play();

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(Crossfade(a, b, dur));
        _activeMusicIndex = next;
    }

    public void StopMusic(float fadeSeconds = -1f)
    {
        float dur = fadeSeconds >= 0f ? fadeSeconds : _defaultMusicFade;
        var a = _musicSrc[_activeMusicIndex];
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(FadeOut(a, dur));
    }

    public void PauseMusic() => _musicSrc[_activeMusicIndex].Pause();
    public void ResumeMusic() => _musicSrc[_activeMusicIndex].UnPause();
#endregion

    #region One-Shot & Loop (SFX/Voice/Ambient/UI/Footsteps)
    public AudioSource Play(AudioClip clip, AudioChannel channel, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null) { Debug.LogWarning("[AudioManager] Play called with null clip."); return null; }
        var src = GetPooledSource(channel);
        Configure2D(src, clip, volume, pitch, loop);
        src.Play();
        TrackNonLooping(src);
        return src;
    }

    public AudioSource PlayAtPoint(AudioClip clip, AudioChannel channel, Vector3 position, float spatialBlend = 1f, float minDist = 1f, float maxDist = 25f, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null) { Debug.LogWarning("[AudioManager] PlayAtPoint called with null clip."); return null; }
        var src = GetPooledSource(channel);
        Configure3D(src, clip, volume, pitch, loop, spatialBlend, minDist, maxDist);
        src.transform.position = position;
        src.Play();
        TrackNonLooping(src);
        return src;
    }

    public AudioSource PlayAttached(AudioClip clip, AudioChannel channel, Transform target, float spatialBlend = 1f, float minDist = 1f, float maxDist = 25f, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null || target == null) { Debug.LogWarning("[AudioManager] PlayAttached called with null."); return null; }
        var src = GetPooledSource(channel);
        Configure3D(src, clip, volume, pitch, loop, spatialBlend, minDist, maxDist);
        src.transform.SetParent(target);
        src.transform.localPosition = Vector3.zero;
        src.Play();
        TrackNonLooping(src);
        return src;
    }

    public AudioSource PlayCue(AudioCue cue, Vector3? worldPos = null, Transform attachTo = null)
    {
        if (cue == null) { Debug.LogWarning("[AudioManager] PlayCue called with null."); return null; }
        var clip = PickClip(cue.clips);
        if (clip == null) { Debug.LogWarning("[AudioManager] AudioCue has no clips."); return null; }

        // Cooldown by cue key (SO instance ID)
        string key = cue.name;
        if (cue.cooldownSeconds > 0f)
        {
            if (_cooldownUntilTime.TryGetValue(key, out float until) && Time.unscaledTime < until)
                return null;
            _cooldownUntilTime[key] = Time.unscaledTime + cue.cooldownSeconds;
        }

        float vol = Random.Range(cue.volumeRange.x, cue.volumeRange.y);
        float pit = Random.Range(cue.pitchRange.x, cue.pitchRange.y);

        if (attachTo != null)
            return PlayAttached(clip, cue.channel, attachTo, cue.spatialBlend, cue.minDistance, cue.maxDistance, vol, pit, cue.loop);

        if (worldPos.HasValue)
            return PlayAtPoint(clip, cue.channel, worldPos.Value, cue.spatialBlend, cue.minDistance, cue.maxDistance, vol, pit, cue.loop);

        return Play(clip, cue.channel, vol, pit, cue.loop);
    }

    public void StopChannel(AudioChannel channel)
    {
        if (!_pools.TryGetValue(channel, out var list)) return;
        foreach (var s in list) if (s.isPlaying) s.Stop();
    }
    #endregion

    #region Voice Ducking
    public AudioSource PlayVoice(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        var src = Play(clip, AudioChannel.Voice, volume, pitch, loop);
        if (src != null && _duckMusicOnVoice)
        {
            _activeVoices++;
            if (_activeVoices == 1) StartCoroutine(SetMixerDbOverTime(_musicParam, _duckAmountDb, _duckFadeSeconds));
            StartCoroutine(WatchVoiceEnd(src));
        }
        return src;
    }

    private IEnumerator WatchVoiceEnd(AudioSource src)
    {
        while (src != null && src.isPlaying) yield return null;
        _activeVoices = Mathf.Max(0, _activeVoices - 1);
        if (_activeVoices == 0 && _duckMusicOnVoice)
            StartCoroutine(SetMixerDbOverTime(_musicParam, 0f, _duckFadeSeconds)); // return to baseline (0 dB offset)
    }
    #endregion

    #region Helpers
    private AudioSource GetPooledSource(AudioChannel channel)
    {
        if (!_pools.TryGetValue(channel, out var list) || list == null || list.Count == 0)
        {
            Debug.LogWarning($"[AudioManager] No pool for channel '{channel}'.");
            return null;
        }

        int idx = _poolIndices[channel];
        _poolIndices[channel] = (idx + 1) % list.Count;
        var src = list[idx];
        src.outputAudioMixerGroup = ResolveGroup(channel);
        return src;
    }

    private void Configure2D(AudioSource src, AudioClip clip, float volume, float pitch, bool loop)
    {
        src.clip = clip;
        src.loop = loop;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        src.spatialBlend = 0f;
        src.minDistance = 1f;
        src.maxDistance = 25f;
        src.transform.SetParent(transform);
        src.transform.localPosition = Vector3.zero;
    }

    private void Configure3D(AudioSource src, AudioClip clip, float volume, float pitch, bool loop, float spatialBlend, float minDist, float maxDist)
    {
        src.clip = clip;
        src.loop = loop;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        src.spatialBlend = Mathf.Clamp01(spatialBlend);
        src.minDistance = Mathf.Max(0.01f, minDist);
        src.maxDistance = Mathf.Max(src.minDistance + 0.01f, maxDist);
    }

    private AudioClip PickClip(AudioClip[] arr)
    {
        return (arr != null && arr.Length > 0) ? arr[Random.Range(0, arr.Length)] : null;
    }

    private AudioMixerGroup ResolveGroup(AudioChannel ch)
    {
        return ch switch
        {
            AudioChannel.Music => _musicGroup,
            AudioChannel.SFX => _sfxGroup,
            AudioChannel.Voice => _voiceGroup,
            AudioChannel.Ambient => _ambientGroup,
            AudioChannel.UI => _uiGroup,
            AudioChannel.Footsteps => _footstepsGroup,
            _ => LogAndReturnGroup(ch)
        };
    }

    private AudioMixerGroup LogAndReturnGroup(AudioChannel ch)
    {
        Debug.LogWarning($"[AudioManager] No mixer group mapped for channel '{ch}'.");
        return _sfxGroup != null ? _sfxGroup : null;
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            if (from != null) from.volume = 1f - k;
            if (to != null) to.volume = k;
            yield return null;
        }
        if (from != null) { from.Stop(); from.volume = 1f; }
        if (to != null) { to.volume = 1f; }
        _musicFadeRoutine = null;
    }

    private IEnumerator FadeOut(AudioSource src, float seconds)
    {
        float start = src != null ? src.volume : 0f;
        float t = 0f;
        while (src != null && t < seconds)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / seconds);
            yield return null;
        }
        if (src != null) { src.Stop(); src.volume = 1f; }
        _musicFadeRoutine = null;
    }

    private void SetMixerDb(string param, float db, string saveKey)
    {
        if (_mixer == null) return;
        _mixer.SetFloat(param, db);
        PlayerPrefs.SetFloat(saveKey, db);
    }

    private void LoadMixerVolumes()
    {
        TryLoad(_masterParam, _ppMaster);
        TryLoad(_musicParam, _ppMusic);
        TryLoad(_sfxParam, _ppSFX);
        TryLoad(_voiceParam, _ppVoice);
        TryLoad(_ambientParam, _ppAmbient);
        TryLoad(_uiParam, _ppUI);
        TryLoad(_footstepsParam, _ppFoot);
    }

    private void TryLoad(string param, string key)
    {
        if (_mixer == null) return;
        if (PlayerPrefs.HasKey(key))
            _mixer.SetFloat(param, PlayerPrefs.GetFloat(key));
    }

    private IEnumerator SetMixerDbOverTime(string param, float targetDb, float seconds)
    {
        if (_mixer == null) yield break;

        _mixer.GetFloat(param, out float startDb);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(startDb, targetDb, t / seconds);
            _mixer.SetFloat(param, v);
            yield return null;
        }
        _mixer.SetFloat(param, targetDb);
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoSingleton<AudioManager>
{
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

    [Header("Music")]
    [SerializeField] private int _musicSources = 2;
    [SerializeField] private float _defaultMusicFade = 0.8f;

    [Header("Ducking")]
    [SerializeField] private bool _duckMusicOnVoice = true;
    [SerializeField] private float _duckAmountDb = -10f;
    [SerializeField] private float _duckFadeSeconds = 0.15f;

    private readonly Dictionary<AudioChannel, List<AudioSource>> _active = new();

    private AudioSource[] _musicSrc;
    private int _activeMusicIndex;
    private Coroutine _musicFadeRoutine;

    private int _activeVoices;
    private readonly Dictionary<string, float> _cooldownUntilTime = new();
    private readonly Dictionary<AudioCue, float> _cueUntil = new();
    private readonly Dictionary<AudioCue, List<AudioSource>> _cueSources = new();

    private const string _ppMaster = "vol_master";
    private const string _ppMusic = "vol_music";
    private const string _ppSFX = "vol_sfx";
    private const string _ppVoice = "vol_voice";
    private const string _ppAmbient = "vol_ambient";
    private const string _ppUI = "vol_ui";
    private const string _ppFoot = "vol_foot";

    protected override void Awake()
    {
        base.Awake();
        LoadMixerVolumes();
        _activeMusicIndex = 0;
    }

    public void SetMasterVolumeDb(float db) => SetMixerDb(_masterParam, db, _ppMaster);
    public void SetMusicVolumeDb(float db) => SetMixerDb(_musicParam, db, _ppMusic);
    public void SetSFXVolumeDb(float db) => SetMixerDb(_sfxParam, db, _ppSFX);
    public void SetVoiceVolumeDb(float db) => SetMixerDb(_voiceParam, db, _ppVoice);
    public void SetAmbientVolumeDb(float db) => SetMixerDb(_ambientParam, db, _ppAmbient);
    public void SetUIVolumeDb(float db) => SetMixerDb(_uiParam, db, _ppUI);
    public void SetFootstepsVolumeDb(float db) => SetMixerDb(_footstepsParam, db, _ppFoot);

    public float GetMixerVolumeDb(string param)
    {
        if (_mixer != null && _mixer.GetFloat(param, out float v)) return v;
        DebugManager.LogWarning($"Mixer param '{param}' not found", this);
        return 0f;
    }

    public void PlayMusic(AudioClip clip, float fadeSeconds = -1f)
    {
        if (clip == null) { DebugManager.LogWarning("PlayMusic null.", this); return; }
        EnsureMusicSources();

        float dur = fadeSeconds >= 0f ? fadeSeconds : _defaultMusicFade;

        int next = 1 - _activeMusicIndex;
        var a = _musicSrc[_activeMusicIndex];
        var b = _musicSrc[next];

        b.clip = clip;
        b.time = 0f;
        b.volume = 0f;
        b.loop = true;
        b.Play();

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(Crossfade(a, b, dur));
        _activeMusicIndex = next;
    }

    public void StopMusic(float fadeSeconds = -1f)
    {
        if (_musicSrc == null) return;
        float dur = fadeSeconds >= 0f ? fadeSeconds : _defaultMusicFade;
        var a = _musicSrc[_activeMusicIndex];
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(FadeOut(a, dur));
    }

    public void PauseMusic() { if (_musicSrc != null) _musicSrc[_activeMusicIndex].Pause(); }
    public void ResumeMusic() { if (_musicSrc != null) _musicSrc[_activeMusicIndex].UnPause(); }

    public AudioSource Play(AudioClip clip, AudioChannel channel, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null) { DebugManager.LogWarning("Play null clip.", this); return null; }
        var src = CreateSource(channel);
        Configure2D(src, clip, volume, pitch, loop);
        src.Play();
        TrackLifetime(channel, src);
        return src;
    }

    public AudioSource PlayAtPoint(AudioClip clip, AudioChannel channel, Vector3 position, float spatialBlend = 1f, float minDist = 1f, float maxDist = 25f, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null) { DebugManager.LogWarning("PlayAtPoint null clip.", this); return null; }
        var src = CreateSource(channel);
        Configure3D(src, clip, volume, pitch, loop, spatialBlend, minDist, maxDist);
        src.transform.position = position;
        src.Play();
        TrackLifetime(channel, src);
        return src;
    }

    public AudioSource PlayAttached(AudioClip clip, AudioChannel channel, Transform target, float spatialBlend = 1f, float minDist = 1f, float maxDist = 25f, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null || target == null) { DebugManager.LogWarning("PlayAttached null.", this); return null; }
        var src = CreateSource(channel);
        Configure3D(src, clip, volume, pitch, loop, spatialBlend, minDist, maxDist);
        src.transform.SetParent(target);
        src.transform.localPosition = Vector3.zero;
        src.Play();
        TrackLifetime(channel, src);
        return src;
    }

    public AudioSource PlayCue(AudioCue cue, Vector3? worldPos = null, Transform attachTo = null)
    {
        if (cue == null) { DebugManager.LogWarning("PlayCue null.", this); return null; }

        if (IsCuePlaying(cue)) return null;

        var clip = PickClip(cue.clips);
        if (clip == null) { DebugManager.LogWarning("AudioCue has no clips.", this); return null; }

        string key = cue.name;
        if (cue.cooldownSeconds > 0f)
        {
            if (_cooldownUntilTime.TryGetValue(key, out float until) && Time.unscaledTime < until)
                return null;
            _cooldownUntilTime[key] = Time.unscaledTime + cue.cooldownSeconds;
        }

        float vol = Random.Range(cue.volumeRange.x, cue.volumeRange.y);
        float pit = Random.Range(cue.pitchRange.x, cue.pitchRange.y);

        AudioSource src;
        if (attachTo != null)
            src = PlayAttached(clip, cue.channel, attachTo, cue.spatialBlend, cue.minDistance, cue.maxDistance, vol, pit, cue.loop);
        else if (worldPos.HasValue)
            src = PlayAtPoint(clip, cue.channel, worldPos.Value, cue.spatialBlend, cue.minDistance, cue.maxDistance, vol, pit, cue.loop);
        else
            src = Play(clip, cue.channel, vol, pit, cue.loop);

        if (src != null) RegisterCuePlay(cue, src, clip, pit);

        return src;
    }

    public void StopChannel(AudioChannel channel)
    {
        if (!_active.TryGetValue(channel, out var list)) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var s = list[i];
            if (s != null) Destroy(s.gameObject);
        }
        list.Clear();
    }

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
            StartCoroutine(SetMixerDbOverTime(_musicParam, 0f, _duckFadeSeconds));
    }

    private AudioSource CreateSource(AudioChannel channel)
    {
        var go = new GameObject($"Audio_{channel}");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.outputAudioMixerGroup = ResolveGroup(channel);

        if (!_active.TryGetValue(channel, out var list))
        {
            list = new List<AudioSource>(8);
            _active[channel] = list;
        }
        list.Add(src);
        return src;
    }

    private void TrackLifetime(AudioChannel channel, AudioSource src)
    {
        if (src.loop) return;
        StartCoroutine(DestroyWhenFinished(channel, src));
    }

    private IEnumerator DestroyWhenFinished(AudioChannel channel, AudioSource src)
    {
        if (src == null) yield break;
        var clip = src.clip;
        if (clip == null) { RemoveAndDestroy(channel, src); yield break; }

        float t = clip.length / Mathf.Max(0.01f, src.pitch);
        yield return new WaitForSecondsRealtime(t + 0.05f);
        RemoveAndDestroy(channel, src);
    }

    private void RemoveAndDestroy(AudioChannel channel, AudioSource src)
    {
        if (_active.TryGetValue(channel, out var list))
            list.Remove(src);
        if (src != null) Destroy(src.gameObject);
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

    private AudioClip PickClip(AudioClip[] arr) => (arr != null && arr.Length > 0) ? arr[Random.Range(0, arr.Length)] : null;

    private void EnsureMusicSources()
    {
        if (_musicSrc != null && _musicSrc.Length == _musicSources) return;
        _musicSrc = new AudioSource[_musicSources];
        for (int i = 0; i < _musicSources; i++)
        {
            var go = new GameObject($"Music_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.outputAudioMixerGroup = _musicGroup;
            src.spatialBlend = 0f;
            _musicSrc[i] = src;
        }
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
            _ => _sfxGroup
        };
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
        if (to != null) to.volume = 1f;
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

    public bool IsCuePlaying(AudioCue cue)
    {
        if (!cue) return false;

        if (_cueUntil.TryGetValue(cue, out var until) && until > Time.unscaledTime) return true;

        if (_cueSources.TryGetValue(cue, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var s = list[i];
                if (!s) { list.RemoveAt(i); continue; }
                if (s.isPlaying) return true;

                list.RemoveAt(i);
                Destroy(s.gameObject);
            }
        }
        return false;
    }

    private void RegisterCuePlay(AudioCue cue, AudioSource src, AudioClip clip, float pitch)
    {
        if (!cue || !clip || !src) return;

        if (!_cueSources.TryGetValue(cue, out var list))
        {
            list = new List<AudioSource>(2);
            _cueSources[cue] = list;
        }
        list.Add(src);

        if (!cue.loop)
        {
            var dur = clip.length / Mathf.Max(0.01f, pitch);
            _cueUntil[cue] = Time.unscaledTime + dur;
            StartCoroutine(ClearCueWhenAudioStops(cue, src));
        }
        else
        {

            _cueUntil[cue] = float.PositiveInfinity;
            StartCoroutine(ClearCueWhenAudioStops(cue, src));
        }
    }

    private IEnumerator ClearCueWhenAudioStops(AudioCue cue, AudioSource src)
    {

        while (src && src.isPlaying) yield return null;

        if (_cueSources.TryGetValue(cue, out var list))
        {
            list.Remove(src);
            if (list.Count == 0) _cueSources.Remove(cue);
        }

        if (!_cueSources.ContainsKey(cue))
            _cueUntil.Remove(cue);
    }

    public void StopCue(AudioCue cue)
    {
        if (!cue) return;
        if (_cueSources.TryGetValue(cue, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var s = list[i];
                if (s) Destroy(s.gameObject);
            }
            _cueSources.Remove(cue);
        }
        _cueUntil.Remove(cue);
    }
}

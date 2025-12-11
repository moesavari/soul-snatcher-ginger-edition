using UnityEngine;

[CreateAssetMenu(fileName = "AudioCue", menuName = "Audio/Audio Cue", order = 0)]
public class AudioCue : ScriptableObject
{
    [Header("Clips")]
    [SerializeField] private AudioClip[] _clips;

    [Header("Channel")]
    [SerializeField] private AudioChannel _channel = AudioChannel.SFX;

    [Header("Ranges")]
    [SerializeField] private Vector2 _volumeRange = new Vector2(0.9f, 1.0f);
    [SerializeField] private Vector2 _pitchRange = new Vector2(0.98f, 1.02f);

    [Header("Playback")]
    [SerializeField] private bool _loop = false;
    [SerializeField, Range(0f, 1f)] private float _spatialBlend = 0f;
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _maxDistance = 25f;

    [Header("Spam Control")]
    [SerializeField] private float _cooldownSeconds = 0f;

    public AudioClip[] clips => _clips;
    public AudioChannel channel => _channel;
    public Vector2 volumeRange => _volumeRange;
    public Vector2 pitchRange => _pitchRange;
    public bool loop => _loop;
    public float spatialBlend => _spatialBlend;
    public float minDistance => _minDistance;
    public float maxDistance => _maxDistance;
    public float cooldownSeconds => _cooldownSeconds;
}

using UnityEngine;

public class AudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioCue _cue;
    [SerializeField] private bool _attachToThis = true;

    public void Play()
    {
        if(_cue == null)
        {
            DebugManager.LogWarning("[AudioEmitter] No AudioCue set.");
            return;
        }

        if (_attachToThis)
            AudioManager.Instance.PlayCue(_cue, attachTo: transform);
        else
            AudioManager.Instance.PlayCue(_cue, worldPos: transform.position);
    }

    public void PlayOneShot(AudioClip clip, AudioChannel channel = AudioChannel.SFX, float volume = 1f, float pitch = 1f)
    {
        if (_attachToThis)
            AudioManager.Instance.PlayAttached(clip, channel, transform, 1f, 1f, 25f, volume, pitch, false);
        else
            AudioManager.Instance.PlayAtPoint(clip, channel, transform.position, 1f, 1f, 25f, volume, pitch, false);
    }
}

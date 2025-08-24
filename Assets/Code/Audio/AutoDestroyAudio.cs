using System.Collections;
using UnityEngine;

public class AutoDestroyAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _source;
    [SerializeField] private float _grace = 0.05f;

    public AudioSource source => _source;

    private void Awake()
    {
        _source ??= GetComponent<AudioSource>();
    }

    public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if(!_source) _source = gameObject.AddComponent<AudioSource>();

        _source.clip = clip;
        _source.volume = volume;
        _source.pitch = pitch;

        _source.Play();

        StartCoroutine(DestroyWhenDone());
    }

    private IEnumerator DestroyWhenDone()
    {
        yield return new WaitWhile(() => _source &&  _source.isPlaying);
        yield return new WaitForSeconds(_grace);
        Destroy(gameObject);
    }
}

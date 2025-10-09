using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HitFlash2D : MonoBehaviour
{
    [SerializeField] private List<SpriteRenderer> _targets = new List<SpriteRenderer>();
    [SerializeField] private Color _flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float _flashSeconds = 0.08f;
    [SerializeField] private int _flashCount = 2;

    private Stats _stats;
    private readonly List<Color> _original = new List<Color>();
    private Coroutine _routine;

    private void Awake()
    {
        _stats = GetComponent<Stats>();

        if (_targets.Count == 0)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) _targets.Add(sr);
        }
        _original.Clear();
        foreach (var r in _targets) _original.Add(r != null ? r.color : Color.white);
    }

    private void OnEnable()
    {
        if (_stats.tag == "Player") _stats.GetComponent<PlayerController>().OnDamaged += OnDamagedController;
        if (_stats.tag == "Enemy") _stats.GetComponent<Zombie>().OnDamaged += OnDamagedHealth;
    }

    private void OnDisable()
    {
        if (_stats.tag == "Player") _stats.GetComponent<PlayerController>().OnDamaged -= OnDamagedController;
        if (_stats.tag == "Enemy") _stats.GetComponent<Zombie>().OnDamaged -= OnDamagedHealth;
    }

    private void OnDamagedHealth()
    {
        Flash();
    }

    private void OnDamagedController(int amount)
    {
        Flash();
    }

    private void Flash()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        for (int i = 0; i < _flashCount; i++)
        {
            for (int t = 0; t < _targets.Count; t++) if (_targets[t] != null) _targets[t].color = _flashColor;
            yield return new WaitForSeconds(_flashSeconds);

            for (int t = 0; t < _targets.Count; t++) if (_targets[t] != null) _targets[t].color = _original[t];
            yield return new WaitForSeconds(_flashSeconds * 0.5f);
        }
        _routine = null;
    }
}

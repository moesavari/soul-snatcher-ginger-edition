using System;
using System.Collections.Generic;
using UnityEngine;

public class UIGridPool<TCell> : MonoBehaviour where TCell : Component
{
    [Header("Pool Setup")]
    [SerializeField] private Transform _parent;
    [SerializeField] private TCell _template;
    [SerializeField, Min(0)] private int _initialCount = 20;

    private readonly List<TCell> _cells = new List<TCell>(32);
    public IReadOnlyList<TCell> Cells => _cells;

    public void BuildOnce()
    {
        if (_cells.Count > 0) return;
        if (!_template)
        {
            DebugManager.LogError($"{name}: UIGridPool needs a template.", this);
            return;
        }

        bool prevActive = _template.gameObject.activeSelf;
        _template.gameObject.SetActive(true);

        for (int i = 0; i < _initialCount; i++)
        {
            var clone = Instantiate(_template, _parent);
            clone.name = $"{_template.name} ({i + 1})";
            _cells.Add(clone);
        }

        _template.gameObject.SetActive(prevActive);
        _template.gameObject.SetActive(false);
    }

    public void ForEach(Action<TCell, int> action)
    {
        for (int i = 0; i < _cells.Count; i++) action(_cells[i], i);
    }
}

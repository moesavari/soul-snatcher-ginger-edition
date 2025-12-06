using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuantityPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TMP_InputField _input;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private Action<int> _onConfirm;
    private int _min = 1;
    private int _max = 1;

    private void Awake()
    {
        _confirmButton.onClick.AddListener(Confirm);
        _cancelButton.onClick.AddListener(Close);
        Hide();
    }

    public void Open(Vector2 screenPos, int max, Action<int> onConfirm, int min = 1, int initial = 1)
    {
        _min = Mathf.Max(1, min);
        _max = Mathf.Max(_min, max);
        _onConfirm = onConfirm;
        _input.text = Mathf.Clamp(initial, _min, _max).ToString();

        _root.SetActive(true);

        var rt = _root.transform as RectTransform;
        if (rt != null && rt.parent is RectTransform pr)
        {
            Vector2 lp;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(pr, screenPos, null, out lp);
            rt.anchoredPosition = lp;
        }
    }

    public void Hide() => _root.SetActive(false);

    private void Close() { _onConfirm = null; Hide(); }

    private void Confirm()
    {
        if (!int.TryParse(_input.text, out var n)) n = _min;
        n = Mathf.Clamp(n, _min, _max);
        var cb = _onConfirm; _onConfirm = null;
        Hide();
        cb?.Invoke(n);
    }
}

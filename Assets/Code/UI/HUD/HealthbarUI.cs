using UnityEngine;
using UnityEngine.UI;

public class HealthbarUI : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    public void Set(int current, int max)
    {
        if (!_slider) return;
        _slider.maxValue = Mathf.Max(1, max);
        _slider.value = Mathf.Clamp(current, 0, _slider.maxValue);
    }
}

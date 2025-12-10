using UnityEngine;
using UnityEngine.UI;

public class EquipmentPaperdollUI : MonoBehaviour
{
    [SerializeField] private Image _image;

    [Header("Male Sprites")]
    [SerializeField] private Sprite _maleStage0;
    [SerializeField] private Sprite _maleStage1;
    [SerializeField] private Sprite _maleStage2;

    [Header("Female Sprites")]
    [SerializeField] private Sprite _femaleStage0;
    [SerializeField] private Sprite _femaleStage1;
    [SerializeField] private Sprite _femaleStage2;

    public void SetPaperdoll(bool isMale, int hairStage)
    {
        int index = Mathf.Clamp(hairStage - 1, 0, 2);

        Sprite sprite = isMale
            ? GetMaleSprite(index)
            : GetFemaleSprite(index);

        _image.sprite = sprite;
    }

    private Sprite GetMaleSprite(int index)
    {
        return index switch
        {
            0 => _maleStage0,
            1 => _maleStage1,
            2 => _maleStage2,
            _ => LogFallbackSprite(_maleStage2, "Male", index)
        };
    }

    private Sprite GetFemaleSprite(int index)
    {
        return index switch
        {
            0 => _femaleStage0,
            1 => _femaleStage1,
            2 => _femaleStage2,
            _ => LogFallbackSprite(_femaleStage2, "Female", index)
        };
    }

    private Sprite LogFallbackSprite(Sprite fallback, string gender, int index)
    {
        Debug.LogWarning(
            $"[EquipmentPaperdollUI] No sprite mapped for gender={gender}, " +
            $"index={index}. Using fallback.");
        return fallback;
    }
}
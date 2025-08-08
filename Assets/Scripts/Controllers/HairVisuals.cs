using UnityEngine;

public class HairVisuals : MonoBehaviour
{
    public enum GenderType { Male, Female }

    [Header("Settings")]
    [SerializeField] private GenderType _gender = GenderType.Male;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Hair Stage Sprites - Male")]
    [SerializeField] private Sprite _maleStage1;
    [SerializeField] private Sprite _maleStage2;
    [SerializeField] private Sprite _maleStage3;

    [Header("Hair Stage Sprites - Female")]
    [SerializeField] private Sprite _femaleStage1;
    [SerializeField] private Sprite _femaleStage2;
    [SerializeField] private Sprite _femaleStage3;

    public void SetHairStage(int stage)
    {
        Sprite sprite = stage switch
        {
            1 => _gender == GenderType.Male ? _maleStage1 : _femaleStage1,
            2 => _gender == GenderType.Male ? _maleStage2 : _femaleStage2,
            3 => _gender == GenderType.Male ? _maleStage3 : _femaleStage3,
            _ => LogAndReturnNull(stage)
        };

        if (sprite != null)
            _spriteRenderer.sprite = sprite;
    }

    private Sprite LogAndReturnNull(int stage)
    {
        Debug.LogWarning($"[HairVisuals] Unknown hair stage '{stage}' passed. Expected 1–3. No sprite was set.");
        return null;
    }

    public void SetGender(GenderType gender) => _gender = gender;

    public GenderType gender => _gender;
}

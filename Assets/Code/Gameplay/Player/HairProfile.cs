using UnityEngine;

public enum GenderType { Male, Female }

[CreateAssetMenu(fileName = "HairProfile", menuName = "Appearance/Hair Profile")]
public class HairProfile : ScriptableObject
{
    [SerializeField] private GenderType _gender;
    [SerializeField] private Sprite[] _stages;

    public GenderType gender => _gender;
    public int stageCount => _stages != null ? _stages.Length : 0;

    public Sprite GetStageSprite(int stageIndex)
    {
        if (_stages == null || _stages.Length == 0) return null;

        int i = Mathf.Clamp(stageIndex - 1, 0, _stages.Length - 1);
        return _stages[i];
    }
}

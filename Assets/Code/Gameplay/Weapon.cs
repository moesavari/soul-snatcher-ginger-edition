using UnityEngine;

public class Weapon
{
    [SerializeField] private int _minDamage;
    [SerializeField] private int _maxDamage;
    [SerializeField] private float _critBonus;

    public int GetRollDamage()
    {
        return Random.Range(_minDamage, _maxDamage + 1);
    }

    public float critBonus { get => _critBonus; set => _critBonus = value; }
}

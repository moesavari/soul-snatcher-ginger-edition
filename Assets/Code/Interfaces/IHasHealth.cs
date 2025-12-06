public interface IHasHealth
{
    int currentHp { get; }

    void Heal(int amount);
}

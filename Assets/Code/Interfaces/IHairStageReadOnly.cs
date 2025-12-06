public interface IHairStageReadOnly
{
    int hairStage { get; }
    event System.Action<int> HairStageChanged;
}

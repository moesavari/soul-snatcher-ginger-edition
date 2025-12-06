public interface ISoulReadOnly
{
    int souls { get; }
    event System.Action<int> SoulsChanged;
}

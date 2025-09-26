using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Starter Loadout")]
public class StarterLoadout : ScriptableObject
{
    [System.Serializable] public struct Entry { public ItemDef item; public int count; }
    public Entry[] items;
}

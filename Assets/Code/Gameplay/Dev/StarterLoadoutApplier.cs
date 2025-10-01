using System.Collections;
using System.Reflection;
using UnityEngine;

public class StarterLoadoutApplier : MonoBehaviour
{
    [SerializeField] private StarterLoadout loadout;
    [SerializeField] private bool clearInventoryFirst = true;
    [SerializeField] private bool applyOnce = true;

    private bool _applied;

    private void OnEnable()
    {
        StartCoroutine(ApplyWhenReady());
    }

    private IEnumerator ApplyWhenReady()
    {
        if (_applied && applyOnce) yield break;

        // Wait for PlayerContext and facade to exist (player is spawned later)
        while (PlayerContext.Instance == null) yield return null;
        while (PlayerContext.Instance.facade == null) yield return null;

        // One more frame so player subsystems finish Start()
        yield return null;

        var inv = PlayerContext.Instance.facade.inventory;
        if (inv == null)
        {
            DebugManager.LogWarning("Inventory is null on player's facade.", this);
            yield break;
        }

        if (_applied && applyOnce) yield break;

        if (clearInventoryFirst && HasMethod(inv, "Clear"))
        {
            inv.Clear(); // you added this earlier; if you keep it private, make it public/internal
        }

        int addedStacks = 0;
        int totalLeftover = 0;

        if (loadout != null)
        {
            foreach (var e in loadout.items)
            {
                if (!e.item || e.count <= 0) continue;

                int left;
                var ok = inv.TryAdd(e.item, e.count, out left);
                if (!ok || left > 0)
                {
                    totalLeftover += left;
                    DebugManager.LogWarning($"Could not fully add {e.count}x {e.item.name}. Leftover={left}", this);
                }
                if (ok) addedStacks++;
            }
        }

        _applied = true;
        DebugManager.Log($"Applied. Stacks added: {addedStacks}, leftover total: {totalLeftover}", this);
        // No UI poke needed: Inventory events should already refresh the grid.
    }

    private static bool HasMethod(object obj, string name)
    {
        return obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;
    }
}

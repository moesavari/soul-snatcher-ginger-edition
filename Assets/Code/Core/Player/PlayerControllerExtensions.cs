using UnityEngine;

public static class PlayerControllerExtensions
{
    // Allows code to call player.SetRooted(bool) even if the original class doesn’t have it.
    public static void SetRooted(this PlayerController player, bool rooted)
    {
        // If your PlayerController has a real method, this extension won’t be used.
        // This no-op prevents compile errors without forcing changes.
        // Optionally: set a public field or raise an event if available.
    }

    // Convenience overload used inside MeleeWeapon via 'this' (component to player)
    public static void SetRooted(MeleeWeapon weapon, bool rooted)
    {
        var pc = weapon ? weapon.GetComponentInParent<PlayerController>() : null;
        if (pc != null) pc.SetRooted(rooted);
    }
}

using UnityEngine;

public static class ComponentUtil
{
    /// <summary>
    /// Cache a required component on the same GameObject.
    /// Logs a clear warning during play if missing.
    /// </summary>
    public static T Require<T>(this Component c) where T : Component
    {
        if (c == null)
        {
            DebugManager.LogWarning($"Caller component is null.");
            return null;
        }

        var t = c.GetComponent<T>();
        if (t == null)
            DebugManager.LogWarning($"Missing required {typeof(T).Name} on '{c.gameObject.name}'.");
        return t;
    }

    /// <summary>
    /// Gets a component somewhere under this object (once). Use for fixed child deps.
    /// </summary>
    public static T RequireInChildren<T>(this Component c, bool includeInactive = false) where T : Component
    {
        var t = c.GetComponentInChildren<T>(includeInactive);
        if (t == null)
            DebugManager.LogWarning($"Missing child {typeof(T).Name} under '{c.gameObject.name}'.");
        return t;
    }

    /// <summary>
    /// Gets a component in parent chain (once).
    /// </summary>
    public static T RequireInParent<T>(this Component c) where T : Component
    {
        var t = c.GetComponentInParent<T>();
        if (t == null)
            DebugManager.LogWarning($"Missing parent {typeof(T).Name} for '{c.gameObject.name}'.");
        return t;
    }
}

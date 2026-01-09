using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance => _instance;

    public static bool IsReady => _instance != null;

    protected static T Require(object caller)
    {
        if (_instance != null) return _instance;
        DebugManager.LogError($"[{typeof(T).Name}] Instance is not ready. Called by: {caller}", caller as Object);
        return null;
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnSingletonAwake() { }
}

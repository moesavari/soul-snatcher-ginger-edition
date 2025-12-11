using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance => _instance;

    [SerializeField] private bool _persistAcrossScenes = true;

    public static bool IsReady => _instance != null;

    protected static T Require(object caller)
    {
        if (_instance != null) return _instance;
        Debug.LogError($"[{typeof(T).Name}] Instance is not ready. Called by: {caller}", caller as Object);
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

        if (_persistAcrossScenes)
        {
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }

        OnSingletonAwake();
    }

    protected virtual void OnSingletonAwake() { }
}

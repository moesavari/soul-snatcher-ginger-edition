using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static readonly object _lock = new();
    private static T _instance;
    private static bool _quitting;

    public static T Instance
    {
        get
        {
            if (_quitting) return null; // don’t spawn during shutdown
            lock (_lock)
            {
                if (_instance != null) return _instance;

                _instance = FindObjectOfType<T>(true);
                if (_instance != null) return _instance;

                // create only if playing
                if (!Application.isPlaying) return null;

                var go = new GameObject($"{typeof(T).Name} (Singleton)");
                _instance = go.AddComponent<T>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = (T)this;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnApplicationQuit()
    {
        _quitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}

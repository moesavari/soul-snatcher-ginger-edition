using UnityEngine;

/// <summary>
/// Abstract singleton base for MonoBehaviours. Ensures only one instance exists across scenes.
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static readonly object _lock = new();
    private static T _instance;

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance != null) return _instance;

                _instance = FindObjectOfType<T>();

                if (_instance != null) return _instance;

                var singletonObj = new GameObject($"{typeof(T)} (Singleton)");
                _instance = singletonObj.AddComponent<T>();
                DontDestroyOnLoad(singletonObj);

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
#if UNITY_EDITOR
            Debug.LogWarning($"Duplicate singleton '{typeof(T)}' destroyed on GameObject '{gameObject.name}'.");
#endif
        }
    }
}

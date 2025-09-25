using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance => _instance;

    [SerializeField] private bool _persistAcrossScenes = true;

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Another instance exists → destroy this one
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (_persistAcrossScenes)
        {
            // Ensure GameObject is at the root before marking as persistent
            if (transform.parent != null)
                transform.SetParent(null, true); // keep world position

            // Call on the root object to silence the warning
            DontDestroyOnLoad(gameObject);
        }

        OnSingletonAwake();
    }

    /// <summary>Optional override for subclass-specific initialization.</summary>
    protected virtual void OnSingletonAwake() { }
}

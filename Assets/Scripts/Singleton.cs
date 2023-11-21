using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    /// <summary>
    /// For Singletons Don't use Awake to initialize the object, since we don't want to init if there is more than one in the scene.
    /// Instead use the Initialize function provided by this class.
    /// </summary>
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this as T;
        } else if(Instance != null) Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    protected virtual void Initialize()
    { }
}

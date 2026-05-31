using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T instance { get; protected set; }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);

            OnAwake();
        }
    }

    /// <summary>
    /// Only call for the first instance
    /// </summary>
    protected virtual void OnAwake()
    {
    }
}
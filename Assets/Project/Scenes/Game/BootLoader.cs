using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    private void Awake()
    {
        if (!SceneManager.GetSceneByName("Persistent").isLoaded)
            SceneManager.LoadScene("Persistent", LoadSceneMode.Additive);
    }
}
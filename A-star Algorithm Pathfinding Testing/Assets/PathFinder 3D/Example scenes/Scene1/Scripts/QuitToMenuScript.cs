using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitToMenuScript : MonoBehaviour {
    public void Quit()
    {
        SceneManager.LoadScene("menuScene");
    }
}

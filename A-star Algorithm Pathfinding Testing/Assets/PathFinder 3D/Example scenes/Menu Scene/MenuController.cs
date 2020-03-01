using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void LoadFirstScene()
    {
        SceneManager.LoadScene("demoScene1");
    }
    public void LoadSecondScene()
    {
        SceneManager.LoadScene("demoScene2");
    }
    public void LoadThirdScene()
    {
        SceneManager.LoadScene("demoScene3");
    }
    public void LoadFourthScene()
    {
        SceneManager.LoadScene("MazeScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}

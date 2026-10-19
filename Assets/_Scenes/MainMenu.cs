using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
   public void StartNewGame()
    {
        SceneManager.LoadScene("Map1");
    }

    public void QuitTheGame()
    {
        Application.Quit();
    }
}

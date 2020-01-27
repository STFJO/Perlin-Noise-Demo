using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuInputController : MonoBehaviour
{

    private void Start()
    {
        Time.timeScale = 1;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit(0);
        }
    }

    public void LoadLevel(int level)
    {
        SceneManager.LoadScene(level);
    }
}

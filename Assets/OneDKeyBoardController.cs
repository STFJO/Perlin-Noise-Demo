using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OneDKeyBoardController : MonoBehaviour
{
    private float TimeScale = 1f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.timeScale > 0.5f)
            {
                Time.timeScale = 0.001f;
            }
            else Time.timeScale = 1;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene(0);
    }
}

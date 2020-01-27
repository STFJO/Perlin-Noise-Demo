using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputControllerBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Mathf.Approximately(Time.timeScale, 0))
                Time.timeScale = 1;
            else
                Time.timeScale = 0;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float RotationDegreesPerSecond = 5f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        transform.Rotate(new Vector3(0, RotationDegreesPerSecond * Time.fixedDeltaTime, 0));
    }
}

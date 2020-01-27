using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Assets.GeneratorParameter))]
public class LineGenerator : MonoBehaviour
{
    public Transform LineOrigin;
    public Transform Reference;
    private Assets.TerrainGenerator generator;
    private Assets.GeneratorParameter config;
    private int counter = 0;
    public int Octaves = 8;
    public int minOctaves = 0;

    // Start is called before the first frame update
    void Start()
    {
        config = GetComponent<Assets.GeneratorParameter>();
        generator = new Assets.TerrainGenerator();
        generator.Config = config;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float multiplier = Mathf.Pow(2, Octaves);
        LineOrigin.position = Reference.position + new Vector3(0, generator.GetHeight(new Vector2Int(counter, 0), Octaves, minOctaves)*4,0);
        counter += 50;
    }
}

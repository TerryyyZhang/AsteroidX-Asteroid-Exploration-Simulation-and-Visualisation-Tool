using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Polarith.AI.Move;

public class UpdateLayer : MonoBehaviour
{
    private AIMEnvironment Env;
    // Start is called before the first frame update
    void Start()
    {
        Env = GetComponent<AIMEnvironment>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < 0.5f)
            Env.UpdateLayerGameObjects();
    }
}

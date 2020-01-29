using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameControl : MonoBehaviour
{
    private Light flame;
    ParticleSystem particle;
    ShipInput SI;
    // Start is called before the first frame update
    void Start()
    {
        flame = GetComponentInChildren<Light>();
        particle = GetComponent<ParticleSystem>();
        SI = GetComponentInParent<ShipInput>();
        flame.enabled = false;
        particle.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if (SI.throttle > 0) { flame.enabled = true; particle.Play(); }
        else { flame.enabled = false; particle.Stop(); }
    }
}

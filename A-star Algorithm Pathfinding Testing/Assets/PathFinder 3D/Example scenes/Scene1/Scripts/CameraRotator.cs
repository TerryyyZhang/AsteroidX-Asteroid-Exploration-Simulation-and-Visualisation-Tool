using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotator : MonoBehaviour
{

    float alpha = 0;
    public float radius = 24;
    public float x0;
    public float z0;
    public Transform lookAtObject;
    private void Start()
    {
        alpha = 180;
        float radAlpha = (alpha * (float)Mathf.PI) / 180.0F;
        float x = radius * (float)Mathf.Cos(radAlpha);
        float z = radius * (float)Mathf.Sin(radAlpha);

        transform.position = new Vector3(x0 + x, transform.position.y, z0 + z);
        transform.rotation = Quaternion.LookRotation(lookAtObject.position - transform.position);
    }
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            alpha += 0.5f;
            float rad_alpha = (alpha * (float)Mathf.PI) / 180.0F;
            float x = radius * (float)Mathf.Cos(rad_alpha);
            float z = radius * (float)Mathf.Sin(rad_alpha);

            transform.position = new Vector3(x0 + x, transform.position.y, z0 + z);
            if (alpha == 360) alpha = 0;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            alpha -= 0.5f;
            float rad_alpha = (alpha * (float)Mathf.PI) / 180.0F;
            float x = radius * (float)Mathf.Cos(rad_alpha);
            float z = radius * (float)Mathf.Sin(rad_alpha);

            transform.position = new Vector3(x0 + x, transform.position.y, z0 + z);
            if (alpha == 360) alpha = 0;
        }


        transform.rotation = Quaternion.LookRotation(lookAtObject.position - transform.position);
    }
}

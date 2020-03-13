using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class lagCamera : MonoBehaviour
{
    //Speed at which the camera rotates. (Camera uses Slerp for rotation.)
    public float rotateSpeed = 90.0f;
    //If the parented object is using FixedUpdate for movement, check this box for smoother movement.")
    public bool usedFixedUpdate = true;

    private Transform target;
    private Vector3 startOffset;

    // Start is called before the first frame update
    void Start()
    {
        target = transform.parent;

        if (target == null)
            Debug.LogWarning(name + ": Lag Camera will not function correctly without a target.");
        if (transform.parent == null)
            Debug.LogWarning(name + ": Lag Camera will not function correctly without a parent to derive the initial offset from.");

        startOffset = transform.localPosition;
        transform.SetParent(null);
    }

    // Update is called once per frame
    void Update()
    {
        if (!usedFixedUpdate)
            UpdateCamera();
    }

    private void FixedUpdate()
    {
        if (usedFixedUpdate)
            UpdateCamera();
    }

    private void UpdateCamera()
    {
        if (target != null)
        {
            transform.position = target.TransformPoint(startOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotateSpeed * Time.deltaTime);
        }
    }
}

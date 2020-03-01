using UnityEngine;

public class PointsMotion : MonoBehaviour
{
    public Transform redPoint;
    public Transform greenPoint;

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.A) && redPoint.position.x < 23.5f) redPoint.position += new Vector3(.4f, 0, 0);
        if (Input.GetKey(KeyCode.D) && redPoint.position.x > -19.5f) redPoint.position += new Vector3(-.4f, 0, 0);
        if (Input.GetKey(KeyCode.W) && redPoint.position.y < 53.5f) redPoint.position += new Vector3(0, .4f, 0);
        if (Input.GetKey(KeyCode.S) && redPoint.position.y > 10f) redPoint.position += new Vector3(0, -.4f, 0);

        if (Input.GetKey(KeyCode.LeftArrow) && greenPoint.position.x < 23.5f) greenPoint.position += new Vector3(.4f, 0, 0);
        if (Input.GetKey(KeyCode.RightArrow) && greenPoint.position.x > -19.5f) greenPoint.position += new Vector3(-.4f, 0, 0);
        if (Input.GetKey(KeyCode.UpArrow) && greenPoint.position.z > -23.5f) greenPoint.position += new Vector3(0, 0, -.4f);
        if (Input.GetKey(KeyCode.DownArrow) && greenPoint.position.z < 19.5f) greenPoint.position += new Vector3(0, 0, .4f);
    }
}

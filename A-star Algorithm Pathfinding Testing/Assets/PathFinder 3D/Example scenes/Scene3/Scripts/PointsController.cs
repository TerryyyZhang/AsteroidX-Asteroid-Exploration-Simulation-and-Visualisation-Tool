using UnityEngine;

public class PointsController : MonoBehaviour
{
    public Transform fromPoint;
    public Transform toPoint;

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.A)) fromPoint.position += new Vector3(-.2f, 0, 0);
        if (Input.GetKey(KeyCode.D)) fromPoint.position += new Vector3(.2f, 0, 0);
        if (Input.GetKey(KeyCode.W)) fromPoint.position += new Vector3(0, .2f, 0);
        if (Input.GetKey(KeyCode.S)) fromPoint.position += new Vector3(0, -.2f, 0);

        if (Input.GetKey(KeyCode.LeftArrow)) toPoint.position += new Vector3(-.2f, 0, 0);
        if (Input.GetKey(KeyCode.RightArrow)) toPoint.position += new Vector3(.2f, 0, 0);
        if (Input.GetKey(KeyCode.UpArrow)) toPoint.position += new Vector3(0, .2f, 0);
        if (Input.GetKey(KeyCode.DownArrow)) toPoint.position += new Vector3(0, -.2f, 0);
    }
}

using UnityEngine;
using System.Collections;

public class Hole : MonoBehaviour {

	// Use this for initialization
	public Vector3 angle;
	public Vector3 cameraPos = new Vector3(0,0,1000);
	public int coreSize = 10;
	private Material mMat;
	private bool isLeftDown;
	private bool isRightDown;
	void Start () 
	{
		mMat = RenderSettings.skybox;
	}

	void OnGUI()
	{
		GUI.color = Color.white;
		GUILayout.BeginHorizontal ();
		GUILayout.Label ("虫洞半径：");
		int.TryParse(GUILayout.TextField (coreSize.ToString()),out coreSize);
		if (coreSize < 1)
			coreSize = 1;
		GUILayout.EndHorizontal ();
		GUILayout.Label ("左键拖动调整视角");
		GUILayout.Label ("右键拖动使摄像机绕虫洞旋转");
		GUILayout.Label ("滚轮调整距离");
	}

	// Update is called once per frame
	void Update () 
	{
		Matrix4x4 m;
		if (cameraPos.z < 0)
		{
			m = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(angle.x + 180,angle.y,angle.z + 180)), new Vector3(-1,1,1));
		}
		else
		{
			m = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(angle), Vector3.one);
		}	

		mMat.SetFloat ("_CoreSize", coreSize);
		mMat.SetMatrix ("_CamAngle", m);
		mMat.SetVector ("_CamPos", new Vector4 (cameraPos.x, cameraPos.y, cameraPos.z == 0 ? 0.01f : cameraPos.z, cameraPos.z < 0 ? -1 : 1));

		angle.y += Time.deltaTime * 0.5f;
		
		if (Input.GetMouseButtonDown(0))
			isLeftDown = true;

		if (Input.GetMouseButtonUp(0))
			isLeftDown = false;

		if (Input.GetMouseButtonDown(1))
			isRightDown = true;

		if (Input.GetMouseButtonUp(1))
			isRightDown = false;

		if (isLeftDown)
		{
			Vector3 cameraAngles = Camera.main.transform.eulerAngles;
			cameraAngles.y += Input.GetAxis("Mouse X") * 5f;
			cameraAngles.x -= Input.GetAxis("Mouse Y") * 5f;
			Camera.main.transform.eulerAngles = cameraAngles;
		}

		if (isRightDown)
		{
			angle.y += Input.GetAxis("Mouse X") * 5f;
			angle.x += Input.GetAxis("Mouse Y") * 5f;
		}

		cameraPos.z -=	Input.GetAxis ("Mouse ScrollWheel") * 100f;
		
//		if (Input.GetKey(KeyCode.LeftArrow))
//		{
//			cameraPos.x += 2f;
//		}
//		if (Input.GetKey(KeyCode.RightArrow))
//		{
//			cameraPos.x -= 2f;
//		}
//		if (Input.GetKey(KeyCode.UpArrow))
//		{
//			cameraPos.y += 2f;
//		}
//		if (Input.GetKey(KeyCode.DownArrow))
//		{
//			cameraPos.y -= 2f;
//		}
	}
}

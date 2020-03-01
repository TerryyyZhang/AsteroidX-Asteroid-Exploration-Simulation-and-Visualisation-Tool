using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Pursuer))]
public class AgentController : MonoBehaviour {
    Pursuer thisPursuerInstance;
    public Transform target;
    public float circleRadius = 30;
    Toggle traceToggle, optimizeToggle, smoothToggle;
    private void Start()
    {
        traceToggle = GameObject.Find("drawToggle").GetComponent<Toggle>();
        optimizeToggle = GameObject.Find("optimizeToggle").GetComponent<Toggle>();
        smoothToggle = GameObject.Find("smoothToggle").GetComponent<Toggle>();
        thisPursuerInstance = gameObject.GetComponent<Pursuer>();
    }
    public void TheGraphIsReady()
    {
        thisPursuerInstance.MoveTo(target);
    }
    public void EventTargetReached() {
        transform.position = RandomCirclePos();
        thisPursuerInstance.tracePath = traceToggle.isOn;
        thisPursuerInstance.trajectoryOptimization = optimizeToggle.isOn;
        thisPursuerInstance.trajectorySmoothing = smoothToggle.isOn;
        thisPursuerInstance.MoveTo(target);
    }
    Vector3 RandomCirclePos()
    {
        float alpha = Random.Range(0f,360f);
        float radAlpha = (alpha * (float)Mathf.PI) / 180.0F;
        float x = circleRadius * (float)Mathf.Cos(radAlpha);
        float z = circleRadius * (float)Mathf.Sin(radAlpha);
        return new Vector3(x, 0, z);
    }
}
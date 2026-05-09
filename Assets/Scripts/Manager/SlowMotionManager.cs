using UnityEngine;

public class SlowMotionManager : MonoBehaviour
{
    [SerializeField] private float slowMotionScale = 0.25f;

    private void Start()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
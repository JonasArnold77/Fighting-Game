using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RootMotionBlocker : MonoBehaviour
{
    [SerializeField] private bool lockRootPosition = true;
    [SerializeField] private bool lockRootRotation = true;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove()
    {
        if (animator == null)
            return;

        if (!lockRootPosition)
        {
            transform.position += animator.deltaPosition;
        }

        if (!lockRootRotation)
        {
            transform.rotation *= animator.deltaRotation;
        }

        // Wenn beide true sind, wird die Root Motion gelesen,
        // aber nicht auf die Figur angewendet.
    }
}
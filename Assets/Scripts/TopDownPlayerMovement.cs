using UnityEngine;

public class TopDownPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Fighter fighter;

    private float verticalVelocity;

    private void Update()
    {
        HandleGravity();

        if (!fighter.CanMove)
        {
            ApplyVerticalMovementOnly();
            fighter.SetMoveSpeed(0f);
            return;
        }

        HandleMovement();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        fighter.SetMoveSpeed(moveDirection.magnitude);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            RotateTowardsMovement(moveDirection);
        }
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVelocity;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void ApplyVerticalMovementOnly()
    {
        Vector3 velocity = new Vector3(0f, verticalVelocity, 0f);
        characterController.Move(velocity * Time.deltaTime);
    }

    private void RotateTowardsMovement(Vector3 moveDirection)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
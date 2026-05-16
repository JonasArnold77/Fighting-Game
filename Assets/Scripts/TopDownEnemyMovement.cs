using UnityEngine;

public class TopDownEnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Chase Behaviour")]
    [Tooltip("Distanz in Metern, ab der der Gegner stehen bleibt.")]
    [SerializeField] private float stoppingDistance = 1.5f;
    [Tooltip("Distanz, ab der der Gegner anfängt dem Spieler zu folgen.")]
    [SerializeField] private float detectionRange = 15f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Fighter fighter;
    [SerializeField] private Transform playerTransform;

    private float verticalVelocity;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (fighter == null)
            fighter = GetComponent<Fighter>();
    }

    private void Update()
    {
        HandleGravity();

        if (!fighter.CanMove)
        {
            ApplyVerticalMovementOnly();
            fighter.SetMoveSpeed(0f);
            return;
        }

        if (playerTransform == null)
        {
            fighter.SetMoveSpeed(0f);
            return;
        }

        HandleChase();
    }

    private void HandleChase()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Außerhalb der Detection Range oder zu nah → stehen bleiben
        if (distanceToPlayer > detectionRange || distanceToPlayer <= stoppingDistance)
        {
            fighter.SetMoveSpeed(0f);
            ApplyVerticalMovementOnly();
            return;
        }

        // Richtung zum Spieler berechnen (nur horizontal)
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0f;
        directionToPlayer.Normalize();

        // Bewegung anwenden
        Vector3 velocity = directionToPlayer * moveSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        // Animation treiben
        fighter.SetMoveSpeed(1f);

        // Zum Spieler drehen
        RotateTowards(directionToPlayer);
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

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}

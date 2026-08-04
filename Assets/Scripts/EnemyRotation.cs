using UnityEngine;

public class EnemyRotation : MonoBehaviour
{
    [Tooltip("The maximum angle (in degrees) to rotate in either direction from the start rotation.")]
    public float maxRotationAngle = 45f;

    [Tooltip("The time in seconds it takes to complete one full back-and-forth rotation cycle.")]
    public float cycleDuration = 2f;

    [Tooltip("The axis around which the enemy will rotate.")]
    public Vector3 rotationAxis = Vector3.up;

    private Quaternion _startRotation;

    void Start()
    {
        // Store the initial rotation so we can rotate relative to it
        _startRotation = transform.rotation;
    }

    void Update()
    {
        if (cycleDuration <= 0f) return;

        // Calculate the current phase of the rotation cycle based on time.
        // A full sine wave cycle is 2 * PI. Dividing by cycleDuration makes one cycle take 'cycleDuration' seconds.
        float phase = (Time.time * Mathf.PI * 2f) / cycleDuration;
        
        // Calculate the angular offset using Sine for smooth easing in and out
        float angleOffset = Mathf.Sin(phase) * maxRotationAngle;

        // Apply the rotation relative to the initial starting rotation
        transform.rotation = _startRotation * Quaternion.AngleAxis(angleOffset, rotationAxis);
    }
}

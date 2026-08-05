using UnityEngine;

public class Sentient : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float currentSpeed = 0f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionMultiplier = 1f;

    public float NormalizedSpeed { get; private set; } = 0f;
    public float DetectionMultiplier => detectionMultiplier;
    public Collider SentientCollider { get; private set; }

    protected float MaxSpeed => maxSpeed;
    protected float CurrentSpeed 
    { 
        get => currentSpeed; 
        set => currentSpeed = value; 
    }
    
    protected virtual void Start()
    {
        SentientCollider = GetComponentInChildren<Collider>();
        if (maxSpeed != 0) 
        {
            NormalizedSpeed = currentSpeed / maxSpeed;
        }
    }

    protected virtual void Update()
    {
        if (maxSpeed != 0) 
        {
            NormalizedSpeed = currentSpeed / maxSpeed;
        }
    }
}

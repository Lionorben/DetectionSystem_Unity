using UnityEngine;

public class Sentient : MonoBehaviour
{
    [SerializeField] protected float maxSpeed = 5;
    [SerializeField] protected float currentSpeed = 0;
    public float normalizedSpeed = 0;
    [Header("Detection Settings")]
    public float detectionMultiplier = 1;

    public Collider collider;
    
    protected virtual void Start()
    {
        collider = GetComponentInChildren<Collider>();
        if (maxSpeed != 0) 
        {
            normalizedSpeed = currentSpeed / maxSpeed;
        }
    }

    protected virtual void Update()
    {
        if (maxSpeed != 0) 
        {
            normalizedSpeed = currentSpeed / maxSpeed;
        }
    }
}

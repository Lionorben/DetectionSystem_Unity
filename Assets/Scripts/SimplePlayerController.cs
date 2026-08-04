using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
public class SimplePlayerController : Sentient
{
    private Rigidbody rb;
    private Vector3 moveDirection;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
    }

    protected override void Update()
    {
        base.Update();
        
        float moveX = 0;
        float moveZ = 0;
        if (Keyboard.current.wKey.isPressed) 
        {
            moveZ++;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            moveZ--;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            moveX++;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            moveX--;
        }

        // Normalize the vector so diagonals are not faster
        moveDirection = new Vector3(moveX, 0f, moveZ).normalized;
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            // Move the Rigidbody using velocity. We keep the current Y velocity to allow gravity to act on it.
            rb.linearVelocity = new Vector3(moveDirection.x * maxSpeed, rb.linearVelocity.y, moveDirection.z * maxSpeed);

            // Update the current speed in the Sentient class
            currentSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
public class SimplePlayerController : Sentient
{
    private Rigidbody _rb;
    private Vector3 _moveDirection;

    protected override void Start()
    {
        base.Start();
        _rb = GetComponent<Rigidbody>();
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
        _moveDirection = new Vector3(moveX, 0f, moveZ).normalized;
    }

    private void FixedUpdate()
    {
        if (_rb != null)
        {
            // Move the Rigidbody using velocity. We keep the current Y velocity to allow gravity to act on it.
            _rb.linearVelocity = new Vector3(_moveDirection.x * MaxSpeed, _rb.linearVelocity.y, _moveDirection.z * MaxSpeed);

            // Update the current speed in the Sentient class
            CurrentSpeed = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z).magnitude;
        }
    }
}

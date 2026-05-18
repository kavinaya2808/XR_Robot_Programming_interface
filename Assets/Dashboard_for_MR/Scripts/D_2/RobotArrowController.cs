using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RobotArrowController : MonoBehaviour
{
    public float moveSpeed = 2f;         // units per second
    public float rotationSpeedDeg = 180f; // degrees per second (yaw)
    public bool zeroVelocityOnCollision = true;

    Rigidbody rb;
    float inputMove = 0f;
    float inputRotate = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Recommended Rigidbody settings
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // read inputs in Update for responsiveness
        inputMove = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) inputMove = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) inputMove = -1f;

        inputRotate = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) inputRotate = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) inputRotate = 1f;
    }

    void FixedUpdate()
    {
        // Rotation: apply yaw rotation using MoveRotation
        if (Mathf.Abs(inputRotate) > 0.001f)
        {
            float yaw = inputRotate * rotationSpeedDeg * Time.fixedDeltaTime;
            Quaternion delta = Quaternion.Euler(0f, yaw, 0f);
            Quaternion newRot = rb.rotation * delta;
            rb.MoveRotation(newRot);
        }

        // Movement: move along the Rigidbody's forward vector
        if (Mathf.Abs(inputMove) > 0.001f)
        {
            Vector3 displacement = transform.forward * (inputMove * moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + displacement);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!zeroVelocityOnCollision) return;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}

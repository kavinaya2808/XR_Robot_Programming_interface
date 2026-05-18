using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RobotWASDController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotationSpeedDeg = 180f;
    public bool zeroVelocityOnCollision = true;

    Rigidbody rb;
    float inputMove = 0f;
    float inputRotate = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        inputMove = 0f;
        if (Input.GetKey(KeyCode.W)) inputMove = 1f;
        if (Input.GetKey(KeyCode.S)) inputMove = -1f;

        inputRotate = 0f;
        if (Input.GetKey(KeyCode.A)) inputRotate = -1f;
        if (Input.GetKey(KeyCode.D)) inputRotate = 1f;
    }

    void FixedUpdate()
    {
        if (Mathf.Abs(inputRotate) > 0.001f)
        {
            float yaw = inputRotate * rotationSpeedDeg * Time.fixedDeltaTime;
            Quaternion delta = Quaternion.Euler(0f, yaw, 0f);
            Quaternion newRot = rb.rotation * delta;
            rb.MoveRotation(newRot);
        }

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

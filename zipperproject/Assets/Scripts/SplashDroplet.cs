using System;
using UnityEngine;

/// <summary>
/// 溅起的一滴水花：抛物线飞行，落回水面时回调，由 Level1Director 决定它变成鲨鱼还是小虾米。
/// </summary>
public class SplashDroplet : MonoBehaviour
{
    [SerializeField] float gravity = -20f;
    [SerializeField] float spinSpeed = 260f;

    Vector3 velocity;
    float waterLevel;
    Action<SplashDroplet, Vector3> onLand;
    bool landed;

    public void Launch(Vector3 initialVelocity, float waterY, Action<SplashDroplet, Vector3> landCallback)
    {
        velocity = initialVelocity;
        waterLevel = waterY;
        onLand = landCallback;
        landed = false;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || landed) return;

        velocity.y += gravity * dt;
        transform.position += velocity * dt;
        transform.Rotate(Vector3.right, spinSpeed * dt, Space.Self);

        if (transform.position.y <= waterLevel)
        {
            landed = true;
            Vector3 point = new Vector3(transform.position.x, waterLevel, transform.position.z);
            if (onLand != null) onLand(this, point);
            Destroy(gameObject);
        }
    }
}

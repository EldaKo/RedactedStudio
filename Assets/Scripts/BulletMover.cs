using UnityEngine;

/// <summary>
/// 총알 시각효과용 무브먼트.
/// 일직선 비행 후 자동 소멸.
/// </summary>
public class BulletMover : MonoBehaviour
{
    Vector3 velocity;
    float lifetime;
    float age;

    public void Init(Vector3 velocity, float lifetime)
    {
        this.velocity = velocity;
        this.lifetime = lifetime;
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;

        age += Time.deltaTime;
        if (age >= lifetime) Destroy(gameObject);
    }
}
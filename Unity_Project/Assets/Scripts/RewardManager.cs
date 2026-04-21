using UnityEngine;

public class RewardManager : MonoBehaviour
{
    //public float totalReward;
    public float TotalReward => totalReward;
    public float TotalDistance => totalDistance;

    private float totalDistance;

    private Vector3 lastPosition;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
    }

    public void ResetReward()
    {
        totalReward = 0f;
        totalDistance = 0f;
    }

    void FixedUpdate()
    {
        Vector3 delta = transform.position - lastPosition;
        float distance = delta.magnitude;

        float speed = rb.linearVelocity.magnitude;

        float fuelPenalty = speed > 12f ? 0.01f * speed : 0f;

        totalReward += distance * 0.5f;
        totalReward += speed * 0.02f;
        totalReward -= fuelPenalty;
        totalDistance += distance;

        lastPosition = transform.position;
    }

    public void AddCrashPenalty()
    {
        totalReward -= 5f;
    }

    public void FinishBonus()
    {
        totalReward += 20f;
    }
}

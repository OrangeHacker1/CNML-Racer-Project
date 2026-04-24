using UnityEngine;

public class RewardManager : MonoBehaviour
{

    // Internal stored values
    private float totalReward;
    private float totalDistance;

    // Public read-only access
    //public float totalReward;
    public float TotalReward => totalReward;
    public float TotalDistance => totalDistance;

    //private float totalDistance;

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
        lastPosition = transform.position;
    }


    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 delta = transform.position - lastPosition;
        float distance = delta.magnitude;

        float speed = rb.linearVelocity.magnitude;

        float reward = 0f;

        // progress
        reward += distance * 1.0f;

        // efficient speed sweet spot
        if (speed > 3f && speed < 10f)
            reward += 0.05f * speed;

        // wasteful speeding
        if (speed > 12f)
            reward -= 0.03f * speed;

        // idle punishment
        if (speed < 0.5f)
            reward -= 0.02f;

        totalReward += reward;
        totalDistance += distance;

        lastPosition = transform.position;
    }

    /* OLD
    void FixedUpdate()
    {

        if (rb == null) return;

        Vector3 delta = transform.position - lastPosition;
        float distance = delta.magnitude;

        float speed = rb.linearVelocity.magnitude;

        float fuelPenalty = speed > 12f ? 0.01f * speed : 0f;

        // Reward movement + progress
        totalReward += distance * 0.5f;
        totalReward += speed * 0.02f;
        // Subtract fuel cost
        totalReward -= fuelPenalty;


        totalDistance += distance;

        lastPosition = transform.position;
    }
    */


    public void AddCrashPenalty()
    {
        totalReward -= 5f;
    }

    public void FinishBonus()
    {
        totalReward += 20f;
    }
}

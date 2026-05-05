using UnityEngine;

public class CrashDetector : MonoBehaviour
{
    public RewardManager reward;
    public TrackManager manager;

    void Start()
    {
        //reward = GetComponent<RewardManager>();
        if (reward == null) reward = GetComponent<RewardManager>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Barrier")) return;

        reward.AddCrashPenalty();

        if (manager != null)
            manager.HandleCrash();
    }
}
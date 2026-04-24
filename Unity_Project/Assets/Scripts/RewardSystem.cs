// OUTDATED

using UnityEngine;

public class RewardSystem : MonoBehaviour
{
    public CarAgentController agent;

    public float GetReward()
    {
        CarState s = agent.GetState();

        float reward = 0f;

        reward += s.speed * 0.02f;                 // encourage movement
        reward -= Mathf.Abs(s.localVelocity.x) * 0.05f; // penalize sliding

        if (s.speed < 1f)
            reward -= 0.1f;

        return reward;
    }
}
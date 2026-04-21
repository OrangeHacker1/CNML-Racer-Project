using UnityEngine;

public class EpisodeManager : MonoBehaviour
{
    public CarAgentController agent;
    public Transform spawnPoint;

    public float maxEpisodeTime = 30f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= maxEpisodeTime)
        {
            ResetEpisode();
        }
    }

    public void ResetEpisode()
    {
        agent.ResetAgent(spawnPoint.position, spawnPoint.rotation);
        timer = 0f;
    }
}
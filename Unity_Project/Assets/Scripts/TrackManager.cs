using System.IO;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    public enum RunMode
    {
        Train,
        Validation,
        Test
    }

    [Header("References")]
    public TrackLoader loader;
    public RewardManager rewardManager;
    public Transform car;

    [Header("Experiment")]
    public RunMode mode = RunMode.Train;
    public int totalEpisodes = 100;
    public float episodeTimeout = 60f;

    private int currentEpisode;
    private float timer;
    private string[] trackFiles;
    private bool running;

    void Start()
    {
        BeginExperiment();
    }

    void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;

        if (timer >= episodeTimeout)
        {
            EndEpisode(false, false);
        }
    }

    public void BeginExperiment()
    {
        LoadTrackList();
        currentEpisode = 0;
        StartEpisode();
    }

    void LoadTrackList()
    {
        string folder =
            Application.dataPath +
            "/Scenes/GeneratedTracks/" +
            mode.ToString() + "/";

        trackFiles = Directory.GetFiles(folder, "*.json");
    }

    void StartEpisode()
    {
        if (currentEpisode >= totalEpisodes)
        {
            Debug.Log("Experiment Complete");
            running = false;
            return;
        }

        timer = 0f;

        string file = SelectTrack();
        loader.LoadByPath(file);

        rewardManager.ResetReward();

        running = true;
    }

    string SelectTrack()
    {
        if (mode == RunMode.Train)
        {
            int idx = Random.Range(0, trackFiles.Length);
            return trackFiles[idx];
        }

        int sequential = currentEpisode % trackFiles.Length;
        return trackFiles[sequential];
    }

    public void EndEpisode(bool finished, bool crashed)
    {
        running = false;

        EpisodeResult result = new EpisodeResult();
        result.mode = mode.ToString();
        result.trackName = Path.GetFileNameWithoutExtension(loader.CurrentTrackFile);
        result.episodeIndex = currentEpisode;
        result.reward = rewardManager.TotalReward;
        result.distance = rewardManager.TotalDistance;
        result.time = timer;
        result.crashed = crashed;
        result.finished = finished;

        ResultLogger.Save(result);

        currentEpisode++;
        StartEpisode();
    }
}

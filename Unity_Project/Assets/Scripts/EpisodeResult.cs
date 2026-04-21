using System;

[Serializable]
public class EpisodeResult
{
    public string mode;
    public string trackName;
    public int episodeIndex;
    public float reward;
    public float distance;
    public float time;
    public bool crashed;
    public bool finished;
    public int seed;
}

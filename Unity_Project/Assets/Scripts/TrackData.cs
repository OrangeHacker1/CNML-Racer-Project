using System;
using UnityEngine;

[Serializable]
public class TrackData
{
    public string trackName;
    public int seed;
    public Vector3[] points;
    public int segmentCount;
    public float segmentLength;
    public float roadWidth;
    public float difficulty;
}

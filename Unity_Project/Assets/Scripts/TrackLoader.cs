using System.IO;
using UnityEngine;

public class TrackLoader : MonoBehaviour
{
    [SerializeField] private ProceduralTrackGenerator generator;
    [SerializeField] private string fileName = "Track_1000.json";

    public void LoadTrack()
    {
        if (generator == null)
        {
            generator = FindFirstObjectByType<ProceduralTrackGenerator>();
        }

        string path = Application.dataPath + "/Scenes/GeneratedTracks/" + fileName;

        if (!File.Exists(path))
        {
            Debug.LogError("Track file not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        TrackData data = JsonUtility.FromJson<TrackData>(json);

        generator.LoadTrackData(data);

        Debug.Log("Loaded: " + fileName);
    }
}

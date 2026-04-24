using System.IO;
using UnityEngine;

public class TrackLoader : MonoBehaviour
{
    [SerializeField] private ProceduralTrackGenerator generator;
    [SerializeField] private string fileName = "Track_1000.json";

    public string CurrentTrackFile { get; private set; }

    public void LoadTrack()
    {
        string path = Path.Combine(
            Application.dataPath,
            "Scenes",
            "GeneratedTracks",
            fileName
        );

        LoadByPath(path);
    }

    public void LoadByPath(string path)
    {

        CurrentTrackFile = path;

        if (generator == null)
        {
            generator = FindFirstObjectByType<ProceduralTrackGenerator>();
        }

        if (generator == null)
        {
            Debug.LogError("No ProceduralTrackGenerator found in scene.");
            return;
        }

        if (!File.Exists(path))
        {
            Debug.LogError("Track file not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        TrackData data = JsonUtility.FromJson<TrackData>(json);

        generator.LoadTrackData(data);

        Debug.Log("Loaded track: " + Path.GetFileName(path));
    }
}
using System.Collections.Generic;
using UnityEngine;

public class CarRecorder : MonoBehaviour
{
    public List<CarState> recordedStates = new List<CarState>();

    public void Record(CarState state)
    {
        recordedStates.Add(state);
    }

    public void ClearLog()
    {
        recordedStates.Clear();
    }

    public int GetLogCount()
    {
        return recordedStates.Count;
    }
}

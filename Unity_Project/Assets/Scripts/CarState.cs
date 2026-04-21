using UnityEngine;

[System.Serializable]
public class CarState
{
    public Vector3 worldPosition;
    public Vector3 worldRotation;
    public Vector3 velocity;
    public Vector3 localVelocity;

    public float speed;
    public float steeringInput;

    public bool isDrifting;
    public bool tractionLocked;

    public float distanceTravelled;
    public float timeAlive;

}

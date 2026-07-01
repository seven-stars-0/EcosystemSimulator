using System;

[Serializable]
public class AppSettings
{
    public CameraSettings camera = new CameraSettings();
    public int skyboxIndex = 0;
    public int preyAnimalIndex = 0;
    public int predatorAnimalIndex = 1;
}

[Serializable]
public class CameraSettings
{
    public float orbitSpeedX = 0.3f;
    public float orbitSpeedY = 0.2f;
    public float pitchMin = 10f;
    public float pitchMax = 85f;
    public float zoomSpeed = 3f;
    public float panSpeed = 0.05f;
    public float panDamping = 8f;
    public float arrowSpeed = 20f;
    public float povEyeHeight = 0.8f;
    public float povSensitivity = 0.3f;
}
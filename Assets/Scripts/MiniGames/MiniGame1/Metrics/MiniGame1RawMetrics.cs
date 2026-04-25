using System;
using UnityEngine;

[Serializable]
public struct MiniGame1RawMetrics
{
    [Header("Path")]
    public float averageLateralDistanceMeters;

    [Header("Correction")]
    public float correctionAbsoluteErrorDeg;
    public int correctionSamples;

    [Header("Response Time")]
    public float responseTimeSumSeconds;
    public int responseTimeSamples;

    [Header("Speed")]
    public float speedStdDev;
    public float speedTarget;

    [Header("Camera")]
    public float cameraAngleErrorDegAvg;
    public int cameraSamples;

    public void Reset()
    {
        averageLateralDistanceMeters = 0f;
        correctionAbsoluteErrorDeg = 0f;
        correctionSamples = 0;
        responseTimeSumSeconds = 0f;
        responseTimeSamples = 0;
        speedStdDev = 0f;
        speedTarget = 0f;
        cameraAngleErrorDegAvg = 0f;
        cameraSamples = 0;
    }

    public void AddResponseTime(float seconds)
    {
        responseTimeSumSeconds += Mathf.Max(0f, seconds);
        responseTimeSamples++;
    }

    public float GetAverageResponseTime()
    {
        return responseTimeSamples <= 0 ? 0f : (responseTimeSumSeconds / responseTimeSamples);
    }

    public void AddCorrectionErrorDeg(float absErrorDeg)
    {
        correctionAbsoluteErrorDeg += Mathf.Max(0f, absErrorDeg);
        correctionSamples++;
    }

    public float GetAverageCorrectionErrorDeg()
    {
        return correctionSamples <= 0 ? 0f : (correctionAbsoluteErrorDeg / correctionSamples);
    }

    public void AddCameraErrorDeg(float absErrorDeg)
    {
        cameraAngleErrorDegAvg += Mathf.Max(0f, absErrorDeg);
        cameraSamples++;
    }

    public float GetAverageCameraErrorDeg()
    {
        return cameraSamples <= 0 ? 0f : (cameraAngleErrorDegAvg / cameraSamples);
    }
}


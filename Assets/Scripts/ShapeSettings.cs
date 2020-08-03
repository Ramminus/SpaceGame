using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ShapeSettings
{
    public float planetSize = 1f;
    public NoiseLayer[] noiseLayers;

    
}

[System.Serializable]
public class NoiseLayer
{
    public bool enabled;

    public NoiseSettings noiseSettings;
    public NoiseLayer()
    {
        enabled = true;
        noiseSettings = new NoiseSettings();
    }
    public void GetSeedValues(Planet planet, int index)
    {
        noiseSettings.GetSeedValues(planet, index);

    }
}
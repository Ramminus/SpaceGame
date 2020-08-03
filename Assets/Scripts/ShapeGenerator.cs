using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShapeGenerator 
{
    [SerializeField]
    ShapeSettings shapeSettings;
    NoiseFilter[] noiseFilters;

    public ShapeGenerator(ShapeSettings shapeSettings)
    {
        this.shapeSettings = shapeSettings;
        noiseFilters = new NoiseFilter[shapeSettings.noiseLayers.Length];
        for (int i = 0; i < noiseFilters.Length; i++)
        {
            noiseFilters[i] = new NoiseFilter(shapeSettings.noiseLayers[i].noiseSettings);
        }
    }

    public ShapeSettings ShapeSettings { get => shapeSettings; }

    public Vector3 CalculatePointOnPlanet(Vector3 pointOnUnitSphere)
    {
        float firstNoise = 0;
        float elevation =0;
        for (int i = 0; i < noiseFilters.Length; i++)
        {
            if(i == 0)
            {
                firstNoise = elevation += noiseFilters[i].Evaluate(pointOnUnitSphere);
            }
            if (shapeSettings.noiseLayers[i].enabled)
            {
                
                if(shapeSettings.noiseLayers[i].noiseSettings.useFirstLayerAsMask && i != 0 && firstNoise >0)
                {
                    elevation += noiseFilters[i].Evaluate(pointOnUnitSphere);
                }
                else if(!shapeSettings.noiseLayers[i].noiseSettings.useFirstLayerAsMask) elevation += noiseFilters[i].Evaluate(pointOnUnitSphere);
            }
        }
        return pointOnUnitSphere * shapeSettings.planetSize * (1 +elevation);
    }
}

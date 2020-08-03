using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class NoiseSettings 
{
    public enum NoiseType { Simple, Rigid, Squared, Crater, Terraced}
    public NoiseType noiseType;
    public bool useFirstLayerAsMask;
    public float strength = 1f;
    [Range(1, 8)]
    public int numberOfLayers;
    public float baseRoughness = 1f;
    public float roughness = 2f;
    public float persistence = 0.5f;
    public Vector3 centre;
    public float minValue;
   
    public void GetSeedValues(Planet planet, int index)
    {
        System.Random generator;
        if (GlobalVariables.rand == null)
        {
            generator = new System.Random();
        }
        else generator = GlobalVariables.rand;
        noiseType = NoiseType.Simple;
        if(index > 0)
        {
            int genNum = generator.Next(System.Enum.GetNames(typeof(NoiseType)).Length);
           
            noiseType = (NoiseType)genNum;
        }
        Vector2 vec = GetMinMaxForVariable(NoiseSettingsVariables.Strength, index);
        strength = Mathf.Lerp(vec.x,vec.y, (float)generator.NextDouble());
        vec = GetMinMaxForVariable(NoiseSettingsVariables.NumberOfLayers, index);
        numberOfLayers = generator.Next((int)vec.x, (int)vec.y);
        vec = GetMinMaxForVariable(NoiseSettingsVariables.BaseRoughness, index);
        baseRoughness = Mathf.Lerp(vec.x, vec.y, (float)generator.NextDouble());
        vec = GetMinMaxForVariable(NoiseSettingsVariables.Roughness, index);
        roughness = Mathf.Lerp(vec.x, vec.y, (float)generator.NextDouble());
        vec = GetMinMaxForVariable(NoiseSettingsVariables.Persistence, index);
        persistence = Mathf.Lerp(vec.x, vec.y, (float)generator.NextDouble());
        centre = planet.transform.position + new Vector3(generator.Next(1000), generator.Next(1000), generator.Next(1000)) ;
        vec = GetMinMaxForVariable(NoiseSettingsVariables.MinValue, index);
        minValue = Mathf.Lerp((strength + persistence) * 0.7f, strength + persistence, (float)generator.NextDouble());
        useFirstLayerAsMask = index == 0 ? false : generator.Next(2) != 0;
    }
    public Vector2 GetMinMaxForVariable(NoiseSettingsVariables var, int noiseLayerIndex)
    {
        switch (var)
        {
            case NoiseSettingsVariables.Type:
                if (noiseLayerIndex == 0) return new Vector2(0, .3f);
                else return new Vector2(.2f, .4f);
            case NoiseSettingsVariables.Strength:
                if (noiseLayerIndex == 0) return new Vector2(.01f, .05f);
                else return new Vector2(.01f, .1f);
            case NoiseSettingsVariables.NumberOfLayers:
                if (noiseLayerIndex == 0) return new Vector2(1, 3);
                else return new Vector2(2, 5);
            case NoiseSettingsVariables.BaseRoughness:
                if (noiseLayerIndex == 0) return new Vector2(3f, 4f);
                else return new Vector2(5f, 7f);
            case NoiseSettingsVariables.Roughness:
                if (noiseLayerIndex == 0) return new Vector2(1f, 1.5f);
                else return new Vector2(3f, 6f);
            case NoiseSettingsVariables.Persistence:
                if (noiseLayerIndex == 0) return new Vector2(.1f, .2f);
                else return new Vector2(.1f, .3f);
            case NoiseSettingsVariables.MinValue:
                if (noiseLayerIndex == 0) return new Vector2(.4f, .2f);
                else return new Vector2(.8f, .7f);
            default:
                    return Vector2.zero;
        }
    }
    public enum NoiseSettingsVariables
    {
        Type,
        Strength,
        NumberOfLayers,
        BaseRoughness,
        Roughness,
        Persistence,
        Centre,
        MinValue
    }
}

public struct ComputeNoiseData
{
    public int type;
    public float strength;
    public int numberOfLayers;
    public float baseRoughness;
    public float roughness;
    public float persistence;
    public Vector3 centre;
    public float minValue;
    public int useFirstLayerAsMask;

    Vector3 dummy;

    public ComputeNoiseData(NoiseSettings settings)
    {
        this.type = (int)settings.noiseType;
        this.strength = settings.strength;
        this.numberOfLayers = settings.numberOfLayers;
        this.baseRoughness = settings.baseRoughness;
        this.roughness = settings.roughness;
        this.persistence = settings.persistence;
        this.centre = settings.centre;
        this.minValue = settings.minValue;
        this.useFirstLayerAsMask = settings.useFirstLayerAsMask ? 1 : 0;
        dummy = Vector3.zero;
    }
}
public struct TBool
{
    private readonly byte _value;
    public TBool(bool value) { _value = (byte)(value ? 1 : 0); }
    public static implicit operator TBool(bool value) { return new TBool(value); }
    public static implicit operator bool(TBool value) { return value._value != 0; }
}
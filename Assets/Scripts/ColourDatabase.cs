using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewColourDatabase", menuName = "Planet/Colour Database")]
public class ColourDatabase : ScriptableObject
{
    [SerializeField]
    Material waterMaterial;
    public ColourSettings[] colours;
    public Material[] waterMaterials;
    public Material[] lowQualityWaterMaterials;
    public WaterColour[] waterColours;



    public void Initialize()
    {

        waterMaterials = new Material[waterColours.Length];
        int index = 0;
        foreach (WaterColour col in waterColours)
        {
            Material mat = new Material(waterMaterial);
            mat.SetColor("Color_CEA15639", col.waterColour);
            mat.SetColor("Color_B3D0693E", col.foamColour);
            waterMaterials[index] = mat;
            index++;
        }
        GlobalVariables.ColoursInitialized = true;
    }
    public ColourSettings GetRandomColourSettings()
    {
        return colours[GlobalVariables.rand.Next(colours.Length)];
    }
    public Material[] GetRandomWaterMat()
    {
        if (!GlobalVariables.ColoursInitialized) Initialize();
        int index = GlobalVariables.rand.Next(waterMaterials.Length);
        return new Material[]{ waterMaterials[index],  lowQualityWaterMaterials[index]};
    }
    
   
}
[System.Serializable]
public struct WaterColour
{

    public Color waterColour;
    [ColorUsage(false, true)]
    public Color foamColour;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewColourSetting", menuName = "Planet/New Colour Setting")]
public class ColourSettings : ScriptableObject
{
    public Gradient planetColour;
    public Color noiseColour;
}

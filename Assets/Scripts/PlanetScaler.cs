using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetScaler : MonoBehaviour
{
    
    public Vector3d planetWorldPos;
    Planet planet;

    private void Awake()
    {
        planet = GetComponent<Planet>();
        
    }
    private void LateUpdate()
    {
        if(GlobalVariables.CurrentPlanet == null)transform.position = (Vector3)planetWorldPos - (Vector3)GlobalVariables.playerWorldPos;
    }


    public double GetDistanceFromPlayer()
    {
        return Vector3d.Distance(planetWorldPos, GlobalVariables.playerWorldPos);
    }
}


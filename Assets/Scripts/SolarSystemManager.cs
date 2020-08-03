using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarSystemManager : MonoBehaviour
{
    [SerializeField]
    Planet planetPrefab;
    [SerializeField]
    float minSphereSize = 40000;
    [SerializeField]
    float maxSphereSize = 60000;
    public LODSettings[] lodSettings;
    public LODSettings[] chunkSettings;
    public LODSettings[] quadTreeLODs;
    public LODSettings[] quadTreeLODsWater;
    public float nodeSize = 50000;
    public float startPoint = 100000;
    [SerializeField]
    int maxPlanets;
    [SerializeField, ReadOnly]
    float gridStart;
    [SerializeField]
    Planet[] planetsInSolarSystem;
    System.Action OnCompleteCoroutine;

    public static SolarSystemManager instance;
    int index = -1;
    int completedPlanets = 0;
    private void Awake()
    {
        if (instance == null) instance = this;
        OnCompleteCoroutine += CreateColliders;
    }
    private void Start()
    {
        SpawnPlanets();
    }
    public void SpawnPlanets()
    {
        int amountLeft = GlobalVariables.rand.Next(maxPlanets + 1);
        amountLeft += 1;
        
        float startDistance = startPoint;
        for (int x = 0; x < amountLeft; x++)
        {

            
                Planet planetInstance = Instantiate(planetPrefab, Vector3.zero, Quaternion.identity);
                Vector3 pos = Vector3.zero + Vector3.forward * startDistance;
                float angle = Mathf.Lerp(0f, 360f, (float)GlobalVariables.rand.NextDouble());
                pos = Quaternion.AngleAxis(angle, Vector3.up) * pos;
                planetInstance.SetWorldPos(new Vector3d(pos));
                planetInstance.transform.parent = transform;

                planetInstance.SetSphereSize(Mathf.Lerp(minSphereSize, maxSphereSize, (float)GlobalVariables.rand.NextDouble()));
                startDistance += nodeSize;
            
        }




    }
    public int GetTerrainFaceLodIndex(Vector3 terrainFacePos, float sphereSize)
    {
        float distance = Vector3.Distance(terrainFacePos, GlobalVariables.playerObject.transform.position);

        for (int i = 0; i < lodSettings.Length; i++)
        {
            if (distance < lodSettings[i].distanceThreshold * sphereSize || i == lodSettings.Length - 1)
            {
                return i;
            }
        }
        return lodSettings.Length - 1;
    }
    public int GetQuadTreeLODIndex(Vector3 terrainFacePos, float sphereSize, bool isWater)
    {
        float distance = Vector3.Distance(terrainFacePos, GlobalVariables.playerObject.transform.position);
        if (!isWater)
        {
            for (int i = 0; i < quadTreeLODs.Length; i++)
            {
                if (distance < quadTreeLODs[i].distanceThreshold  || i == quadTreeLODs.Length - 1)
                {
                    return i;
                }
            }
            return quadTreeLODs.Length - 1;
        }
        else
        {
            for (int i = 0; i < quadTreeLODsWater.Length; i++)
            {
                if (distance < quadTreeLODsWater[i].distanceThreshold  || i == quadTreeLODsWater.Length - 1)
                {
                    return i;
                }
            }
            return quadTreeLODsWater.Length - 1;
        }
    }
    public LODSettings getLodSettingsByIndex(int index)
    {
        return lodSettings[index];
    }
    public LODSettings GetQuadTreeLOD(int index)
    {
        return quadTreeLODs[index];
    }
    public void OnPlanetCompleteRender()
    {
        completedPlanets++;
        if (completedPlanets == planetsInSolarSystem.Length)
        {
            //Debug.Log(planetsInSolarSystem[0].terrainFaces.Length);
            //CreateColliders();
        }
    }
    public void CreateColliders()
    {
        index++;
        if (index == planetsInSolarSystem.Length) return;
        //StartCoroutine(planetsInSolarSystem[index].CreateColliders(OnCompleteCoroutine));
    }
}
[System.Serializable]
public struct LODSettings
{
    public int resolution;
    public float distanceThreshold;
   
}
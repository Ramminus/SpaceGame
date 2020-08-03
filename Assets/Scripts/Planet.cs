using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class Planet : MonoBehaviour
{
    public bool subscribeToLod;

    public GameObject chunkPrefab;
    public enum Directions { Up,Down,Right,Left,Forward,Back}
    Vector3[] directions = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
    [Header("Thread Settings")]
    public bool useThreading;
    [Header("Water Settings")]    
    public bool isWater;
    public Planet waterObj;
    public Planet lowWaterObj;
    float waterHeight;
    public bool randomize;
    [Range(1,124)]
    public int resolution = 10;
    public int resolutionMultiplier = 8;
    [Range(1.2f, 3f)]
    public float atmosphereMultiplier = 1.4f;
    float atmosphereLevel;
    public float AtmosphereLevel { get => atmosphereLevel; }
    bool inAtmosphere = false;
    public bool InAtmosphere { get => inAtmosphere; }
    [SerializeField]
    float sphereSize;
    public float SphereSize { get => sphereSize; }
    //MeshFilter[] meshFilters;
    public int chunksPerFace = 1;
    public MinMax minMax;
    //[HideInInspector]
    //public TerrainFace[] terrainFaces;
    [HideInInspector]
    PlanetFace[] planetFaces;
    [SerializeField]
    Material mat;
    [SerializeField]
    bool debugMaterialMode;
    Material matInstance;
    [SerializeField]
    SphereCollider col;
    Transform parent;
    [SerializeField]
    ComputeShader shader, waterShader;
    [HideInInspector]
    public bool shapeFoldout, coloutFoldout;    
    public ShapeSettings shapeSettings;

    public ColourSettings colourSettings;

    ShapeGenerator shapeGenerator;
    [SerializeField]
    PlanetScaler planetScaler;
    Texture2D planetTex;
    const int textureResolution = 50;
    int completedRenders = 0;
    public Queue<TerrainFace> LodUpdateQueue = new Queue<TerrainFace>();
    bool createdColliders;

    int currentChunkLODindex;
    public int CurrentLODIndex { get => currentChunkLODindex; }


    public List<TerrainFace> chunksToLoad = new List<TerrainFace>();
    private void Awake()
    {

        if (!isWater)
        {
            GlobalVariables.OnUpdatePlayerPos += UpdatePlanetLod;
        }
    }
    private void OnDestroy()
    {
        if (!isWater)
        {
            GlobalVariables.OnUpdatePlayerPos -= UpdatePlanetLod;
        }
    }
    private void Start()
    {
        
        if (!isWater)
        {
            name = GlobalVariables.instance.GetRandomName();
            col.transform.localScale = Vector3.one * sphereSize * 2;
            atmosphereLevel = atmosphereMultiplier * sphereSize;
            waterHeight = (float)GlobalVariables.rand.NextDouble();
            waterHeight = Mathf.Clamp(0.1f, 0.35f, waterHeight);
            Material[] waterMats = GlobalVariables.instance.colourDatabase.GetRandomWaterMat();
            waterObj.SetMat(waterMats[0]);
            lowWaterObj.SetMat(waterMats[1]);
         
            waterObj.sphereSize = 1;
            
            CreatePlanet();
            waterObj.gameObject.SetActive(false);
        }
    }
    private void Update()
    {
    

        if (chunksToLoad.Count > 0)
        {
            chunksToLoad[0].ConstructMesh();
            chunksToLoad.RemoveAt(0);
        }
        
        
        //if(planetScaler.GetDistanceFromPlayer()< atmosphereMultiplier && !createdColliders )
        //{
        //    createdColliders = true;
            
        //    Debug.Log("Creating Colliders");
        //}
    }
    public void SetWorldPos(Vector3d worldPos)
    {
        planetScaler.planetWorldPos = worldPos;
    }
    public void SetSphereSize(float sphereSize)
    {
        this.sphereSize = sphereSize;
    }
    public void UpdatePlanetLod()
    {
        if (GlobalVariables.CurrentPlanet != null && GlobalVariables.CurrentPlanet != this) return;
        double distFromPlayer = Vector3d.Distance(GlobalVariables.playerWorldPos, planetScaler.planetWorldPos);
        
        //GetChunkResolotion(distFromPlayer);
        bool isCurrentlyInAtmosphere = distFromPlayer < atmosphereLevel;
        if(inAtmosphere && !isCurrentlyInAtmosphere)
        {
            OnExitAtmosphere();
            lowWaterObj.gameObject.SetActive(true);
            waterObj.gameObject.SetActive(false);
        }
        else if(!inAtmosphere && isCurrentlyInAtmosphere)
        {
            GlobalVariables.EnterAtmosphere(this);
            lowWaterObj.gameObject.SetActive(false);
            waterObj.gameObject.SetActive(true);
        }
        inAtmosphere = isCurrentlyInAtmosphere;
        if (inAtmosphere)
        {
            //SetTerrainFaceLOD();
            //StartCoroutine(UpdateChunkLOD());
        }
    }

    private void OnExitAtmosphere()
    {
        GlobalVariables.ExitAtmosphere(this);
    }

    IEnumerator UpdateChunkLOD()
    {
        
        
        int queueCount = LodUpdateQueue.Count;
        for (int i = 0; i < queueCount; i++)
        {
            if (LodUpdateQueue.Count == 0) yield break;
            LodUpdateQueue.Peek().ConstructMesh();
            LodUpdateQueue.Dequeue();
            if (i % 2 == 0 && i != 0) yield return new WaitForEndOfFrame();
        }
        yield break;

    }
    public void SetTerrainFaceLOD()
    {
        int[] directions = GetFacingFacesToPlayer(0.2f);
        foreach (int dir in directions)
        {
            foreach (TerrainFace face in planetFaces[dir].chunks)
            {
                if (face != null)
                {
                    int index = SolarSystemManager.instance.GetTerrainFaceLodIndex(face.ChunkPosition, sphereSize);
                    LODSettings settings = SolarSystemManager.instance.getLodSettingsByIndex(index);
                    face.ChangeLod(index, settings);
                }
            }
        }
    }
    void GetChunkResolotion(double distFromPlayer)
    {
        for (int i = 0; i < SolarSystemManager.instance.chunkSettings.Length; i++)
        {
            if(distFromPlayer < SolarSystemManager.instance.chunkSettings[i].distanceThreshold * SphereSize|| i == SolarSystemManager.instance.chunkSettings.Length-1)
            {
                if(currentChunkLODindex != i)
                {
                    currentChunkLODindex = i;
                    chunksPerFace = SolarSystemManager.instance.chunkSettings[i].resolution;
                    StopCoroutine(updateChunksPerFace());
                    StartCoroutine(updateChunksPerFace());
                    
                
                    return;
                }
                else
                {
                    return;
                }
            }
        }
    }
    List<int> GetDirectionsToPlayer()
    {
        List<int> prioList = new List<int>();
        prioList.Add(GetDirectionFromPlayer());
        List<int> list = new List<int>();
        Vector3 playerVec = (transform.position - GlobalVariables.playerObject.transform.position).normalized;
        int index =  0;
        foreach(Vector3 dir in directions)
        {
            if(Vector3.Dot(dir, playerVec )< 0)
            {
               if(!prioList.Contains(index)) prioList.Add(index);
            }
            else
            {
                list.Add(index);
            }
            index++;
        }
        prioList.AddRange(list);
        return prioList;
    }
    public int GetDirectionFromPlayer()
    {
        Vector3 playerVec = (transform.position - GlobalVariables.playerObject.transform.position).normalized;
        int index = 0;
        float value = -2;
        int currentIndex = 0;
        foreach (Vector3 dir in directions)
        {
            float dot = Vector3.Dot(dir, playerVec);
            dot *= -1;
            if (dot > value)
            {
                value = dot;
                index = currentIndex;
            }
            currentIndex++;
        }
        return index;
    }
    public int[] GetFacingFacesToPlayer(float dotThreshold)
    {
        Vector3 playerVec = (transform.position - GlobalVariables.playerObject.transform.position).normalized;
        List<int> tempList = new List<int>();
        int currentIndex = 0;
        foreach (Vector3 dir in directions)
        {
            float dot = Vector3.Dot(dir, playerVec);
            dot *= -1;
            if (dot > dotThreshold)
            {
                tempList.Add(currentIndex);
                
            }
            currentIndex++;
        }
        return tempList.ToArray();
    }
    public Vector3 GetUIPanelPoint()
    {
        return transform.position + (GlobalVariables.playerObject.transform.TransformDirection(Vector3.right).normalized) * sphereSize;
        
    }
    IEnumerator updateChunksPerFace()
    {
        if (planetFaces == null) yield break;
        string[] enumNames = System.Enum.GetNames(typeof(Directions));
        GetDirectionsToPlayer();
    
        List<int> prioList = GetDirectionsToPlayer();
        for (int i = 0; i < 6; i++)
        {
            
            Directions direction = (Directions)prioList[i];
            
            

                int chunksPerFaceSquared = chunksPerFace * chunksPerFace;
                TerrainFace[] oldFaces = null;
                if (planetFaces[(int)direction].chunks != null) oldFaces = planetFaces[(int)direction].chunks;

                planetFaces[(int)direction] = new PlanetFace(new TerrainFace[chunksPerFaceSquared], directions[(int)direction]);
                PlanetFace planetFace = planetFaces[(int)direction];

                int nextFrameUpdate = 0;

                for (int j = 0; j < chunksPerFaceSquared; j++)
                {
                    int index = j;


                    CreateFaceChunks(planetFace, index, false);
                    nextFrameUpdate++;
                    if (nextFrameUpdate == 5)
                    {
                        yield return new WaitForEndOfFrame();
                        nextFrameUpdate = 0;
                    }

                }
                GenerateColours();
                if (oldFaces != null)
                {
                    for (int x = 0; x < oldFaces.Length; x++)
                    {
                        if (oldFaces[x] != null) Destroy(oldFaces[x].gameObject);
                    }
                }
            
            
           
        }
        
    }
    [Button]
    public void CreatePlanet()
    {
        
        if (randomize && !isWater)
        {
          
            shapeSettings.noiseLayers = new NoiseLayer[GlobalVariables.rand.Next(3, 4)];
            for (int i = 0; i < shapeSettings.noiseLayers.Length; i++)
            {
                shapeSettings.noiseLayers[i] = new NoiseLayer();
                shapeSettings.noiseLayers[i].GetSeedValues(this, i);

            }
        }
        
    
        Initialize();
        GenerateMesh();
        GenerateColours();
    }
    public void SetMat(Material material)
    {
        mat = material;
    }
    
    [Button]
    public Vector2 getChunkStart(int currentChunkNumber)
    {
        float x = currentChunkNumber % chunksPerFace;
        
        x = (x * (1f/(float)chunksPerFace));
        float y = Mathf.Floor(currentChunkNumber / chunksPerFace) * (1f / (float)chunksPerFace);
        Vector2 vec = new Vector2(x, y);

        return vec;
    }
    public Vector2 getChunkStart(int currentChunkNumber, int chunksPerFace)
    {
        float x = currentChunkNumber % chunksPerFace;

        x = (x * (1f / (float)chunksPerFace));
        float y = Mathf.Floor(currentChunkNumber / chunksPerFace) * (1f / (float)chunksPerFace);
        Vector2 vec = new Vector2(x, y);

        return vec;
    }
    void Initialize()
    {

        if (randomize && !isWater)
        {
            colourSettings = GlobalVariables.instance.colourDatabase.GetRandomColourSettings();
            
            chunksPerFace = SolarSystemManager.instance.chunkSettings[SolarSystemManager.instance.chunkSettings.Length - 1].resolution;
            currentChunkLODindex = SolarSystemManager.instance.chunkSettings.Length - 1;

        }
        planetFaces = new PlanetFace[6];
        if(!debugMaterialMode)matInstance = new Material(mat);
        else matInstance = mat;
        if (isWater) matInstance = mat;
        minMax = new MinMax(float.MaxValue, float.MinValue);
        shapeGenerator = new ShapeGenerator(shapeSettings);
        if (parent != null) DestroyImmediate(parent.gameObject);
        parent = new GameObject().GetComponent<Transform>();
        parent.parent = transform;
        parent.localScale = Vector3.one;
        parent.transform.localPosition = Vector3.zero;
        int chunksPerFaceSquared = chunksPerFace * chunksPerFace;
        //if (meshFilters == null || terrainFaces == null || meshFilters.Length != chunksPerFaceSquared * 6 || terrainFaces.Length != chunksPerFaceSquared * 6)
        //{
        //    meshFilters = new MeshFilter[6 * (chunksPerFace * chunksPerFace)];
        //    terrainFaces = new TerrainFace[6 * (chunksPerFace * chunksPerFace)];
        //}
      

        
        for (int i = 0; i < 6; i++)
        {
            planetFaces[i] = new PlanetFace(new TerrainFace[chunksPerFaceSquared],  directions[i]);
            PlanetFace planetFace = planetFaces[i];
            for (int j = 0; j < chunksPerFaceSquared; j++)
            {
                int index = j;
                CreateFaceChunks(planetFace, index, true);



            }
            


        }
        
       
    }
    public void CreateFaceChunks(PlanetFace planetFace, int index, bool firstInitialization)
    {
        TerrainFace face = Instantiate(chunkPrefab).GetComponent<TerrainFace>();
        planetFace.chunks[index] = face;
       
        //MeshCollider col  = meshFilters[index].gameObject.AddComponent<MeshCollider>();
        //col.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning;
        planetFace.chunks[index].shader = isWater ? waterShader : shader;
        planetFace.chunks[index].startXY = getChunkStart(index, chunksPerFace);
        planetFace.chunks[index].planet = this;
        planetFace.chunks[index].chunksPerFace = chunksPerFace;
        planetFace.chunks[index].firstInitilization = firstInitialization;
        planetFace.chunks[index].Init(shapeGenerator, parent, matInstance, !isWater? SolarSystemManager.instance.quadTreeLODs[SolarSystemManager.instance.quadTreeLODs.Length-1].resolution : resolution, planetFace.direction, sphereSize);

        planetFace.chunks[index].quadTreeLod = !isWater ? SolarSystemManager.instance.quadTreeLODs.Length-1 : SolarSystemManager.instance.quadTreeLODsWater.Length - 1;
        planetFace.chunks[index].isRoot = true;

        planetFace.chunks[index].ExecuteCompute();
        //planetFace.chunks[index].ConstructMesh();
        //planetFace.chunks[index].CreateQuadTreeChildren();


    }
    [Button]
    public void SetFaceResolution(Directions direction,int resolution, int chunksPerFace)
    {

       
        int chunksPerFaceSquared = chunksPerFace * chunksPerFace;
        
        for (int i = 0; i < planetFaces[(int)direction].chunks.Length; i++)
        {
            Destroy(planetFaces[(int)direction].chunks[i].gameObject);
        }
        planetFaces[(int)direction] = new PlanetFace(new TerrainFace[chunksPerFaceSquared],  directions[(int)direction]);
        PlanetFace planetFace = planetFaces[(int)direction];


       
        for (int j = 0; j < chunksPerFaceSquared; j++)
        {
            int index = j;


            CreateFaceChunks(planetFace, index, false);
        }
        GenerateColours();
    }
    public void OnCompleteRender()
    {
        completedRenders++;
        if (completedRenders < 6 * chunksPerFace * chunksPerFace - 1) return;
        if (!isWater && waterObj != null)
        {
            waterObj.transform.localScale = Vector3.one * Mathf.Lerp(minMax.min, minMax.max, waterHeight);
            lowWaterObj.transform.localScale = (Vector3.one ) * Mathf.Lerp(minMax.min, minMax.max, waterHeight);
            //Debug.Log(terrainFaces.Length);
            SolarSystemManager.instance.OnPlanetCompleteRender();
            waterObj.CreatePlanet();
            lowWaterObj.CreatePlanet();
            waterObj.UpdateColoursAsWater();
            lowWaterObj.UpdateColoursAsWater();
        }

    }
    public void GenerateMesh()
    {
        int i = 0;
        foreach (PlanetFace face in planetFaces)
        {
            foreach (TerrainFace terrainFace in face.chunks)
            {

                terrainFace.ConstructMesh();
                i++;
            }
        }
    }
    public void GenerateColours()
    {
        if (isWater) return;
        UpdateColours();
        //foreach (PlanetFace face in planetFaces)
        //{
        //    foreach (MeshFilter m in face.meshFilters)
        //    {

        //        m.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_MinMax", new Vector4(minMax.min, minMax.max));
        //        m.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_Texture", planetTex);
        //    }
        //}
        matInstance.SetVector("_MinMax", new Vector4(minMax.min, minMax.max));
        matInstance.SetTexture("_Texture", planetTex);
        matInstance.SetColor("_NoiseColour", colourSettings.noiseColour);

    }
    public void GenerateColour()
    {

    }
    public void UpdateColoursAsWater()
    {
        foreach (PlanetFace face in planetFaces)
        {
            foreach (TerrainFace terrainFace in face.chunks)
            {
                terrainFace.MeshRenderer.sharedMaterial = mat;
            }
        }
    }
    public void UpdateColours()
    {
        Color[] colours = new Color[textureResolution];
        for (int i = 0; i < colours.Length; i++)
        {
            colours[i] = colourSettings.planetColour.Evaluate(i / (textureResolution - 1f));
        }
        planetTex = new Texture2D(textureResolution, 1);
        planetTex.SetPixels(colours);
        planetTex.Apply();


    }

    //public IEnumerator CreateColliders(System.Action onComplete)
    //{
    //    //yield return new WaitForEndOfFrame();
    //    //foreach (TerrainFace face in terrainFaces)
    //    //{
    //    //    face.GenerateCollider();
    //    //    yield return new WaitForEndOfFrame();
           
            
    //    //}
    //    //onComplete.Invoke();
    //    //Debug.Log("completed");
    //}
}
[System.Serializable]
public class PlanetFace
{

    public TerrainFace[] chunks;
    public Vector3 direction;
    public PlanetFace(TerrainFace[] chunks, Vector3 direction)
    {
        this.chunks = chunks;

        this.direction = direction;
    }

    public void ShowChunks(bool show)
    {
        foreach(TerrainFace chunk in chunks)
        {
            chunk.gameObject.SetActive(show);
        }
    }
}
[System.Serializable]
public struct MinMax
{
    public float min;
    public float max;

    public MinMax(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
    public void Evaluate(Vector2 newMinMax)
    {
        if (newMinMax.x < min) min = newMinMax.x;
        if (newMinMax.y > max) max = newMinMax.y;
    }
    public void Evaluate(float newMinMax)
    {
        if (newMinMax < min) min = newMinMax;
        if (newMinMax > max) max = newMinMax;
    }
}
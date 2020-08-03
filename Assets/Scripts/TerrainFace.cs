using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Threading;
[System.Serializable]

public class TerrainFace : MonoBehaviour
{
    Mesh mesh;
    [SerializeField, ReadOnly]
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    float sphereSize;
    [HideInInspector]
    public ComputeShader shader;
    [ReadOnly]
    public Vector2 startXY;
    [SerializeField]
    ShapeGenerator shapeGenerator;
    [HideInInspector]
    public Planet planet;
    ComputeBuffer computeBuffer;
    ComputeBuffer layerData;
    ComputeBuffer uvBuffer;
    MeshData threadData;
    [HideInInspector]
    public bool firstInitilization;
    bool threadComplete = false;
    [SerializeField, HideInInspector]
    MeshData[] meshData;
    WaterMeshData[] watermMeshData;
    List<ComputeNoiseData> layerDataList = new List<ComputeNoiseData>();
    public int chunksPerFace;
    [SerializeField]
    int currentLodindex;
    [SerializeField]
    MeshCollider col;
    public MeshCollider Col { get => col; }
    [SerializeField,ReadOnly]
    Vector3 chunkOffset;
    public Vector3 ChunkPosition { get => planet.transform.position + chunkOffset; }
    [SerializeField]
    MeshFilter meshFilter;
    public MeshFilter MeshFilter { get => meshFilter; }
    [SerializeField] MeshRenderer meshRenderer;
    public MeshRenderer MeshRenderer { get => meshRenderer; }
    [SerializeField,ReadOnly]
    float distToPlayer;

    //QuadTree Implementation
    TerrainFace[] quadTreeChildren;
    TerrainFace quadTreeParent;
    public int quadTreeLod;
    public bool isRoot;
    public int childrenReady;

    [SerializeField,ReadOnly]
    bool active;
    bool created;
    bool built;
    // Start is called before the first frame update


    private void OnDestroy()
    {
        if (planet.subscribeToLod) GlobalVariables.OnUpdatePlayerPos -= CheckChunkPositions;
    }
    private void CheckChunkPositions()
    {
        if (!active) return;
        distToPlayer = Vector3.Distance(ChunkPosition, GlobalVariables.playerObject.transform.position);
        int index = SolarSystemManager.instance.GetQuadTreeLODIndex(ChunkPosition, sphereSize, planet.isWater);
        int parentLod = !planet.isWater ?  SolarSystemManager.instance.quadTreeLODs.Length : SolarSystemManager.instance.quadTreeLODsWater.Length;
        if (!isRoot) parentLod = SolarSystemManager.instance.GetQuadTreeLODIndex(quadTreeParent.ChunkPosition, sphereSize, planet.isWater);


        if (!isRoot && quadTreeParent.quadTreeLod < parentLod)
        {
            quadTreeParent.ActivateChunk();
            Debug.Log("Decreaseing LOD");
            return;
        }
        else
        {
            if ((index < quadTreeLod)) CreateQuadTreeChildren();
         
        }





    }

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, float sphereSize)
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
        this.sphereSize = sphereSize;
        this.shapeGenerator = shapeGenerator;

    }

    public void Init(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, float sphereSize)
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
        this.sphereSize = sphereSize;
        this.shapeGenerator = shapeGenerator;
        Vector2 percent = new Vector2(0.5f / (float)planet.chunksPerFace, 0.5f / (float)planet.chunksPerFace);
        percent += startXY;
        chunkOffset = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
        chunkOffset = planet.isWater ? Vector3.Normalize(chunkOffset) * planet.transform.localScale.x : Vector3.Normalize(chunkOffset) * planet.SphereSize;
        currentLodindex = SolarSystemManager.instance.lodSettings.Length - 1;

        if (planet.isWater) Debug.Log(transform.localScale.x);

    }
    public void Init(ShapeGenerator shapeGenerator, Transform parent, Material mat, int resolution, Vector3 localUp, float sphereSize)
    {
        active = true;
        transform.parent = parent;
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;
        mesh = new Mesh();
        MeshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = mat;
        if (planet.isWater) meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        this.resolution = resolution;
        this.localUp = localUp;
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
        this.sphereSize = sphereSize;
        this.shapeGenerator = shapeGenerator;
        Vector2 percent = new Vector2(0.5f / (float)chunksPerFace, 0.5f / (float)chunksPerFace);
        percent += startXY;
        chunkOffset = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
        chunkOffset = planet.isWater ? Vector3.Normalize(chunkOffset) * planet.transform.localScale.x : Vector3.Normalize(chunkOffset) * planet.SphereSize;
        currentLodindex = SolarSystemManager.instance.lodSettings.Length - 1;
        if (planet.subscribeToLod) GlobalVariables.OnUpdatePlayerPos += CheckChunkPositions;
        if (quadTreeLod > 1) col.enabled = false;
        quadTreeChildren = new TerrainFace[4];

    }


    public void ChangeLod(int index, LODSettings settings)
    {
        if (index == currentLodindex) return;
        else
        {

            currentLodindex = index;
            resolution = settings.resolution;
            if (!planet.LodUpdateQueue.Contains(this)) planet.LodUpdateQueue.Enqueue(this);

        }
    }
    //private void Update()
    //{
    //    if (threadComplete)
    //    {
    //        ConstructMeshThread();
    //        threadComplete = false;
    //    }
    //}
    [Sirenix.OdinInspector.Button]
    public void ConstructMesh()
    {
        if (planet.isWater) ConstructMeshWater();
        else ConstructMeshPlanet();
       
    }
    public void ExecuteCompute()
    {
        if (!planet.chunksToLoad.Contains(this)) planet.chunksToLoad.Add(this);
        if (!planet.isWater) ExecuteComputePlanet();
        else ExecuteComputeWater();
        
    }
    void ExecuteComputePlanet()
    {
        if (created) return;


        int trueReso = planet.resolutionMultiplier * resolution;
        meshData = new MeshData[trueReso * trueReso];


        computeBuffer = new ComputeBuffer(meshData.Length, 24);



        layerDataList = new List<ComputeNoiseData>();
        for (int i = 0; i < shapeGenerator.ShapeSettings.noiseLayers.Length; i++)
        {
            if (shapeGenerator.ShapeSettings.noiseLayers[i].enabled) layerDataList.Add(new ComputeNoiseData(shapeGenerator.ShapeSettings.noiseLayers[i].noiseSettings));
        }

        layerData = new ComputeBuffer(layerDataList.Count, 56);
        layerData.SetData(layerDataList);
        int kernal = shader.FindKernel("CSMain");
        shader.SetBuffer(kernal, "meshData", computeBuffer);
        shader.SetBuffer(kernal, "noiseData", layerData);
        shader.SetInt("dataLength", layerDataList.Count);
        shader.SetInt("resolution", resolution);
        shader.SetFloat("sphereSize", planet.SphereSize);

        shader.SetVector("localUp", localUp);
        shader.SetVector("startXY", startXY);
        shader.SetInt("chunksPerRow", chunksPerFace);

      

        shader.Dispatch(kernal, resolution, resolution, 1);
    }
    void ExecuteComputeWater()
    {
        if (created) return;

        gameObject.layer = 4;
        int trueReso = planet.resolutionMultiplier * resolution;
        watermMeshData = new WaterMeshData[trueReso * trueReso];
       

        computeBuffer = new ComputeBuffer((trueReso) * (trueReso), 16);






        int kernal = shader.FindKernel("CSMain");
        shader.SetBuffer(kernal, "meshData", computeBuffer);


        shader.SetInt("resolution", resolution);
        shader.SetFloat("sphereSize", planet.SphereSize);

        shader.SetVector("localUp", localUp);
        shader.SetVector("startXY", startXY);
        shader.SetInt("chunksPerRow", chunksPerFace);




        shader.Dispatch(kernal, resolution, resolution, 1);

    }
    public void ConstructMeshPlanet()
    {
        if (created) return;
        int trueReso = planet.resolutionMultiplier * resolution;
        int[] triangles = new int[(trueReso - 1) * (trueReso - 1) * 6];
        int triIndex = 0;
        for (int y = 0; y < trueReso; y++)
        {
            for (int x = 0; x < trueReso; x++)
            {
                int i = x + y * (trueReso);
             

                if (x != trueReso - 1 && y != trueReso - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + trueReso + 1;
                    triangles[triIndex + 2] = i + trueReso;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + trueReso + 1;
                    triIndex += 6;
                }
            }
        }

        computeBuffer.GetData(meshData);

       
        chunkOffset = meshData[((meshData.Length - 1) / 2) + (int)(trueReso * 0.5f)].ver;
        layerData.Dispose();
        computeBuffer.Dispose();
    



        Vector3[] verts = new Vector3[meshData.Length];
        Vector2[] uvs = new Vector2[meshData.Length];
        for (int i = 0; i < meshData.Length; i++)
        {
            verts[i] = meshData[i].ver;
            uvs[i] = meshData[i].uv;
            planet.minMax.Evaluate(meshData[i].ver.magnitude);
        }
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.uv = uvs;

        if (quadTreeLod == 0 && !planet.isWater)
        {
            col = gameObject.AddComponent<MeshCollider>();
        }
        if (firstInitilization) planet.OnCompleteRender();
        created = true;
        if (quadTreeParent != null) quadTreeParent.ChildReady();
        // GetComponent<MeshCollider>().sharedMesh = mesh;
    }
    public void ConstructMeshWater()
    {
        if (created) return;
        int trueReso = planet.resolutionMultiplier * resolution;
        int[] triangles = new int[(trueReso - 1) * (trueReso - 1) * 6];
        int triIndex = 0;
        for (int y = 0; y < trueReso; y++)
        {
            for (int x = 0; x < trueReso; x++)
            {
                int i = x + y * (trueReso);
                //Vector2 percent = new Vector2(x, y) / (resolution - 1);
                //Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                //Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                //verticies[i] = shapeGenerator.CalculatePointOnPlanet(pointOnUnitSphere);

                if (x != trueReso - 1 && y != trueReso - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + trueReso + 1;
                    triangles[triIndex + 2] = i + trueReso;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + trueReso + 1;
                    triIndex += 6;
                }
            }
        }
        computeBuffer.GetData(watermMeshData);
      

        
        computeBuffer.Dispose();


        chunkOffset = watermMeshData[((watermMeshData.Length - 1) / 2) + (int)(trueReso * 0.5f)].ver * planet.transform.localScale.x;
        Vector3[] verts = new Vector3[watermMeshData.Length];
        for (int i = 0; i < watermMeshData.Length; i++)
        {
            verts[i] = watermMeshData[i].ver;
        }
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
      

        
        if (firstInitilization) planet.OnCompleteRender();
        created = true;
        if (quadTreeParent != null && !built)
        {
            quadTreeParent.ChildReady();
            built = true;
        }
        // GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ChunkPosition, planet.SphereSize / chunksPerFace);
    }
 
    struct MeshThreadInfo<T>
    {
        public Action<T> callback;
        public T data;

        public MeshThreadInfo(Action<T> callback, T data)
        {
            this.callback = callback;
            this.data = data;
        }
    }
    [Button]
    public void GenerateCollider()
    {
        MeshCollider col = gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;
    }
    public void ClearMesh()
    {
        meshRenderer.enabled = false;
        if (col) col.enabled = false;
        active = false;
    }
    public void ActivateChunk(bool disableChildren = true)
    {
        if (active) return;
        meshRenderer.enabled = true;
        if (col) col.enabled = true;
        active = true;
        ConstructMesh();
        if (!disableChildren) return;
        foreach(TerrainFace face in quadTreeChildren)
        {
            if(face!=null)face.ClearMesh();
        }
    }
    public void ChildReady()
    {
        childrenReady++;
        if(childrenReady >= 4)
        {
            ClearMesh();
            foreach(TerrainFace childFace in quadTreeChildren)
            {
                childFace.ActivateChunk(false);
            }
        }
    }
    [Button]
    public void CreateQuadTreeChildren()
    {
        if (quadTreeLod == 0) return;
        ClearMesh();
       
        float offSet = 1f/(chunksPerFace*2f);
        for (int i = 0; i < 4; i++)
        {
            if (quadTreeChildren[i] != null)
            {
                quadTreeChildren[i].ActivateChunk();

            }
            else
            {
                Vector2 chunkOffset = new Vector2(i % 2f * offSet, Mathf.Floor(i / 2f) * offSet);

                quadTreeChildren[i] = Instantiate(planet.chunkPrefab).GetComponent<TerrainFace>();
                quadTreeChildren[i].startXY = startXY + chunkOffset;
                quadTreeChildren[i].transform.parent = transform;
                quadTreeChildren[i].transform.localPosition = Vector3.zero;
                quadTreeChildren[i].transform.localScale = Vector3.one;

                quadTreeChildren[i].shader = shader;

                quadTreeChildren[i].planet = planet;
                quadTreeChildren[i].chunksPerFace = chunksPerFace * 2;
                quadTreeChildren[i].firstInitilization = false;
                quadTreeChildren[i].Init(shapeGenerator, transform, meshRenderer.sharedMaterial, SolarSystemManager.instance.quadTreeLODs[quadTreeLod - 1].resolution, localUp, sphereSize);
                quadTreeChildren[i].quadTreeLod = quadTreeLod - 1;
                quadTreeChildren[i].quadTreeParent = this;
                quadTreeChildren[i].ExecuteCompute();
                quadTreeChildren[i].CheckChunkPositions();
                quadTreeChildren[i].ClearMesh();
                
            }

        }
     
    }
}

public struct MeshData
{
    public Vector3 ver;

    public Vector2 uv;

    public float dummy;
}

public struct WaterMeshData
{
    public Vector3 ver;

    public float dummy;
}
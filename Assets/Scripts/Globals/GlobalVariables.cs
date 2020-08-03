using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalVariables : MonoBehaviour
{
    [SerializeField]
    string[] prefixNames, suffixNames;

    public int seed = 1023941051;
    public static System.Random rand;
    public static Vector3d playerWorldPos;
    public static Vector3d posWhenEnteringAtmosp;
    public static Vector3d oldWorldPos;
    public float distanceUntilUpdate = 200000;
    public static GameObject playerObject;
    public static float universeScale = 10000000f;
    public ColourDatabase colourDatabase;
    public static bool ColoursInitialized;
    public static bool isHyperspeed;
    public static GlobalVariables instance;
    public static Planet CurrentPlanet;
    public static float playerSpeed;
    public static System.Action OnUpdatePlayerPos;
    public static System.Action<Planet> OnEnterAtmosphere;
    public static System.Action OnExitAtmosphere;

    public static Queue<TerrainFace> LodUpdateQueue;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);
        rand = new System.Random(seed);
        playerWorldPos = Vector3d.zero;
        LodUpdateQueue = new Queue<TerrainFace>();

    }
    public string GetRandomName()
    {
        return prefixNames[rand.Next(prefixNames.Length)] + suffixNames[rand.Next(suffixNames.Length)];
    }
    void UpdatePlayerPos()
    {
        OnUpdatePlayerPos?.Invoke();
        oldWorldPos = playerWorldPos;
        //StartCoroutine(UpdateLodCoroutine());

    }

    IEnumerator UpdateLodCoroutine()
    {
        int index = 0;
        while (LodUpdateQueue.Count != 0)
        {

            LodUpdateQueue.Peek().ConstructMesh();
            LodUpdateQueue.Dequeue();
            index++;
            if (index == 5)
            {
                index = 0;
                yield return new WaitForEndOfFrame();

            }
        }
    }
    private void Start()
    {
        //UpdatePlayerPos();
        seed = 0;
        rand = new System.Random(seed);
        if(!Application.isEditor)SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
        GlobalVariables.playerWorldPos = Vector3d.zero;
    }
    private void Update()
    {
        float distanceThreshHold = CurrentPlanet == null ? distanceUntilUpdate : distanceUntilUpdate * 0.05f;
        if (Vector3d.Distance(playerWorldPos, oldWorldPos) > distanceThreshHold && !isHyperspeed)
        {
            UpdatePlayerPos();
        }
    }
    public static void AddPlayerWorldPos(Vector3d worldPosDelta)
    {
        playerWorldPos += worldPosDelta;
    }
    public static void EnterAtmosphere(Planet planet)
    {
        CurrentPlanet = planet;
        posWhenEnteringAtmosp = playerWorldPos;
        OnEnterAtmosphere?.Invoke(CurrentPlanet);
    }
    public static void ExitAtmosphere(Planet planet)
    {
        if (CurrentPlanet != planet) throw new System.Exception("Trying to exit an atmosphere of a diffrent planet!");
        CurrentPlanet = null;
        AddPlayerWorldPos(playerObject.transform.position);
        playerObject.transform.position = Vector3.zero;
        OnExitAtmosphere?.Invoke();
    }
    public static void AddPlayerWorldPos(Vector3 offset)
    {
        playerWorldPos = posWhenEnteringAtmosp + new Vector3d(offset);
    }
    [Button]
    public void LoadSolarSystem(int solarSystemNumber)
    {
        SceneManager.UnloadSceneAsync(1);
        seed = solarSystemNumber;
        rand = new System.Random(seed);
        SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
        GlobalVariables.playerWorldPos = Vector3d.zero;
    }
    public void OnEndHyperSpeed()
    {
        isHyperspeed = false;
        UpdatePlayerPos();
    }
}

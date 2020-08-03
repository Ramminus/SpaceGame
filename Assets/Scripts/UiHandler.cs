using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
   

public class UiHandler : MonoBehaviour
{
    public static UiHandler instance;

    [Header("Planet Overlay")]
    [SerializeField]
    RectTransform planetPanel, spaceJumpPanel;
    [SerializeField]
    TextMeshProUGUI planetName, distanceText, ttaText;
    [SerializeField]
    TMP_InputField solarSystemNoInput;

    float ttaTimer;
    Planet lastPlanet;
    private void Awake()
    {
        if (instance == null) instance = this;
        
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            spaceJumpPanel.gameObject.SetActive(!spaceJumpPanel.gameObject.activeSelf);
        }
    }
    public void UpdatePlanetName(Planet planet)
    {
        planetName.text = planet.name;
    }
    public void ShowPlanetPanel(Planet planet)
    {
        if(GlobalVariables.CurrentPlanet != null)
        {
            HidePlanetPanel();
            return;
        }
        int distance = (int)(Vector3.Distance(GlobalVariables.playerObject.transform.position, planet.transform.position) - planet.AtmosphereLevel);
        distanceText.text = "Distance: " + distance + " KM";
        ttaTimer -= Time.deltaTime;
        if (ttaTimer <= 0f || lastPlanet != planet)
        {
            
            
            ttaText.text = "Time until arrival: " + GetTTAText(distance);
            ttaTimer = 1f;
        }
        planetName.text = planet.name;
        planetPanel.gameObject.SetActive(true);
        Vector3 pos = Camera.main.WorldToScreenPoint(planet.GetUIPanelPoint());
        pos.z = 0;
        planetPanel.position = pos;
        lastPlanet = planet;
        
    }
    public void CheckSpaceJump()
    {
        if (solarSystemNoInput.text.Equals("")) return;
        GlobalVariables.instance.LoadSolarSystem(int.Parse(solarSystemNoInput.text));
        spaceJumpPanel.gameObject.SetActive(false);
    }
    
    public void HidePlanetPanel()
    {

        planetPanel.gameObject.SetActive(false);
    }
    public string GetTTAText(int distance)
    {
        int hour, min, sec;
        if (GlobalVariables.playerSpeed == 0) return "Never";
        int time = (int)(distance / GlobalVariables.playerSpeed) + 1;
        
        hour = (int)Mathf.Floor( time / 3600);
        time -= hour * 3600;
        min = (int)Mathf.Floor( time / 60);
        time -= min * 60;
        sec = time;

        string hourString = hour < 10 ? "0" + hour.ToString() : hour.ToString();
        string minString = min < 10 ? "0" + min.ToString() : min.ToString();
        string secString = sec < 10 ? "0" + sec.ToString() : sec.ToString();

        return hourString + ":" + minString + ":" + secString;


    }
}

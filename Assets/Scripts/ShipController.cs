using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ShipController : MonoBehaviour
{
    [SerializeField]
    Rigidbody rigidbody;
    public float shipSpeed = 30f;
    public float shipBoostMultiplier = 10f;
    [SerializeField]
    float hyperdriveSpeed;
    Ray cameraRay;
    RaycastHit hit;
    [SerializeField]
    LayerMask planetLayer;
    [SerializeField]
    Camera rayCam;
    [SerializeField]
    float rayDistance;
    Vector3 rotEuelr;
    [SerializeField]
    float baseFOV, boostFov, hyperdriveFov, hyperdriveShakeMagnitude, hyperdriveSmoothness;
    [SerializeField]
    AnimationCurve x, y;
    Planet hyperSpeedTarget;


    [Header("Visual Effects")]
    [SerializeField]
    VisualEffect boosterEffect;
    [SerializeField]
    VisualEffect hyperDriveEffect;
    bool cursorVisible;

    [Header("Rotation Settings")]
    [InspectorName("Rotation Sensitivity"), SerializeField]
    float rotSensitivity;
    [SerializeField]
    float rotationSmoothness;
    bool rotationContrained;
    float constraintThreshold;
   
    private void Awake()
    {
        GlobalVariables.playerObject = gameObject;
    }
    private void Start()
    {
        rotEuelr = transform.eulerAngles;
        hyperDriveEffect.Stop();
        GlobalVariables.OnEnterAtmosphere += OnEnterAtmosphere;
        GlobalVariables.OnExitAtmosphere += OnExitAtmosphere;
        SetCursor();
    }
    public void SetCursor()
    {
        Cursor.visible = cursorVisible;
        if (cursorVisible) Cursor.lockState = CursorLockMode.None;
        else Cursor.lockState = CursorLockMode.Locked;
    }
    private void OnExitAtmosphere()
    {
        rigidbody.isKinematic = true;
    }

    void OnEnterAtmosphere(Planet planet)
    {
        rigidbody.isKinematic = false;
    }
    public void RotateShip()
    {
        if (GlobalVariables.isHyperspeed) return;
        Vector2 mouseXY = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
        Vector3 localUp = transform.TransformDirection(Vector3.up);
        
        mouseXY.y *= rotSensitivity;
        
        mouseXY.x *= rotSensitivity;
        
     
         
        


        //transform.eulerAngles = rotEuelr;
        transform.Rotate(new Vector3(mouseXY.y, mouseXY.x, 0));
    }
    private void Update()
    {




       RotateShip();
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            cursorVisible = !cursorVisible;
            SetCursor();
        }
        float currentSpeed = shipSpeed;
        

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= shipBoostMultiplier;
            boosterEffect.Play();
            rayCam.fieldOfView = Mathf.Lerp(rayCam.fieldOfView, boostFov, Time.deltaTime);
        }
        else
        {
            boosterEffect.Stop();
            rayCam.fieldOfView = Mathf.Lerp(rayCam.fieldOfView, baseFOV, Time.deltaTime);
        }
        Vector3 dir = transform.TransformDirection(Vector3.forward);
        float forwardMotion = Input.GetAxisRaw("Vertical");

        //variables set when hyperspeeding
        if (GlobalVariables.isHyperspeed)
        {
            forwardMotion = 1f;
            currentSpeed = hyperdriveSpeed;
            rayCam.fieldOfView = Mathf.Lerp(rayCam.fieldOfView, hyperdriveFov, Time.deltaTime);
        }
        currentSpeed *= forwardMotion;
        GlobalVariables.playerSpeed = currentSpeed;
        if(GlobalVariables.CurrentPlanet == null) currentSpeed *= Time.deltaTime;
        Vector3d posDelta = new Vector3d(dir) * currentSpeed;
        if (GlobalVariables.CurrentPlanet == null)
        {
            
            GlobalVariables.AddPlayerWorldPos(posDelta);
        }
        else
        {
            Vector3 gravityUp = (transform.position - GlobalVariables.CurrentPlanet.transform.position).normalized;
            rigidbody.velocity = (Vector3)posDelta;
            if (rotationContrained)
            {
               
                Quaternion targetRot = Quaternion.FromToRotation(transform.up, gravityUp) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothness * Time.deltaTime);
                float mouseY = Mathf.Abs(Input.GetAxis("Mouse Y"));
                if (mouseY > 0.8f)
                {
                    rotationContrained = false;
                    constraintThreshold = 0;
                }
              

            }
            else
            {
                if (Vector3.Dot(transform.up, gravityUp) > 0.95) constraintThreshold += Time.deltaTime;
                else constraintThreshold = 0;
                if (constraintThreshold >= 2)
                {
                    rotationContrained = true;
                    constraintThreshold = 0;
                }
            }

            GlobalVariables.AddPlayerWorldPos(transform.position);
        }
        if (GlobalVariables.isHyperspeed)
        {
            if(hyperSpeedTarget.transform.position.magnitude < hyperSpeedTarget.AtmosphereLevel)
            {
                StopHyperSpeed();
            }
        }
        cameraRay = new Ray(rayCam.transform.position, rayCam.transform.forward);
        
        if (Physics.Raycast(cameraRay,out hit, rayDistance,planetLayer))
        {
            Planet planet = hit.transform.parent.GetComponent<Planet>();
            UiHandler.instance.ShowPlanetPanel(planet);
            if(Input.GetKeyDown(KeyCode.F) && GlobalVariables.CurrentPlanet == null)
            {
                if (!GlobalVariables.isHyperspeed)
                {
                    GlobalVariables.isHyperspeed = true;
                    hyperSpeedTarget = planet;
               
                    hyperDriveEffect.Play();
                    CameraShake.instance.Shake(x,y,hyperdriveShakeMagnitude, hyperdriveSmoothness);


                }
                else
                {
                    StopHyperSpeed();
                }
            }
           
        }
        else
        {
            UiHandler.instance.HidePlanetPanel();
        }
        

    }

    public void FloatingOriginControl()
    {

    }
    void StopHyperSpeed()
    {
        GlobalVariables.instance.OnEndHyperSpeed();
       
        hyperSpeedTarget = null;
        hyperDriveEffect.Stop();
        CameraShake.instance.StopShake();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionUpdater : MonoBehaviour
{

    public static PositionUpdater instance;
    public float speed = 10f;
    private void Awake()
    {
        instance = this;
    }
    // Update is called once per frame
    void Update()
    {
        


        GlobalVariables.playerWorldPos += new Vector3d((transform.forward * Input.GetAxis("Vertical") *speed ));
    }
}

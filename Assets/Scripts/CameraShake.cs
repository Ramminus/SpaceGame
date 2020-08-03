using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;
    bool running;
    Vector3 origonalPos;
    float magnitude;
    float smoothness;
    float timer;
    AnimationCurve xCurve, yCurve;
    public void Awake()
    {
        if (instance == null) instance = this;
    }
    public void Update()
    {
        if (running)
        {
            float x = xCurve.Evaluate(timer) * magnitude + origonalPos.x;
            float y = yCurve.Evaluate(timer) * magnitude + origonalPos.y;
            transform.localPosition = new Vector3(x, y, origonalPos.z);
            timer += Time.deltaTime /smoothness;
            if (timer > 1) timer = 0;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, origonalPos, smoothness);
        }
    }
  
    public void Shake(AnimationCurve x, AnimationCurve y, float magnitude, float smoothness)
    {
        origonalPos = transform.localPosition;
        xCurve = x;
        yCurve = y;
        running = true;
        this.smoothness = smoothness;
        this.magnitude = magnitude;

    }
    public void StopShake()
    {
        running = false;
    }

    
}

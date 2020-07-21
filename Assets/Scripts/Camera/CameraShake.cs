using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class encapsulates all needed behaviour for a camera shake
public class CameraShake : MonoBehaviour {

    public Camera mainCamera;

    private Vector3 cameraOrigin;
    private float shakeAmount = 0;



    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Cahce the original position of the camera so that we know where to
        cameraOrigin = mainCamera.transform.position;
    }

    public void Shake(float amount, float length)
    {
        shakeAmount = amount;
        InvokeRepeating("Shake", 0, 0.01f);
        Invoke("StopShake", length);
    }

    void Shake()
    {
        if(shakeAmount > 0)
        {
            Vector3 camPos = mainCamera.transform.position;

            float OFFSET_X = Random.value * shakeAmount * 2 - shakeAmount;
            float OFFSET_Y = Random.value * shakeAmount * 2 - shakeAmount;

            camPos.x += OFFSET_X;
            camPos.y += OFFSET_Y;

            mainCamera.transform.position = camPos;
        }
    }

    void StopShake()
    {
        CancelInvoke("Shake");
        mainCamera.transform.position = cameraOrigin;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraGyroscope : MonoBehaviour
{
    private GameObject cameraContainer;
    private Quaternion rot;
    // Start is called before the first frame update
    void Start()
    {
        cameraContainer = new GameObject("Camera Container");
        cameraContainer.transform.position = transform.position;
        transform.SetParent(cameraContainer.transform);

        cameraContainer.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
        rot = new Quaternion(0, 0, 1, 0);
    }

    // Update is called once per frame
    // float x = 0.0f;
    void Update()
    {
        transform.localRotation = Input.gyro.attitude * rot;
        // x += Time.deltaTime * 10.0f;
        // if (x > 360.0f) x = 0.0f;
        // transform.localRotation = Quaternion.Euler(x, 0, 0);
    }

    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
}

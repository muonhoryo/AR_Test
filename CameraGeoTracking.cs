using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class CameraGeoTracking : MonoBehaviour
{
    [SerializeField] private AnchorScanning Scanning;
    [SerializeField] private GameObject CameraObject;

    [SerializeField] private Text DebugText;

    private void Awake()
    {
        Scanning.FoundAnchorEvent += FoundAnchorAction;
        enabled = false;
    }
    private void FoundAnchorAction(Vector3 offset)
    {
        CameraObject.transform.position = offset;
        enabled = true;
        InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
        InputSystem.EnableDevice(Accelerometer.current);
        Input.gyro.enabled = true;
        Scanning.FoundAnchorEvent -= FoundAnchorAction;
    }

    private void FixedUpdate()
    {
        DebugText.text = $"Attitude: {Input.gyro.attitude}\nAcceleration: {Input.acceleration}";
        Vector3 velocity = UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue();
        CameraObject.transform.eulerAngles+=velocity;
        CameraObject.transform.position +=Accelerometer.current.acceleration.ReadValue();
    }
}

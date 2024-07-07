using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonSupARObject : MonoBehaviour
{
    [SerializeField] private AnchorScanning Scanning;

    private void Awake()
    {
        Scanning.FoundAnchorEvent += ScanningDoneAction;
        gameObject.SetActive(false);
    }
    private void ScanningDoneAction(Vector3 i)
    {
        Scanning.FoundAnchorEvent -= ScanningDoneAction;
        gameObject.SetActive(true);
    }
}



using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;

public sealed class ARSupportingChecker : MonoBehaviour
{
    [SerializeField] private GameObject ARUnsupportedObject;
    [SerializeField] private GameObject ARSupportedObject;

    private void Awake()
    {
        StartCoroutine(CheckSupporting());
    }
    private IEnumerator CheckSupporting()
    {
        if (ARSession.state == ARSessionState.None ||
            ARSession.state == ARSessionState.CheckingAvailability)
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            TempleMessagesManager.Instance_.CreateMessage("ARCore is not available on this device.",1000000);
            RunARUnsupported();
        }
        else
        {
            RunARSupported();
        }
    }
    private void RunARSupported()
    {
        Destroy(ARUnsupportedObject);
        ARSupportedObject.SetActive(true);
        CheckingDone();
    }
    private void RunARUnsupported()
    {
        Destroy(ARSupportedObject);
        ARUnsupportedObject.SetActive(true);
        CheckingDone();
    }
    private void CheckingDone()
    {
        Destroy(this);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public sealed class BGRendering : MonoBehaviour
{
    public event Action<Vector2Int> StartRenderingEvent = delegate { }; 

    [SerializeField] private AnchorScanning Scanning;
    [SerializeField] private Material SkyboxMaterial;

    public WebCamTexture Texture_ { get; private set; }
    private void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }
        else
        {
            ActivateRender();
        }
    }
    private void Update()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            ActivateRender();
        }
    }
    private void ActivateRender()
    {
        if (enabled)
        {
            Texture_ = new WebCamTexture();
            Texture_.Play();
            SkyboxMaterial.mainTexture = Texture_;
            Scanning.StartChecking();
            StartRenderingEvent(new Vector2Int(Texture_.width, Texture_.height));
            enabled = false;
        }
    }
}

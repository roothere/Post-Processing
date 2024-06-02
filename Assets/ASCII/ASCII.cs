using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASCII : MonoBehaviour {
    public Shader asciiShader;

    public Texture asciiTex;
    
    [Range(0.0f, 10.0f)]
    public float gamma = 1.0f;

    private Material asciiMat;
    
    void OnEnable() {
        asciiMat = new Material(asciiShader);
        asciiMat.hideFlags = HideFlags.HideAndDontSave;
    }

    void OnDisable() {
        asciiMat = null;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        asciiMat.SetTexture("_AsciiTex", asciiTex);

        var ping = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        
        var luminance = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RHalf);
        var sobel = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        var downscale = RenderTexture.GetTemporary(source.width / 10, source.height / 10, 0, source.format);
        
        Graphics.Blit(source, luminance, asciiMat, 0); // Luminance

        asciiMat.SetTexture("_LuminanceTex", luminance);
        Graphics.Blit(luminance, ping, asciiMat, 1); // Sobel Horizontal Pass
        
        Graphics.Blit(ping, sobel, asciiMat, 2); // Sobel Vertical Pass
        
        Graphics.Blit(sobel, destination);
        
        RenderTexture.ReleaseTemporary(downscale);
        RenderTexture.ReleaseTemporary(ping);
        RenderTexture.ReleaseTemporary(luminance);
        RenderTexture.ReleaseTemporary(sobel);
    }
}

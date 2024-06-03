using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASCII : MonoBehaviour {
    public Shader asciiShader;

    public Texture asciiTex;

    public bool viewSobel = false;
    public bool viewDownscale1 = false;
    public bool viewDownscale2 = false;
    public bool viewDownscale3 = false;
    
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

        var downscale1 = RenderTexture.GetTemporary(source.width / 2, source.height / 2, 0, source.format);
        var downscale2 = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0, source.format);
        var downscale3 = RenderTexture.GetTemporary(source.width / 8, source.height / 8, 0, source.format);
        
        Graphics.Blit(source, luminance, asciiMat, 1); // Luminance

        asciiMat.SetTexture("_LuminanceTex", luminance);
        Graphics.Blit(luminance, ping, asciiMat, 2); // Sobel Horizontal Pass
        
        Graphics.Blit(ping, sobel, asciiMat, 3); // Sobel Vertical Pass

        Graphics.Blit(sobel, downscale1, asciiMat, 4);
        Graphics.Blit(downscale1, downscale2, asciiMat, 4);
        Graphics.Blit(downscale2, downscale3, asciiMat, 4);

        Graphics.Blit(downscale3, destination, asciiMat, 5);

        if (viewSobel)
            Graphics.Blit(sobel, destination, asciiMat, 0);

        if (viewDownscale1)
            Graphics.Blit(downscale1, destination, asciiMat, 0);
        if (viewDownscale2)
            Graphics.Blit(downscale2, destination, asciiMat, 0);
        if (viewDownscale3)
            Graphics.Blit(downscale3, destination, asciiMat, 0);
        
        // Graphics.Blit(downscale, destination, asciiMat, 5);
        
        RenderTexture.ReleaseTemporary(downscale1);
        RenderTexture.ReleaseTemporary(downscale2);
        RenderTexture.ReleaseTemporary(downscale3);
        RenderTexture.ReleaseTemporary(ping);
        RenderTexture.ReleaseTemporary(luminance);
        RenderTexture.ReleaseTemporary(sobel);
    }
}

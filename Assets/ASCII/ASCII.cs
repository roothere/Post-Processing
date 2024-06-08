using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASCII : MonoBehaviour {
    public Shader asciiShader;
    public ComputeShader asciiCompute;
    public Texture asciiTex, edgeTex;

    public bool viewSobel = false;
    public bool viewGrid = false;
    public bool debugEdges = false;
    public bool viewUncompressedEdges = false;
    public bool viewQuantizedSobel = false;
    public bool noEdges = false;

    [Range(0, 64)]
    public int edgeThreshold = 8;

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
        var edgeAscii = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        edgeAscii.enableRandomWrite = true;
        
        Graphics.Blit(source, luminance, asciiMat, 1); // Luminance

        asciiMat.SetTexture("_LuminanceTex", luminance);
        Graphics.Blit(luminance, ping, asciiMat, 2); // Sobel Horizontal Pass
        
        Graphics.Blit(ping, sobel, asciiMat, 3); // Sobel Vertical Pass

        var downscale1 = RenderTexture.GetTemporary(source.width / 2, source.height / 2, 0, RenderTextureFormat.RHalf);
        var downscale2 = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0, RenderTextureFormat.RHalf);
        var downscale3 = RenderTexture.GetTemporary(source.width / 8, source.height / 8, 0, RenderTextureFormat.RHalf);

        Graphics.Blit(luminance, downscale1, asciiMat, 0);
        Graphics.Blit(downscale1, downscale2, asciiMat, 0);
        Graphics.Blit(downscale2, downscale3, asciiMat, 0);


        asciiCompute.SetTexture(0, "_SobelTex", sobel);
        asciiCompute.SetTexture(0, "_Result", edgeAscii);
        asciiCompute.SetTexture(0, "_EdgeAsciiTex", edgeTex);
        asciiCompute.SetTexture(0, "_AsciiTex", asciiTex);
        asciiCompute.SetTexture(0, "_LuminanceTex", downscale3);
        asciiCompute.SetInt("_ViewUncompressed", viewUncompressedEdges ? 1 : 0);
        asciiCompute.SetInt("_DebugEdges", debugEdges ? 1 : 0);
        asciiCompute.SetInt("_Grid", viewGrid ? 1 : 0);
        asciiCompute.SetInt("_NoEdges", noEdges ? 1 : 0);
        asciiCompute.SetInt("_EdgeThreshold", edgeThreshold);
        asciiCompute.Dispatch(0, Mathf.CeilToInt(source.width / 8), Mathf.CeilToInt(source.width / 8), 1);
        
        Graphics.Blit(edgeAscii, destination);


        if (viewSobel)
            Graphics.Blit(sobel, destination, asciiMat, 0);

        if (viewQuantizedSobel || viewUncompressedEdges || debugEdges || viewGrid)
            Graphics.Blit(edgeAscii, destination, asciiMat, 0);
        
        
        RenderTexture.ReleaseTemporary(ping);
        RenderTexture.ReleaseTemporary(luminance);
        RenderTexture.ReleaseTemporary(sobel);
        RenderTexture.ReleaseTemporary(edgeAscii);
        RenderTexture.ReleaseTemporary(downscale1);
        RenderTexture.ReleaseTemporary(downscale2);
        RenderTexture.ReleaseTemporary(downscale3);
    }
}

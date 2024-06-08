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
        var quantizedSobel = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        quantizedSobel.enableRandomWrite = true;
        
        Graphics.Blit(source, luminance, asciiMat, 1); // Luminance

        asciiMat.SetTexture("_LuminanceTex", luminance);
        Graphics.Blit(luminance, ping, asciiMat, 2); // Sobel Horizontal Pass
        
        Graphics.Blit(ping, sobel, asciiMat, 3); // Sobel Vertical Pass

        asciiCompute.SetTexture(0, "_SobelTex", sobel);
        asciiCompute.SetTexture(0, "_Result", quantizedSobel);
        asciiCompute.SetTexture(0, "_EdgeTex", edgeTex);
        asciiCompute.SetInt("_ViewUncompressed", viewUncompressedEdges ? 1 : 0);
        asciiCompute.SetInt("_DebugEdges", debugEdges ? 1 : 0);
        asciiCompute.SetInt("_Grid", viewGrid ? 1 : 0);
        asciiCompute.SetInt("_EdgeThreshold", edgeThreshold);
        asciiCompute.Dispatch(0, Mathf.CeilToInt(source.width / 8), Mathf.CeilToInt(source.width / 8), 1);
        
        Graphics.Blit(quantizedSobel, destination);


        if (viewSobel)
            Graphics.Blit(sobel, destination, asciiMat, 0);

        if (viewQuantizedSobel || viewUncompressedEdges || debugEdges || viewGrid)
            Graphics.Blit(quantizedSobel, destination, asciiMat, 0);
        
        
        RenderTexture.ReleaseTemporary(ping);
        RenderTexture.ReleaseTemporary(luminance);
        RenderTexture.ReleaseTemporary(sobel);
        RenderTexture.ReleaseTemporary(quantizedSobel);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASCII : MonoBehaviour {
    public Shader asciiShader;
    
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
        asciiMat.SetFloat("_Gamma", gamma);
        Graphics.Blit(source, destination, asciiMat);
    }
}

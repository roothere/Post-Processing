Shader "Hidden/ASCII" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {
        CGINCLUDE
        
        #include "UnityCG.cginc"

        Texture2D _MainTex, _AsciiTex;
        float4 _MainTex_TexelSize;
        SamplerState point_clamp_sampler;

        struct VertexData {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vp(VertexData v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        float luminance(float3 color) {
            return dot(color, float3(0.299f, 0.587f, 0.114f));
        }

        ENDCG

        Pass { // Luminance Pass
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float fp(v2f i) : SV_Target {
                float4 col = saturate(_MainTex.Sample(point_clamp_sampler, i.uv));
                float lum = luminance(col.rgb);

                return lum;
            }
            ENDCG
        }

        Pass { // Sobel Filter Horizontal Pass
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float lum1 = _MainTex.Sample(point_clamp_sampler, i.uv - float2(1, 0) * _MainTex_TexelSize.xy);
                float lum2 = _MainTex.Sample(point_clamp_sampler, i.uv);
                float lum3 = _MainTex.Sample(point_clamp_sampler, i.uv + float2(1, 0) * _MainTex_TexelSize.xy);

                float Gx = 1 * lum1 + 0 * lum2 + -1 * lum3;
                float Gy = 1 + lum1 * 2 * lum2 + 1 * lum3;

                return float4(Gx, Gy, 0, 0);
            }
            ENDCG
        }

        Pass { // Sobel Filter Vertical Pass
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            Texture2D _LuminanceTex;

            float4 fp(v2f i) : SV_Target {
                float2 grad1 = _MainTex.Sample(point_clamp_sampler, i.uv - float2(0, 1) * _MainTex_TexelSize.xy).xy;
                float2 grad2 = _MainTex.Sample(point_clamp_sampler, i.uv).xy;
                float2 grad3 = _MainTex.Sample(point_clamp_sampler, i.uv + float2(0, 1) * _MainTex_TexelSize.xy).xy;

                float Gx = 1 * grad1.x + 2 * grad2.x + 1 * grad3.x;
                float Gy = 1 * grad1.y + 0 * grad2.y + -1 * grad3.y;

                float magnitude = length(float2(Gx, Gy));
                float theta = atan2(Gy, Gx);

                return pow(saturate(float4(Gx, Gy, magnitude, theta)), 1.5f);
            }
            ENDCG
        }

        Pass { // Quantize
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float4 col = _MainTex.Sample(point_clamp_sampler, i.uv);

                float2 localUV;
                localUV.x = (i.vertex.x % 10 / 100) + col.w;
                localUV.y = (i.vertex.y % 10 / 10);

                float4 ascii = _AsciiTex.Sample(point_clamp_sampler, localUV) * col * saturate(((col.w + 0.3f) * 1.0f));

                return ascii;

                return localUV.x * col + localUV.y * col;
            }
            ENDCG
        }
    }
}
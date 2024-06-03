Shader "Hidden/ASCII" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {
        CGINCLUDE
        
        #include "UnityCG.cginc"
        #define PI 3.14159265358979323846f

        Texture2D _MainTex, _AsciiTex;
        float4 _MainTex_TexelSize;
        SamplerState point_clamp_sampler, linear_clamp_sampler;

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

        Pass { // Point Sampler
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float4 col = _MainTex.Sample(point_clamp_sampler, i.uv);

                return col;
            }
            ENDCG
        }

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

                float Gx = 3 * lum1 + 0 * lum2 + -3 * lum3;
                float Gy = 3 + lum1 + 10 * lum2 + 3 * lum3;

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

                float Gx = 3 * grad1.x + 10 * grad2.x + 3 * grad3.x;
                float Gy = 3 * grad1.y + 0 * grad2.y + -3 * grad3.y;

                float2 G = float2(Gx, Gy);
                G = normalize(G);

                float magnitude = length(float2(Gx, Gy));
                float theta = atan2(G.y, G.x);

                // if ((-3.0f * PI / 5.0f) < theta && theta < (-2.0 * PI / 5)) theta = 1;
                // else theta = 0;


                float absTheta = abs(theta) / PI;
                float3 direction = 0.0f;
                if ((0.45f < absTheta) && (absTheta < 0.55f)) direction = float3(0, 1, 0); // FLAT (Green)
                else if ((0.0f <= absTheta) && (absTheta < 0.05f)) direction = float3(1, 0, 0); // VERTICAL (Red)
                else if ((0.9f < absTheta) && (absTheta <= 1.0f)) direction = float3(1, 0, 0);
                else if (0.05f < absTheta && absTheta < 0.45f) direction = float3(1, 1, 0); // DIAGONAL 1 (Yellow)
                else if (0.55f < absTheta && absTheta < 0.9f) direction = float3(0, 1, 1); // DIAGONAL 2 (Cyan)
                else direction = 0;

                return float4(direction * (magnitude >= 0.01f), 1.0f);

                return float4(G.x, G.y, saturate(magnitude), saturate((theta + PI) / (2.0f * PI)));
            }
            ENDCG
        }

        Pass { // Downsample edge information
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float angleSum = 0.0f;

                float4 ne = _MainTex.Sample(point_clamp_sampler, i.uv + float2(-1, -1) * _MainTex_TexelSize.xy);
                float4 se = _MainTex.Sample(point_clamp_sampler, i.uv + float2(1, -1) * _MainTex_TexelSize.xy);
                float4 nw = _MainTex.Sample(point_clamp_sampler, i.uv + float2(-1, 1) * _MainTex_TexelSize.xy);
                float4 sw = _MainTex.Sample(point_clamp_sampler, i.uv + float2(1, 1) * _MainTex_TexelSize.xy);

                return (ne + se + nw + sw) * float4(0, 1, 0, 0);
            }
            ENDCG
        }

        Pass { // upscale and draw edges
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            Texture2D _LuminanceTex;

            float4 fp(v2f i) : SV_Target {
                float4 sobel = _MainTex.Sample(point_clamp_sampler, i.uv);

                return sobel;

                float angle = sobel.w;

                return floor(angle * 2.0f) / 2.0f;
                

                
                return float4(sobel.x, sobel.y, 0, 0);
            }
            ENDCG
        }

        Pass { // Quantize
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float4 col = _MainTex.Sample(point_clamp_sampler, i.uv);
                col.r = floor(col.r * 10) / 10;

                float2 localUV;
                localUV.x = (i.vertex.x % 8 / 80) + col.r;
                localUV.y = (i.vertex.y % 8 / 8);

                float4 ascii = _AsciiTex.Sample(point_clamp_sampler, localUV) * col;

                return ascii;

                return localUV.x * col + localUV.y * col;
            }
            ENDCG
        }
    }
}
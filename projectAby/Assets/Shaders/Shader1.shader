Shader "Unlit/Shader0" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorA ("ColorA", Color) = (1,1,1,1)
        _ColorB ("ColorB", Color) = (1,1,1,1)
        _StartColor("Start Color", Range(0,1)) = 0
        _EndColor("End Color", Range(0,1)) = 1
        _Scale ("Scaling", float) = 1
        _Offset("Offset", float) = 0
        _WaveAmp("WaveAmp", Range(0,1)) = 0.1
    }
    SubShader {
        Tags { "RenderType" = "Trasparent"
               "Queue" = "Transparent"
        }
             
        LOD 100

        Pass {

            Cull Off
            zWrite Off
            Blend DstColor Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define TAU 2*3.1415926535

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ColorA;
            float4 _ColorB;
            float _StartColor;
            float _EndColor;
            float _Scale;
            float _Offset;
            float _WaveAmp;

            //meshData
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            //interpolators
            struct v2f {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            float inverseLerp(float a, float b, float v) {
                return (v - a) / (b - a);
            }

            float getWave(float2 uv) {

                float2 centerUV = 2*uv - 1;
                float distFromCenter = length(centerUV);
                float wave = cos((distFromCenter - _Time.y * 0.1) * TAU * 5) * 0.5 + 0.5;
                wave *= 1 - distFromCenter;

                return wave;
            }


            v2f vert (appdata v) {
                v2f o;

                float waveX = cos((v.uv.x - _Time.y * 0.03) * TAU * 8);
                float waveY = cos((v.uv.y - _Time.y * 0.03) * TAU * 8);                
                v.vertex.y = waveY * waveX * _WaveAmp;                            // water like
//                v.vertex.y = getWave(v.uv) * _WaveAmp;                              // ripples
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);                     // mul((float3x3)unity_ObjectToWorld, v.normal);
                o.uv = (v.uv + _Offset) * _Scale;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.uv.x += _Time.y * 0.1;
                return o;
            }



            float4 frag(v2f i) : SV_Target{
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);

//                return getWave(i.uv);
                col *= lerp(_ColorA, _ColorB, i.uv.y);

                return col;
            }
            ENDCG
        }
    }
}

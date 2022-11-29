Shader "Unlit/Shader0" {
    Properties {
        _ColorA ("ColorA", Color) = (1,1,1,1)
        _ColorB ("ColorB", Color) = (1,1,1,1)
        _StartColor("Start Color", Range(0,1)) = 0
        _EndColor("End Color", Range(0,1)) = 1
        _Scale ("Scaling", float) = 1
        _Offset("Offset", float) = 0
    }
    SubShader {
        Tags { "RenderType" = "Transparent" 
               "Queue" = "Transparent"
        }
             
        LOD 100

        Pass {
            Cull Off
            zWrite Off

            // Blending src*A +- dst*B
//            Blend One One        // additive blending
            Blend  DstColor Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define TAU 2*3.1415926535

            float4 _ColorA;
            float4 _ColorB;
            float _StartColor;
            float _EndColor;
            float _Scale;
            float _Offset;

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

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);                             // mul((float3x3)unity_ObjectToWorld, v.normal);
                o.uv = (v.uv + _Offset) * _Scale;

                return o;
            }

            float inverseLerp(float a, float b, float v) {
                return (v - a) / (b - a);
            }

            float4 frag(v2f i) : SV_Target{

                float t0 = inverseLerp(_StartColor, _EndColor, i.uv.x);
                float t = saturate(t0);                                                    //clamp between 0-1
//                float off = cos(i.uv.x * TAU * 8) * 0.01; 
//                float t = cos((i.uv.y + off - _Time.z * 0.1) * TAU * 5) * 0.5 + 0.5;

//                t *= 1 - i.uv.y;
//                t *= (abs(i.normal.y) < 0.9999);

                float4 col = lerp(_ColorA, _ColorB, t);
                return col;
            }
            ENDCG
        }
    }
}

Shader "Unlit/Shader0" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Mask ("Mask", 2D) = "white" {}
        _Details ("Details", 2D) = "white" {}

    }
    SubShader {
        Tags { "RenderType" = "Opaque" 
        }
             
        LOD 100

        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
//            #define TAU 2*3.1415926535

            sampler2D _MainTex;
            sampler2D _Mask;
            sampler2D _Details;
            float4 _MainTex_ST;

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
                float3 worldPos : TEXCOORD2;
            };



            v2f vert (appdata v) {

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);                     // mul((float3x3)unity_ObjectToWorld, v.normal);
                o.uv = v.uv;
                o.worldPos = mul(UNITY_MATRIX_M, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }



            float4 frag(v2f i) : SV_Target{

                float4 albedo = tex2D(_MainTex, i.worldPos.xz);
                float4 mask = tex2D(_Mask, i.uv);
                float4 rocks = tex2D(_Details, i.uv);
                float4 col =  lerp(albedo, rocks, mask);

                return col;
            }
            ENDCG
        }
    }
}

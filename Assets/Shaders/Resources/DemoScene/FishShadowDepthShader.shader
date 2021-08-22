Shader "Unlit/FishShadowDepthShader"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            // -------------------------------------------------------------
            // declaration
            struct appdata
            {
                float4 vertex  : POSITION;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                float4 lightPos  : TEXCOORD0;
            };


            float4x4  _WorldToLightCam;
            float     _FishTime;
            float     _movementSpeed;
#define PI 3.1415926

            float3 vectorPlaneIntersection(float3 planeNormal, float3 PlaneCenter, float3 vectorDirection, float3 vectorOrigin)
            {
                float t = (dot(planeNormal, PlaneCenter) - dot(planeNormal, vectorOrigin)) / dot(planeNormal, vectorDirection);
                return vectorOrigin + vectorDirection * t;
            }


            // -------------------------------------------------------------
            // Vertex Shader
            v2f vert (appdata v)
            {
                v2f o;

                float3 localPos = v.vertex;
                localPos.x += sin(localPos.z*10. + _FishTime)*0.35  * _movementSpeed* smoothstep(-0.1, 0.5, localPos.z);
                localPos.x += sin(localPos.z*8. + _FishTime*0.8 + 2.612)*0.3  * _movementSpeed * smoothstep(-0.08, 0.5, localPos.z);
                
                float twist_angle = cos(_FishTime + localPos.z*10) * 3.6 * _movementSpeed * smoothstep(-0.1, 0.5, localPos.z);
                float2x2 twist_matrix = float2x2(float2(cos(twist_angle), -sin(twist_angle)), float2(sin(twist_angle), cos(twist_angle)));
                localPos.xy = mul(twist_matrix, localPos.xy);

                float4 worldPos = mul(unity_ObjectToWorld, float4(localPos,1.));
                o.vertex   = mul(_WorldToLightCam,  float4(worldPos.xyz, 1.));

                return o;
            }




            // -------------------------------------------------------------
            // Fragment Shader

            fixed4 frag (v2f i) : SV_Target
            {
   
                float3 camPos = i.vertex.xyz / i.vertex.w;
                float depth = camPos.z;
   
                return depth;
            }
            ENDCG
        }
    }/* FallBack "VertexLit"*/

}
 
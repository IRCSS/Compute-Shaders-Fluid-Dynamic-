Shader "Unlit/FountainWater"
{
    Properties
    {
        _Color ("water color", Color) = (1, 1, 1, 1)
        _Color2("foam color",  Color) = (1, 1, 1, 1)
        _displacment("water Displacemnet", Float) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv       : TEXCOORD0;
                float4 vertex   : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };
             
            sampler2D _fountain_pressure_buffer;
            float4    _Color;
            float4    _Color2;
            float4    _fountain_downLeft;
            float4    _fountain_upRight;
            float     _displacment;
            float2    _canvas_texel_size;
            float3    _lightDirection;
            float4x4  _fountain2World;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                float2 uv  = (o.worldPos.zx - _fountain_downLeft.zx) ;
                       uv  = uv / abs(_fountain_upRight.zx - _fountain_downLeft.zx);
               float   pressureBuffer = tex2Dlod(_fountain_pressure_buffer, float4(uv.xy, 0, 0));

               float clampAt = 1.5f;

               float clampValue = clampAt + pressureBuffer / 40.;

               //pressureBuffer = lerp(clampValue, pressureBuffer, saturate((clampAt - pressureBuffer) / 2.));

                o.vertex   = UnityObjectToClipPos(v.vertex + float4(0., _displacment * pressureBuffer, 0., 0.));
                o.uv       = uv;
                return o;
            }

            float3 filterNormal(float2 uv, float texelSize)
            {
                float4 h;
                float2 t = uv + texelSize * float2(0, -1);
                h[0] = tex2Dlod(_fountain_pressure_buffer, float4(t.x, t.y, 0., 0.))* _displacment;
                       t = uv + texelSize * float2(-1,  0);
                h[1] = tex2Dlod(_fountain_pressure_buffer, float4(t.x, t.y, 0., 0.))* _displacment;
                       t = uv + texelSize * float2( 1,  0);
                h[2] = tex2Dlod(_fountain_pressure_buffer, float4(t.x, t.y, 0., 0.))* _displacment;
                       t = uv + texelSize * float2( 0,  1) ;
                h[3] = tex2Dlod(_fountain_pressure_buffer, float4(t.x, t.y, 0., 0.))* _displacment;

                float3 n;
                n.x = -(h[0] - h[3]);
                n.z =  (h[1] - h[2]); 
                n.y = 2 * texelSize; // pixel space -> uv space

                return normalize(n);
            }

            fixed4 frag (v2f i) : SV_Target
            {


                float3 normal  = filterNormal(i.uv, _canvas_texel_size);
                       normal  = mul(_fountain2World, float4(normal.xyz, 0.));
                       //normal.xzy = normal.zxy;
                float  diffuse = saturate(dot(normal, -1.0*_lightDirection));
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);

                float3 specularReflection;
                if (dot(normal, -_lightDirection) < 0.0) specularReflection = float3(0.0, 0.0, 0.0);
                else 
                {
                    specularReflection = float3(1., 1., 0.8) * pow(max(0., dot(reflect(_lightDirection, normal), viewDir)), 40.);
                }
                


                fixed4 col = tex2Dlod(_fountain_pressure_buffer, float4(i.uv.xy,0.,0.));
                col.xyz = smoothstep(-0.1, 0.1, col.xyz);
                col.xyz = lerp(_Color, _Color2, col.x) * diffuse + float3(0.2, 0.2, 0.2) + specularReflection;
                //return float4(i.uv.xy, 0., 1.);
                return float4(col.xyz, _Color.a);
            }
            ENDCG
        }
    }
}

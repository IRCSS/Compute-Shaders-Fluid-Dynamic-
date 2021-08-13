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
                float4 refCamPos: TEXCOORD2;
                float4 vertex   : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };
             
            sampler2D _fountain_pressure_buffer;
            sampler2D _Refelection_texture;
            float4    _Color;
            float4    _Color2;
            float4    _fountain_downLeft;
            float4    _fountain_upRight;
            float     _displacment;
            float2    _canvas_texel_size;
            float3    _lightDirection;
            float4x4  _fountain2World;
            float4x4  _ref_cam_tranform;
            float2    _refCamScreenParm;

            inline float4 computeRefCamScreePos(float4 pos)
            {
                float4 o = pos * 0.5f;
                o.xy = float2(o.x, o.y *-1.0f) + o.w;
                o.zw = pos.zw;
                return o;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                float4 originalWorldPos = o.worldPos;

                float2 uv  = (o.worldPos.zx - _fountain_downLeft.zx) ;
                       uv  = uv / abs(_fountain_upRight.zx - _fountain_downLeft.zx);
               float   pressureBuffer = tex2Dlod(_fountain_pressure_buffer, float4(uv.xy, 0, 0));

               float clampAt = 1.5f;

               float clampValue = clamp( pressureBuffer,-0.25, 0.25) ;

               float4 disVector = float4(0., _displacment * clampValue, 0., 0.);

               //pressureBuffer = lerp(clampValue, pressureBuffer, saturate((clampAt - pressureBuffer) / 2.));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex + disVector);
                o.vertex   = UnityObjectToClipPos(v.vertex + disVector);
                o.uv       = uv;
                o.refCamPos = computeRefCamScreePos(mul(_ref_cam_tranform, float4(originalWorldPos.xyz, 1.)));
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
               float3 normalInRefSpace = mul(_ref_cam_tranform, float4(normal.xyz - float3(0.,1.,0.), 0.));

                float  diffuse = saturate(dot(normal, -1.0*_lightDirection));
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);

                float4 specularReflection;
                float specRefAmount;
                if (dot(normal, -_lightDirection) < 0.0) specularReflection = float4(0.0, 0.0, 0.0, 0.);
                else 
                {
                    specRefAmount = pow(max(0., dot(reflect(_lightDirection, normal), viewDir)), 100.);
                    specularReflection = float4(1., 1., 1., 1.) *specRefAmount;
                }
                
                float2 refCamUV = i.refCamPos.xy / i.refCamPos.w;
                float4  refCamCol = tex2Dlod(_Refelection_texture, float4(refCamUV.xy  + clamp(float2( normalInRefSpace.x, normalInRefSpace.y)*0.1,-0.01, 0.01), 0., 0.));


                fixed4 col = tex2Dlod(_fountain_pressure_buffer, float4(i.uv.xy,0.,0.));
                col.xyz = smoothstep(-0.1, 0.1, col.xyz);
                col.xyzw = /*lerp(_Color, _Color2, col.x) * diffuse + *//*float4(0.2, 0.2, 0.2, 0.1)*/ refCamCol  + specularReflection;
                //return float4(i.uv.xy, 0., 1.);
                return float4(col.xyzw  );
            }
            ENDCG
        }
    }
}

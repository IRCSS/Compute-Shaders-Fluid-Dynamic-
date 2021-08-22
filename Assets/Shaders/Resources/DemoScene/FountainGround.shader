Shader "Unlit/FountainGround"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DisplacementStrength("Displacement Strength", Float) = 5
        _MaxDisplacement("water max Displacemnet", Float) = 5
        _DisplacementDistribution("Displacemnet Distribution", Float) = 0.2

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

            float4x4  _WorldToLightCam;

            sampler2D  _MainTex; 
            sampler2D  _fountain_pressure_buffer;
            sampler2D _LightDepthTexture;
            float4     _MainTex_ST;

            float4    _fountain_downLeft;
            float4    _fountain_upRight;
            float4    _lightDirection;
            float4    _pointOnWaterPlane;
            float     _MaxDisplacement;
            float     _DisplacementStrength;
            float     _DisplacementDistribution;
            float2    _canvas_texel_size;
            
            float4x4   _BounceCausticsLightDirection;

            float4 vectorPlaneIntersection(float4 planeNormal, float4 PlaneCenter, float4 vectorDirection, float4 vectorOrigin)
            {
                   float t = (dot(planeNormal, PlaneCenter) - dot(planeNormal, vectorOrigin)) / dot(planeNormal, vectorDirection);
                   return vectorOrigin + vectorDirection * t;
            }

            float FishSoftShadow(float2 uv, float currentDepth)
            {
                float4 s;

                float displacement = 0.0006;

                s[0] = tex2D(_LightDepthTexture, uv + float2( 0., -1.) * displacement);
                s[1] = tex2D(_LightDepthTexture, uv + float2( 0.,  1.) * displacement);
                s[2] = tex2D(_LightDepthTexture, uv + float2( 1.,  0.) * displacement);
                s[3] = tex2D(_LightDepthTexture, uv + float2(-1.,  0.) * displacement);

                s = step(s, currentDepth.xxxx);

                return dot(s, float4(0.25, 0.25, 0.25, 0.25));

            }

            float pressureToneMapping(float pressure)
            {

                float clampValue = clamp(pressure / _MaxDisplacement, -1.0, 1.0);
                      clampValue = pow(abs(clampValue), _DisplacementDistribution) * sign(clampValue);
                      clampValue = lerp(clampValue, pressure / 30. , pow(1. - saturate(abs(pressure)),4.2)) * _DisplacementStrength;

                      //float clampValue = abs(pressure);
                      //      clampValue = clampValue/(clampValue+1.0);
                      //      clampValue = pow(abs(clampValue), _DisplacementDistribution) * _DisplacementStrength * sign(pressure);

                      return clampValue;
                  }

                        // Calculate a normal from a height map
            float3 filterNormal(float2 uv, float texelSize)
            {
                float4 h;
                float2 t = uv + texelSize * float2(0, -1);
                h[0] = pressureToneMapping(tex2Dlod(_fountain_pressure_buffer, float4(t.x, t.y, 0., 0.)).x);
                t = uv + texelSize * float2(-1, 0);
                h[1] = pressureToneMapping(tex2Dlod(_fountain_pressure_buffer, float4(t.x, t.y, 0., 0.)).x);
                t = uv + texelSize * float2(1, 0);
                h[2] = pressureToneMapping(tex2Dlod(_fountain_pressure_buffer, float4(t.x, t.y, 0., 0.)).x);
                t = uv + texelSize * float2(0, 1);
                h[3] = pressureToneMapping(tex2Dlod(_fountain_pressure_buffer, float4(t.x, t.y, 0., 0.)).x);

                float2 n;
                n.x = -(h[0] - h[3]);
                n.y = (h[1] - h[2]);

                float3 toReturn = float3(0., 0., 0. );
                toReturn.x = sqrt(pow(n.x, 2.)+ pow(n.y, 2.));

                toReturn.yz = n.xy;


                return toReturn;
            }


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv       : TEXCOORD0;
                float2 uvFlu    : TEXCOORD1;
                float2 uvFlu2   : TEXCOORD5;   // For caustics above the water
                float4 worldPos : TEXCOORD2;
                float3 normal   : TEXCOORD3;
                float4 lightPos : TEXCOORD4;
                float4 vertex   : SV_POSITION;

            };

            float4 GetColumn(int index)
            {
                return float4(_BounceCausticsLightDirection[0][index], _BounceCausticsLightDirection[1][index], _BounceCausticsLightDirection[2][index], _BounceCausticsLightDirection[3][index]);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);



                o.lightPos = mul(_WorldToLightCam, float4(o.worldPos.xyz, 1.));

               float4 hitPosOnWater = vectorPlaneIntersection(float4(0., 1., 0., 0.), _pointOnWaterPlane, -_lightDirection, o.worldPos);


                float2 uv  = (hitPosOnWater.zx - _fountain_downLeft.zx) ;
                       uv  = uv / abs(_fountain_upRight.zx - _fountain_downLeft.zx);

                o.uvFlu  = float2(uv.x, uv.y);

                float4 c;
                c[0] = o.worldPos.x >  1.5 ? 1.0 : 0.0;
                c[1] = o.worldPos.x < -1.5 ? 1.0 : 0.0;
                c[2] = o.worldPos.z >  1.5 ? 1.0 : 0.0;
                c[3] = o.worldPos.z < -1.5 ? 1.0 : 0.0;


                float sum = dot(c, float4(1., 1., 1., 1.));
                float4 aboveWaterLightDir = (GetColumn(0) * c[0] + GetColumn(1) * c[1] + GetColumn(2) * c[2] + GetColumn(3) * c[3]) / max(0.0001, sum);


                aboveWaterLightDir = lerp(float4(1., 1., 0., 0.), aboveWaterLightDir, min(1., sum));

                float4 aboveWaterhitPos = vectorPlaneIntersection(float4(0., 1., 0., 0.), _pointOnWaterPlane, -normalize(aboveWaterLightDir), o.worldPos);

                    uv = (aboveWaterhitPos.zx - _fountain_downLeft.zx);
                    uv = uv / abs(_fountain_upRight.zx - _fountain_downLeft.zx);

                    o.uvFlu2 = uv.xy;

                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
            
                float planeMask = 1.-step(_pointOnWaterPlane.y, i.worldPos.y);
               
                float4 c;
                c[0] = i.worldPos.x >  1.5 ? 1.0 : 0.0;
                c[1] = i.worldPos.x < -1.5 ? 1.0 : 0.0;
                c[2] = i.worldPos.z >  1.5 ? 1.0 : 0.0;
                c[3] = i.worldPos.z < -1.5 ? 1.0 : 0.0;

                float sum = dot(c, float4(1., 1., 1., 1.));
                float4 aboveWaterLightDir = (GetColumn(0) * c[0] + GetColumn(1) * c[1] + GetColumn(2) * c[2] + GetColumn(3) * c[3]) / max(0.0001, sum);

                float4 lightDirection = lerp(-aboveWaterLightDir, _lightDirection, planeMask);
                
                float2 uv = lerp(i.uvFlu2, i.uvFlu, planeMask);

                float3 pressureBufInfo  = filterNormal(uv, _canvas_texel_size);
                float3 pressureBufInfoB = filterNormal(uv + float2(1.0, 0.0)*  _canvas_texel_size * 5. * max(0.2,planeMask), _canvas_texel_size);
                float3 pressureBufInfoG = filterNormal(uv + float2(0.,  1.0)* -_canvas_texel_size * 5. * max(0.2,planeMask), _canvas_texel_size);

             
                float causticStrength = lerp(1.6, 0., smoothstep(_pointOnWaterPlane.y, _pointOnWaterPlane.y + 0.25, i.worldPos.y));

                float presureCenter = pressureToneMapping(tex2Dlod(_fountain_pressure_buffer, float4(uv, 0., 0.)).x);
                col += float4(pressureBufInfo.x, pressureBufInfoB.x, pressureBufInfoG.x, 0. )  * max(0., dot(i.normal, -lightDirection)) * causticStrength;
                col = lerp(col, col * float4(0.65,0.65,0.75, 1.), smoothstep(0.45, 1.0, abs(presureCenter)));

                // fishShadow
                float3 lighCoord = i.lightPos.xyz / i.lightPos.w;
                lighCoord.xy = lighCoord.xy * 0.5 + 0.5;
                lighCoord.y = 1.0 - lighCoord.y;

                float fishShadow = FishSoftShadow(lighCoord.xy, lighCoord.z);

                col.xyz = lerp(col.xyz *float3(.55, 0.58, 0.64), col.xyz, fishShadow);


                return col;
            }
            ENDCG 
        }
    }
}

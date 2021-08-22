Shader "Unlit/FishShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _normalMap ("NormalMap", 2D) = "white" {}
        _roughness ("Roughness", Float) = 0.5
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
            // -------------------------------------------------------------
            // declaration
            struct appdata
            {
                float4 vertex  : POSITION;
                float2 uv      : TEXCOORD0;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv        : TEXCOORD0;
                float4 vertex    : SV_POSITION;
                float3 normal    : TEXCOORD1;
                float4 tangent   : TEXCOORD2;
                float4 worldPos  : TEXCOORD3;
                float2 uvFlu     : TEXCOORD4;
                
            };

            sampler2D _MainTex;
            sampler2D _normalMap;
            sampler2D  _fountain_pressure_buffer;
            float4    _MainTex_ST;
            float     _roughness;
            float3    _lightDirection;
            float4    _pointOnWaterPlane;
            float     _MaxDisplacement;
            float     _DisplacementStrength;
            float     _DisplacementDistribution;
            float2    _canvas_texel_size;
            float4    _fountain_downLeft;
            float4    _fountain_upRight;
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

                o.vertex   = UnityObjectToClipPos(localPos);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal   = UnityObjectToWorldNormal(v.normal);
                o.tangent  = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
                o.worldPos = mul(unity_ObjectToWorld, float4(localPos,1.));

                float3 hitPosOnWater = vectorPlaneIntersection(float3(0., 1., 0.), _pointOnWaterPlane, -_lightDirection, o.worldPos);


                float2 uv = (hitPosOnWater.zx - _fountain_downLeft.zx);
                       uv = uv / abs(_fountain_upRight.zx - _fountain_downLeft.zx);

                o.uvFlu = float2(uv.x, uv.y);

                return o;
            }



            // ---------------------------------------------------------------------
            // Helper Funcitons 


            float pressureToneMapping(float pressure)
            {

                float clampValue = clamp(pressure / _MaxDisplacement, -1.0, 1.0);
                clampValue = pow(abs(clampValue), _DisplacementDistribution) * sign(clampValue);
                clampValue = lerp(clampValue, pressure / 30., pow(1. - saturate(abs(pressure)), 4.2)) * _DisplacementStrength;

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

                float3 toReturn = float3(0., 0., 0.);
                toReturn.x = sqrt(pow(n.x, 2.) + pow(n.y, 2.));

                toReturn.yz = n.xy;


                return toReturn;
            }


                // The fresnel factor determines how much reflection should exist based on view direction
            float3 fresnelSchlick(float cosTheta, float3 F0)  // cos theta is the dot product between half vector and view direction and F0 is the fresnel factor for 0 degree (looking up side down) on the water, for water this value is float3(0.02)
            {
                return F0 + (1.0 - F0) *  pow(max(1.0 - cosTheta, 0.0), 5.0);
            }

            float3 fresnelSchlickRoughness(float cosTheta, float3 F0, float roughness)  // I am not taking the geometry and ndf into account for the specularity of the planar reflection. To compensate for too strong planar refelection, there is a variation that takes the roughness, as explained by Sebastien Lagarde https://seblagarde.wordpress.com/2011/08/17/hello-world/
            {
                return F0 + (max(float3((1.0 - roughness).xxx), F0) - F0) *  pow(max(1.0 - cosTheta, 0.0), 5.0);
            }

            // Calculates how aligned the micro facets are to the half way vector
            float DistributionGGX(float3 N, float3 H, float roughness) // H is the half way vector 
            {
                float a = roughness * roughness;
                float a2 = a * a;
                float NdotH = max(dot(N, H), 0.0);
                float NdotH2 = NdotH * NdotH;

                float num = a2;
                float denom = (NdotH2 * (a2 - 1.0) + 1.0);
                denom = PI * denom * denom;

                return num / denom;
            }

            // This takes care of the self shadowing aspect of the surface. Technically, it is questionably how this would effect something like water, I left it in
            float GeometrySchlickGGX(float NdotV, float roughness)
            {
                float r = (roughness + 1.0);
                float k = (r*r) / 8.0;

                float num = NdotV;
                float denom = NdotV * (1.0 - k) + k;

                return num / denom;
            }
            float GeometrySmith(float3 N, float3 V, float3 L, float roughness) 
            {
                float NdotV = max(dot(N, V), 0.0);
                float NdotL = max(dot(N, L), 0.0);
                float ggx2 = GeometrySchlickGGX(NdotV, roughness);
                float ggx1 = GeometrySchlickGGX(NdotL, roughness);

                return ggx1 * ggx2;
            }

            // -------------------------------------------------------------
            // Fragment Shader

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv);
                
            
            
                // Calculate normal, tangent space -> world
                float3 noramlMap = float3(0.,0.,0.);
                noramlMap.xy = tex2D(_normalMap, i.uv).wy *2. -1;
                noramlMap.z  = sqrt(1 - dot(noramlMap.xy, noramlMap.xy));

                noramlMap.xyz = noramlMap.xzy;

                float3 binormal = cross(i.normal, i.tangent.xyz) * i.tangent.w;

                float3 normal = normalize(
                    noramlMap.x * i.tangent +
                    noramlMap.y * i.normal +
                    noramlMap.z * binormal
                );


                // Calculate lighting paramters
                float3 L       = -_lightDirection;
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 halfVec = normalize(L + viewDir);

                float4 LightRadiance = float4(1., 1., 1., 1.) *3.0;
                float  NdotL         = max(dot(normal, L), 0.0);
                float  NdotV         = dot(normal, viewDir);

                // Fresnel factor
                float3 F0 = float3(0.04, 0.04, 0.04);

                float3 F = fresnelSchlick(max(dot(halfVec, viewDir), 0.0), F0);  // Fresnel factor
                float NDF = DistributionGGX(normal, halfVec, _roughness);
                float G   = GeometrySmith(normal, viewDir, L, _roughness);

                float3 kS = F;
                float3 kD = float3(1., 1., 1.) - kS;


                float3 numerator = NDF * G * F;
                float  denominator = 4.0 * max(NdotV, 0.0) * NdotL;
                float3 specular = numerator / max(denominator, 0.001);

                float3 specularDirLight =  specular * LightRadiance * NdotL; 

                kS += saturate(fresnelSchlickRoughness(max(NdotV, 0.0), F0, _roughness));

                float pointingDown = dot(normal, float3(0., -1., 0.)*0.5 + 0.5);

                float3 reflection = lerp(float3(0.7, 0.8, 1.), float3(1., 1., 1.), pointingDown);

                float4 finalCol = float4(0., 0., 0., 1.);
                       finalCol.xyz = kS * reflection + (1. - kS)  *albedo * NdotL *LightRadiance / PI + specularDirLight;
                       finalCol.xyz += (float3(0.7,0.7,0.7) + float3(0., 0.05,0.2) * pointingDown)*albedo;  // ambient Lighting


                       // Caustics

                       float3 pressureBufInfo  = filterNormal(i.uvFlu, _canvas_texel_size);
                       float3 pressureBufInfoB = filterNormal(i.uvFlu + float2(1.0, 0.0)*  _canvas_texel_size * 5. , _canvas_texel_size);
                       float3 pressureBufInfoG = filterNormal(i.uvFlu + float2(0., 1.0) * -_canvas_texel_size * 5.  , _canvas_texel_size);
                       float  presureCenter    = pressureToneMapping(tex2Dlod(_fountain_pressure_buffer, float4(i.uvFlu, 0., 0.)).x);

                       finalCol += float4(pressureBufInfo.x, pressureBufInfoB.x, pressureBufInfoG.x, 0.) * max(0., dot(normal, -_lightDirection)) * 12.;
                       finalCol = lerp(finalCol, finalCol * float4(0.65, 0.65, 0.75, 1.), smoothstep(0.45, 1.0, abs(presureCenter)));

                return finalCol;
            }
            ENDCG
        }
    }/* FallBack "VertexLit"*/

}
 
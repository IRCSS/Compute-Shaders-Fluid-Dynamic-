Shader "Unlit/FountainWater"
{
    // ----------------------------------------------------------------
    // PROPERTIES

    Properties
    {
        _DisplacementStrength    ("water  Displacemnet, displacement", Float) = 5
        _MaxDisplacement         ("water max Displacemnet", Float) = 5
        _DisplacementDistribution("Displacemnet Distribution", Float) = 0.2
        _roughness  ("water roughness", Float) = 0.1
        _waterColor ("Color", Color) = (1., 1., 1., 1.)
        
    }
    SubShader
    {
        // ----------------------------------------------------------------
        // Draw bindings
        Tags { "RenderType" = "Opaque" }
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
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv        : TEXCOORD0;
                float4 refCamPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float4 vertex    : SV_POSITION;
                float4 worldPos  : TEXCOORD1;
            };
             
            sampler2D _fountain_pressure_buffer;
            sampler2D _Refelection_texture;
            sampler2D _Refraction_texture;
            sampler2D _CameraDepth_Texture;

            float4    _fountain_downLeft;
            float4    _fountain_upRight;
            float4    _waterColor;
            float     _MaxDisplacement;
            float     _DisplacementStrength;
            float     _DisplacementDistribution;
            float     _aspect_ration_multiplier;
            float2    _canvas_texel_size;
            float3    _lightDirection;
            float4x4  _fountain2World;
            float4x4  _ref_cam_tranform;
            float2    _refCamScreenParm;
            float3    _main_camera_forward;
            float3    _ref_cam_position;
            float     _roughness;
            #define PI 3.1415926


            // -------------------------------------------------------------
            // Helper functions

            // This function is used in to conver a position from clip space to screen postion
            inline float4 computeRefCamScreePos(float4 pos)
            {
                float4 o = pos * 0.5f;
                o.xy = float2(o.x, o.y *-1.0f) + o.w;                                               // This is the place to flip the y component, if the vector is upside dowm. This might vary based on the API or platform
                o.zw = pos.zw;
                return o;
            }

            inline float3 GetCameraForward()                 //
            {
                return float3(UNITY_MATRIX_V[2][0], UNITY_MATRIX_T_MV[2][1], UNITY_MATRIX_T_MV[2][2]);
            }
  

            float pressureToneMapping(float pressure)
            {

                float clampValue = clamp(pressure / _MaxDisplacement, -1.0, 1.0);
                      clampValue = pow(abs(clampValue), _DisplacementDistribution) * sign(clampValue);
                      clampValue = lerp(clampValue, pressure/30. , pow(1.-saturate(abs(pressure)),4.2)) * _DisplacementStrength;

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

                float3 n;
                n.x = -(h[0] - h[3]);
                n.z = (h[1] - h[2]);
                n.y = 2 * texelSize; // pixel space -> uv space

                return normalize(n);
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
            // Vertex Shader
            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                float4 originalWorldPos = o.worldPos;

                float2 uv  = (o.worldPos.zx - _fountain_downLeft.zx) ;
                       uv  = uv / abs(_fountain_upRight.zx - _fountain_downLeft.zx);
               float   pressureBuffer = tex2Dlod(_fountain_pressure_buffer, float4(uv.xy, 0, 0));


               float4 disVector = float4(0., pressureToneMapping(pressureBuffer), 0., 0.);

                o.worldPos  = mul(unity_ObjectToWorld, v.vertex + disVector);
                o.vertex    = UnityObjectToClipPos(v.vertex + disVector);
                o.uv        = uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                o.refCamPos = computeRefCamScreePos(mul(_ref_cam_tranform, float4(originalWorldPos.xyz, 1.)));
                return o;
            }


            // -------------------------------------------------------------
            // Fragment Shader
            fixed4 frag (v2f i) : SV_Target
            {


                // Lighting 
                
                float3 normal  = filterNormal(i.uv, _canvas_texel_size);
                       normal  = mul(_fountain2World, float4(normal.xyz, 0.));                                  // Calculate the surface normal. This is the normal of the plane, but modified by the height map displacement

                float3 normalInRefSpace = mul(_ref_cam_tranform, float4(normal.xyz - float3(0.,1.,0.), 0.));    // This is used to offset the texture read of the planar camera. Based on how much the normal deviates from the standard plane normal
                float3 normalInCamSpace = mul(UNITY_MATRIX_V, normal);

                float3 L       = -_lightDirection;
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 viewDirRefCam = normalize(_ref_cam_position.xyz - i.worldPos.xyz);
                float3 halfVec = normalize(L + viewDir);
                // The concept behind lighting the water is to sum up the refracted and refelected components of the light. The refracted is the screen texture behind the water plane, and the reflected is the palanr refelection camera texture + specular highlights of the directional light

                float LightRadiance = float4(1., 1., 1., 1.) *10.0;
                float NdotL         = max(dot(normal, L), 0.0);
                float NdotV         = dot(normal, viewDir);
                float3 VRefN        = reflect(viewDir, normal);
                float3 VRefNCamSpace = mul(UNITY_MATRIX_V, VRefN);
                // Fresnel factor
                float3 F0        = float3(0.02, 0.02, 0.02);                        // value of F0 for water
      
                float3 F  = fresnelSchlick(max(dot(halfVec, viewDir), 0.0), F0);  // Fresnel factor

                float NDF = DistributionGGX(normal, halfVec, _roughness);
                float G   = GeometrySmith  (normal, viewDir, L, _roughness);

                float3 kS = F;
                float3 kD = float3(1.,1.,1.) - kS;

                float3 numerator   =NDF * G * F;
                float  denominator = 4.0 * max(NdotV, 0.0) * NdotL;
                float3 specular    = numerator / max(denominator, 0.001);



                float3 specularDirLight =  specular * LightRadiance * NdotL; // No need for a diffuse term, as clean transparent water doesnt have any. The refaraction part is light incoming from the other side of the water, so the energy conservation term is different: out going light = in coming specular reflected light + light coming from under the water (the rendered scene)
               
                // Add the planar refelection
                float2  refCamUV = i.refCamPos.xy / i.refCamPos.w;
                float4  refCamCol = tex2Dlod(_Refelection_texture, float4(refCamUV.xy + clamp(float2(normalInRefSpace.x* _aspect_ration_multiplier, normalInRefSpace.y )*0.2, -0.02, 0.02), 0., 0.));
                
                kS = fresnelSchlickRoughness(max(NdotV, 0.0), F0, _roughness);
        

                float2 screenPosition = (i.screenPos.xy / i.screenPos.w);
                float refractionDepthOriginal = tex2D(_CameraDepth_Texture, screenPosition);


                float2 cornerFix = 1. -(smoothstep(0.05, 0., screenPosition * float2(_aspect_ration_multiplier, 1.))
                    + smoothstep(float2(1. - 0.05 / _aspect_ration_multiplier, 0.95), float2(1., 1.), screenPosition));


                float3 SSROffsetdDepth = tex2D(_CameraDepth_Texture, screenPosition + normalInCamSpace.xy*0.025 * float2(_aspect_ration_multiplier, 1.) * cornerFix);
                float3 SSR = tex2D(_Refraction_texture, screenPosition + normalInCamSpace.xy*0.025 * float2(_aspect_ration_multiplier, 1.) * cornerFix * step(SSROffsetdDepth - 0.00008, refractionDepthOriginal));
                float correctionFactor = max(dot(VRefN, viewDirRefCam),0.);
                      correctionFactor = smoothstep(1., 0.5, correctionFactor);
                float3 reflection = lerp(refCamCol.xyz, SSR * _waterColor, correctionFactor);


            
                float refractionDepthOffset   = tex2D(_CameraDepth_Texture, screenPosition + normalInCamSpace.xy *-0.015 * float2(_aspect_ration_multiplier, 1.) * cornerFix);
                float fixOffsetError          = step(refractionDepthOffset - 0.00008, refractionDepthOriginal);
                float3 refraction      = tex2D(_Refraction_texture, screenPosition + normalInCamSpace.xy *- 0.015 * float2(_aspect_ration_multiplier, 1.) * (cornerFix) * fixOffsetError);
 

                float3 col = kS * reflection + (1.-kS)  *refraction* /** NdotL **/_waterColor + specularDirLight;


                return float4(col.xyz, 1.);

            }
            ENDCG
        }
    }
}

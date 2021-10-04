﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PersianGardenDemoSceneMaster : MonoBehaviour
{

    // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public FluidSimulater fluid_simulater;

    [Header("Fountain Managment")]
    public Transform  Tran_downLeft;
    public Transform  Tran_upRight;
    public Transform  mainDirectionLight;
    public GameObject Fountain;
    public GameObject FountainPlain;
    public Texture2D  boundaryTexture;
    public Camera     LightCamera;

    //___________
    // private
    private FluidGPUResources resources;
    private Material          fountainMaterial;

    private RenderTexture     pressure_texture;

    private Camera            main_cam;
    private Camera            reflection_cam;
    private RenderTexture     reflection_cam_texture;
    private RenderTexture     refraction_cam_texture;
    private RenderTexture     camera_depth_texture;
    private RenderTexture     fish_shadow_depth_texture;
    private RenderTexture     velocity_texture;

    private CommandBuffer     render_water_cb;
    private CommandBuffer     fish_shadow_cb;

    // ------------------------------------------------------------------
    // INITALISATION
    void Start()
    {

        main_cam = Camera.main;
        if (main_cam == null) Debug.LogError("Could not find main camera, make sure the camera is tagged as main");
        main_cam.depthTextureMode = DepthTextureMode.Depth;

        // Set up the refelection camera
        GameObject refCamGB = new GameObject("RefelectionCamera");
        reflection_cam =  refCamGB.AddComponent<Camera>();
        reflection_cam.CopyFrom(main_cam); // This needs to sync if cam changes in run time

        reflection_cam_texture = new RenderTexture(main_cam.pixelWidth, main_cam.pixelHeight, 8);
        reflection_cam_texture.Create();

        refraction_cam_texture = new RenderTexture(reflection_cam_texture.descriptor);
        refraction_cam_texture.Create();


        camera_depth_texture = new RenderTexture(reflection_cam_texture.descriptor)
        {
            format = RenderTextureFormat.RFloat
        };
        camera_depth_texture.Create();

        reflection_cam.targetTexture = reflection_cam_texture;

        Shader.SetGlobalTexture("_Refelection_texture", reflection_cam_texture);
        Shader.SetGlobalTexture("_Refraction_texture",  refraction_cam_texture);
        Shader.SetGlobalVector ("_refCamScreenParm",    new Vector2(main_cam.pixelWidth, main_cam.pixelHeight));
        Shader.SetGlobalTexture("_CameraDepth_Texture", camera_depth_texture);
        Shader.SetGlobalVector ("_pointOnWaterPlane",   FountainPlain.transform.position);
        
        Vector4[] b = GenerateRefelectionLightDirectionForFountain();
        

        Shader.SetGlobalMatrix("_BounceCausticsLightDirection",new Matrix4x4(b[0], b[1], b[2], b[3]));
        //--
        // Initialize the fluid simulator engine
        fluid_simulater.Initialize();
        resources = new FluidGPUResources(fluid_simulater);
        resources.Create();

        fluid_simulater.SubmitMousePosOverrideDelegate(GetMousePosInSimulationSpaceUnitValue);              // The default implementation treats the fluid simulation to be the whole screen space. If you want to map the fluid simulation to a specific part of your scene, you can use this function to correctly convert mouse position to fluid simulation space

        // Create textures for visualizing presure or velocity
        pressure_texture = new RenderTexture((int)fluid_simulater.canvas_dimension, (int)fluid_simulater.canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = true,
             graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
            filterMode        = FilterMode.Trilinear,
            anisoLevel        = 7,
            format            = RenderTextureFormat.RFloat,
            wrapMode          = TextureWrapMode.Clamp,
            
        };
        pressure_texture.Create();
        velocity_texture = new RenderTexture((int)fluid_simulater.canvas_dimension, (int)fluid_simulater.canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap = true,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 7,
            format = RenderTextureFormat.RGFloat,
            wrapMode = TextureWrapMode.Clamp,
        };
        velocity_texture.Create();


        //--

        Shader.SetGlobalVector("_canvas_texel_size"       , new Vector4(1.0f/fluid_simulater.canvas_dimension, 1.0f / fluid_simulater.canvas_dimension, 0.0f, 0.0f));
        Shader.SetGlobalVector("_lightDirection"          , mainDirectionLight.forward);
        Shader.SetGlobalMatrix("_fountain2World"          , Tran_downLeft.localToWorldMatrix);
        Shader.SetGlobalFloat ("_aspect_ration_multiplier", (float)main_cam.pixelWidth/ (float)main_cam.pixelHeight);

        //--
        // Build the Fluid Pipeline
        fluid_simulater.UpdateArbitaryBoundaryOffsets(boundaryTexture, resources);

        Vector2 waterpipePosition  = new Vector2(fluid_simulater.simulation_dimension / 2, fluid_simulater.simulation_dimension - fluid_simulater.simulation_dimension*0.1f);
        Vector2 waterPipeDirection = new Vector2(0.0f, -1.0f); 

        fluid_simulater.AddUserForce           (resources.velocity_buffer                                   );
        fluid_simulater.AddConstantForceSource (resources.velocity_buffer, waterpipePosition,
                                                waterPipeDirection, 6.4f, fluid_simulater.simulation_dimension* 0.0025f, fluid_simulater.simulation_dimension*0.001f);

        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity);
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer, resources.boundary_pressure_offset_buffer);
        fluid_simulater.Diffuse                (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer, resources.boundary_pressure_offset_buffer);
        fluid_simulater.Advect                 (resources.velocity_buffer, resources.velocity_buffer, fluid_simulater.velocity_dissapation);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer, resources.boundary_pressure_offset_buffer);




        fluid_simulater.CopyPressureBufferToTexture(pressure_texture, resources.pressure_buffer);
        fluid_simulater.CopyVelocityBufferToTexture(velocity_texture, resources.velocity_buffer);

        Shader.SetGlobalTexture("_fountain_pressure_buffer", pressure_texture);
        Shader.SetGlobalTexture("_fountain_velocity_buffer", velocity_texture);

        fluid_simulater.BindCommandBuffer();


        render_water_cb = new CommandBuffer()
        {
            name = "Render Water Plane"
        };

        MeshRenderer fountainRender = Fountain.GetComponent<MeshRenderer>();
        if (!fountainRender)
        {
            Debug.LogError("Could not find the Rendeer on the provided game object for the Fountain water mesh");
            return;
        }
        Material fountainMat = fountainRender.sharedMaterial;
        if (!fountainMat)
        {
            Debug.LogError("No Valid Material found on the mesh renderer of the provided Fountain Mesh");
            return;
        }

        // Copy the content of the frame buffer into a texture to be sampled for refraction
        render_water_cb.Blit(BuiltinRenderTextureType.CameraTarget,  refraction_cam_texture);
        render_water_cb.Blit(BuiltinRenderTextureType.Depth, camera_depth_texture  );
        render_water_cb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

       

        render_water_cb.DrawRenderer(fountainRender, fountainMat);


        main_cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, render_water_cb);


        // --------------------- Fish shadowMap


        Matrix4x4 worldToLightCamMaterix = GL.GetGPUProjectionMatrix(LightCamera.projectionMatrix, true) * LightCamera.worldToCameraMatrix;

        Shader.SetGlobalMatrix("_WorldToLightCam", worldToLightCamMaterix);

        fish_shadow_cb = new CommandBuffer()
        {
            name = "fishShadowMap"
        };

        fish_shadow_depth_texture = new RenderTexture(2048, 2048, 16)
        {
            format = RenderTextureFormat.RFloat
        };

        fish_shadow_depth_texture.Create();

        Shader.SetGlobalTexture("_LightDepthTexture", fish_shadow_depth_texture);

        fish_shadow_cb.SetRenderTarget(fish_shadow_depth_texture);
        fish_shadow_cb.ClearRenderTarget(true, true, Color.black);

        Fish[] allFish = GameObject.FindObjectsOfType<Fish>();
        if (allFish.Length <= 0) Debug.LogWarning("couldnt find fish in the scene, this pond is supposed to have fish");




        foreach (Fish f in allFish)
        {
            Renderer r = f.GetRenderer();
            fish_shadow_cb.DrawRenderer(r, f.GetFishDepthMat());
        }

        fish_shadow_cb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

        main_cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, fish_shadow_cb);

    }

    // ------------------------------------------------------------------
    // DESTRUCTOR
    void OnDisable()
    {
        fluid_simulater.Release();
        resources      .Release();
    }

    // ------------------------------------------------------------------
    // LOOP
    void Update()
    {
        fluid_simulater.Tick(Time.deltaTime);


        Shader.SetGlobalVector("_fountain_downLeft",   Tran_downLeft.position);
        Shader.SetGlobalVector("_fountain_upRight",    Tran_upRight.position  );
        Shader.SetGlobalVector("_main_camera_forward", main_cam.transform.forward);

        

        // Update ref cam




        // Refelecting the camera

        Vector4 mainCamPos = FountainPlain.transform.InverseTransformPoint(main_cam.transform.position);
        Vector4 refCamPos  = new Vector4(mainCamPos.x, -1.0f * mainCamPos.y, mainCamPos.z, 1.0f);
                refCamPos  = FountainPlain.transform.TransformPoint(refCamPos);
        reflection_cam.transform.position = refCamPos;

        Vector4 mainCamForward = FountainPlain.transform.InverseTransformDirection(main_cam.transform.forward);
        Vector4 refCamForward = new Vector4(mainCamForward.x, -mainCamForward.y, mainCamForward.z, 0.0f); 
                refCamForward = FountainPlain.transform.TransformDirection(refCamForward);
      
        Vector4 mainCamUp = FountainPlain.transform.InverseTransformDirection(main_cam.transform.up);
        Vector4 refCamUp  = new Vector4(mainCamUp.x, -mainCamUp.y, mainCamUp.z, 0.0f);
                refCamUp  = FountainPlain.transform.TransformDirection(refCamUp);

        reflection_cam.transform.LookAt(refCamPos + refCamForward, refCamUp);

        Vector3 waterPlanePosInCameraSpace    = reflection_cam.worldToCameraMatrix.MultiplyPoint (FountainPlain.transform.position);
        Vector3 waterPlaneNormalInCameraSpace = reflection_cam.worldToCameraMatrix.MultiplyVector(FountainPlain.transform.up);
        float   DstInCameraSpace = -Vector3.Dot(waterPlanePosInCameraSpace, waterPlaneNormalInCameraSpace) ;

        Vector4 clipPlaneCameraSpace = new Vector4(waterPlaneNormalInCameraSpace.x, waterPlaneNormalInCameraSpace.y, waterPlaneNormalInCameraSpace.z, DstInCameraSpace);
        Matrix4x4 newMat = main_cam.CalculateObliqueMatrix(clipPlaneCameraSpace);

        reflection_cam.projectionMatrix = newMat;


        Shader.SetGlobalMatrix("_ref_cam_tranform", GL.GetGPUProjectionMatrix(reflection_cam.projectionMatrix, true) * reflection_cam.worldToCameraMatrix);
        Shader.SetGlobalVector("_ref_cam_position", reflection_cam.transform.position);
    }

    // ------------------------------------------------------------------
    // FUNCTIONS

    Vector2 GetMousePosInSimulationSpaceUnitValue(ref bool isInBound)
    {
        RaycastHit results;
        Ray ray = main_cam.ScreenPointToRay(Input.mousePosition);

        isInBound = false;
        if (Physics.Raycast(ray, out results, 100.0f))
        {

            if (results.collider.gameObject != Fountain) return new Vector2(-20.0f, -20.0f);

            Vector2 hitPositionInSimulationSpace = new Vector2(results.point.z, results.point.x);
                    hitPositionInSimulationSpace = hitPositionInSimulationSpace - new Vector2(Tran_downLeft.position.z, Tran_downLeft.position.x);

            Vector2 span = new Vector2(Tran_upRight.position.z, Tran_upRight.position.x) - new Vector2(Tran_downLeft.position.z, Tran_downLeft.position.x);

             hitPositionInSimulationSpace = new Vector2(hitPositionInSimulationSpace.x / Mathf.Abs(span.x), hitPositionInSimulationSpace.y / Mathf.Abs(span.y));
            isInBound = true;
            return hitPositionInSimulationSpace;

        }

        return new Vector2(-20.0f, -20.0f);
    }







    // Coding scheme is:    x > 1.5    x < -1.5   z > 1.5   z < - 1.5
    //                      Case 1     Case 2     Case 3    Case 4

    //           Case 3 
    //        ------------
    //        |          | 
    // Case 2 |          | Case 1
    //        |          | 
    //        |          | 
    //        ------------
    //           Case 4

    Vector4[] GenerateRefelectionLightDirectionForFountain()
    {

        Vector4[] toReturn = new Vector4[4];

        toReturn[0] = Quaternion.Euler(0.0f, 90.0f, 0.0f) * mainDirectionLight.forward;
        toReturn[1] = Quaternion.Euler(0.0f,-90.0f, 0.0f) * mainDirectionLight.forward;
        toReturn[2] = mainDirectionLight.forward;
        toReturn[3] = new Vector3(mainDirectionLight.forward.x , mainDirectionLight.forward.y, -mainDirectionLight.forward.z);
        return toReturn;
        
    }


}

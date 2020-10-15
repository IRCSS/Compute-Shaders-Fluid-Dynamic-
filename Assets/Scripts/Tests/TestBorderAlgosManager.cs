using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TestBorderAlgosManager : MonoBehaviour
{
    // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public ComputeShader boundrayShader;




    //___________
    // private
    private int           debugHandle;
    private Camera        main_cam;
    private CommandBuffer cb;
    private RenderTexture debug_texture;

    private int           canvas_dimension = 512;

    // ------------------------------------------------------------------
    // INITALISATION
    void Start()
    {

        // -----------------------
        main_cam = Camera.main;
        if (main_cam == null) Debug.LogError("Could not find main camera, make sure the camera is tagged as main");
        
        main_cam.orthographic     = true;                                                                     // Make sure the camera is ortho. Perspecitve camera has a transformation matrix which will screw with everything
        main_cam.aspect           = 1.0f;
        main_cam.orthographicSize = 1;

        // -----------------------
        debug_texture = new RenderTexture(canvas_dimension, canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = false,
            filterMode        = FilterMode.Bilinear
        };


        debug_texture.Create();

        // -----------------------

        debugHandle = boundrayShader.FindKernel("debugBoundaryAlgo");
        boundrayShader.SetTexture(debugHandle, "_boundary_debug_texture", debug_texture);
        boundrayShader.SetInt("i_Resolution", canvas_dimension);

        uint groupSizeX, groupSizeY, groupSizeZ;

        boundrayShader.GetKernelThreadGroupSizes(debugHandle,
                            out groupSizeX, out groupSizeY, out groupSizeZ);

        // -----------------------

        cb = new CommandBuffer()
        {
            name = "Debug_boundary_counditions",
        };

        cb.DispatchCompute(boundrayShader, debugHandle, canvas_dimension * 4 / (int) groupSizeX, 1, 1);

        cb.Blit(debug_texture, BuiltinRenderTextureType.CameraTarget);

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, cb);

        
    }


    // ------------------------------------------------------------------
    // LOOP
    
    void Update()
    {
        
    }
}

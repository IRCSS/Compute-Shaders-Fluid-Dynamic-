using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TestAddDye : MonoBehaviour
{

    // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public  ComputeShader addDyeComputeShader;
    public  ComputeShader structuredBufferToTextureShader;

    //___________
    // private
    private int           addDyeHandle;
    private int           st2txHandle;
    private RenderTexture visulasation_texture;
    private ComputeBuffer dye_buffer;
    private CommandBuffer cb;
    private Camera        main_cam;
    private int           canvas_dimension = 512;                         // Resolution of the render target used at the end, this can be lower or higher than the actual simulation grid resoltion
    private int           i_Resolution     = 256;                         // Resolution of the simulation grid

    private Color         dye_color        = Color.red;
    private Vector2       mouse_fallout    = new Vector2(2.0f, 4.0f);

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
        visulasation_texture = new RenderTexture(canvas_dimension, canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = false,
        };


        visulasation_texture.Create();
        // -----------------------

        dye_buffer = new ComputeBuffer(i_Resolution * i_Resolution, sizeof(float) * 4);
        
        // -----------------------
        addDyeHandle = addDyeComputeShader.FindKernel("AddDye");

        addDyeComputeShader.SetInt   ("i_Resolution",                 i_Resolution);
        addDyeComputeShader.SetVector("_dye_color",                   dye_color);
        addDyeComputeShader.SetFloat ("_mouse_dye_radius",            mouse_fallout.x);
        addDyeComputeShader.SetFloat ("_mouse_dye_falloff",           mouse_fallout.y);
        addDyeComputeShader.SetBuffer(addDyeHandle, "_dye_buffer",    dye_buffer);
        //___

        st2txHandle = structuredBufferToTextureShader.FindKernel("StructeredToTextureBillinear");
        structuredBufferToTextureShader.SetInt    ("i_Resolution",          i_Resolution);
        structuredBufferToTextureShader.SetInt    ("_Results_Resolution",   canvas_dimension);
        structuredBufferToTextureShader.SetBuffer (st2txHandle, "_Source",  dye_buffer);
        structuredBufferToTextureShader.SetTexture(st2txHandle, "_Results", visulasation_texture);

        // -----------------------
        cb = new CommandBuffer()
        {
            name = "Debug_Add_Dye_kernl",
        };

        uint add_dye_group_size_x, add_dye_group_size_y, add_dye_group_size_z;

        addDyeComputeShader.GetKernelThreadGroupSizes(addDyeHandle,
                                                      out add_dye_group_size_x,
                                                      out add_dye_group_size_y,
                                                      out add_dye_group_size_z);

        cb.DispatchCompute(addDyeComputeShader, addDyeHandle, i_Resolution / (int)add_dye_group_size_x,
                                                              i_Resolution / (int)add_dye_group_size_y,
                                                              1);


        uint st2tx_group_size_x, st2tx_group_size_y, st2tx_group_size_z;

        structuredBufferToTextureShader.GetKernelThreadGroupSizes(st2txHandle, out st2tx_group_size_x,
                                                                               out st2tx_group_size_y,
                                                                               out st2tx_group_size_z);

        cb.DispatchCompute(structuredBufferToTextureShader, st2txHandle, canvas_dimension / (int)st2tx_group_size_x,
                                                                         canvas_dimension / (int)st2tx_group_size_y,
                                                                         1);

        cb.Blit(visulasation_texture, BuiltinRenderTextureType.CameraTarget);

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, cb);
    }

    // ------------------------------------------------------------------
    // LOOP
    void Update()
    {
        float mouse_pressed = 0.0f;

        if (Input.GetKey(KeyCode.Mouse0)) mouse_pressed = 1.0f;

        addDyeComputeShader.SetFloat("_mouse_pressed", mouse_pressed);

        Vector3 mouse_pos_pixel_coord = Input.mousePosition;
        Vector2 mouse_pos_normalized  = main_cam.ScreenToViewportPoint(mouse_pos_pixel_coord);
                mouse_pos_normalized  = new Vector2(Mathf.Clamp01(mouse_pos_normalized.x), Mathf.Clamp01(mouse_pos_normalized.y));
        Vector2 mouse_pos_struct_pos  = new Vector2(mouse_pos_normalized.x * i_Resolution, mouse_pos_normalized.y * i_Resolution);

        print(mouse_pos_struct_pos);
        addDyeComputeShader.SetVector("_mouse_position", mouse_pos_struct_pos);                   // Pass on the mouse position already in the coordinate system of the structured buffer as 2D coord


    }
}

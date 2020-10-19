using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TestAddForce : MonoBehaviour
{
    // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public  ComputeShader userInput_shader;
    public  ComputeShader structuredBufferToTextureShader;

    //___________
    // private
    private int           addForceHandle;
    private int           st2txHandle;
    private RenderTexture visulasation_texture;
    private ComputeBuffer velocity_buffer;
    private CommandBuffer cb;
    private Camera        main_cam;
    private int           canvas_dimension = 512;                         // Resolution of the render target used at the end, this can be lower or higher than the actual simulation grid resoltion
    private int           i_Resolution     = 256;                         // Resolution of the simulation grid
    private float         force_strength   = 0.6f;
    private float         force_radius     = 1;
    private float         force_falloff    = 2;

    private Vector2       mouse_previus_pos;

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

        //__
        Vector3 mouse_pos_pixel_coord = Input.mousePosition;
        Vector2 mouse_pos_normalized  = main_cam.ScreenToViewportPoint(mouse_pos_pixel_coord);
                mouse_pos_normalized  = new Vector2(Mathf.Clamp01(mouse_pos_normalized.x), Mathf.Clamp01(mouse_pos_normalized.y));
        Vector2 mouse_pos_struct_pos  = new Vector2(mouse_pos_normalized.x * i_Resolution, mouse_pos_normalized.y * i_Resolution);
                mouse_previus_pos     = mouse_pos_struct_pos;

        // -----------------------
        visulasation_texture = new RenderTexture(canvas_dimension, canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = false,
        };


        visulasation_texture.Create();
        // -----------------------

        velocity_buffer = new ComputeBuffer(i_Resolution * i_Resolution, sizeof(float) * 4);

        // -----------------------
        addForceHandle = userInput_shader.FindKernel("AddForce_mouse");

        userInput_shader.SetInt   ("i_Resolution",         i_Resolution);
        userInput_shader.SetFloat ("_force_multiplier",    force_strength);
        userInput_shader.SetFloat ("_force_effect_radius", force_radius);
        userInput_shader.SetFloat ("_force_falloff",       force_falloff);
        userInput_shader.SetBuffer(addForceHandle, "_user_applied_force_buffer", velocity_buffer);

        // ___
        st2txHandle = structuredBufferToTextureShader.FindKernel("StructeredToTextureBillinear");
        structuredBufferToTextureShader.SetInt    ("i_Resolution",          i_Resolution);
        structuredBufferToTextureShader.SetInt    ("_Results_Resolution",   canvas_dimension);
        structuredBufferToTextureShader.SetBuffer (st2txHandle, "_Source",  velocity_buffer);
        structuredBufferToTextureShader.SetTexture(st2txHandle, "_Results", visulasation_texture);

        // -----------------------
        cb = new CommandBuffer()
        {
            name = "Debug_Add_Force_Kernel",
        };

        DispatchDimensions addForce_dispatchDim = ComputeShaderUtility.CheckGetDispatchDimensions(userInput_shader, addForceHandle, (uint) i_Resolution, (uint) i_Resolution, 1);

        cb.DispatchCompute(userInput_shader, addForceHandle, (int) addForce_dispatchDim.dispatch_x, (int) addForce_dispatchDim.dispatch_y, (int) addForce_dispatchDim.dispatch_z);

        DispatchDimensions st2tx_dispatchDim    = ComputeShaderUtility.CheckGetDispatchDimensions(structuredBufferToTextureShader, st2txHandle, (uint)canvas_dimension, (uint)canvas_dimension, 1);

        cb.DispatchCompute(structuredBufferToTextureShader, st2txHandle, (int) st2tx_dispatchDim.dispatch_x, (int) st2tx_dispatchDim.dispatch_y, (int) st2tx_dispatchDim.dispatch_z);

        cb.Blit(visulasation_texture, BuiltinRenderTextureType.CameraTarget);

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, cb);

    }

    // ------------------------------------------------------------------
    // LOOP
    void Update()
    {
        Vector3 mouse_pos_pixel_coord = Input.mousePosition;
        Vector2 mouse_pos_normalized  = main_cam.ScreenToViewportPoint(mouse_pos_pixel_coord);
                mouse_pos_normalized  = new Vector2(Mathf.Clamp01(mouse_pos_normalized.x), Mathf.Clamp01(mouse_pos_normalized.y));
        Vector2 mouse_pos_struct_pos  = new Vector2(mouse_pos_normalized.x * i_Resolution, mouse_pos_normalized.y * i_Resolution);


        userInput_shader.SetVector("_mouse_pos_current", mouse_pos_struct_pos);                   // Pass on the mouse position already in the coordinate system of the structured buffer as 2D coord
        userInput_shader.SetVector("_mouse_pos_prev",    mouse_previus_pos);                      // Pass on the mouse position already in the coordinate system of the structured buffer as 2D coord

        print(string.Format("The current mouse pos is {0}, and the previous one is {1}", mouse_pos_struct_pos, mouse_previus_pos));

        mouse_previus_pos = mouse_pos_struct_pos;


    }
}

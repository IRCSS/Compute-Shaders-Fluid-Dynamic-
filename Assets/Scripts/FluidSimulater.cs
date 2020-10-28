using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class FluidSimulater
{
    // ------------------------------------------------------------------
    // VARIABLES
    //___________
    // public

    [Header("Compute Shader Refs")]
    [Space(2)]
    public ComputeShader StokeNavierShader;
    public ComputeShader SolverShader;
    public ComputeShader BorderShader;
    public ComputeShader StructuredBufferToTextureShader;
    public ComputeShader UserInputShader;
    public ComputeShader StructuredBufferUtilityShader;

    [Space(4)]
    [Header("Simulation Settings")]
    [Space(2)]
    public uint          canvas_dimension     = 512;          // Resolution of the render target used at the end, this can be lower or higher than the actual simulation grid resoltion
    public uint          simulation_dimension = 256;          // Resolution of the simulation grid
    public uint          solver_iteration_num = 80;
    public float         grid_scale           = 1;
    public float         time_step            = 1;
    public float         Viscosity            = 0.5f;
    public float         force_radius         = 1;
    public float         force_falloff        = 2;
    public float         dye_radius           = 1.0f;
    public float         dye_falloff          = 2.0f;


    //___________
    // private

    private Camera        main_cam;

    private CommandBuffer sim_command_buffer;
    private RenderTexture visulasation_texture;


    private int           _handle_add_dye;
    private int           _handle_st2tx;
    private int           _handle_Jacobi_Solve;
    private int           _handle_Copy_StructuredBuffer;
    private int           _handle_Clear_StructuredBuffer;

    // ------------------------------------------------------------------
    // CONSTRUCTOR

    public FluidSimulater()                       // Default Constructor
    {
        canvas_dimension     = 512   ;
        simulation_dimension = 256   ;
        solver_iteration_num = 80    ;
        grid_scale           = 1     ;
        force_radius         = 1     ;
        force_falloff        = 2     ;
        dye_radius           = 1.0f  ;
        dye_falloff          = 2.0f  ;

    }

    public FluidSimulater(FluidSimulater other)   // Copy Constructor
    {
        canvas_dimension     = other.canvas_dimension     ;
        simulation_dimension = other.simulation_dimension ;
        solver_iteration_num = other.solver_iteration_num ;
        grid_scale           = other.grid_scale           ;
        force_radius         = other.force_radius         ;
        force_falloff        = other.force_falloff        ;
        dye_radius           = other.dye_radius           ;
        dye_falloff          = other.dye_falloff          ;
    }

    // ------------------------------------------------------------------
    // DESTRUCTOR

        
    public void Release()
    {
        visulasation_texture.Release();
        ComputeShaderUtility.Release();
    }
    // ------------------------------------------------------------------
    // INITALISATION

    public void Initialize()
    {

        ComputeShaderUtility.Initialize();

        // -----------------------
        main_cam = Camera.main;
        if (main_cam == null) Debug.LogError("Could not find main camera, make sure the camera is tagged as main");
        
        main_cam.orthographic     = true;                                                                     // Make sure the camera is ortho. Perspecitve camera has a transformation matrix which will screw with everything
        main_cam.aspect           = 1.0f;
        main_cam.orthographicSize = 1;

        // -----------------------
        visulasation_texture = new RenderTexture((int) canvas_dimension, (int)canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = false,
        };

        visulasation_texture.Create();
        // -----------------------
        // Setting kernel handles

        _handle_add_dye                 =  ComputeShaderUtility.GetKernelHandle( UserInputShader                , "AddDye"                      );
        _handle_st2tx                   =  ComputeShaderUtility.GetKernelHandle( StructuredBufferToTextureShader, "StructeredToTextureBillinear");
        _handle_Jacobi_Solve            =  ComputeShaderUtility.GetKernelHandle( SolverShader                   , "Jacobi_Solve"                );
        _handle_Copy_StructuredBuffer   =  ComputeShaderUtility.GetKernelHandle( StructuredBufferUtilityShader  , "Copy_StructuredBuffer"       );
        _handle_Clear_StructuredBuffer  =  ComputeShaderUtility.GetKernelHandle( StructuredBufferUtilityShader  , "Clear_StructuredBuffer"      );

        // -----------------------
        // Initialize Kernel Parameters, buffers our bound by the actual shader dispatch functions

        UserInputShader    .SetInt("i_Resolution", (int) simulation_dimension);

        // __

        UpdateRuntimeKernelParameters();

        StructuredBufferToTextureShader.SetInt    ("i_Resolution",            (int) simulation_dimension);
        StructuredBufferToTextureShader.SetInt    ("_Results_Resolution",     (int) canvas_dimension    );
        StructuredBufferToTextureShader.SetTexture(_handle_st2tx, "_Results",       visulasation_texture);

        // -----------------------

        sim_command_buffer = new CommandBuffer()
        {
            name = "Simulation_Command_Buffer",
        };

        
    }
    // ------------------------------------------------------------------
    // LOOP

    public void Tick(float deltaTime)
    {
        UpdateRuntimeKernelParameters();
    }

    // ------------------------------------------------------------------
    // SIMULATION STEPS

    public void AddUserForce(ComputeBuffer force_buffer)
    {

    }

    public void AddDye(ComputeBuffer dye_buffer)
    {
        if (!IsValid()) return;

        SetBufferOnCommandList(sim_command_buffer, dye_buffer, "_dye_buffer");
        DispatchComputeOnCommandBuffer(sim_command_buffer, UserInputShader, _handle_add_dye, simulation_dimension, simulation_dimension, 1);
    }

    public void Diffuse(ComputeBuffer buffer_to_diffuse)
    {
        if (!IsValid())     return;
        if (Viscosity <= 0) return;        // Fluid with a viscosity of zero does not diffuse

        if (!FluidGPUResources.StaticResourcesCreated()) return;

        float centerFactor           = grid_scale * grid_scale / (Viscosity * time_step);
        float reciprocal_of_diagonal = 1.0f / (4.0f + centerFactor);

        bool ping_as_results = false;

        for (int i = 0; i < solver_iteration_num; i++)
        {
            ping_as_results = !ping_as_results;
            if (ping_as_results)                     // Ping ponging back and forth to insure no racing condition. 
            {
                SetBufferOnCommandList(sim_command_buffer, buffer_to_diffuse,              "_b_buffer");
                SetBufferOnCommandList(sim_command_buffer, buffer_to_diffuse,              "_updated_x_buffer");
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping,  "_results");
            } else
            {
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping,  "_b_buffer");
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping,  "_updated_x_buffer");
                SetBufferOnCommandList(sim_command_buffer, buffer_to_diffuse,              "_results");
            }

            sim_command_buffer.SetGlobalInt("_current_iteration", i);
            DispatchComputeOnCommandBuffer(sim_command_buffer, SolverShader, _handle_Jacobi_Solve, simulation_dimension, simulation_dimension, 1);
        }

        if (ping_as_results)                         // The Ping ponging ended on the helper buffer ping. Copy it to the buffer_to_diffuse buffer
        {
            Debug.Log("Diffuse Ended on a Ping Target, now copying over the Ping to the buffer which was supposed to be diffused");
            SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_Copy_Source");
            SetBufferOnCommandList(sim_command_buffer, buffer_to_diffuse,             "_Copy_Target");
            DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Copy_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);
        }

        sim_command_buffer.SetGlobalVector("_Clear_Value_StructuredBuffer", new Vector4(0.0f,0.0f,0.0f,0.0f));
        SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_Clear_Target_StructuredBuffer");
        DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Clear_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);
    }

    public void Advect(ComputeBuffer buffer_to_advect, ComputeBuffer velocity_buffer)
    {

    }

    public void Project(ComputeBuffer buffer_to_visualize)
    {

    }

    public void Visualiuse(ComputeBuffer buffer_to_visualize)
    {
        if (!IsValid()) return;

        SetBufferOnCommandList(sim_command_buffer, buffer_to_visualize, "_Source");
        DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferToTextureShader, _handle_st2tx, canvas_dimension, canvas_dimension, 1);
        sim_command_buffer.Blit(visulasation_texture, BuiltinRenderTextureType.CameraTarget);

    }

    public bool BindCommandBuffer()
    {
        if (!IsValid()) return false;

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, sim_command_buffer);
        return true;
    }
    // ------------------------------------------------------------------
    // HELPER FUNCTIONS

    private void Diverge()
    {

    }

    private void Gradient()
    {

    }

    private void Solve()
    {

    }


    private void UpdateRuntimeKernelParameters()
    {
        // ------------------------------------------------------------------------------
        // USER INPUT ADD DYE 
        UserInputShader.SetVector("_dye_color",         Color.HSVToRGB(0.2f, 0.8f, 0.6f));
        UserInputShader.SetFloat ("_mouse_dye_radius",  dye_radius                      );
        UserInputShader.SetFloat ("_mouse_dye_falloff", dye_falloff                     );

        float mouse_pressed = 0.0f;

        if (Input.GetKey(KeyCode.Mouse0)) mouse_pressed = 1.0f;

        UserInputShader.SetFloat("_mouse_pressed", mouse_pressed);

        Vector3 mouse_pos_pixel_coord = Input.mousePosition;
        Vector2 mouse_pos_normalized  = main_cam.ScreenToViewportPoint(mouse_pos_pixel_coord);
                mouse_pos_normalized  = new Vector2(Mathf.Clamp01(mouse_pos_normalized.x), Mathf.Clamp01(mouse_pos_normalized.y));
        Vector2 mouse_pos_struct_pos  = new Vector2(mouse_pos_normalized.x * simulation_dimension, mouse_pos_normalized.y * simulation_dimension);

        UserInputShader.SetVector("_mouse_position", mouse_pos_struct_pos);                   // Pass on the mouse position already in the coordinate system of the structured buffer as 2D coord

    }

    // _______________
    public bool IsValid()
    {

        if (sim_command_buffer   == null) { Debug.LogError("ERROR: The Fluid Simulater Object is not correctly initalized. The CommandBuffer is NULL");        return false;}
        if (visulasation_texture == null) { Debug.LogError("ERROR: The Fluid Simulater Object is not correctly initalized. The visulasation Texture is NULL"); return false;}
        if (main_cam             == null) { Debug.LogError("ERROR: The Fluid Simulater Object is not correctly initalized. The camera reference is NULL");     return false;}

        return true;
    }


    private void SetBufferOnCommandList(CommandBuffer cb, ComputeBuffer buffer, string buffer_name)
    {
        cb.SetGlobalBuffer(buffer_name, buffer);
    }

    private void DispatchComputeOnCommandBuffer(CommandBuffer cb, ComputeShader toDispatch, int kernel, uint thread_num_x, uint thread_num_y, uint thread_num_z)
    {
        DispatchDimensions group_nums = ComputeShaderUtility.CheckGetDispatchDimensions(toDispatch, kernel, thread_num_x, thread_num_y, thread_num_z);
        cb.DispatchCompute(toDispatch, kernel, (int) group_nums.dispatch_x, (int) group_nums.dispatch_y, (int) group_nums.dispatch_z);

        // Debug
        Debug.Log(string.Format("Attached the computeshader {0}, at kernel {1}, to the commandbuffer {2}." +
            "Dispatch group numbers are, in x, y,z respectivly: {3}", 
            toDispatch.name, ComputeShaderUtility.GetKernelNameFromHandle(toDispatch, kernel), cb.name,
            group_nums.ToString()));
    }
}


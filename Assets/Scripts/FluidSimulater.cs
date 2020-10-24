using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

    [Space(4)]
    [Header("Simulation Settings")]
    [Space(2)]
    public int           canvas_dimension     = 512;          // Resolution of the render target used at the end, this can be lower or higher than the actual simulation grid resoltion
    public int           simulation_dimension = 256;          // Resolution of the simulation grid
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
    


    // ------------------------------------------------------------------
    // CONSTRUCTOR

    public FluidSimulater()                       // Default Constructor
    {
        canvas_dimension     = 512   ;
        simulation_dimension = 256   ;
        force_radius         = 1     ;
        force_falloff        = 2     ;
        dye_radius           = 1.0f  ;
        dye_falloff          = 2.0f  ;

    }

    public FluidSimulater(FluidSimulater other)   // Copy Constructor
    {
        canvas_dimension     = other.canvas_dimension     ;
        simulation_dimension = other.simulation_dimension ;
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
        visulasation_texture = new RenderTexture(canvas_dimension, canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = false,
        };

        visulasation_texture.Create();
        // -----------------------
        // Setting kernel handles

        _handle_add_dye =  ComputeShaderUtility.GetKernelHandle( UserInputShader                , "AddDye"                      );
        _handle_st2tx   =  ComputeShaderUtility.GetKernelHandle( StructuredBufferToTextureShader, "StructeredToTextureBillinear");

        // -----------------------
        // Initialize Kernel Parameters, buffers our bound by the actual shader dispatch functions

        UserInputShader    .SetInt("i_Resolution", simulation_dimension);

        // __

        StructuredBufferToTextureShader.SetInt    ("i_Resolution",            simulation_dimension);
        StructuredBufferToTextureShader.SetInt    ("_Results_Resolution",     canvas_dimension    );
        StructuredBufferToTextureShader.SetTexture(_handle_st2tx, "_Results", visulasation_texture);

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

    }

    // ------------------------------------------------------------------
    // SIMULATION STEPS

    public void AddUserForce(ComputeBuffer force_buffer)
    {

    }

    public void AddDye(ComputeBuffer dye_buffer)
    {

    }

    public void Diffuse(ComputeBuffer buffer_to_diffuse)
    {

    }

    public void Advect(ComputeBuffer buffer_to_advect, ComputeBuffer velocity_buffer)
    {

    }

    public void Project(ComputeBuffer buffer_to_visualize)
    {

    }

    public void Visualiuse(ComputeBuffer buffer_to_visualize)
    {


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
        UserInputShader.SetVector("_dye_color",         Color.HSVToRGB(0.2f, 0.8f, 0.6f));
        UserInputShader.SetFloat ("_mouse_dye_radius",  dye_radius                      );
        UserInputShader.SetFloat ("_mouse_dye_falloff", dye_falloff                     );

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


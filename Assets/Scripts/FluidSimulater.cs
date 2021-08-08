using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum FieldType
{
    Velocity, Pressure, Dye 
}


[System.Serializable]
public class FluidSimulater
{

    // ------------------------------------------------------------------
    // TYPES
    //___________
    // public

    [HideInInspector]
    public delegate Vector2 GetMousePositionCallBack(ref bool isInBound);

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
    public float         force_strength       = 1.0f;
    public float         force_radius         = 1;
    public float         force_falloff        = 2;
    public float         dye_radius           = 1.0f;
    public float         dye_falloff          = 2.0f;
    public float         velocity_dissapation = 0.999f;


    [Space(4)]
    [Header("Control Settings")]
    [Space(2)]

    public KeyCode      ApplyDyeKey  ;
    public KeyCode      ApplyForceKey;

    //___________
    // private

    private Camera        main_cam;

    private CommandBuffer sim_command_buffer;
    private RenderTexture visulasation_texture;

    private GetMousePositionCallBack mousPosOverrider;               // If this is NULL it is assumed the calculation is happening in screen space and the screen space pos is used for input position

    private int           _handle_add_dye;
    private int           _handle_st2tx;
    private int           _handle_Jacobi_Solve;
    private int           _handle_Copy_StructuredBuffer;
    private int           _handle_Clear_StructuredBuffer;
    private int           _handle_add_dye_from_texture;
    private int           _handle_NeuMannBoundary;
    private int           _handle_addForceWithMouse;
    private int           _handle_advection;
    private int           _handle_divergence;
    private int           _handle_calculate_divergence_free;
    private int           _handle_update_arbitary_boundary_offset;
    private int           _handle_arbitary_boundary_velocity;
    private int           _handle_arbitary_boundary_pressure;
    private int           _handle_arbitary_boundary_dye           ;

    private Vector2       mouse_previus_pos;
    private bool          mouse_previus_outofBound;
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
        velocity_dissapation = 0.999f;
        mousPosOverrider = null;

        ApplyDyeKey   = KeyCode.Mouse0;
        ApplyForceKey = KeyCode.Mouse1;

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
        velocity_dissapation = other.velocity_dissapation ;
        mousPosOverrider = null;

        ApplyDyeKey   = KeyCode.Mouse0;
        ApplyForceKey = KeyCode.Mouse1;
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
        mousPosOverrider = null;

        // -----------------------
        main_cam = Camera.main;
        if (main_cam == null) Debug.LogError("Could not find main camera, make sure the camera is tagged as main");
        
        //main_cam.orthographic     = true;                                                                     // Make sure the camera is ortho. Perspecitve camera has a transformation matrix which will screw with everything
        //main_cam.aspect           = 1.0f;
        //main_cam.orthographicSize = 1;

        // -----------------------
        
        mouse_previus_pos = GetCurrentMouseInSimulationSpace();

        // -----------------------
        visulasation_texture = new RenderTexture((int) canvas_dimension, (int)canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = false,
        };

        visulasation_texture.Create();
        // -----------------------
        // Setting kernel handles

        _handle_add_dye                         =  ComputeShaderUtility.GetKernelHandle( UserInputShader                , "AddDye"                      );
        _handle_add_dye_from_texture            =  ComputeShaderUtility.GetKernelHandle( UserInputShader                , "AddDye_from_picture"         );
        _handle_st2tx                           =  ComputeShaderUtility.GetKernelHandle( StructuredBufferToTextureShader, "StructeredToTextureBillinear");
        _handle_Jacobi_Solve                    =  ComputeShaderUtility.GetKernelHandle( SolverShader                   , "Jacobi_Solve"                );
        _handle_Copy_StructuredBuffer           =  ComputeShaderUtility.GetKernelHandle( StructuredBufferUtilityShader  , "Copy_StructuredBuffer"       );
        _handle_Clear_StructuredBuffer          =  ComputeShaderUtility.GetKernelHandle( StructuredBufferUtilityShader  , "Clear_StructuredBuffer"      );
        _handle_NeuMannBoundary                 =  ComputeShaderUtility.GetKernelHandle( BorderShader                   , "NeuMannBoundary"             );
        _handle_addForceWithMouse               =  ComputeShaderUtility.GetKernelHandle( UserInputShader                , "AddForce_mouse"              );
        _handle_advection                       =  ComputeShaderUtility.GetKernelHandle( StokeNavierShader              , "advection"                   );
        _handle_divergence                      =  ComputeShaderUtility.GetKernelHandle( StokeNavierShader              , "divergence"                  );
        _handle_calculate_divergence_free       =  ComputeShaderUtility.GetKernelHandle( StokeNavierShader              , "calculate_divergence_free"   );
        _handle_update_arbitary_boundary_offset =  ComputeShaderUtility.GetKernelHandle( BorderShader                   , "UpdateArbitaryBoundaryOffset");
        _handle_arbitary_boundary_velocity      =  ComputeShaderUtility.GetKernelHandle( BorderShader                   , "ArbitaryBoundaryVelocity"    );
        _handle_arbitary_boundary_pressure      =  ComputeShaderUtility.GetKernelHandle( BorderShader                   , "ArbitaryBoundaryPressure"    );
        _handle_arbitary_boundary_dye           =  ComputeShaderUtility.GetKernelHandle( BorderShader                   , "ArbitaryBoundaryDye"         );

        
        
       
        // -----------------------
        // Initialize Kernel Parameters, buffers our bound by the actual shader dispatch functions


        // __

        UpdateRuntimeKernelParameters();
        
        StructuredBufferToTextureShader.SetInt    ("_Results_Resolution",     (int) canvas_dimension    );
        StructuredBufferToTextureShader.SetTexture(_handle_st2tx, "_Results",       visulasation_texture);

        // -----------------------

        sim_command_buffer = new CommandBuffer()
        {
            name = "Simulation_Command_Buffer",
        };

        // Global Parameters that are immutable in runtime
        sim_command_buffer.SetGlobalInt  ("i_Resolution", (int)simulation_dimension);
        sim_command_buffer.SetGlobalFloat("i_timeStep",        time_step           );
        sim_command_buffer.SetGlobalFloat("i_grid_scale",      grid_scale          );
        
        

    }

    public void SubmitMousePosOverrideDelegate(GetMousePositionCallBack getterFunction)
    {
        mousPosOverrider = getterFunction;
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
        SetBufferOnCommandList(sim_command_buffer, force_buffer, "_user_applied_force_buffer");
        DispatchComputeOnCommandBuffer(sim_command_buffer, UserInputShader, _handle_addForceWithMouse, simulation_dimension, simulation_dimension, 1);
    }


    public void AddDye(ComputeBuffer dye_buffer)
    {
        if (!IsValid()) return;

        SetBufferOnCommandList(sim_command_buffer, dye_buffer, "_dye_buffer");
        DispatchComputeOnCommandBuffer(sim_command_buffer, UserInputShader, _handle_add_dye, simulation_dimension, simulation_dimension, 1);
    }

    public void AddDyeFromTexture(ComputeBuffer dye_buffer, Texture2D source_tex, bool one_time)
    {
        if (!IsValid()) return;

        if (one_time)
        {
            UserInputShader.SetBuffer (_handle_add_dye_from_texture, "_dye_buffer",         dye_buffer);
            UserInputShader.SetTexture(_handle_add_dye_from_texture, "_dye_source_texture", source_tex);
            UserInputShader.SetInt    ("i_Resolution", (int) simulation_dimension);
            DispatchDimensions group_nums = ComputeShaderUtility.CheckGetDispatchDimensions(UserInputShader, 
                                _handle_add_dye_from_texture, simulation_dimension, simulation_dimension, 1);
            UserInputShader.Dispatch(_handle_add_dye_from_texture, 
                (int)group_nums.dispatch_x, (int)group_nums.dispatch_y, (int)group_nums.dispatch_z);
        }
        else
        {
            Debug.LogError("Feature still needs to be implemented!");
            //SetBufferOnCommandList(sim_command_buffer, dye_buffer, "_dye_buffer");
            //sim_command_buffer.SetGlobalTexture("_dye_source_texture", source_tex);
            //DispatchComputeOnCommandBuffer(sim_command_buffer, UserInputShader, _handle_add_dye_from_texture, simulation_dimension, simulation_dimension, 1);
        }
    }

    public void Diffuse(ComputeBuffer buffer_to_diffuse)
    {
        if (!IsValid())     return;
        if (Viscosity <= 0) return;                  // Fluid with a viscosity of zero does not diffuse

        if (!FluidGPUResources.StaticResourcesCreated()) return;

        

        float centerFactor           = grid_scale * grid_scale / (Viscosity * time_step);
        float reciprocal_of_diagonal = 1.0f / (4.0f + centerFactor);

        sim_command_buffer.SetGlobalFloat("_centerFactor",  centerFactor          );
        sim_command_buffer.SetGlobalFloat("_rDiagonal",     reciprocal_of_diagonal);

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
        
        ClearBuffer(FluidGPUResources.buffer_ping, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

    }

    public void Advect(ComputeBuffer buffer_to_advect, ComputeBuffer velocity_buffer, float disspationFactor)
    {
        if (!IsValid()) return;

        sim_command_buffer.SetGlobalFloat("_dissipationFactor", disspationFactor);

        SetBufferOnCommandList(sim_command_buffer, velocity_buffer,               "_velocity_field_buffer" );
        SetBufferOnCommandList(sim_command_buffer, buffer_to_advect,              "_field_to_advect_buffer");
        SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_new_advected_field"    );

        
        DispatchComputeOnCommandBuffer(sim_command_buffer, StokeNavierShader, _handle_advection, simulation_dimension, simulation_dimension, 1);

        // -------------
        SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_Copy_Source");
        SetBufferOnCommandList(sim_command_buffer, buffer_to_advect,              "_Copy_Target");

        DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Copy_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);

        // -------------
        ClearBuffer(FluidGPUResources.buffer_ping, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));


    }

    public void Project(ComputeBuffer buffer_to_make_divergence_free, ComputeBuffer divergence_field, ComputeBuffer pressure_field)
    {
        if (!IsValid())                                  return;
        if (!FluidGPUResources.StaticResourcesCreated()) return;

        CalculateFieldDivergence(buffer_to_make_divergence_free, divergence_field);

        // ---------------

        float centerFactor   = -1.0f * grid_scale * grid_scale;
        float diagonalFactor = 0.25f;

        sim_command_buffer.SetGlobalFloat("_centerFactor", centerFactor);
        sim_command_buffer.SetGlobalFloat("_rDiagonal",    diagonalFactor);

        SetBufferOnCommandList(sim_command_buffer, divergence_field, "_b_buffer");

        ClearBuffer(pressure_field, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

        
        bool ping_as_results = false;

        for (int i = 0; i < solver_iteration_num; i++)
        {
            ping_as_results = !ping_as_results;
            if (ping_as_results)                     // Ping ponging back and forth to insure no racing condition. 
            {
                HandleCornerBoundaries(pressure_field, FieldType.Pressure);
                SetBufferOnCommandList(sim_command_buffer, pressure_field,                 "_updated_x_buffer");
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping,  "_results");
            } else
            {
                HandleCornerBoundaries(FluidGPUResources.buffer_ping, FieldType.Pressure);
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping,  "_updated_x_buffer");
                SetBufferOnCommandList(sim_command_buffer, pressure_field,                 "_results");
            }

            sim_command_buffer.SetGlobalInt("_current_iteration", i);
            DispatchComputeOnCommandBuffer(sim_command_buffer, SolverShader, _handle_Jacobi_Solve, simulation_dimension, simulation_dimension, 1);
        }

        if (ping_as_results)                         // The Ping ponging ended on the helper buffer ping. Copy it to the buffer_to_diffuse buffer
        {
            SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_Copy_Source");
            SetBufferOnCommandList(sim_command_buffer, pressure_field,                "_Copy_Target");
            DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Copy_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);
        }
        
        ClearBuffer(FluidGPUResources.buffer_ping, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

        // ---------------

        CalculateDivergenceFreeFromPressureField(buffer_to_make_divergence_free, pressure_field, FluidGPUResources.buffer_pong, FluidGPUResources.buffer_ping);

        SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping,  "_Copy_Source");
        SetBufferOnCommandList(sim_command_buffer, buffer_to_make_divergence_free, "_Copy_Target");
        DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Copy_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);

        ClearBuffer(FluidGPUResources.buffer_ping, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        ClearBuffer(FluidGPUResources.buffer_pong, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
    }

    public void UpdateArbitaryBoundaryOffsets(Texture2D boundaryTexture, FluidGPUResources resource_instance)
    {
        // CHECKS
        if(!boundaryTexture)
        {
            Debug.LogError("No valid texture was provided as boundary for the update arbitary boundery method.");
            return;
        }

        if(boundaryTexture.width != boundaryTexture.height)
        {
            Debug.LogError("The provided boundary texture to Update Abitrary Boundary Offset is not valid, it is non quardatic texture");
            return;
        }
        // -------------



        BorderShader.SetTexture(_handle_update_arbitary_boundary_offset, "_arbitary_boundaries_texture", boundaryTexture);
        BorderShader.SetInt    ("i_Resolution", (int)simulation_dimension);
        BorderShader.SetFloat  ("_arbitary_boundaries_texel_size",  1.0f / ((float)boundaryTexture.width));

        // Velocity
        // ----------

        BorderShader.SetBuffer(_handle_update_arbitary_boundary_offset, "_arbitaryBoundaryCardinalDirectionsLUT", resource_instance.cardinal_diections_LUT_Velocity);
        BorderShader.SetBuffer(_handle_update_arbitary_boundary_offset, "_perCellArbitaryBoundryOffsets",         resource_instance.boundary_velocity_offset_buffer);

        DispatchDimensions group_nums = ComputeShaderUtility.CheckGetDispatchDimensions(BorderShader,
                            _handle_update_arbitary_boundary_offset, simulation_dimension, simulation_dimension, 1);
        BorderShader.Dispatch(_handle_update_arbitary_boundary_offset,
            (int)group_nums.dispatch_x, (int)group_nums.dispatch_y, (int)group_nums.dispatch_z);

        // Pressure
        // ----------

        BorderShader.SetBuffer(_handle_update_arbitary_boundary_offset, "_arbitaryBoundaryCardinalDirectionsLUT", resource_instance.cardinal_diections_LUT_Presure );
        BorderShader.SetBuffer(_handle_update_arbitary_boundary_offset, "_perCellArbitaryBoundryOffsets",         resource_instance.boundary_pressure_offset_buffer);

        BorderShader.Dispatch(_handle_update_arbitary_boundary_offset,
            (int)group_nums.dispatch_x, (int)group_nums.dispatch_y, (int)group_nums.dispatch_z);

        // Dye
        // ----------
        BorderShader.SetBuffer(_handle_update_arbitary_boundary_offset, "_arbitaryBoundaryCardinalDirectionsLUT", resource_instance.cardinal_diections_LUT_Dye);
        BorderShader.SetBuffer(_handle_update_arbitary_boundary_offset, "_perCellArbitaryBoundryOffsets",         resource_instance.boundary_dye_offset_buffer);

        BorderShader.Dispatch(_handle_update_arbitary_boundary_offset,
            (int)group_nums.dispatch_x, (int)group_nums.dispatch_y, (int)group_nums.dispatch_z);

    }

    public void HandleArbitaryBoundary(ComputeBuffer SetBoundaryOn, ComputeBuffer offsetBuffer, FieldType fieldType)
    {
        if (!IsValid()) return;
        switch (fieldType)
        {
            case FieldType.Velocity:

                SetBufferOnCommandList(sim_command_buffer, SetBoundaryOn, "_velocity_buffer");
                SetBufferOnCommandList(sim_command_buffer, offsetBuffer,  "_perCellArbitaryBoundryOffsetsVellocity");
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_new_handleded_velocity");

                DispatchComputeOnCommandBuffer(sim_command_buffer, BorderShader, _handle_arbitary_boundary_velocity, simulation_dimension, simulation_dimension, 1);

                // -------------
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_Copy_Source");
                SetBufferOnCommandList(sim_command_buffer, SetBoundaryOn,                 "_Copy_Target");

                DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Copy_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);

                // -------------
                ClearBuffer(FluidGPUResources.buffer_ping, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

                return;


            case FieldType.Pressure:

                SetBufferOnCommandList(sim_command_buffer, SetBoundaryOn, "_pressure_buffer");
                SetBufferOnCommandList(sim_command_buffer, offsetBuffer,  "_perCellArbitaryBoundryOffsetsPressure");
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_new_handleded_pressure");

                DispatchComputeOnCommandBuffer(sim_command_buffer, BorderShader, _handle_arbitary_boundary_pressure, simulation_dimension, simulation_dimension, 1);

                // -------------
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_Copy_Source");
                SetBufferOnCommandList(sim_command_buffer, SetBoundaryOn,                 "_Copy_Target");

                DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Copy_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);

                // -------------
                ClearBuffer(FluidGPUResources.buffer_ping, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                
                return;
            case FieldType.Dye:

                SetBufferOnCommandList(sim_command_buffer, SetBoundaryOn, "_dye_buffer");
                SetBufferOnCommandList(sim_command_buffer, offsetBuffer, "_perCellArbitaryBoundryOffsetsDye");
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_new_handleded_dye");

                DispatchComputeOnCommandBuffer(sim_command_buffer, BorderShader, _handle_arbitary_boundary_dye, simulation_dimension, simulation_dimension, 1);

                // -------------
                SetBufferOnCommandList(sim_command_buffer, FluidGPUResources.buffer_ping, "_Copy_Source");
                SetBufferOnCommandList(sim_command_buffer, SetBoundaryOn, "_Copy_Target");

                DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Copy_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);

                // -------------
                ClearBuffer(FluidGPUResources.buffer_ping, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

                return;
        }
    }


    public void Visualiuse(ComputeBuffer buffer_to_visualize)
    {
        if (!IsValid()) return;

        SetBufferOnCommandList(sim_command_buffer, buffer_to_visualize, "_Source");
        StructuredBufferToTextureShader.SetTexture(_handle_st2tx, "_Results", visulasation_texture);

        DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferToTextureShader, _handle_st2tx, canvas_dimension, canvas_dimension, 1);

        sim_command_buffer.Blit(visulasation_texture, BuiltinRenderTextureType.CameraTarget);

    }

    public void CopyBufferToTexture(RenderTexture texture, ComputeBuffer buffer_to_visualize)
    {
        if (!IsValid()) return;

        SetBufferOnCommandList(sim_command_buffer, buffer_to_visualize, "_Source");
        StructuredBufferToTextureShader.SetTexture(_handle_st2tx, "_Results", texture);

        DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferToTextureShader, _handle_st2tx, canvas_dimension, canvas_dimension, 1);
        
    }

    public void HandleCornerBoundaries(ComputeBuffer SetBoundaryOn, FieldType fieldType)
    {
        float scale;
        switch (fieldType)
        {
            case FieldType.Dye:      scale =  0.0f; break;
            case FieldType.Velocity: scale = -1.0f; break;
            case FieldType.Pressure: scale =  1.0f; break;
            default:                 scale =  0.0f; break;
        }

        sim_command_buffer.SetGlobalFloat("_neumaboundary_scale", scale);
        SetBufferOnCommandList(sim_command_buffer, SetBoundaryOn, "_neumaboundary_field_to_contain");
        DispatchComputeOnCommandBuffer(sim_command_buffer, BorderShader, _handle_NeuMannBoundary, simulation_dimension * 4, 1, 1);
    }

    public bool BindCommandBuffer()
    {
        if (!IsValid()) return false;

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, sim_command_buffer);
        return true;
    }
    // ------------------------------------------------------------------
    // HELPER FUNCTIONS

    private void CalculateFieldDivergence(ComputeBuffer field_to_calculate, ComputeBuffer divergnece_buffer)
    {

        SetBufferOnCommandList(sim_command_buffer, field_to_calculate, "_divergence_vector_field");        // Input
        SetBufferOnCommandList(sim_command_buffer, divergnece_buffer,  "_divergence_values"      );        // Output
        DispatchComputeOnCommandBuffer(sim_command_buffer, StokeNavierShader, _handle_divergence, simulation_dimension, simulation_dimension, 1);

    }

    private void CalculateDivergenceFreeFromPressureField(ComputeBuffer non_zero_vector_field, ComputeBuffer pressure_field, ComputeBuffer debug_pressure_gradient, ComputeBuffer divergence_free)
    {
        SetBufferOnCommandList(sim_command_buffer, non_zero_vector_field,   "_non_zero_divergence_velocity_field");        // Input
        SetBufferOnCommandList(sim_command_buffer, pressure_field,          "_pressure_field"                    );        // Input
        SetBufferOnCommandList(sim_command_buffer, debug_pressure_gradient, "_pressure_gradient"                 );        // Output
        SetBufferOnCommandList(sim_command_buffer, divergence_free,         "_divergence_free_field"             );        // Output

        DispatchComputeOnCommandBuffer(sim_command_buffer, StokeNavierShader, _handle_calculate_divergence_free, simulation_dimension, simulation_dimension, 1);
    }

    private void ClearBuffer(ComputeBuffer buffer, Vector4 clear_value)
    {
        sim_command_buffer.SetGlobalVector("_Clear_Value_StructuredBuffer", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        SetBufferOnCommandList(sim_command_buffer, buffer, "_Clear_Target_StructuredBuffer");
        DispatchComputeOnCommandBuffer(sim_command_buffer, StructuredBufferUtilityShader, _handle_Clear_StructuredBuffer, simulation_dimension * simulation_dimension, 1, 1);
    }

    private void UpdateRuntimeKernelParameters()
    {
        // ------------------------------------------------------------------------------
        // USER INPUT ADD DYE 
        //UserInputShader.SetVector("_dye_color",         Color.HSVToRGB(0.2f, 0.8f, 0.6f));
        float randomHue = Mathf.Abs(Mathf.Sin(Time.time * 0.8f + 1.2f) + Mathf.Sin(Time.time * 0.7f + 2.0f));
              randomHue = randomHue - Mathf.Floor(randomHue);
        UserInputShader.SetVector("_dye_color", Color.HSVToRGB(randomHue, Mathf.Abs(Mathf.Sin(Time.time * 0.8f + 1.2f))*0.2f + 0.8f, Mathf.Abs(Mathf.Sin(Time.time * 0.7f + 2.0f)) * 0.2f + 0.5f));
        UserInputShader.SetFloat ("_mouse_dye_radius",  dye_radius                      );
        UserInputShader.SetFloat ("_mouse_dye_falloff", dye_falloff                     );

        // USER INPUT ADD FORCE WITH MOUSE

        float forceController = 0;

        if (Input.GetKey(ApplyForceKey)) forceController = force_strength;

        UserInputShader.SetFloat ("_force_multiplier",    forceController               );
        UserInputShader.SetFloat ("_force_effect_radius", force_radius                  );
        UserInputShader.SetFloat ("_force_falloff",       force_falloff                 );

        float mouse_pressed = 0.0f;

        
        

        if (Input.GetKey(ApplyDyeKey)) mouse_pressed = 1.0f;

        UserInputShader.SetFloat("_mouse_pressed", mouse_pressed);

        Vector2 mouse_pos_struct_pos = GetCurrentMouseInSimulationSpace();
        


        UserInputShader.SetVector("_mouse_position",    mouse_pos_struct_pos);                   // Pass on the mouse position already in the coordinate system of the structured buffer as 2D coord
        UserInputShader.SetVector("_mouse_pos_current", mouse_pos_struct_pos);                   // Pass on the mouse position already in the coordinate system of the structured buffer as 2D coord
        UserInputShader.SetVector("_mouse_pos_prev",    mouse_previus_pos);                      // Pass on the mouse position already in the coordinate system of the structured buffer as 2D coord

        mouse_previus_pos = mouse_pos_struct_pos;
    }

    // _______________


    public bool IsValid()
    {
        if (StokeNavierShader               == null) { Debug.LogError("ERROR: The Stoke Navier Compute Shader reference is not set in inspector");                   return false;}
        if (SolverShader                    == null) { Debug.LogError("ERROR: The Solver Compute Shader Shader reference is not set in inspector");                  return false;}
        if (BorderShader                    == null) { Debug.LogError("ERROR: The Border Compute Shader reference is not set in inspector");                         return false;}
        if (StructuredBufferToTextureShader == null) { Debug.LogError("ERROR: The  User Input Compute Shader reference is not set in inspector");                    return false;}
        if (UserInputShader                 == null) { Debug.LogError("ERROR: The Structured Buffer To Texture Compute Shader reference is not set in inspector");   return false;}
        if (StructuredBufferUtilityShader   == null) { Debug.LogError("ERROR: The Structured BufferUtility Compute Shader reference is not set in inspector");       return false;}

        if (sim_command_buffer   == null) { Debug.LogError("ERROR: The Fluid Simulater Object is not correctly initalized. The CommandBuffer is NULL");        return false;}
        if (visulasation_texture == null) { Debug.LogError("ERROR: The Fluid Simulater Object is not correctly initalized. The visulasation Texture is NULL"); return false;}
        if (main_cam             == null) { Debug.LogError("ERROR: The Fluid Simulater Object is not correctly initalized. The camera reference is NULL");     return false;}

        return true;
    }


    private Vector2 GetCurrentMouseInSimulationSpace()
    {
        if(mousPosOverrider == null)
        {
           Vector3 mouse_pos_pixel_coord = Input.mousePosition;
           Vector2 mouse_pos_normalized  = main_cam.ScreenToViewportPoint(mouse_pos_pixel_coord);
                   mouse_pos_normalized  = new Vector2(Mathf.Clamp01(mouse_pos_normalized.x), Mathf.Clamp01(mouse_pos_normalized.y));
           return new Vector2(mouse_pos_normalized.x * simulation_dimension, mouse_pos_normalized.y * simulation_dimension);
        }

        // case there is a overrider
        bool isInBound = true;
        Vector2 mousPosInUnitSpace = mousPosOverrider(ref isInBound);



        if (!isInBound)
        {
            mouse_previus_outofBound = !isInBound;
            return mouse_previus_pos;
        }

        if (mouse_previus_outofBound) mouse_previus_pos = mousPosInUnitSpace * simulation_dimension;
         mouse_previus_outofBound = !isInBound;


        return mousPosInUnitSpace * simulation_dimension;
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


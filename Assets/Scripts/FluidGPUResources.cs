using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidGPUResources
{
    // ------------------------------------------------------------------
    // VARIABLES
    //___________
    // public

    [Header("Compute Buffers")]
    [Space(2)]

    public ComputeBuffer dye_buffer       ;                        // Contains the amount of dye per cell across the whole field. For a colored Dye, this is a RGB value
    public ComputeBuffer velocity_buffer  ;                        // The amount of velocity per cell
    public ComputeBuffer divergence_buffer;                        // How much divergence is in each cell. This is calculated every frame from the velocity buffer
    public ComputeBuffer pressure_buffer  ;                        // How much pressure is build up in each frame, calculated from velocity buffer and divergence buffer

    public ComputeBuffer cardinal_diections_LUT_Velocity;          // Use for arbitrary bounderies . A look up table containing the different normals permutation of boundaries
    public ComputeBuffer cardinal_diections_LUT_Presure ;          // Use for arbitrary bounderies . A look up table containing the different normals permutation of boundaries
    public ComputeBuffer cardinal_diections_LUT_Dye     ;          // Use for arbitrary bounderies . A look up table containing the different normals permutation of boundaries
    public ComputeBuffer boundary_velocity_offset_buffer;          // These buffers contain the per cell normals of the arbitrary bounderies
    public ComputeBuffer boundary_pressure_offset_buffer;          // These buffers contain the per cell normals of the arbitrary bounderies
    public ComputeBuffer boundary_dye_offset_buffer     ;          // These buffers contain the per cell normals of the arbitrary bounderies

    public static ComputeBuffer buffer_ping;                       // Used for solver loops, by ping ponging back and forth between these two, you can execture a loop
    public static ComputeBuffer buffer_pong;                       // Used for solver loops, by ping ponging back and forth between these two, you can execture a loop

    public static RenderTexture coppier_texture;

    //___________
    // private

    private int simulation_dimensions;                 // The resolution of the simulation grid. This is recieved from the fluid simulator

    // ------------------------------------------------------------------
    // CONSTRUCTOR

    public FluidGPUResources()                         // Default Constructor, should never be used, is there for editor sake
    {
        simulation_dimensions = 256;

    }

    public FluidGPUResources(FluidGPUResources other)  // Copy Contructor
    {
        simulation_dimensions = other.simulation_dimensions;
    }

    public FluidGPUResources(FluidSimulater fso)       // The actual contructor used by the code
    {
        simulation_dimensions = (int) fso.simulation_dimension;
    }


    // ------------------------------------------------------------------
    // DESTRUCTOR
    public void Release()
    {
        velocity_buffer                 .Release();
        dye_buffer                      .Release();
        divergence_buffer               .Release();
        pressure_buffer                 .Release();
        buffer_ping                     .Release();
        buffer_pong                     .Release();
        boundary_velocity_offset_buffer .Release();
        boundary_dye_offset_buffer      .Release();
        boundary_pressure_offset_buffer .Release();
        cardinal_diections_LUT_Velocity .Release();
        cardinal_diections_LUT_Presure  .Release();
        cardinal_diections_LUT_Dye      .Release();
    }

    // ------------------------------------------------------------------
    // INITALISATION

    public void Create()
    {
        velocity_buffer                   = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        dye_buffer                        = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        divergence_buffer                 = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        pressure_buffer                   = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        buffer_pong                       = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        buffer_ping                       = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        cardinal_diections_LUT_Velocity   = new ComputeBuffer(32                                           , sizeof(int)   * 4);
        cardinal_diections_LUT_Presure    = new ComputeBuffer(32                                           , sizeof(int)   * 4);
        cardinal_diections_LUT_Dye        = new ComputeBuffer(32                                           , sizeof(int)   * 4);
        boundary_velocity_offset_buffer   = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        boundary_pressure_offset_buffer   = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        boundary_dye_offset_buffer        = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);

        cardinal_diections_LUT_Velocity.SetData(ArbitaryBoundaryLUTGenerator.GetVelocityLUT(), 0, 0,  32);
        cardinal_diections_LUT_Presure .SetData(ArbitaryBoundaryLUTGenerator.GetPressureLUT(), 0, 0,  32);
        cardinal_diections_LUT_Dye     .SetData(ArbitaryBoundaryLUTGenerator.GetDyeLUT(),      0, 0,  32);

        
    }

    public static bool StaticResourcesCreated()
    {

        if (!buffer_ping.IsValid() || !buffer_pong.IsValid()) { Debug.LogError("Static Resources are still not created. Should not be accessed!"); return false; }
        return true;
    }

}

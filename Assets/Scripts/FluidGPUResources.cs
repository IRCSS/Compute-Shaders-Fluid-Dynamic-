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

    public ComputeBuffer dye_buffer;
    public ComputeBuffer velocity_buffer;
    public ComputeBuffer divergence_buffer;
    public ComputeBuffer pressure_buffer;

    public ComputeBuffer cardinal_diections_LUT;
    public ComputeBuffer boundary_velocity_buffer;
    public ComputeBuffer boundary_pressure_buffer;
    public ComputeBuffer boundary_dye_buffer     ;

    public static ComputeBuffer buffer_ping;
    public static ComputeBuffer buffer_pong;

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
        velocity_buffer         .Release();
        dye_buffer              .Release();
        divergence_buffer       .Release();
        pressure_buffer         .Release();
        buffer_ping             .Release();
        buffer_pong             .Release();
        cardinal_diections_LUT  .Release();
        boundary_velocity_buffer.Release();
    }

    // ------------------------------------------------------------------
    // INITALISATION

    public void Create()
    {
        velocity_buffer          = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        dye_buffer               = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        divergence_buffer        = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        pressure_buffer          = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        buffer_ping              = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        buffer_pong              = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        cardinal_diections_LUT   = new ComputeBuffer(32                                           , sizeof(float) * 4);
        boundary_velocity_buffer = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        boundary_pressure_buffer = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);
        boundary_dye_buffer      = new ComputeBuffer(simulation_dimensions * simulation_dimensions, sizeof(float) * 4);

        



    }

    public static bool StaticResourcesCreated()
    {

        if (!buffer_ping.IsValid() || !buffer_pong.IsValid()) { Debug.LogError("Static Resources are still not created. Should not be accessed!"); return false; }
        return true;
    }

}

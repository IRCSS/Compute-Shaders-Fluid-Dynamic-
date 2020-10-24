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
        simulation_dimensions = fso.simulation_dimension;
    }


    // ------------------------------------------------------------------
    // DESTRUCTOR
    public void Release()
    {
        velocity_buffer.Release();
        dye_buffer     .Release();
    }

    // ------------------------------------------------------------------
    // INITALISATION

    public void Create(int simulation_dimension)
    {
        velocity_buffer = new ComputeBuffer(simulation_dimension * simulation_dimension, sizeof(float) * 4);
        dye_buffer      = new ComputeBuffer(simulation_dimension * simulation_dimension, sizeof(float) * 4);

    }


}

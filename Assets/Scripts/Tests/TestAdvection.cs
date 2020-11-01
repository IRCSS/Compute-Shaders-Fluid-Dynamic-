using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAdvection : MonoBehaviour
{

 // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public  FluidSimulater    fluid_simulater;

    //___________
    // private
    private FluidGPUResources resources;


    // ------------------------------------------------------------------
    // INITALISATION
    void Start()
    {
        fluid_simulater.Initialize();
        resources = new FluidGPUResources(fluid_simulater);
        resources.Create();

        //--
        fluid_simulater.AddDye                 (resources.dye_buffer);
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye);
        fluid_simulater.Diffuse                (resources.dye_buffer);

        fluid_simulater.AddUserForce           (resources.velocity_buffer);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity);
        fluid_simulater.Diffuse                (resources.velocity_buffer);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity);

        fluid_simulater.Visualiuse             (resources.velocity_buffer);

        fluid_simulater.BindCommandBuffer();
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
    }
}

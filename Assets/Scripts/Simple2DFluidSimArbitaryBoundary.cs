using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simple2DFluidSimArbitaryBoundary : MonoBehaviour
{


 // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public  FluidSimulater    fluid_simulater;
    public  Texture2D         boundaryTexture;
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

        fluid_simulater.UpdateArbitaryBoundaryOffsets(boundaryTexture, resources);

        //--


        Vector2 sourcePosition  = new Vector2(fluid_simulater.simulation_dimension / 2, fluid_simulater.simulation_dimension);
        Vector2 sourceDirection = new Vector2(0.0f, -1.0f); 


        fluid_simulater.AddUserForce           (resources.velocity_buffer                                   );
        fluid_simulater.AddConstantForceSource (resources.velocity_buffer, sourcePosition,
                                                sourceDirection, 10.0f, fluid_simulater.force_radius*2.0f, fluid_simulater.force_falloff*2.0f);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleCornerBoundaries (resources.pressure_buffer, FieldType.Pressure               );
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.HandleArbitaryBoundary (resources.pressure_buffer, resources.boundary_pressure_offset_buffer, FieldType.Pressure);
        fluid_simulater.Diffuse                (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleCornerBoundaries (resources.pressure_buffer, FieldType.Pressure               );
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.HandleArbitaryBoundary (resources.pressure_buffer, resources.boundary_pressure_offset_buffer, FieldType.Pressure);
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer);
        fluid_simulater.Advect                 (resources.velocity_buffer, resources.velocity_buffer, fluid_simulater.velocity_dissapation);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleCornerBoundaries (resources.pressure_buffer, FieldType.Pressure               );
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.HandleArbitaryBoundary (resources.pressure_buffer, resources.boundary_pressure_offset_buffer, FieldType.Pressure);

        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer);


        fluid_simulater.AddDye                 (resources.dye_buffer                                        );
        fluid_simulater.AddConstantDyeSource   (resources.dye_buffer, sourcePosition);
        fluid_simulater.Advect                 (resources.dye_buffer, resources.velocity_buffer, 0.995f);
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );
        fluid_simulater.HandleArbitaryBoundary (resources.dye_buffer, resources.boundary_dye_offset_buffer, FieldType.Dye);
        fluid_simulater.Diffuse                (resources.dye_buffer                                        );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );
        fluid_simulater.HandleArbitaryBoundary (resources.dye_buffer, resources.boundary_dye_offset_buffer, FieldType.Dye);

        fluid_simulater.Visualiuse             (resources.dye_buffer);

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

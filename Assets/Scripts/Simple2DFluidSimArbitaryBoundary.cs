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
    public  Shader            boundaryBlitShader;
    //___________
    // private
    private FluidGPUResources resources;
    private Material          boundaryBlitMat;

    // ------------------------------------------------------------------
    // INITALISATION
    void Start()
    {
        fluid_simulater.Initialize();
        resources = new FluidGPUResources(fluid_simulater);
        resources.Create();

        fluid_simulater.UpdateArbitaryBoundaryOffsets(boundaryTexture, resources);

        //--

        boundaryBlitMat = new Material(boundaryBlitShader);
        boundaryBlitMat.SetTexture("_extraToOverlayImage", boundaryTexture);

        Vector2 sourcePosition  = new Vector2(fluid_simulater.simulation_dimension / 2, fluid_simulater.simulation_dimension);
        Vector2 sourceDirection = new Vector2(0.0f, -1.0f); 

        Vector2 sourcePositoin2 = new Vector2(fluid_simulater.simulation_dimension /4, fluid_simulater.simulation_dimension);
        Vector2 sourcePosition3 = new Vector2(fluid_simulater.simulation_dimension, fluid_simulater.simulation_dimension*3.5f/4f);
        Vector2 sourceDirection3 = new Vector2(-1.0f, -0.1f);
        fluid_simulater.AddUserForce           (resources.velocity_buffer                                   );
        //fluid_simulater.AddConstantForceSource (resources.velocity_buffer, sourcePosition,
        //                                        sourceDirection, 10.0f, fluid_simulater.force_radius*2.0f, fluid_simulater.force_falloff*2.0f);

        fluid_simulater.AddConstantForceSource(resources.velocity_buffer, sourcePositoin2,
                                        sourceDirection, 10.0f, fluid_simulater.force_radius * 2.0f, fluid_simulater.force_falloff * 2.0f);

        fluid_simulater.AddConstantForceSource(resources.velocity_buffer, sourcePosition3,
                                sourceDirection3, 10.0f, fluid_simulater.force_radius * 1.2f, fluid_simulater.force_falloff * 2.0f);

        fluid_simulater.AddConstantUniformForce(resources.velocity_buffer, new Vector2(0.0f, -1.0f)*0.02f   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.Diffuse                (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer, resources.boundary_velocity_offset_buffer);
        fluid_simulater.Advect                 (resources.velocity_buffer, resources.velocity_buffer, fluid_simulater.velocity_dissapation);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleArbitaryBoundary (resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);

        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer, resources.boundary_velocity_offset_buffer);


        fluid_simulater.AddDye                 (resources.dye_buffer                                        );
        //fluid_simulater.AddConstantDyeSource   (resources.dye_buffer, sourcePosition);
        fluid_simulater.AddConstantDyeSource   (resources.dye_buffer, sourcePositoin2);
        fluid_simulater.AddConstantDyeSource   (resources.dye_buffer, sourcePosition3);
        fluid_simulater.Advect                 (resources.dye_buffer, resources.velocity_buffer, 0.997f);
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );
        fluid_simulater.HandleArbitaryBoundary (resources.dye_buffer, resources.boundary_dye_offset_buffer, FieldType.Dye);
        fluid_simulater.Diffuse                (resources.dye_buffer                                        );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );
        fluid_simulater.HandleArbitaryBoundary (resources.dye_buffer, resources.boundary_dye_offset_buffer, FieldType.Dye);

        fluid_simulater.Visualiuse             (resources.dye_buffer, boundaryBlitMat);

        fluid_simulater.BindCommandBuffer();
    }

    // ------------------------------------------------------------------
    // DESTRUCTOR
    void OnDisable()
    {
        fluid_simulater.Release();
        resources      .Release();
        Destroy(boundaryBlitMat);
    }

    // ------------------------------------------------------------------
    // LOOP
    void Update()
    {
        fluid_simulater.Tick(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.R))
        {
            Application.LoadLevel(Application.loadedLevel);
        }

    }
}

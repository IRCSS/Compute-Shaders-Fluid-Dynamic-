using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestProject : MonoBehaviour
{

 // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public  FluidSimulater    fluid_simulater;
    public  Texture2D         source_texture;
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


        fluid_simulater.AddDyeFromTexture(resources.dye_buffer, source_texture, true);

        fluid_simulater.AddUserForce          (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries(resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Advect                (resources.velocity_buffer, resources.velocity_buffer, 0.995f);
        fluid_simulater.HandleCornerBoundaries(resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Diffuse               (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries(resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Project               (resources.velocity_buffer,resources.divergence_buffer, resources.pressure_buffer);

        fluid_simulater.AddDye                 (resources.dye_buffer                                       );
        fluid_simulater.Advect                 (resources.dye_buffer, resources.velocity_buffer, 0.985f    );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                        );
        //fluid_simulater.Diffuse                (resources.dye_buffer                                       );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                        );

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

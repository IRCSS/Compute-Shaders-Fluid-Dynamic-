using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersianGardenDemoSceneMaster : MonoBehaviour
{

    // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public FluidSimulater fluid_simulater;

    [Header("Fountain Managment")]
    public Transform  Tran_downLeft;
    public Transform  Tran_upRight;

    public GameObject Fountain;

    public bool       debug_ink;

    //___________
    // private
    private FluidGPUResources resources;
    private Material          fountainMaterial;

    private RenderTexture     pressure_texture;

    // ------------------------------------------------------------------
    // INITALISATION
    void Start()
    {

 
        //--

        fluid_simulater.Initialize();
        resources = new FluidGPUResources(fluid_simulater);
        resources.Create();


        pressure_texture = new RenderTexture((int)fluid_simulater.canvas_dimension, (int)fluid_simulater.canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = false,
        };

        pressure_texture.Create();

        //--


        //--

        fluid_simulater.AddUserForce          (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries(resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Advect                (resources.velocity_buffer, resources.velocity_buffer, 0.995f);
        fluid_simulater.HandleCornerBoundaries(resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Diffuse               (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries(resources.velocity_buffer, FieldType.Velocity               );

        fluid_simulater.AddDye                 (resources.dye_buffer                                       );
        fluid_simulater.Advect                 (resources.dye_buffer, resources.velocity_buffer, 0.975f    );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                        );
        fluid_simulater.Diffuse                (resources.dye_buffer                                       );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                        );

        fluid_simulater.CopyBufferToTexture(pressure_texture, resources.dye_buffer);

        Shader.SetGlobalTexture("_fountain_pressure_buffer", pressure_texture);

        //fluid_simulater.Visualiuse             (resources.dye_buffer);

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


        Shader.SetGlobalVector("_fountain_downLeft", Tran_downLeft.position);
        Shader.SetGlobalVector("_fountain_upRight", Tran_upRight.position  );
        
    }
}

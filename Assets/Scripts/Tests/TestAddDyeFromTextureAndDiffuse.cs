using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAddDyeFromTextureAndDiffuse : MonoBehaviour
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
        fluid_simulater.AddDye           (resources.dye_buffer);
        fluid_simulater.Diffuse          (resources.dye_buffer);
        fluid_simulater.Visualiuse       (resources.dye_buffer);

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

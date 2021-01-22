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
    public Transform  mainDirectionLight;
    public GameObject Fountain;

    public bool       debug_ink;

    //___________
    // private
    private FluidGPUResources resources;
    private Material          fountainMaterial;

    private RenderTexture     pressure_texture;

    private Camera            main_cam;
    // ------------------------------------------------------------------
    // INITALISATION
    void Start()
    {

        main_cam = Camera.main;
        if (main_cam == null) Debug.LogError("Could not find main camera, make sure the camera is tagged as main");
 
        //--

        fluid_simulater.Initialize();
        resources = new FluidGPUResources(fluid_simulater);
        resources.Create();

        fluid_simulater.SubmitMousePosOverrideDelegate(GetMousePosInSimulationSpaceUnitValue);

        pressure_texture = new RenderTexture((int)fluid_simulater.canvas_dimension, (int)fluid_simulater.canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = true,
            filterMode        = FilterMode.Trilinear,
            anisoLevel        = 7,
            format            = RenderTextureFormat.RFloat,
            wrapMode          = TextureWrapMode.Clamp,
        };

        pressure_texture.Create();

        //--

        Shader.SetGlobalVector("_canvas_texel_size", new Vector4(1.0f/fluid_simulater.canvas_dimension, 1.0f / fluid_simulater.canvas_dimension, 0.0f, 0.0f));
        Shader.SetGlobalVector("_lightDirection"   , mainDirectionLight.forward);


        //--

        fluid_simulater.AddUserForce           (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Diffuse                (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer);
        fluid_simulater.Advect                 (resources.velocity_buffer, resources.velocity_buffer, 0.99f );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );

        fluid_simulater.AddDye                 (resources.dye_buffer                                        );
        fluid_simulater.Advect                 (resources.dye_buffer, resources.velocity_buffer, 0.99f      );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );
        fluid_simulater.Diffuse                (resources.dye_buffer                                        );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );


        fluid_simulater.CopyBufferToTexture(pressure_texture, resources.pressure_buffer);

        Shader.SetGlobalTexture("_fountain_pressure_buffer", pressure_texture);

        


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

    // ------------------------------------------------------------------
    // FUNCTIONS

    Vector2 GetMousePosInSimulationSpaceUnitValue()
    {
        RaycastHit results;
        Ray ray = main_cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out results, 100.0f))
        {
            if (results.collider.gameObject != Fountain) return new Vector2(-20.0f, -20.0f);


            Vector2 hitPositionInSimulationSpace = new Vector2(results.point.z, results.point.x);
                    hitPositionInSimulationSpace = hitPositionInSimulationSpace - new Vector2(Tran_downLeft.position.z, Tran_downLeft.position.x);

            Vector2 span = new Vector2(Tran_upRight.position.z, Tran_upRight.position.x) - new Vector2(Tran_downLeft.position.z, Tran_downLeft.position.x);

             hitPositionInSimulationSpace = new Vector2(hitPositionInSimulationSpace.x / Mathf.Abs(span.x), hitPositionInSimulationSpace.y / Mathf.Abs(span.y));
          
            return hitPositionInSimulationSpace;

        }

        return new Vector2(-20.0f, -20.0f);
    }
}

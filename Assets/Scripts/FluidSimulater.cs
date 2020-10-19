using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FluidSimulater
{
    // ------------------------------------------------------------------
    // VARIABLES
    //___________
    // public

    [Header("Compute Shader Refs")]
    [Space(2)]
    public ComputeShader StokeNavierShader;
    public ComputeShader SolverShader;
    public ComputeShader BorderShader;
    public ComputeShader StructuredBufferToShader;
    public ComputeShader UserInputShader;

    [Space(4)]
    [Header("Simulation Settings")]
    [Space(2)]
    public int           canvas_dimension     = 512;          // Resolution of the render target used at the end, this can be lower or higher than the actual simulation grid resoltion
    public int           simulation_dimension = 256;          // Resolution of the simulation grid
    public float         force_radius         = 1;
    public float         force_falloff        = 2;
    public float         dye_radius           = 1.0f;
    public float         dye_falloff          = 2.0f;


    //___________
    // private

    private Camera        main_cam;

    private RenderTexture visulasation_texture;


    // ------------------------------------------------------------------
    // CONSTRUCTOR

    public FluidSimulater()                       // Default Constructor
    {
        canvas_dimension     = 512;
        simulation_dimension = 256;
    }

    public FluidSimulater(FluidSimulater other)   // Copy Constructor
    {
        canvas_dimension     = other.canvas_dimension;
        simulation_dimension = other.simulation_dimension;
    }

    // ------------------------------------------------------------------
    // DESTRUCTOR


    // ------------------------------------------------------------------
    // INITALISATION

    public void Initialize()
    {
        // -----------------------
        main_cam = Camera.main;
        if (main_cam == null) Debug.LogError("Could not find main camera, make sure the camera is tagged as main");
        
        main_cam.orthographic     = true;                                                                     // Make sure the camera is ortho. Perspecitve camera has a transformation matrix which will screw with everything
        main_cam.aspect           = 1.0f;
        main_cam.orthographicSize = 1;

        // -----------------------
        visulasation_texture = new RenderTexture(canvas_dimension, canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = false,
        };

        visulasation_texture.Create();
        // -----------------------

    }
    // ------------------------------------------------------------------
    // LOOP

    public void Tick(float deltaTime)
    {

    }

    // ------------------------------------------------------------------
    // SIMULATION STEPS

    public void AddUserForce()
    {

    }

    public void AddDye()
    {

    }

    public void Diffuse()
    {

    }

    public void Advect()
    {

    }

    public void Project()
    {

    }

    public void Visualiuse()
    {

    }

    // ------------------------------------------------------------------
    // HELPER FUNCTIONS

    private void Diverge()
    {

    }

    private void Gradient()
    {

    }

    private void Solve()
    {

    }
}



public class FluidSimulaterBuffers
{

    // ------------------------------------------------------------------
    // VARIABLES
    //___________
    // public
    public ComputeBuffer velocity_buffer;
    public ComputeBuffer dye_buffer;


    // ------------------------------------------------------------------
    // INITALISATION

    public void Create(int simulation_dimension)
    {
        velocity_buffer = new ComputeBuffer(simulation_dimension * simulation_dimension, sizeof(float) * 4);
        dye_buffer      = new ComputeBuffer(simulation_dimension * simulation_dimension, sizeof(float) * 4);
        
    }

    // ------------------------------------------------------------------
    // DESTRUCTOR

    public void Release()
    {
        velocity_buffer.Release();
        dye_buffer     .Release();
    }
}

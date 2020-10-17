using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TestAddForce : MonoBehaviour
{
    // ------------------------------------------------------------------
    // VARIABLES

    //___________
    // public
    public  ComputeShader userInput_shader;
    public  ComputeShader structuredBufferToTextureShader;

    //___________
    // private
    private int           addForceHandle;
    private int           st2txHandle;
    private RenderTexture visulasation_texture;
    private ComputeBuffer velocity_buffer;
    private CommandBuffer cb;
    private Camera        main_cam;
    private int           canvas_dimension = 512;                         // Resolution of the render target used at the end, this can be lower or higher than the actual simulation grid resoltion
    private int           i_Resolution     = 256;                         // Resolution of the simulation grid

    void Start()
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

        velocity_buffer = new ComputeBuffer(i_Resolution * i_Resolution, sizeof(float) * 4);
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

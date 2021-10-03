Fluid Simulation Implemented in Compute shaders in Unity 3D
=================
This repo contains code for a fluid simulation pipeline implmented in Compute shaders. The code has been tested on Unity 2019.1.14f1 Windows DX11, with the Legacy renderer. 

You can read the full explaination of the technique on my blog: [Gentle Introduction to Fluid Simulation for Programmers and Technical Artists](https://medium.com/@shahriyarshahrabi/gentle-introduction-to-fluid-simulation-for-programmers-and-technical-artists-7c0045c40bac)



![screenshot](documentation/FluidSimulationGif.gif)

--------------------------

To get started with the repo, select any of the 3 demo scenes, under Assets/Scenes. The three scenes are:
1. **2DFluid**: this is a simple 2D fluid setup, with the left click you can add dye to the screen, and while holding the right click and moving your mouse you can apply force the system.  
2. **2DFluid Arbitary Boundary**: Just like the one above, but there is a constant stream of dye input+force, and boundaries in the middle of the simulation grid
3. **Persian Garden Demo**: This is the level you can see in the gif, it holds a *fake* 3D fluid simulation, where a 2D simulation is mapped on a 3D plane. You can't add dye in this scene, with left click you can apply force and manipulate the pool and while holding the left click you can fly around the scene with the WSAD or the arrows and zoom, up down with E and Q and finally change fov with the middle scroll. 

Each of these scenes has a Manager Game object and a Manager script on it. Viewing this script in your code editor, you can see examples of how to properly initialize the fluid simulator engine and call its various functions. 

I suggest looking at the 2D Fluid scene first, since it has the simplest implementation. 
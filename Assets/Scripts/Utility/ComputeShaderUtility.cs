using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public struct DispatchDimensions
{
    public uint dispatch_x;
    public uint dispatch_y;
    public uint dispatch_z;
}

public static class ComputeShaderUtility { 
       public static DispatchDimensions CheckGetDispatchDimensions(ComputeShader shader, int handle, uint desired_threadNum_x, uint desired_threadNum_y, uint desired_threadNum_z)
       {
       
           uint group_size_x, group_size_y, group_size_z;
       
           shader.GetKernelThreadGroupSizes(handle, out group_size_x, out group_size_y, out group_size_z);
       
           if(desired_threadNum_x % group_size_x != 0 ||
              desired_threadNum_y % group_size_y != 0 ||
              desired_threadNum_z % group_size_z != 0)
           {
               Debug.LogError(string.Format("ERROR: Shader {0}, on kernel {1}, has mismatched desired thread numbers and thread groupd sizes. The desired thread "   +
                                            "number needs to be a multiply of the group size. Either change the desired thread number or change the " +
                                            "group size in the shader.", shader, handle));
               Debug.Break();
           }
       
       
           DispatchDimensions dp;
       
           dp.dispatch_x = desired_threadNum_x / group_size_x;
           dp.dispatch_y = desired_threadNum_y / group_size_y;
           dp.dispatch_z = desired_threadNum_z / group_size_z;
        uint totalThreadNumber = desired_threadNum_x * desired_threadNum_y * desired_threadNum_z;
           if (totalThreadNumber < 64) Debug.LogWarning(string.Format("Total threads number is: {0}, this is probably too low and causes under utilization of GPU block!", totalThreadNumber));
       
           return dp;
       }
}





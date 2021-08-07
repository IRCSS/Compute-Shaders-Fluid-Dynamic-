using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct int4
{
    public int x, y, z, w;
    public int4(int _x, int _y, int _z, int _w)
    {
        x = _x;
        y = _y;
        z = _z;
        w = _w;
    }
}



// STENCIL MAP
// The binary map is following. East is the first bit, North the second, West the third, South the fourth and Center the 5. So 
//        |00010|
//   _____|_____|_____
//   00100|10000|00001
//   _____|_____|_____
//        |01000|
//        |     |


// Some edge cases are very undesirable and not handled in this system. Any thin boundery that is only pixel wide can break the simulation, but not in a very visible way
public static class ArbitaryBoundaryLUTGenerator
{
    public static List<int4> GetVelocityLUT()
    {

        // X = offset for the X component of velocity
        // Y = offset for the Y component of velocity
        // Z = Multiplier for the X Component of velocity
        // W = Multiplier for the Y Component of velocity

         List<int4> CardinalDirectionLUT = new List<int4>
         {
             // All Fields starting with zero, field is in fluid
             new int4( 0,  0,  1,  1),// in Binary 00000 . In HEX 0x0000   .   No neighbour is boundary
             new int4( 0,  0,  1,  1),// in Binary 00001 . In HEX 0x0001   .   1 Neibour is boundary: East  
             new int4( 0,  0,  1,  1),// in Binary 00010 . In HEX 0x0002   .   1 Neibour is boundary: North 
             new int4( 0,  0,  1,  1),// in Binary 00011 . In HEX 0x0003   .   2 Neighbours boundary: East and North
             new int4( 0,  0,  1,  1),// in Binary 00100 . In HEX 0x0004   .   1 Neibour is boundary: West i
             new int4( 0,  0,  1,  1),// in Binary 00101 . In HEX 0x0005   .   2 Neighbours boundary: East and West
             new int4( 0,  0,  1,  1),// in Binary 00110 . In HEX 0x0006   .   2 Neighbours boundary: North and West
             new int4( 0,  0,  1,  1),// in Binary 00111 . In HEX 0x0007   .   3 Neighbours boundary: East, North and West
             new int4( 0,  0,  1,  1),// in Binary 01000 . In HEX 0x0008   .   1 Neibour is boundary: South
             new int4( 0,  0,  1,  1),// in Binary 01001 . In HEX 0x0009   .   2 Neighbours boundary: East and South
             new int4( 0,  0,  1,  1),// in Binary 01010 . In HEX 0x000A   .   2 Neighbours boundary: Noth and South
             new int4( 0,  0,  1,  1),// in Binary 01011 . In HEX 0x000B   .   3 Neighbours boundary: East, North and South
             new int4( 0,  0,  1,  1),// in Binary 01100 . In HEX 0x000C   .   2 Neighbours boundary: West and South
             new int4( 0,  0,  1,  1),// in Binary 01101 . In HEX 0x000D   .   3 Neighbours boundary: East, West and South
             new int4( 0,  0,  1,  1),// in Binary 01110 . In HEX 0x000E   .   3 Neighbours boundary: North, West and South
             new int4( 0,  0,  0,  0),// in Binary 01111 . In HEX 0x000F   .   4 Neighbours boundary: East, North, West and South  > We can zero everything here, because this is baisicly a closed space within our closed system
             // All starting with one, the centeral Field is in obstcle 
             new int4( 0,  0, -1, -1),// in Binary 10000 . In HEX 0x0010   .   No neighbour is boundary > Ignore this kind of, it is a single floating pixel, should never happen
             new int4(-1,  0, -1, -1),// in Binary 10001 . In HEX 0x0011   .   1 Neibour is boundary: East  
             new int4( 0, -1, -1, -1),// in Binary 10010 . In HEX 0x0012   .   1 Neibour is boundary: North 
             new int4(-1, -1, -1, -1),// in Binary 10011 . In HEX 0x0013   .   2 Neighbours boundary: East and North
             new int4( 1,  0, -1, -1),// in Binary 10100 . In HEX 0x0014   .   1 Neibour is boundary: West 
             new int4( 0, -1, -1, -1),// in Binary 10101 . In HEX 0x0015   .   2 Neighbours boundary: East and West
             new int4( 1, -1, -1, -1),// in Binary 10110 . In HEX 0x0016   .   2 Neighbours boundary: North and West
             new int4( 0, -1, -1, -1),// in Binary 10111 . In HEX 0x0017   .   3 Neighbours boundary: East, North and West
             new int4( 0,  1, -1, -1),// in Binary 11000 . In HEX 0x0018   .   1 Neibour is boundary: South
             new int4(-1,  1, -1, -1),// in Binary 11001 . In HEX 0x0019   .   2 Neighbours boundary: East and South
             new int4(-1,  0, -1, -1),// in Binary 11010 . In HEX 0x001A   .   2 Neighbours boundary: North and South
             new int4(-1,  0, -1, -1),// in Binary 11011 . In HEX 0x001B   .   3 Neighbours boundary: East, North and South
             new int4( 1,  1, -1, -1),// in Binary 11100 . In HEX 0x001C   .   2 Neighbours boundary: West and South
             new int4( 0,  1, -1, -1),// in Binary 11101 . In HEX 0x001D   .   3 Neighbours boundary: East, West and South
             new int4( 1,  0, -1, -1),// in Binary 11110 . In HEX 0x001E   .   3 Neighbours boundary: North, West and South
             new int4( 0,  0,  0,  0) // in Binary 11111 . In HEX 0x001F   .   4 Neighbours boundary: East, North, West and South
         };

        return CardinalDirectionLUT;
    }


    public static List<int4> GetPressureLUT()
    {


        // XY First texture read offset
        // ZW Second texture read offset
        // The pressure boundary is calculated by averaging across two reads of the neighbouring cells
        // For example if it is the classic condition that there is a boundary to the right, you want to 
        // Just set the boundary cell to have the pressure value of the cell on the left. So it would be id = (-1, 0, -1, 0)
        // (cell(id.xy) + cell(id.zw))/2. = cell(id.xy). 
        // however if you are dealing with a corner cell, for example boundary is to south east (down right). You want to average the cell to the 
        // north and west. (up and left).  id = (-1, 0, 0, 1). 

        List<int4> CardinalDirectionLUT = new List<int4>
         {
             // All Fields starting with zero, field is in fluid
             new int4( 0,  0,  1,  1),// in Binary 00000 . In HEX 0x0000   .   No neighbour is boundary
             new int4( 0,  0,  1,  1),// in Binary 00001 . In HEX 0x0001   .   1 Neibour is boundary: East  
             new int4( 0,  0,  1,  1),// in Binary 00010 . In HEX 0x0002   .   1 Neibour is boundary: North 
             new int4( 0,  0,  1,  1),// in Binary 00011 . In HEX 0x0003   .   2 Neighbours boundary: East and North
             new int4( 0,  0,  1,  1),// in Binary 00100 . In HEX 0x0004   .   1 Neibour is boundary: West i
             new int4( 0,  0,  1,  1),// in Binary 00101 . In HEX 0x0005   .   2 Neighbours boundary: East and West
             new int4( 0,  0,  1,  1),// in Binary 00110 . In HEX 0x0006   .   2 Neighbours boundary: North and West
             new int4( 0,  0,  1,  1),// in Binary 00111 . In HEX 0x0007   .   3 Neighbours boundary: East, North and West
             new int4( 0,  0,  1,  1),// in Binary 01000 . In HEX 0x0008   .   1 Neibour is boundary: South
             new int4( 0,  0,  1,  1),// in Binary 01001 . In HEX 0x0009   .   2 Neighbours boundary: East and South
             new int4( 0,  0,  1,  1),// in Binary 01010 . In HEX 0x000A   .   2 Neighbours boundary: Noth and South
             new int4( 0,  0,  1,  1),// in Binary 01011 . In HEX 0x000B   .   3 Neighbours boundary: East, North and South
             new int4( 0,  0,  1,  1),// in Binary 01100 . In HEX 0x000C   .   2 Neighbours boundary: West and South
             new int4( 0,  0,  1,  1),// in Binary 01101 . In HEX 0x000D   .   3 Neighbours boundary: East, West and South
             new int4( 0,  0,  1,  1),// in Binary 01110 . In HEX 0x000E   .   3 Neighbours boundary: North, West and South
             new int4( 0,  0,  0,  0),// in Binary 01111 . In HEX 0x000F   .   4 Neighbours boundary: East, North, West and South  > We can zero everything here, because this is baisicly a closed space within our closed system
             // All starting with one, the centeral Field is in obstcle 
             new int4( 0,  0,  1,  1),// in Binary 10000 . In HEX 0x0010   .   No neighbour is boundary > Ignore this kind of, it is a single floating pixel, should never happen
             new int4(-1,  0, -1,  0),// in Binary 10001 . In HEX 0x0011   .   1 Neibour is boundary: East  
             new int4( 0, -1,  0, -1),// in Binary 10010 . In HEX 0x0012   .   1 Neibour is boundary: North 
             new int4(-1,  0,  0, -1),// in Binary 10011 . In HEX 0x0013   .   2 Neighbours boundary: East and North
             new int4( 1,  0,  1,  0),// in Binary 10100 . In HEX 0x0014   .   1 Neibour is boundary: West 
             new int4( 0,  0,  0,  0),// in Binary 10101 . In HEX 0x0015   .   2 Neighbours boundary: East and West
             new int4( 1,  0,  0, -1),// in Binary 10110 . In HEX 0x0016   .   2 Neighbours boundary: North and West
             new int4( 0, -1,  0, -1),// in Binary 10111 . In HEX 0x0017   .   3 Neighbours boundary: East, North and West
             new int4( 0,  1,  0,  1),// in Binary 11000 . In HEX 0x0018   .   1 Neibour is boundary: South
             new int4(-1,  0,  0,  1),// in Binary 11001 . In HEX 0x0019   .   2 Neighbours boundary: East and South
             new int4( 0,  0,  0,  0),// in Binary 11010 . In HEX 0x001A   .   2 Neighbours boundary: North and South
             new int4(-1,  0, -1,  0),// in Binary 11011 . In HEX 0x001B   .   3 Neighbours boundary: East, North and South
             new int4( 1,  0,  0,  1),// in Binary 11100 . In HEX 0x001C   .   2 Neighbours boundary: West and South
             new int4( 0,  1,  0,  1),// in Binary 11101 . In HEX 0x001D   .   3 Neighbours boundary: East, West and South
             new int4( 1,  0,  1,  0),// in Binary 11110 . In HEX 0x001E   .   3 Neighbours boundary: North, West and South
             new int4( 0,  0,  0,  0) // in Binary 11111 . In HEX 0x001F   .   4 Neighbours boundary: East, North, West and South
         };

        return CardinalDirectionLUT;
    }

    public static List<int4> GetDyeLUT()
    {


         List<int4> CardinalDirectionLUT = new List<int4>
         {
             // All Fields starting with zero, field is in fluid
             new int4( 0,  0,  1,  1),// in Binary 00000 . In HEX 0x0000   .   No neighbour is boundary
             new int4( 0,  0,  1,  1),// in Binary 00001 . In HEX 0x0001   .   1 Neibour is boundary: East  
             new int4( 0,  0,  1,  1),// in Binary 00010 . In HEX 0x0002   .   1 Neibour is boundary: North 
             new int4( 0,  0,  1,  1),// in Binary 00011 . In HEX 0x0003   .   2 Neighbours boundary: East and North
             new int4( 0,  0,  1,  1),// in Binary 00100 . In HEX 0x0004   .   1 Neibour is boundary: West i
             new int4( 0,  0,  1,  1),// in Binary 00101 . In HEX 0x0005   .   2 Neighbours boundary: East and West
             new int4( 0,  0,  1,  1),// in Binary 00110 . In HEX 0x0006   .   2 Neighbours boundary: North and West
             new int4( 0,  0,  1,  1),// in Binary 00111 . In HEX 0x0007   .   3 Neighbours boundary: East, North and West
             new int4( 0,  0,  1,  1),// in Binary 01000 . In HEX 0x0008   .   1 Neibour is boundary: South
             new int4( 0,  0,  1,  1),// in Binary 01001 . In HEX 0x0009   .   2 Neighbours boundary: East and South
             new int4( 0,  0,  1,  1),// in Binary 01010 . In HEX 0x000A   .   2 Neighbours boundary: Noth and South
             new int4( 0,  0,  1,  1),// in Binary 01011 . In HEX 0x000B   .   3 Neighbours boundary: East, North and South
             new int4( 0,  0,  1,  1),// in Binary 01100 . In HEX 0x000C   .   2 Neighbours boundary: West and South
             new int4( 0,  0,  1,  1),// in Binary 01101 . In HEX 0x000D   .   3 Neighbours boundary: East, West and South
             new int4( 0,  0,  1,  1),// in Binary 01110 . In HEX 0x000E   .   3 Neighbours boundary: North, West and South
             new int4( 0,  0,  0,  0),// in Binary 01111 . In HEX 0x000F   .   4 Neighbours boundary: East, North, West and South  > We can zero everything here, because this is baisicly a closed space within our closed system
             // All starting with one, the centeral Field is in obstcle 
             new int4( 0,  0,  0,  0),// in Binary 10000 . In HEX 0x0010   .   No neighbour is boundary > Ignore this kind of, it is a single floating pixel, should never happen
             new int4( 0,  0,  0,  0),// in Binary 10001 . In HEX 0x0011   .   1 Neibour is boundary: East  
             new int4( 0,  0,  0,  0),// in Binary 10010 . In HEX 0x0012   .   1 Neibour is boundary: North 
             new int4( 0,  0,  0,  0),// in Binary 10011 . In HEX 0x0013   .   2 Neighbours boundary: East and North
             new int4( 0,  0,  0,  0),// in Binary 10100 . In HEX 0x0014   .   1 Neibour is boundary: West 
             new int4( 0,  0,  0,  0),// in Binary 10101 . In HEX 0x0015   .   2 Neighbours boundary: East and West
             new int4( 0,  0,  0,  0),// in Binary 10110 . In HEX 0x0016   .   2 Neighbours boundary: North and West
             new int4( 0,  0,  0,  0),// in Binary 10111 . In HEX 0x0017   .   3 Neighbours boundary: East, North and West
             new int4( 0,  0,  0,  0),// in Binary 11000 . In HEX 0x0018   .   1 Neibour is boundary: South
             new int4( 0,  0,  0,  0),// in Binary 11001 . In HEX 0x0019   .   2 Neighbours boundary: East and South
             new int4( 0,  0,  0,  0),// in Binary 11010 . In HEX 0x001A   .   2 Neighbours boundary: North and South
             new int4( 0,  0,  0,  0),// in Binary 11011 . In HEX 0x001B   .   3 Neighbours boundary: East, North and South
             new int4( 0,  0,  0,  0),// in Binary 11100 . In HEX 0x001C   .   2 Neighbours boundary: West and South
             new int4( 0,  0,  0,  0),// in Binary 11101 . In HEX 0x001D   .   3 Neighbours boundary: East, West and South
             new int4( 0,  0,  0,  0),// in Binary 11110 . In HEX 0x001E   .   3 Neighbours boundary: North, West and South
             new int4( 0,  0,  0,  0) // in Binary 11111 . In HEX 0x001F   .   4 Neighbours boundary: East, North, West and South
         };

        return CardinalDirectionLUT;
    }


}

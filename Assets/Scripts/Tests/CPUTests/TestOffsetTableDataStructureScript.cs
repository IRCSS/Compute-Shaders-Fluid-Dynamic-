using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestOffsetTableDataStructureScript : MonoBehaviour
{


    float[] offsetTable;


    // Start is called before the first frame update
    void Start()
    {
        offsetTable = new float[32];

        // All Fields starting with zero, field is in fluid
        offsetTable[0 ] = 0 ;// in Binary 00000 . In HEX 0x0000
        offsetTable[1 ] = 1 ;// in Binary 00001 . In HEX 0x0001
        offsetTable[2 ] = 2 ;// in Binary 00010 . In HEX 0x0002
        offsetTable[3 ] = 3 ;// in Binary 00011 . In HEX 0x0003
        offsetTable[4 ] = 4 ;// in Binary 00100 . In HEX 0x0004
        offsetTable[5 ] = 5 ;// in Binary 00101 . In HEX 0x0005
        offsetTable[6 ] = 6 ;// in Binary 00110 . In HEX 0x0006
        offsetTable[7 ] = 7 ;// in Binary 00111 . In HEX 0x0007
        offsetTable[8 ] = 8 ;// in Binary 01000 . In HEX 0x0008
        offsetTable[9 ] = 9 ;// in Binary 01001 . In HEX 0x0009
        offsetTable[10] = 10;// in Binary 01010 . In HEX 0x000A
        offsetTable[11] = 11;// in Binary 01011 . In HEX 0x000B
        offsetTable[12] = 12;// in Binary 01100 . In HEX 0x000C
        offsetTable[13] = 13;// in Binary 01101 . In HEX 0x000D
        offsetTable[14] = 14;// in Binary 01110 . In HEX 0x000E
        offsetTable[15] = 15;// in Binary 01111 . In HEX 0x000F
        // All starting with one, the centeral Field is in obstcle 
        offsetTable[16] = 16;// in Binary 10000 . In HEX 0x0010
        offsetTable[17] = 17;// in Binary 10001 . In HEX 0x0011
        offsetTable[18] = 18;// in Binary 10010 . In HEX 0x0012
        offsetTable[19] = 19;// in Binary 10011 . In HEX 0x0013
        offsetTable[20] = 20;// in Binary 10100 . In HEX 0x0014
        offsetTable[21] = 21;// in Binary 10101 . In HEX 0x0015
        offsetTable[22] = 22;// in Binary 10110 . In HEX 0x0016
        offsetTable[23] = 23;// in Binary 10111 . In HEX 0x0017
        offsetTable[24] = 24;// in Binary 11000 . In HEX 0x0018
        offsetTable[25] = 25;// in Binary 11001 . In HEX 0x0019
        offsetTable[26] = 26;// in Binary 11010 . In HEX 0x001A
        offsetTable[27] = 27;// in Binary 11011 . In HEX 0x001B
        offsetTable[28] = 28;// in Binary 11100 . In HEX 0x001C
        offsetTable[29] = 29;// in Binary 11101 . In HEX 0x001D
        offsetTable[30] = 30;// in Binary 11110 . In HEX 0x001E
        offsetTable[31] = 31;// in Binary 11111 . In HEX 0x001F


        //  index is in binary    00001    or    00010    or    00100    or    01000    or   10000
        int lookupIndedx =    (1 * 0x0001) | (0 * 0x0002) | (0 * 0x0004) | (0 * 0x0008) | (1 * 0x0010);

        print(offsetTable[lookupIndedx]);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

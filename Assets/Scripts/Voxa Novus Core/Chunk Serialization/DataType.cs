using System;

namespace VoxaNovus.DataTypes
{
    [Flags]
    enum DataType : byte
    {
    	Byte 	= 1 << 0,
    	Short 	= 1 << 1,
    	Int 	= 1 << 2,
    	Long 	= 1 << 3,
    	Float 	= 1 << 4,
    	Double 	= 1 << 5,
    	Char 	= 1 << 6,
    	Signed 	= 1 << 7,
    }
}
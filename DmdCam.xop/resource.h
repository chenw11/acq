//{{NO_DEPENDENCIES}}
// Microsoft Visual C++ generated include file.
// Used by XOPExports.rc

// Next default values for new objects
// 
#ifdef APSTUDIO_INVOKED
#ifndef APSTUDIO_READONLY_SYMBOLS
#define _APS_NEXT_RESOURCE_VALUE        101
#define _APS_NEXT_COMMAND_VALUE         40001
#define _APS_NEXT_CONTROL_VALUE         1001
#define _APS_NEXT_SYMED_VALUE           101
#endif
#endif


// Macros to simplify the definition of XOP functions
#define MyFuncCategory (F_UTIL | F_EXTERNAL)

#define TStruct (FV_STRUCT_TYPE | FV_REF_TYPE)
#define TDbl NT_FP64
#define TWave WAVE_TYPE
#define TString HSTRING_TYPE


 // returns double with no params
#define P0( name ) 	name, MyFuncCategory, NT_FP64, 0

 // returns double given 1 argument
#define P1( name, arg1Type ) 	name, MyFuncCategory, NT_FP64, arg1Type, 0

// returns double given 2 arguments
#define P2( name, arg1Type, arg2Type ) name, MyFuncCategory, NT_FP64, arg1Type, arg2Type, 0

// returns double given 3 arguments
#define P3( name, arg1Type, arg2Type, arg3Type ) name, MyFuncCategory, NT_FP64, arg1Type, arg2Type, arg3Type, 0

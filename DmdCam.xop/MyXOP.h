
/* Prototypes */
HOST_IMPORT int XOPMain(IORecHandle ioRecHandle);


/* custom error codes */

#define CUSTOM_ERROR				1 + FIRST_XOP_ERR
#define REQUIRES_IGOR_620			2 + FIRST_XOP_ERR
#define MESSAGE_UNSUPPORTED         3 + FIRST_XOP_ERR
#define REQUIRES_DP_WAVE			4 + FIRST_XOP_ERR
#define ERROR_NULL_POINTER			5 + FIRST_XOP_ERR
#define NON_EXISTENT_WAVE			6 + FIRST_XOP_ERR
#define MULTIDIM_FAIL				7 + FIRST_XOP_ERR
#define INCORRECT_NUM_DIMS			8 + FIRST_XOP_ERR
#define INVALID_RECT				9 + FIRST_XOP_ERR
#define REQUIRES_U16_WAVE			10 + FIRST_XOP_ERR
#define FRAMEGRABBER_ERROR			11 + FIRST_XOP_ERR
#define BUFFER_SIZE_MISMATCH		12 + FIRST_XOP_ERR
#define MEMCPY_S_ERR				13 + FIRST_XOP_ERR
#define ANALOG_RANGE_ERROR			14 + FIRST_XOP_ERR

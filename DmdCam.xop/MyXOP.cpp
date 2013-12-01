#include "DllResolver.h"

#include "XOPStandardHeaders.h"			// Include ANSI headers, Mac headers, IgorXOP.h, XOP.h and XOPSupport.h
#include "MyXOP.h"


using namespace eas_lab::acq::DmdCam;


#define XF eas_lab::acq::DmdCam::DmdCamXopFrame
#define X (XF::Default)

#pragma pack(2)
// parameters are listed in reverse order, with result at the end
// this is the reverse of the resource file format
typedef struct { double ret; } P0;

template<typename T> struct P1 { T arg1; double ret; } ;

template<typename T1, typename T2> struct P2 {  // parameter order is backwards!
	T2 arg2;  // 2nd param
	T1 arg1;  // 1st param
	double ret; //return value
} ;

#pragma pack()

// error handling.  h is the error handler.  either catch and return code, or let exceptions bubble up
#ifdef NOTREALLY_DEBUG
	#define DoWith(h,x) { { x }; h 0; }
#else
	#define DoWith(h,x) { try {  { x }; h 0; } catch(System::Exception^ e) { h (int)XF::Error(e->Message); } }
#endif
#define Do(x) DoWith(return,x)


#define NotNull( p ) { if ( (p) == NULL ) { return(ERROR_NULL_POINTER); } p->ret = 0; }
#define ExpectStruct( a ) { if ( (a) == NULL ) { return(EXPECT_STRUCT); } }


#define P0(name) static int name (P0* p) { NotNull(p); Do( X::Xop-> name (); ) }
#define P0R(name) static int name (P0* p) { NotNull(p); Do( p->ret = X::Xop-> name (); ) }

#define P1(name,structType) static int name (P1<structType*>* p) { NotNull(p); ExpectStruct( p->arg1);  Do( X::Xop-> name ( *(p->arg1) ); ); }

#define PID_none(name) static int name (P1<double>* p) { NotNull(p); Do ( X -> name ((int)(p->arg1)); ); }
#define PID_primitive(name, primType, primCast) static int name (P2<double, primType>* p) { NotNull(p); Do( X-> name ((int)(p-> arg1), (primCast)(p->arg2)); ); }
#define PID_struct(name, structType) static int name (P2<double, structType*>* p) { NotNull(p); ExpectStruct( p-> arg2); Do( X-> name ((int)(p-> arg1), *(p->arg2)); ); }

// EXPORT 0
static int DmdCam_Reset(P0* p)
{
	NotNull( p );
	XF::Reset();
	return 0;
}

// EXPORT 1: screenId, outSize
static int DmdCam_GetSize(P2<double, eas_lab::RectSize*>* p)
{
	NotNull(p);
	ExpectStruct(p->arg2);

	eas_lab::RectSize outSize;
	int err;
	DoWith(err=, X->DmdCam_GetSize(p->arg1, outSize); );
	*(p->arg2) = outSize;

	return err;
}


// EXPORT 2: screenId, expectedSize
PID_none( DmdCam_Create );  

// EXPORT 3: screenId, visibility
PID_primitive( DmdCam_Preview, double, bool );

// EXPORT 4: screenId, image white levels
static int DmdCam_SetImage(P2<double, waveHndl>* p) 
{
	NotNull(p);
	Handle wavH = p->arg2;

	if (wavH == NULL)
		return(NON_EXISTENT_WAVE);

	if (WaveType(wavH) != NT_FP64)
		return(REQUIRES_DP_WAVE);

	int numDims = 0;
	CountInt dimSizes[MAX_DIMENSIONS+1];

	int err;
	if (err=MDGetWaveDimensions(wavH, &numDims, dimSizes))
		return err;

	if (numDims != 2)
		return(INCORRECT_NUM_DIMS);

	int n = WavePoints(wavH);
	if (n < 1)
		return(MULTIDIM_FAIL);

	array<double,2>^ whitelevels = gcnew array<double,2>(dimSizes[ROWS], dimSizes[COLUMNS]);
	pin_ptr<double> pgcar = &whitelevels[0,0];

	err = MDGetDPDataFromNumericWave(wavH, pgcar);
	if (err != 0)
		return err;

	int screenId = (int)(p->arg1);
	DoWith(err=, X->DmdCam_SetImage(screenId, whitelevels);  );
	return err;
}

// returns function pointers for each exported function
static XOPIORecResult RegisterFunction()
{
	int funcIndex;

	funcIndex = GetXOPItem(0);	/* which function are we getting the address of? */
	switch (funcIndex) {
		case 0:	
			return((XOPIORecResult)DmdCam_Reset);  // This should now be 64-bit safe
		case 1:
			return((XOPIORecResult)DmdCam_GetSize);
		case 2:
			return((XOPIORecResult)DmdCam_Create);
		case 3:
			return((XOPIORecResult)DmdCam_Preview);
		case 4:
			return((XOPIORecResult)DmdCam_SetImage);
		// add more cases for more exported functions here
		// be sure to also add them to the XOPExports.rc resource file
	}
	return(NIL);
}


/*
	Global setup and cleanup code for this XOP.
*/
static void ReportError(System::String^ msg)
{
	array<unsigned char, 1>^ buffer = gcnew array<unsigned char>(msg->Length);
	System::Text::ASCIIEncoding::ASCII->GetBytes(msg, 0, msg->Length, buffer, 0);
	buffer[msg->Length-1] = '\0';
	unsigned char* p = (unsigned char*)malloc(msg->Length);
	System::Runtime::InteropServices::Marshal::Copy(buffer, 0, (System::IntPtr)p, msg->Length);
	XOPNotice((char*)p);
	free(p);
}


static void EnsureErrorHandlerIsSetup()
{
	if (XF::ErrorHandler == nullptr)
		XF::ErrorHandler = gcnew System::Action<System::String^>(ReportError);
}


/*
	Global setup and cleanup code for this XOP.
*/

// called when the XOP is first loaded by Igor
static void GlobalSetup()
{
	// ensure that calls to referenced .NET dlls can be resolved
	// See DllResolver.cpp for more detail.  We call this with true
	// to say that the external C# library will be available in the same
	// directory as this XOP.
	SetupDllResolver(true);
}

// called when the XOP is about to be unloaded by Igor
static XOPIORecResult GlobalCleanup() 
{
	// do cleanup of any remaining managed code
	eas_lab::acq::DmdCam::DmdCamXopFrame::GlobalCleanup();
	return 0; 
} 




// Igor passes messages into here
// It is called first to get the function pointers for all functions in the resource file
// and then is called periodically for idle, window messages, etc.  See XOP Toolkit Docs.
static void XOPEntry(void)
{	
	XOPIORecResult result = 0;
	XOPIORecResult msg = GetXOPMessage();

	if (msg == FUNCADDRS)
		result = RegisterFunction();
	else if (msg == CLEANUP)
		result = GlobalCleanup();
	else if (msg == FUNCTION) // we don't support message-type calling
		result = MESSAGE_UNSUPPORTED;
	
	SetXOPResult(result);

	EnsureErrorHandlerIsSetup();
}


// initial entry point, sets up XOP support stuff, does version check
HOST_IMPORT int XOPMain(IORecHandle ioRecHandle)
{	
	XOPInit(ioRecHandle);	// do standard XOP initialization
	SetXOPEntry(XOPEntry);	// set entry point for future calls

	GlobalSetup();

	if (igorVersion < 620)
	{
		SetXOPResult(REQUIRES_IGOR_620);
		return EXIT_FAILURE;
	}

	SetXOPResult(0L);
	return EXIT_SUCCESS;
}

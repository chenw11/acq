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

template<typename T1, typename T2, typename T3> struct P3 {
	T3 arg3;  // 3rd param
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
static int DmdCam_Create(P1<double>* p) 
{
	NotNull(p);
	Do ( X->DmdCam_Create( (int)(p->arg1) ); );
}

// EXPORT 3: screenId, image white levels
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

	array<double,2>^ whitelevels = gcnew array<double,2>(dimSizes[COLUMNS], dimSizes[ROWS]);
	pin_ptr<double> pgcar = &whitelevels[0,0];

	err = MDGetDPDataFromNumericWave(wavH, pgcar);
	if (err != 0)
		return err;

	int screenId = (int)(p->arg1);
	DoWith(err=, X->DmdCam_SetImage(screenId, whitelevels);  );
	return err;
}


// EXPORT 4: screenId, deviceName, wavelength
static int DmdCam_ConfigPowerMeter(P3<double, Handle, double>* p)
{
	NotNull(p);
	if (p->arg2 == NULL)
		return(ERROR_NULL_POINTER);

	int err;
	int slen = GetHandleSize(p->arg2);
	if (slen < 2)
	{
		err = BAD_NAME;
		goto done;
	}

	char* devName = (char*)malloc(slen + 2);
	err = GetCStringFromHandle(p->arg2, devName, slen);
	if (err != 0)
		goto done;

	devName[slen] = '\0';
	System::String^ gcdevName = gcnew System::String(devName);
	DoWith(err=,  X->DmdCam_ConfigPowerMeter((int)(p->arg1), gcdevName, p->arg3); );

	// goto is bad, but this is copied from example code on Page 143 of XOPMan6.pdf
done:
	// Unlike wave handles, string handles must be disposed by the XOP function
	if (p->arg1 != NULL)
		DisposeHandle(p->arg2);
	return err;
}


// EXPORT 5: screenId
static int DmdCam_MeasurePower(P1<double>* p) 
{
	NotNull(p);
	Do ( p->ret = X->DmdCam_MeasurePower(p->arg1); );
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
			return((XOPIORecResult)DmdCam_SetImage);
		case 4:
			return((XOPIORecResult)DmdCam_ConfigPowerMeter);
		case 5:
			return((XOPIORecResult)DmdCam_MeasurePower);
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
	array<unsigned char>^ buffer = gcnew array<unsigned char>(msg->Length);
	System::Text::ASCIIEncoding::ASCII->GetBytes(msg, 0, msg->Length, buffer, 0);
	unsigned char* p = (unsigned char*)malloc(msg->Length+2);
	System::Runtime::InteropServices::Marshal::Copy(buffer, 0, (System::IntPtr)p, msg->Length);
	p[msg->Length] = '\0';
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

#include "DllResolver.h"

#include "XOPStandardHeaders.h"			// Include ANSI headers, Mac headers, IgorXOP.h, XOP.h and XOPSupport.h
#include "MyXOP.h"



/*
	Define functions to export, and their parameter structures, here
*/

#pragma pack(2)	// All structures passed to Igor are two-byte aligned.
typedef struct {
	double p2;
	double p1;
	double result;
} MyFuncParams;
#pragma pack()

static int MyFunc(MyFuncParams* p)
{
	// call the static C# method.
	p->result = p->p1 * p->p2;

	// you could also allocate objects and do logic directly, here, in C++/CLI,
	// but it is probably easier and safer to manage state in C#.
	return(0); // XFunc error code
}


static int DmdCam_ShowWindows(MyFuncParams* p)
{
	// call the static C# method.
	//int hello = eas_lab::acq::DmdCam::DmdCamAPI::Hello();
	int camId = eas_lab::acq::DmdCam::DmdCamAPI::CreateDmdCam_DLP3000();
	eas_lab::acq::DmdCam::DmdCam^ c = eas_lab::acq::DmdCam::DmdCamAPI::GetDmdCam(camId);
	c->ShowOutputScreen();
	c->ShowPreview(false);
	p->result = 0;

	// you could also allocate objects and do logic directly, here, in C++/CLI,
	// but it is probably easier and safer to manage state in C#.

	return(0); // XFunc error code
}





// returns function pointers for each exported function
static XOPIORecResult RegisterFunction()
{
	int funcIndex;

	funcIndex = GetXOPItem(0);	/* which function are we getting the address of? */
	switch (funcIndex) {
		case 0:	
			return((XOPIORecResult)MyFunc);  // This should now be 64-bit safe
			break;
		// add more cases for more exported functions here
		// be sure to also add them to the XOPExports.rc resource file
	}
	return(NIL);
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
	eas_lab::acq::DmdCam::DmdCamAPI::Cleanup();
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

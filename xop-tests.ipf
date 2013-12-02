#pragma rtGlobals=3		// Use modern global access method and strict wave access.

// Test suite
// to "automate" this, run the following command
//    igor.exe /i /x "Execute/P "LOADFILE C:\\path\\to\\tests.ipf"; Execute/P "COMPILEPROCEDURES"; Execute/P "run_all_tests()""

Function run_all_tests()
	test_DmdCam_Reset()
	test_DmdCam_GetSize()
	test_DmdCam_Create()
	test_DmdCam_SetImage()
	//Execute/P "Quit /N"
End


Function test_DmdCam_Reset()
	VARIABLE expected, actual
	expected = 0
	actual = DmdCam_Reset()
	if (expected != actual)
		Abort "test_DmdCam_reset() failed"
	else
		return 0
	endif
End

Structure RectSize
	uint32 DimX
	uint32 DimY
EndStructure

Function test_DmdCam_GetSize()
	STRUCT RectSize size
	VARIABLE outputDevice, expected, actual

	outputDevice = 1
	expected = 0
	actual = DmdCam_GetSize(outputDevice, size)
	if (expected != actual)
		Abort "test_DmdCam_GetDims() failed"
	else
		return 0
	endif
End

Function test_DmdCam_Create()
	VARIABLE outputDevice, expected, actual
	outputDevice = 1
	expected = 0
	actual = DmdCam_Create(outputDevice)
	if (expected != actual)
		Abort "test_DmdCam_Create() failed"
	else
		return 0
	endif
End

Function test_DmdCam_SetImage()
	VARIABLE outputDevice, expected, actual
	Make/O/D/N=(1280,1024) DMDwave=(p<50)*(q<150)
	outputDevice = 1
	DmdCam_SetImage(outputDevice, DMDwave)
End

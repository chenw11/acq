#pragma rtGlobals=3		// Use modern global access method and strict wave access.

// Test suite
// to "automate" this, run the following command
//    igor.exe /i /x "Execute/P "LOADFILE C:\\path\\to\\tests.ipf"; Execute/P "COMPILEPROCEDURES"; Execute/P "run_all_tests()""

Function run_all_tests()
	test_CcdCam_Reset()
	test_CcdCam_Create()
	test_CcdCam_GetSize()
	test_CcdCam_SetImage()
	//Execute/P "Quit /N"
End


Function test_CcdCam_Reset()
	VARIABLE expected, actual
	expected = 0
	actual = CcdCam_Reset()
	if (expected != actual)
		Abort "test_CcdCam_reset() failed"
	else
		return 0
	endif
End

// Valid camera handles
// 0 = fake
// 1 = QCam
// 2 = Orca ER

Function test_CcdCam_Create()
	VARIABLE device
	VARIABLE expected, actual
	device = 0
	expected = 0
	actual = CcdCam_Create(device)
	if (expected != actual)
		Abort "test_CcdCam_Create() failed"
	else
		return 0
	endif
End


Structure RectSize
	uint32 DimX
	uint32 DimY
EndStructure

Function test_CcdCam_GetSize()
	STRUCT RectSize size
	VARIABLE device
	VARIABLE expected, actual

	device = 0
	expected = 0
	actual = CcdCam_GetSize(device, size)
	if (expected != actual)
		Abort "test_CcdCam_GetDims() failed"
	else
		return 0
	endif
End


// valid trigger modes:
//
// Freerun = 0
// Software = 1
// HardwareEdgeHigh = 2
// HardwareEdgeLow = 4

Structure VideoSettingsStatic
	uint32 Binning
	uint32 RoiX
	uint32 RoiY
	uint32 RoiWidth
	uint32 RoiHeight
	uint32 TriggerMode
EndStructure

Function test_CcdCam_GetVideoSettingsStatic()
	STRUCT VideoSettingsStatic settings
	VARIABLE device
	device = 0
	expected = 0
	actual = CcdCam_GetSettingsStatic(device, settings)
	if (expected != actual)
		Abort "CcdCam_GetSettingsStatic() failed"
	else
		return 0
	endif
End


Structure VideoFrameHeader
	uint32 ErrorCode
	uint32 BitsPerPixel
	uint32 Width
	uint32 Height
	uint32 FrameNumber
	uint32 TimeStamp
	uint32 DataSizeBytes
EndStructure

Function test_CcdCam_GetImage()
	VARIABLE expected, actual
	STRING device = "Lab.Acq.Fake"
	Make/O/D/N=(1280,1024) DMDwave=(p<50)*(q<150)
	outputDevice = 1
	CcdCam_SetImage(outputDevice, DMDwave)
End

#pragma rtGlobals=3		// Use modern global access method and strict wave access.

// Test suite
// to "automate" this, run the following command
//    igor.exe /i /x "Execute/P "LOADFILE C:\\path\\to\\tests.ipf"; Execute/P "COMPILEPROCEDURES"; Execute/P "run_all_tests()""

Function run_all_tests()
	test_CcdCam_Reset()
	test_CcdCam_Create()
	test_CcdCam_GetSize()
	test_CcdCam_SetVideoSettingsStatic()
	test_CcdCam_Start()
	test_CcdCam_Stop()
	test_CcdCam_TryGetFrame()
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

// Valid camera device IDs
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

Function test_CcdCam_SetVideoSettings()
	STRUCT VideoSettingsStatic settings
	VARIABLE device, expected, actual
	device = 0
	settings.Binning = 1
	settings.RoiX = 16
	settings.RoiY = 0
	settings.RoiWidth = 128
	settings.RoiHeight = 256
	settings.TriggerMode = 0
	
	expected = 0
	actual = CcdCam_SetVideoSettingsStatic(device, settings)
	if (expected != actual)
		Abort "CcdCam_SetVideoSettingsStatic() failed"
	else
		return 0
	endif
End

Function test_CcdCam_Start()
	VARIABLE device, expected, actual
	device = 0
	
	expected = 0
	actual = CcdCam_Start(device)
	if (expected != actual)
		Abort "CcdCam_Start() failed"
	else
		return 0
	endif
End


Function test_CcdCam_Stop()
	VARIABLE device, expected, actual
	device = 0
	
	expected = 0
	actual = CcdCam_Stop(device)
	if (expected != actual)
		Abort "CcdCam_Stop() failed"
	else
		return 0
	endif
End

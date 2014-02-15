#pragma rtGlobals=3		// Use modern global access method and strict wave access.


Structure RectSize
	uint32 DimX
	uint32 DimY
EndStructure

Structure VideoSettingsStatic
	uint32 Binning
	uint32 RoiX
	uint32 RoiY
	uint32 RoiWidth
	uint32 RoiHeight
	uint32 TriggerMode
EndStructure

Structure VideoSettingsDynamic
	float AnalogGain
	uint32 AnalogOffset
EndStructure

Function test_CcdCam_Acquire()
	VARIABLE device, frameNum

	STRUCT RectSize size
	STRUCT VideoSettingsStatic settings
	STRUCT VideoSettingsDynamic dyn_settings
	//
	// Ensure system is in a known good state:
	//

	CcdCam_Reset(); AbortOnRTE
	// use AbortOnRTE statement to ensure that
	// the function aborts if this call fails
	print "Reset successful"

	//
	// Initialize the CCD camera device:
	//

	// Valid camera device types:
	// 0 = fake
	// 1 = QCam
	// 2 = Orca ER
	device = 0
	CcdCam_Create(device); AbortOnRTE
	print "Device created"

	//
	// Request the CCD size
	//
	CcdCam_GetSize(device, size); AbortOnRTE
	print "Full CCD size is", size.DimX, "x", size.DimY


	//
	// Set up the acquisition parameters
	//
	settings.Binning = 1
	settings.RoiX = 16
	settings.RoiY = 32
	settings.RoiWidth = 256
	settings.RoiHeight = 128

	// valid trigger modes:
	// Freerun = 0
	// Software = 1
	// HardwareEdgeHigh = 2
	// HardwareEdgeLow = 4
	settings.Triggermode = 0

	CcdCam_SetVideoSettingsStatic(device, settings); AbortOnRTE
	print "Video settings set"


	//
	// Set up the dynamic analog contrast adjustments
	//   On the Orca Camera these can be between 0 and 255
	//   These settings aren't yet implemented on the QImaging camera
	//
	if (device == 2)
		dyn_settings.AnalogGain = 0
		dyn_settings.AnalogOffset = 0
		CcdCam_SetVideoSettingsDynamic(device, dyn_settings); AbortOnRTE
		print "Analog contrast adjustments set"
	endif


	//
	// Create a 2D wave to hold the aquired frames
	//
	Make/N=(settings.RoiWidth,settings.RoiHeight)/W/U/O frame; AbortOnRTE
	print "Frame buffer created"

	//
	// Start acquiring frames
	//
	CcdCam_Start(device); AbortOnRTE
	print "Started acquisition"

	//
	// Read the first 10 frames
	//
	do
		frameNum = CcdCam_TryGetFrame(device, frame); AbortOnRTE
		if (frameNum >= 0)
			print "Got frame", frameNum
			if (frameNum == 10)
				print "And the wave stats are..."
				WaveStats frame
			endif
		else
			Sleep /T 1
		endif
	while (frameNum < 10)


	//
	// Stop acquiring frames
	//
	CcdCam_Stop(device)
	print "Stopped acquisition"

	CcdCam_Reset()
	print "Device reset"
End

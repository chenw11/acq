#pragma rtGlobals=3		// Use modern global access method and strict wave access.

// Test suite
// to "automate" this, run the following command
//    igor.exe /i /x "Execute/P "LOADFILE C:\\path\\to\\tests.ipf"; Execute/P "COMPILEPROCEDURES"; Execute/P "run_all_tests()""

Function run_all_tests()
	test_MyFunc()
	Execute/P "Quit /N"
End


Function test_MyFunc()
	Variable input1, input2
	Variable expected, actual
	
	input1 = 6
	input2 = 7
	expected = 42
	actual = MyFunc(input1, input2)
	Print actual
	if (expected != actual)
		Abort "test_MyFunc: Expected != actual"
	else
		return 0
	endif
End
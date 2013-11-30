#REQUIRES -Version 3.0
# 
# Runs a simple test of the XOP functionality from inside Igor Pro
# A shortcut to the build output (MyXOP.xop) needs to be in your Igor Extensions folder 
#   C:\Users\<MyName>\Documents\WaveMetrics\Igor Pro 6 User Files\Igor Extensions
#
# To run this script, you may need to set the ExecutionPolicy on your machine to RemoteSigned
#


$igor='"C:\Program Files (x86)\WaveMetrics\Igor Pro Folder\Igor.exe"'
$test_file=Join-Path $PSScriptRoot "xop-tests.ipf"
$double_slashed=$test_file.Replace("\","\\")
$ar='/i /x "Execute/P "LOADFILE ' + $double_slashed + '"; Execute/P "COMPILEPROCEDURES "; Execute/P "run_all_tests()""'

$tempscript = Join-Path $PSScriptRoot "local-test-runner.cmd"
"$igor $ar" | Out-File $tempscript -Encoding ASCII
cmd /c call $tempscript
rm $tempscript

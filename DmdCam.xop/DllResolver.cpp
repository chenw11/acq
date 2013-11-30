

/*  Help resolve managed (C# / .NET) DLLs when used by this XOP.
	Call DllResolver::Init() from this XOP's main()                */
using namespace System;
using namespace System::Reflection;


ref class DllResolver
{
	// Container for managed DLL dependencies.
	// Set to the empty string to get default Win32 DLL resolution
	static String^ BaseDirectory = String::Empty;

	static Assembly^ MyEventHandler( Object^ sender, ResolveEventArgs^ args )
	{
		// args->Name has format DisplayName, Version=x.x.x.x, Culture=neutral, PublicKeyToken=....
		// so get the DisplayName and append .dll 
		String^ dllName = args->Name->Split(',')[0] + ".dll"; 
		String^ fullPath = System::IO::Path::Combine(BaseDirectory, dllName);

		// Framework will trigger this handler for dllname.resources.dll 
		// which we don't build, so we need to swallow the exception
		// See http://social.msdn.microsoft.com/Forums/vstudio/en-US/32e458a1-7443-4309-a054-8839b33dfd4f/framework-40-dll-loaded-by-a-20-executable-cannot-find-xxxxresourcesdll?forum=netfxbcl
		try { return Assembly::LoadFrom(fullPath); }
		catch(System::IO::FileNotFoundException^) {	return nullptr; }
	}

public:
	// Initializes the DllResolver.  Call with true to look for DLLs in the 
	// same directory that holds this XOP.  False doesn't look there, and instead uses 
	// the default Windows DLL resolution,
	// as documented here: http://msdn.microsoft.com/en-us/library/ms682586.aspx
	static void Init(bool searchInXOPDirectory)
	{
		AppDomain::CurrentDomain->AssemblyResolve += gcnew ResolveEventHandler( MyEventHandler );

		if (searchInXOPDirectory)
			BaseDirectory = System::IO::Path::GetDirectoryName(Assembly::GetExecutingAssembly()->Location);
	}
};


void SetupDllResolver(bool searchInXOPDirectory) { DllResolver::Init(searchInXOPDirectory); }
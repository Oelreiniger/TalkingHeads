using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TalkingHeads.Commands
{
    class DLLInvoker : IDisposable
    {
        private Dictionary<string, IntPtr> hModules = new Dictionary<string, IntPtr>();

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        static extern bool FreeLibrary(IntPtr hModule);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ExecuteDelegate([MarshalAs(UnmanagedType.LPStr)] string args);

        public bool Load(string moduleName, string dllPath)
        {
            hModules.TryGetValue(moduleName, out IntPtr foundHModule);
            if (foundHModule != IntPtr.Zero)
                return true;

            IntPtr hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero)
                return false;

            IntPtr proc = GetProcAddress(hModule, "Execute");
            if (proc == IntPtr.Zero)
                return false;

            hModules.Add(moduleName, hModule);
            return true;
        }

        public string Execute(string moduleName, string args)
        {
            hModules.TryGetValue(moduleName, out IntPtr foundHModule);
            if (foundHModule == IntPtr.Zero)
                return "[Error] DLL not loaded.";

            IntPtr proc = GetProcAddress(foundHModule, "Execute");
            ExecuteDelegate execute = Marshal.GetDelegateForFunctionPointer<ExecuteDelegate>(proc);
            IntPtr resultPtr = execute(args);
            return Marshal.PtrToStringAnsi(resultPtr)!;
        }

        public void Unload(string moduleName)
        {
            hModules.TryGetValue(moduleName, out IntPtr foundHModule);
            if (foundHModule != IntPtr.Zero)
            {
                FreeLibrary(foundHModule);
                foundHModule = IntPtr.Zero;
                hModules.Remove(moduleName);
            }
        }

        public string GetAllLoadedModules()
        {
            StringBuilder allModulesStringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, IntPtr> hModule in hModules)
            {
                allModulesStringBuilder.Append(hModule.Key).Append(", ");
            }
            
            if(allModulesStringBuilder.Length > 0)
            {
                return allModulesStringBuilder.ToString();
            }
            else
            {
                return "[Empty]";
            }
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, IntPtr> hModule in hModules)
            {
                if (hModule.Value != IntPtr.Zero)
                {
                    FreeLibrary(hModule.Value);
                }
            }

            hModules.Clear();
            GC.SuppressFinalize(this);
        }

        ~DLLInvoker()
        {
            Dispose();
        }
    }
}

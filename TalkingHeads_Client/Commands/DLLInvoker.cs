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

        public string Load(string args)
        {
            string[] parts = args.Split(' ', 2);
            string moduleName = parts[0].ToLower();
            string dllPath = parts.Length > 1 ? parts[1] : "";

            if (string.IsNullOrEmpty(dllPath))
                return "[DLLInvoker] Modulename cant be empty.";

            hModules.TryGetValue(moduleName, out IntPtr foundHModule);
            if (foundHModule != IntPtr.Zero)
                return "[DLLInvoker] Module already loaded.";

            IntPtr hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero)
                return "[DLLInvoker] Path to DLL is invalid.";

            //TODO: Die methode sollte ja eigentlich nur execute heißen und nicht ExecuteWrapper
            IntPtr proc = GetProcAddress(hModule, "ExecuteWrapper");
            if (proc == IntPtr.Zero)
                return "[DLLInvoker] Given DLL is missing the ExecuteWrapper Method. Loading aborted.";

            hModules.Add(moduleName, hModule);
            return "[DLLInvoker] DLL successfully loaded!";
        }

        public string Execute(string args)
        {
            string[] parts = args.Split(' ', 2);
            string function = parts[0].ToLower();
            string functionArgs = parts.Length > 1 ? parts[1] : "";
            switch (function)
            {
                case "load":
                    return Load(functionArgs);
                case "unload":
                    return Unload(functionArgs);
                case "execute":
                    return ExecuteDLL(functionArgs);
                case "list":
                    return GetAllLoadedModules();
            }

            return $"[DLLInvoker] Unknown function: {function}";
        }

        public string ExecuteDLL(string args)
        {
            var parts = args.Split(' ', 2);
            string moduleName = parts[0];
            string executeArgs = parts.Length > 1 ? parts[1] : "";

            hModules.TryGetValue(moduleName, out IntPtr foundHModule);
            if (foundHModule == IntPtr.Zero)
                return "[DLLInvoker] DLL not loaded.";

            IntPtr proc = GetProcAddress(foundHModule, "ExecuteWrapper");
            ExecuteDelegate execute = Marshal.GetDelegateForFunctionPointer<ExecuteDelegate>(proc);
            IntPtr resultPtr = execute(executeArgs);
            return Marshal.PtrToStringAnsi(resultPtr)!;
        }

        public string Unload(string moduleName)
        {
            hModules.TryGetValue(moduleName, out IntPtr foundHModule);
            if (foundHModule != IntPtr.Zero)
            {
                FreeLibrary(foundHModule);
                foundHModule = IntPtr.Zero;
                hModules.Remove(moduleName);
                return "[DLLInvoker] Specified DLL successfuly unloaded.";
            }

            return "[DLLInvoker] Specified DLL not currently running.";
        }

        public string GetAllLoadedModules()
        {
            StringBuilder allModulesStringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, IntPtr> hModule in hModules)
            {
                allModulesStringBuilder.Append(hModule.Key).Append(", ");
            }

            if (allModulesStringBuilder.Length > 0)
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

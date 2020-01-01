using System;
using Reloaded.Hooks.Definitions;
using static SonicHeroes.Utils.OneRedirector.Native.Native;

namespace SonicHeroes.Utils.OneRedirector.Structs
{
    public struct NativeFunctions
    {
        private static bool _instanceMade;
        private static NativeFunctions _instance;

        public IFunction<NtCreateFile> NtCreateFile;
        public IFunction<NtReadFile> NtReadFile;
        public IFunction<NtSetInformationFile> NtSetInformationFile;
        public IFunction<NtQueryInformationFile> NtQueryInformationFile;


        public static NativeFunctions GetInstance(IReloadedHooks hooks)
        {
            if (_instanceMade)
                return _instance;

            var ntdllHandle    = LoadLibraryW("ntdll");
            var ntCreateFilePointer = GetProcAddress(ntdllHandle, "NtCreateFile");
            var ntReadFilePointer = GetProcAddress(ntdllHandle, "NtReadFile");
            var ntSetInformationFile = GetProcAddress(ntdllHandle, "NtSetInformationFile");
            var ntQueryInformationFile = GetProcAddress(ntdllHandle, "NtQueryInformationFile");

            _instance = new NativeFunctions()
            {
                NtCreateFile = hooks.CreateFunction<NtCreateFile>((long) ntCreateFilePointer),
                NtReadFile = hooks.CreateFunction<NtReadFile>((long)ntReadFilePointer),
                NtSetInformationFile = hooks.CreateFunction<NtSetInformationFile>((long)ntSetInformationFile),
                NtQueryInformationFile = hooks.CreateFunction<NtQueryInformationFile>((long)ntQueryInformationFile),
            };

            _instanceMade = true;

            return _instance;
        }
    }
}

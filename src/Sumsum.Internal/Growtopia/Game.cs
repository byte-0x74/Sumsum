﻿using System.Runtime.InteropServices;
using Sumsum.Internal.Growtopia.App;
using Sumsum.Internal.Util;
using static Sumsum.Internal.Util.WinApi;

namespace Sumsum.Internal.Growtopia;

internal static unsafe class Game
{
    internal static TDelegate GetFunction<TDelegate>(string pattern, Memory.FindMode mode, int offset = 0)
    {
        Memory.FindAddress(out var addr, pattern, mode, offset);
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(addr);
    }
    
    internal static void Setup()
    {
        if (!GetModuleInformation(GetCurrentProcess(), GetModuleHandle(null!), out var moduleInfo, (uint)sizeof(MODULEINFO)))
        {
            int error = Marshal.GetLastWin32Error();
            Log.Error($"Error getting module information. Error code: {error}");
        }
        else
        {
            Log.Debug($"Module Base Address: 0x{moduleInfo.lpBaseOfDll.ToInt64():X}");
            Log.Debug($"Module Size: {moduleInfo.SizeOfImage} bytes");
            Log.Debug($"Module Entry Point: 0x{moduleInfo.EntryPoint.ToInt64():X}");
        }
        
        Memory.BaseAddress = moduleInfo.lpBaseOfDll;
        Memory.EndAddress = IntPtr.Add(moduleInfo.lpBaseOfDll, (int)moduleInfo.SizeOfImage);
        
        PatchIntegrityCheck();
        AllowPasting();

        GetFunctions();
        
        BaseApp.SetFpsLimit?.Invoke(IntPtr.Zero, 300f);
        Log.Info("Fps limit set to 300");
        
        Log.Info("Game Initialized");
    }

    private static void PatchIntegrityCheck()
    {
        var integrityCheckAddr = Memory.FindPattern("75 08 85 C9 0F 85 8B 00 00 00");
        if (integrityCheckAddr == 0)
        {
            Log.Error("Integrity check not found");
            return;
        }

        if (!Memory.WriteBytes(integrityCheckAddr, "90 90"))
        {
            Log.Error("Failed to patch integrity check");
        }
        
        Log.Info("Patched integrity check");
    }

    private static void AllowPasting()
    {
        Memory.FindAddress(out var pasteAddress, "74 5C 83 F9 01 0F", Memory.FindMode.Normal);
        if (!Memory.WriteBytes(pasteAddress, "75 5C"))
        {
            Log.Error("Failed to patch paste");
        }
    }
    
    private static void GetFunctions()
    {
        BaseApp.SetFpsLimit = GetFunction<SetFpsLimitDelegate>("E8 E5 6F F2 FF", Memory.FindMode.Call);
    }
}

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public delegate void SetFpsLimitDelegate(IntPtr baseApp, float fps);
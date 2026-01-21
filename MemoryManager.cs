using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ChaosGateTrainer;

public class MemoryManager : IDisposable
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);

    private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;

    private IntPtr _processHandle = IntPtr.Zero;
    private Process? _gameProcess;
    private IntPtr _gameAssemblyBase = IntPtr.Zero;
    private int _gameAssemblySize = 0;

    public bool IsAttached => _processHandle != IntPtr.Zero && _gameProcess != null && !_gameProcess.HasExited;
    public IntPtr GameAssemblyBase => _gameAssemblyBase;

    public bool Attach()
    {
        var processes = Process.GetProcessesByName("ChaosGate");
        if (processes.Length == 0)
            return false;

        _gameProcess = processes[0];
        _processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, _gameProcess.Id);

        if (_processHandle == IntPtr.Zero)
            return false;

        // Find GameAssembly.dll
        foreach (ProcessModule module in _gameProcess.Modules)
        {
            if (module.ModuleName?.Equals("GameAssembly.dll", StringComparison.OrdinalIgnoreCase) == true)
            {
                _gameAssemblyBase = module.BaseAddress;
                _gameAssemblySize = module.ModuleMemorySize;
                break;
            }
        }

        return _gameAssemblyBase != IntPtr.Zero;
    }

    public void Detach()
    {
        if (_processHandle != IntPtr.Zero)
        {
            CloseHandle(_processHandle);
            _processHandle = IntPtr.Zero;
        }
        _gameProcess = null;
        _gameAssemblyBase = IntPtr.Zero;
    }

    public byte[]? ReadMemory(IntPtr address, int size)
    {
        if (!IsAttached) return null;

        var buffer = new byte[size];
        if (ReadProcessMemory(_processHandle, address, buffer, size, out _))
            return buffer;
        return null;
    }

    public bool WriteMemory(IntPtr address, byte[] data)
    {
        if (!IsAttached) return false;

        // Change memory protection to allow writing
        VirtualProtectEx(_processHandle, address, data.Length, PAGE_EXECUTE_READWRITE, out uint oldProtect);
        bool result = WriteProcessMemory(_processHandle, address, data, data.Length, out _);
        VirtualProtectEx(_processHandle, address, data.Length, oldProtect, out _);

        return result;
    }

    public IntPtr? AOBScan(string pattern)
    {
        if (!IsAttached || _gameAssemblyBase == IntPtr.Zero) return null;

        var (patternBytes, mask) = ParsePattern(pattern);

        // Read GameAssembly.dll memory in chunks
        const int chunkSize = 0x100000; // 1MB chunks
        byte[] buffer = new byte[chunkSize + patternBytes.Length];

        for (int offset = 0; offset < _gameAssemblySize; offset += chunkSize)
        {
            int readSize = Math.Min(chunkSize + patternBytes.Length, _gameAssemblySize - offset);
            var chunk = ReadMemory(_gameAssemblyBase + offset, readSize);
            if (chunk == null) continue;

            int index = FindPattern(chunk, patternBytes, mask);
            if (index != -1)
            {
                return _gameAssemblyBase + offset + index;
            }
        }

        return null;
    }

    private (byte[] pattern, bool[] mask) ParsePattern(string pattern)
    {
        var parts = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bytes = new byte[parts.Length];
        var mask = new bool[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "??" || parts[i] == "?")
            {
                bytes[i] = 0;
                mask[i] = false;
            }
            else
            {
                bytes[i] = Convert.ToByte(parts[i], 16);
                mask[i] = true;
            }
        }

        return (bytes, mask);
    }

    private int FindPattern(byte[] data, byte[] pattern, bool[] mask)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (mask[j] && data[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }

    public void Dispose()
    {
        Detach();
    }
}

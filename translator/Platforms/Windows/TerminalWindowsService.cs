using System.Runtime.InteropServices;
using translator.Services;

namespace translator.Platforms.Windows;

internal class TerminalWindowsService : ITerminalService
{
    public void Open()
    {
        AllocConsole();
    }

    public void Close()
    {
        FreeConsole();
    }

    public string? Read()
    {
        return Console.ReadLine();
    }

    public void Write(string value)
    {
        Console.Write(value);
    }

    [DllImport("Kernel32")]
    private static extern void AllocConsole();

    [DllImport("Kernel32")]
    private static extern void FreeConsole();
}

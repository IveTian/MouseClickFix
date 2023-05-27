using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MouseClickFix
{
    public partial class MainForm : Form
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private static IntPtr hookHandle = IntPtr.Zero;
        private static LowLevelMouseProc mouseProc;
        private bool isHooked = false;

        private NotifyIcon notifyIcon;

        public MainForm()
        {
            InitializeComponent();
            InitializeNotifyIcon();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // �������ʱ�Զ���������
            StartHook();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopHook();
            notifyIcon.Dispose();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = this.Icon; // ��������ͼ��Ϊ����ͼ��
            notifyIcon.Text = "��������������"; // ������ʾ�ı�
            notifyIcon.Visible = false;

            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
            }
        }

        private void StartHook()
        {
            if (isHooked)
                return;

            mouseProc = MouseHookCallback;
            hookHandle = SetHook(mouseProc);
            isHooked = true;

            UpdateStatusText(true);
        }

        private void StopHook()
        {
            if (!isHooked)
                return;

            UnhookWindowsHookEx(hookHandle);
            hookHandle = IntPtr.Zero;
            isHooked = false;

            UpdateStatusText(false);
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    // ����������
                    LeftButtonClick();
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private DateTime lastClickTime = DateTime.MinValue;

        private void LeftButtonClick()
        {
            // ���������������߼�...
            // ע�⣺�˴��ڴ���ؼ��Ͻ��в������ɸ�����Ҫ������Ӧ�Ĵ���

            // �������һ�ε����ʱ����
            TimeSpan clickInterval = DateTime.Now - lastClickTime;
            if (clickInterval.TotalMilliseconds < 100)
            {
                // ����������ε�˫��
                return;
            }

            // ���������ĵ����߼�...

            lastClickTime = DateTime.Now;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            StartHook();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            StopHook();
        }

        private void registerStartupButton_Click(object sender, EventArgs e)
        {
            RegisterStartup();
        }

        private void RegisterStartup()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // ��ȡӦ�ó���Ŀ�ִ���ļ�·��
                string appPath = Application.ExecutablePath;

                // ����ע������ֵ��ʹӦ�ó���������ʱ�Զ�����
                rk.SetValue("MouseBrokenClick", appPath);

                MessageBox.Show("�ѽ�Ӧ�ó���ע��Ϊ���������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ע�Ὺ��������ʱ����" + ex.Message, "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatusText(bool isHooked)
        {
            statusLabel.Text = isHooked ? "����������" : "�����ѽ���";
        }

        #region Windows API ����

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}

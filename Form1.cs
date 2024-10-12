using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Drawing;


namespace cfnat.win.gui
{
    public partial class Form1 : Form
    {
        private Process cmdProcess;
        private NotifyIcon notifyIcon;

        public Form1()
        {
            InitializeComponent();
            LoadFromIni();
            this.FormClosing += Form1_FormClosing;
            this.Load += Form1_Load; // 添加这一行来确保 Load 事件被处理
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            GetLocalIPs();

            // 设置窗体为固定大小
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true; // 保留最小化功能
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.Text = "CFnat Windows GUI v" + myFileVersionInfo.FileVersion + " TG:CMLiussss BY:CM喂饭 干货满满";
            // 初始化 NotifyIcon（系统托盘图标）
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = this.Icon;
            notifyIcon.Text = "CFnat Windows GUI: 未运行";
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // 为系统托盘图标添加上下文菜单
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("打开", NotifyIcon_Open);
            contextMenu.MenuItems.Add("退出", NotifyIcon_Exit);
            notifyIcon.ContextMenu = contextMenu;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                this.WindowState = FormWindowState.Minimized; // 先最小化窗体
                this.ShowInTaskbar = false; // 不在任务栏显示
                this.Hide(); // 然后隐藏窗体
                notifyIcon.Visible = true; // 显示托盘图标
            }
        }

        // 当双击系统托盘图标时触发
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            //notifyIcon.Visible = false;
        }

        // 当点击系统托盘菜单中的"打开"选项时触发
        private void NotifyIcon_Open(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            //notifyIcon.Visible = false;
        }

        // 当点击系统托盘菜单中的"退出"选项时触发
        private void NotifyIcon_Exit(object sender, EventArgs e)
        {
            if (button1.Text == "停止")
            {
                button1_Click(sender, e);
            }
                //notifyIcon.Visible = false;
                Application.Exit();
        }

        // 修改 Form1_FormClosing 方法
        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // 取消关闭操作
                Hide(); // 隐藏窗体
                notifyIcon.Visible = true; // 显示系统托盘图标
            }
            else
            {
                e.Cancel = true; // 暂时取消关闭操作
                await StopCommandAsync(); // 停止命令进程
                e.Cancel = false; // 允许关闭
                notifyIcon.Dispose(); // 释放 NotifyIcon 资源
                Application.Exit(); // 确保应用程序完全退出
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "启动")
            {
                outputTextBox.Clear();
                button1.Text = "停止";
                string 系统 = comboBox1.Text;
                string 架构 = comboBox2.Text;
                string 数据中心 = textBox1.Text;
                string 有效延迟 = textBox2.Text;
                string 服务端口 = textBox5.Text;
                string 开机启动 = checkBox1.Checked.ToString();
                notifyIcon.Icon = Properties.Resources.going;
                notifyIcon.Text = $"CFnat Windows GUI: 运行中\n数据中心: {数据中心}\n有效延迟: {有效延迟}\n服务端口: {服务端口}";
                // 保存到 cfnat.ini
                SaveToIni(系统, 架构, 数据中心, 有效延迟, 服务端口, 开机启动);
                await RunCommandAsync($"cfnat-{系统}-{架构}.exe -colo={数据中心} -port=443 -delay={有效延迟} -ips=4 -addr=\"0.0.0.0:{服务端口}\"");
                // 将光标移动到文本的末尾
                outputTextBox.SelectionStart = outputTextBox.Text.Length;
                // 滚动到光标位置
                outputTextBox.ScrollToCaret();
            }
            else
            {
                notifyIcon.Icon = this.Icon;
                notifyIcon.Text = "CFnat Windows GUI: 未运行";
                await StopCommandAsync();
                button1.Text = "启动";
            }
        }

        private async Task RunCommandAsync(string command)
        {
            try
            {
                cmdProcess = new Process();
                cmdProcess.StartInfo.FileName = "cmd.exe";
                cmdProcess.StartInfo.Arguments = "/c chcp 65001 & " + command;
                cmdProcess.StartInfo.RedirectStandardOutput = true;
                cmdProcess.StartInfo.RedirectStandardError = true;
                cmdProcess.StartInfo.UseShellExecute = false;
                cmdProcess.StartInfo.CreateNoWindow = true;
                cmdProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                cmdProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                cmdProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        this.Invoke(new Action(() =>
                        {
                            outputTextBox.AppendText(e.Data + Environment.NewLine);
                        }));
                    }
                };

                cmdProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        this.Invoke(new Action(() =>
                        {
                            outputTextBox.AppendText("ERROR: " + e.Data + Environment.NewLine);
                        }));
                    }
                };

                cmdProcess.Start();
                cmdProcess.BeginOutputReadLine();
                cmdProcess.BeginErrorReadLine();

                await Task.Run(() => cmdProcess.WaitForExit());
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行命令时发生错误: " + ex.Message);
            }
        }

        private async Task StopCommandAsync()
        {
            if (cmdProcess != null && !cmdProcess.HasExited)
            {
                try
                {
                    // 发送Ctrl+C信号
                    bool result = AttachConsole((uint)cmdProcess.Id);
                    SetConsoleCtrlHandler(null, true);
                    GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);

                    // 等待进程退出，最多等待5秒
                    await Task.Run(() => cmdProcess.WaitForExit(5000));

                    if (!cmdProcess.HasExited)
                    {
                        cmdProcess.Kill(); // 如果5秒后进程仍未退出，则强制终止
                    }

                    SetConsoleCtrlHandler(null, false);
                    FreeConsole();

                    cmdProcess.Close();
                    cmdProcess = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("停止命令时发生错误: " + ex.Message);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            outputTextBox.Clear();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 判断 comboBox1 的选中的文本
            if (comboBox1.Text.Equals("windows", StringComparison.OrdinalIgnoreCase))
            {
                // 清空 comboBox2 的项
                comboBox2.Items.Clear();

                // 添加新的项
                comboBox2.Items.Add("386");
                comboBox2.Items.Add("amd64");
                comboBox2.Items.Add("arm");
                comboBox2.Items.Add("arm64");
                comboBox2.Text = "amd64";
            }
            else
            {
                // 如果不是 "windows7"，可以根据需要清空或添加其他项
                comboBox2.Items.Clear();
                // 例如，添加其他项
                comboBox2.Items.Add("386");
                comboBox2.Items.Add("amd64");
                comboBox2.Text = "amd64";
            }
        }
        private void textBox1_Leave(object sender, EventArgs e)
        {
            // 获取文本框中的内容
            string inputText = textBox1.Text;

            // 使用 StringBuilder 来构建新的字符串
            StringBuilder outputText = new StringBuilder();

            foreach (char c in inputText)
            {
                if (char.IsLetter(c)) // 如果是字母
                {
                    outputText.Append(char.ToUpper(c)); // 转换为大写
                }
                else if (char.IsPunctuation(c)) // 如果是标点符号
                {
                    outputText.Append(","); // 替换为逗号
                }
                else
                {
                    outputText.Append(c); // 其他字符保持不变
                }
            }

            // 更新文本框内容
            textBox1.Text = outputText.ToString();
            // 将光标移动到文本框末尾
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.SelectionLength = 0; // 取消任何选定的文本
        }

        private void GetLocalIPs()
        {
            StringBuilder ipAddresses = new StringBuilder();

            // 获取所有网络接口
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 获取每个网络接口的IP地址信息
                foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                {
                    // 只获取IPv4地址，并确保是内网地址
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ip.Address) &&
                        (ip.Address.ToString().StartsWith("10.") ||
                         ip.Address.ToString().StartsWith("172.") ||
                         ip.Address.ToString().StartsWith("192.168.")))
                    {
                        ipAddresses.AppendLine(ip.Address.ToString());
                    }
                }
            }

            // 将获取到的IP地址显示在textBox4中
            textBox4.Text = ipAddresses.ToString();
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 允许控制字符（如退格）
            if (!char.IsControl(e.KeyChar))
            {
                // 只允许数字输入
                if (!char.IsDigit(e.KeyChar))
                {
                    e.Handled = true; // 如果不是数字，拦截该输入
                }
            }
        }

        private void textBox5_Leave(object sender, EventArgs e)
        {
            ValidateNumericRange(textBox5);
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            ValidateNumericRange(textBox2);
        }

        private void ValidateNumericRange(TextBox textBox)
        {
            // 尝试将输入的文本转换为数字
            if (int.TryParse(textBox.Text, out int value))
            {
                // 检查数字是否在范围内
                if (value < 1 || value > 65535)
                {
                    MessageBox.Show("请输入范围在 1 到 65535 之间的数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox.Focus(); // 重新聚焦到文本框
                }
            }
            else
            {
                MessageBox.Show("请输入有效的数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox.Focus(); // 重新聚焦到文本框
            }
        }

        private void SaveToIni(string sys, string arch, string colo, string delay, string port, string on)
        {
            using (StreamWriter writer = new StreamWriter("cfnat.ini"))
            {
                writer.WriteLine($"sys={sys}");
                writer.WriteLine($"arch={arch}");
                writer.WriteLine($"colo={colo}");
                writer.WriteLine($"delay={delay}");
                writer.WriteLine($"port={port}");
                writer.WriteLine($"on={on}");
            }
        }

        private void LoadFromIni()
        {
            // 检查 cfnat.ini 文件是否存在
            if (File.Exists("cfnat.ini"))
            {
                try
                {
                    // 读取文件中的所有行
                    var lines = File.ReadAllLines("cfnat.ini");

                    foreach (var line in lines)
                    {
                        // 确保行不为空并分割成键值对
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                string key = parts[0].Trim();
                                string value = parts[1].Trim();

                                // 根据键更新相应的控件
                                switch (key)
                                {
                                    case "sys":
                                        comboBox1.Text = value;
                                        break;
                                    case "arch":
                                        comboBox2.Text = value;
                                        break;
                                    case "colo":
                                        textBox1.Text = value;
                                        break;
                                    case "delay":
                                        textBox2.Text = value;
                                        break;
                                    case "port":
                                        textBox5.Text = value;
                                        break;
                                    case "on":
                                        if(value == "True")
                                        {
                                            checkBox1.Checked = true;
                                        }
                                        else
                                        {
                                            checkBox1.Checked = false;
                                        }
                                        break;
                                    default:
                                        // 可以添加日志或处理未识别的键
                                        break;
                                }
                            }
                            else
                            {
                                // 可以添加日志，说明某一行格式不正确
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 处理读取文件时的异常
                    MessageBox.Show($"读取配置文件时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // 文件不存在，可以给用户反馈
                MessageBox.Show("配置文件未找到，将使用默认设置。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Windows API 导入
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool AttachConsole(uint dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        delegate bool ConsoleCtrlDelegate(uint CtrlType);

        const uint CTRL_C_EVENT = 0;

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            string 系统 = comboBox1.Text;
            string 架构 = comboBox2.Text;
            string 数据中心 = textBox1.Text;
            string 有效延迟 = textBox2.Text;
            string 服务端口 = textBox5.Text;
            string 开机启动 = checkBox1.Checked.ToString();
            SaveToIni(系统, 架构, 数据中心, 有效延迟, 服务端口, 开机启动);
            // 判断是否需要添加或移除启动项
            if (checkBox1.Checked)
            {
                AddToStartup();
            }
            else
            {
                RemoveFromStartup();
            }
        }

            private void AddToStartup()
        {
            // 获取当前程序的路径
            string programName = "CFnat Windows GUI"; // 这里替换为你的程序名称
            string exePath = Application.ExecutablePath;

            // 使用注册表将程序添加到启动项
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    key.SetValue(programName, exePath);
                }
            }
        }

        private void RemoveFromStartup()
        {
            // 获取当前程序的名称
            string programName = "CFnat Windows GUI"; // 这里替换为你的程序名称

            // 使用注册表将程序移除启动项
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    key.DeleteValue(programName, false); // 如果不存在也不会抛出异常
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                button1_Click(sender, e);
            }
            timer1.Enabled = false;
        }
    }
}
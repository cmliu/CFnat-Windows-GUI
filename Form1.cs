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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using System.Linq;
using System.Collections.Generic;


namespace cfnat.win.gui
{
    public partial class Form1 : Form
    {
        private Process cmdProcess;
        private NotifyIcon notifyIcon;
        private bool isExitingDueToDisclaimer = false;
        private List<Process> cmdProcesses = new List<Process>(); // 用来保存所有启动的cmd进程
        int 执行开关 = 0;
        public Form1()
        {
            InitializeComponent();
            LoadFromIni();
            this.FormClosing += Form1_FormClosing;
            this.Load += Form1_Load; // 添加这一行来确保 Load 事件被处理
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            GetLocalIPs();

            this.Height = 575;
            this.Width = 816;

            // 设置窗体为固定大小
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true; // 保留最小化功能
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.Text = "CFnat Windows GUI v" + myFileVersionInfo.FileVersion + " TG:CMLiussss BY:CM喂饭 干货满满";
            // 初始化 NotifyIcon（系统托盘图标）
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = this.Icon;
            notifyIcon.Text = "CFnat: 未运行";
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
            if (e.CloseReason == CloseReason.UserClosing && !isExitingDueToDisclaimer)
            {
                e.Cancel = true;
                Hide();
                notifyIcon.Visible = true;
            }
            else
            {
                e.Cancel = true;
                await StopCommandAsync();
                e.Cancel = false;
                notifyIcon.Dispose();
                Application.Exit();
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "启动")
            {
                执行开关 = 1;
                checkBox4.Checked = true;
                outputTextBox.Clear();
                button1.Text = "停止";
                groupBox3.Enabled = false;
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox5.ReadOnly = true;
                string 系统 = comboBox1.Text;
                string 架构 = comboBox2.Text;
                string 数据中心 = textBox1.Text;
                string 有效延迟 = textBox2.Text;
                string 服务端口 = textBox5.Text;
                string 开机启动 = checkBox1.Checked.ToString();
                //高级设置参数
                string IP类型 = "4";
                if (comboBox3.Text == "IPv6") IP类型 = "6";
                string 目标端口 = textBox6.Text;
                string 随机IP = "true";
                string tls = "true";
                if (checkBox2.Checked == false)  随机IP = "false";
                string tls描述 = "TLS";
                if (checkBox3.Checked == false)
                {
                    tls = "false";
                    tls描述 = "noTLS";
                }
                string 有效IP = textBox7.Text;
                string 负载IP = textBox8.Text;
                string 并发请求 = textBox9.Text;
                string 检查的域名地址 = textBox10.Text;

                string 数据中心描述 = 数据中心;
                if (数据中心描述.Length > 11) 数据中心描述 = 数据中心.Substring(0,3) + "...";
                notifyIcon.Icon = Properties.Resources.going;
                string 状态栏描述 = $"CFnat: 运行中\nC: {数据中心描述}\nD: {有效延迟}ms\nP: {服务端口}\nIPv{IP类型} {目标端口} {tls描述}";
                if (状态栏描述.Length > 63) 状态栏描述 = 状态栏描述.Substring(0, 60) + "...";
                notifyIcon.Text = 状态栏描述;
                // 保存到 cfnat.ini
                SaveToIni(系统, 架构, 数据中心, 有效延迟, 服务端口, 开机启动, IP类型, 目标端口, tls, 随机IP, 有效IP, 负载IP, 并发请求, 检查的域名地址);
                if (button4.Enabled == true)
                {
                    log($"生成 IPv{IP类型}缓存 IP库");
                    await RunCommandAsync($"colo-windows-{架构}.exe -ips={IP类型} -random={随机IP}  -task={并发请求}", "colo");
                    if (执行开关 != 0)
                    {
                        string[] 数据中心数组 = textBox1.Text.Split(',');

                        // 检测 colo/ip.csv 文件是否存在
                        // 获取程序当前目录并拼接相对路径
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "colo", "ip.csv");

                        if (File.Exists(filePath))
                        {
                            StringBuilder IP库 = new StringBuilder(); // 用于存储符合条件的IP

                            // 读取 ip.csv 文件的内容
                            string[] lines = File.ReadAllLines(filePath);

                            // 跳过第一行（标题行），并逐行处理数据
                            foreach (var line in lines.Skip(1)) // Skip(1) 跳过标题行
                            {
                                string[] columns = line.Split(','); // 按逗号分割列
                                /*
                               if (columns.Length >= 5)
                               {
                                   string ip地址 = columns[0];    // IP地址
                                   string 数据中心名 = columns[1]; // 数据中心

                                   // 如果该IP的 数据中心 在数据中心数组中
                                   if (数据中心数组.Contains(数据中心名))
                                   {
                                       IP库.AppendLine(ip地址); // 将符合条件的IP添加到IP库
                                   }
                               }
                               */
                                IP库.AppendLine(columns[0]); // 将符合条件的IP添加到IP库
                            }

                            // 将IP库内容写入到程序目录的 ips-v4.txt 文件中
                            string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"ips-v{IP类型}.txt");
                            File.WriteAllText(outputPath, IP库.ToString());

                            log("IP库已成功写入到 ips-v4.txt 文件中。");
                        }
                        else
                        {
                            log("文件 colo/ip.csv 不存在。");
                        }
                    }
                }
                await RunCommandAsync($"cfnat-{系统}-{架构}.exe -colo={数据中心} -delay={有效延迟} -addr=\"0.0.0.0:{服务端口}\" -ips={IP类型} -port={目标端口} -tls={tls} -random={随机IP} -ipnum={有效IP} -num={负载IP} -task={并发请求} -domain=\"{检查的域名地址}\"");
            }
            else
            {
                执行开关 = 0;
                checkBox4.Checked = false;
                notifyIcon.Icon = this.Icon;
                notifyIcon.Text = "CFnat: 未运行";
                await StopCommandAsync();
                await StopCommandAsync();
                button1.Text = "启动";
                groupBox3.Enabled = true;
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                textBox5.ReadOnly = false;
            }
        }

        private async Task RunCommandAsync(string command, string workingDirectory = "")
        {
            if (执行开关 != 0) {
                progressBar1.Value = 0;
                comboBox1.Enabled = false;
                comboBox2.Enabled = false;
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

                    if (!string.IsNullOrEmpty(workingDirectory))
                    {
                        cmdProcess.StartInfo.WorkingDirectory = workingDirectory;
                    }

                    cmdProcess.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            this.Invoke(new Action(() =>
                            {
                                string 进度内容 = e.Data + Environment.NewLine;
                                outputTextBox.AppendText(进度内容);

                                if (进度内容.Contains("已完成: ") && 进度内容.Contains("%"))
                                {
                                    string[] parts = 进度内容.Split(' ');
                                    foreach (string part in parts)
                                    {
                                        if (part.Contains("%"))
                                        {
                                            string percentageString = part.Replace("%", "");
                                            if (double.TryParse(percentageString, out double percentage))
                                            {
                                                int newValue = (int)percentage;
                                                if (newValue > progressBar1.Value)  progressBar1.Value = newValue;
                                            }
                                            break;
                                        }
                                    }
                                }
                                if (checkBox4.Checked == true)
                                {
                                    outputTextBox.SelectionStart = outputTextBox.Text.Length;
                                    outputTextBox.ScrollToCaret();
                                }
                            }));
                        }
                    };

                    cmdProcess.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            this.Invoke(new Action(() =>
                            {
                                outputTextBox.AppendText(e.Data + Environment.NewLine);
                            }));
                        }
                    };

                    cmdProcess.Start();
                    cmdProcess.BeginOutputReadLine();
                    cmdProcess.BeginErrorReadLine();

                    // 将进程添加到进程列表中
                    cmdProcesses.Add(cmdProcess);

                    await Task.Run(() => cmdProcess.WaitForExit());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("执行命令时发生错误: " + ex.Message);
                }
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
            }
        }

        private async Task StopCommandAsync()
        {
            if (cmdProcesses.Count > 0)
            {
                foreach (var process in cmdProcesses)
                {
                    if (process != null && !process.HasExited)
                    {
                        try
                        {
                            // 发送Ctrl+C信号
                            bool result = AttachConsole((uint)process.Id);
                            SetConsoleCtrlHandler(null, true);
                            GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);

                            // 等待进程退出，最多等待5秒
                            await Task.Run(() => process.WaitForExit(5000));

                            if (!process.HasExited)
                            {
                                process.Kill(); // 如果5秒后进程仍未退出，则强制终止
                            }

                            SetConsoleCtrlHandler(null, false);
                            FreeConsole();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("停止命令时发生错误: " + ex.Message);
                        }
                    }
                    process.Close();
                }

                // 清空进程列表
                cmdProcesses.Clear();
            }
            comboBox1.Enabled = true;
            comboBox2.Enabled = true;
            progressBar1.Value = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            outputTextBox.Clear();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (button3.Text == "高级设置∧") button3_Click(sender, e);
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
                this.Height = 492; 
            }
            else
            {
                // 如果不是 "windows7"，可以根据需要清空或添加其他项
                comboBox2.Items.Clear();
                // 例如，添加其他项
                comboBox2.Items.Add("386");
                comboBox2.Items.Add("amd64");
                comboBox2.Text = "amd64";
                this.Height = 522;
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

        private void textBox6_Leave(object sender, EventArgs e)
        {
            ValidateNumericRange(textBox6);
        }

        private void textBox7_Leave(object sender, EventArgs e)
        {
            ValidateNumericRange(textBox7);
        }

        private void textBox8_Leave(object sender, EventArgs e)
        {
            ValidateNumericRange(textBox8);
        }

        private void textBox9_Leave(object sender, EventArgs e)
        {
            ValidateNumericRange(textBox9);
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
        //SaveToIni(系统, 架构, 数据中心, 有效延迟, 服务端口, 开机启动);
        private void SaveToIni(string 系统, string 架构, string 数据中心, string 有效延迟, string 服务端口, string 开机启动, string IP类型, string 目标端口, string tls, string  随机IP, string 有效IP, string 负载IP, string 并发请求, string 检查的域名地址)
        {
            using (StreamWriter writer = new StreamWriter("cfnat.ini"))
            {
                writer.WriteLine($"sys={系统}");
                writer.WriteLine($"arch={架构}");
                writer.WriteLine($"colo={数据中心}");
                writer.WriteLine($"delay={有效延迟}");
                writer.WriteLine($"addr={服务端口}");
                writer.WriteLine($"on={开机启动}");
                writer.WriteLine($"ips={IP类型}");
                writer.WriteLine($"port={目标端口}");
                writer.WriteLine($"tls={tls}");
                writer.WriteLine($"random={随机IP}");
                writer.WriteLine($"ipnum={有效IP}");
                writer.WriteLine($"num={负载IP}");
                writer.WriteLine($"task={并发请求}");
                writer.WriteLine($"domain={检查的域名地址}");
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
                                    case "addr":
                                        textBox5.Text = value;
                                        break;
                                    case "on":
                                        if(value.ToLower() == "true") checkBox1.Checked = true;
                                        else checkBox1.Checked = false;
                                        break;
                                    case "ips":
                                        if (value.ToLower() == "4") comboBox3.Text = "IPv4";
                                        else comboBox3.Text = "IPv6";
                                        break;
                                    case "tls":
                                        if (value.ToLower() == "true") checkBox3.Checked = true;
                                        else checkBox3.Checked = false;
                                        break;
                                    case "random":
                                        if (value.ToLower() == "true") checkBox2.Checked = true;
                                        else checkBox2.Checked = false;
                                        break;
                                    case "ipnum":
                                        textBox7.Text = value;
                                        break;
                                    case "num":
                                        textBox8.Text = value;
                                        break;
                                    case "task":
                                        textBox9.Text = value;
                                        break;
                                    case "domain":
                                        textBox10.Text = value;
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
                string 免责声明 = "CFnat-Windows-GUI 项目仅供教育、研究和安全测试目的而设计和开发。本项目旨在为安全研究人员、学术界人士及技术爱好者提供一个探索和实践网络通信技术的工具。\r\n在下载和使用本项目代码时，使用者必须严格遵守其所适用的法律和规定。使用者有责任确保其行为符合所在地区的法律框架、规章制度及其他相关规定。\r\n\r\n使用条款\r\n\r\n教育与研究用途：本软件仅可用于网络技术和编程领域的学习、研究和安全测试。\r\n禁止非法使用：严禁将 CFnat-Windows-GUI 用于任何非法活动或违反使用者所在地区法律法规的行为。\r\n使用时限：基于学习和研究目的，建议用户在完成研究或学习后，或在安装后的24小时内，删除本软件及所有相关文件。\r\n免责声明：CFnat-Windows-GUI 的创建者和贡献者不对因使用或滥用本软件而导致的任何损害或法律问题负责。\r\n用户责任：用户对使用本软件的方式以及由此产生的任何后果完全负责。\r\n无技术支持：本软件的创建者不提供任何技术支持或使用协助。\r\n知情同意：使用 CFnat-Windows-GUI 即表示您已阅读并理解本免责声明，并同意受其条款的约束。\r\n\r\n请记住：本软件的主要目的是促进学习、研究和安全测试。创作者不支持或认可任何其他用途。使用者应当在合法和负责任的前提下使用本工具。\r\n\r\n同意以上条款请点击\"是 / Yes\"，否则程序将退出。";

                // 显示带有 "同意" 和 "拒绝" 选项的对话框
                DialogResult result = MessageBox.Show(免责声明, "CFnat-Windows-GUI 免责声明", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                // 如果用户点击 "拒绝" (对应于 No 按钮)
                if (result == DialogResult.No)
                {
                    // 退出程序
                    Environment.Exit(0); // 立即退出程序
                }
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
            string IP类型 = "4";
            if (comboBox3.Text == "IPv6") IP类型 = "6";
            string 目标端口 = textBox6.Text;
            string 随机IP = "true";
            string tls = "true";
            if (checkBox2.Checked == false) 随机IP = "false";
            if (checkBox3.Checked == false) tls = "false";
            string 有效IP = textBox7.Text;
            string 负载IP = textBox8.Text;
            string 并发请求 = textBox9.Text;
            string 检查的域名地址 = textBox10.Text;
            SaveToIni(系统, 架构, 数据中心, 有效延迟, 服务端口, 开机启动, IP类型, 目标端口, tls, 随机IP, 有效IP, 负载IP, 并发请求, 检查的域名地址);
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
            Check_COLO(sender, e);
            button3_Click(sender, e);
            if (checkBox1.Checked)
            {
                button1_Click(sender, e);
            }
            timer1.Enabled = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text== "高级设置∨") {
                groupBox3.Visible = true; 
                button3.Text = "高级设置∧";
                //this.Height = 492;
                this.Height += 83;
                progressBar1.Location = new Point(12, 502);
            }
            else
            {
                groupBox3.Visible = false;
                button3.Text = "高级设置∨";
                //this.Height = 575;
                this.Height -= 83;
                progressBar1.Location = new Point(12, 502-83);
            }
        }

        private void textBox10_Leave(object sender, EventArgs e)
        {
            if (textBox10.Text.Length >= 8)
            {
                string first8Chars = textBox10.Text.Substring(0, 8).ToLower();
                if (first8Chars == "https://")
                {
                    // 截取 "https://" 之后的内容，并赋值给 textBox10.Text
                    textBox10.Text = textBox10.Text.Substring(8);
                }
                string first7Chars = textBox10.Text.Substring(0, 7).ToLower();
                if (first7Chars == "http://")
                {
                    // 截取 "https://" 之后的内容，并赋值给 textBox10.Text
                    textBox10.Text = textBox10.Text.Substring(7);
                }
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if(outputTextBox.Text.Length > 1047483647) {
                button2_Click(sender, e);
            }
        }

        private void outputTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            checkBox4.Checked = false;
        }

        private void outputTextBox_MouseLeave(object sender, EventArgs e)
        {
            checkBox4.Checked = true;
        }

        private void Check_COLO(object sender, EventArgs e)
        {
            // 获取当前程序的目录
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string coloFolder = Path.Combine(currentDirectory, "colo");

            // 检查是否存在colo文件夹
            if (Directory.Exists(coloFolder))
            {
                string 架构 = comboBox2.Text;
                // 定义要检查的文件路径
                string coloExe = Path.Combine(coloFolder, $"colo-windows-{架构}.exe");
                string ipsV4 = Path.Combine(coloFolder, "ips-v4.txt");
                string ipsV6 = Path.Combine(coloFolder, "ips-v6.txt");
                string locationsJson = Path.Combine(coloFolder, "locations.json");

                // 检查是否存在所需的4个文件
                if (File.Exists(coloExe) && File.Exists(ipsV4) && File.Exists(ipsV6) && File.Exists(locationsJson))
                {
                    button4.Enabled = true;
                    //log($"colo-windows-{架构}.exe 准备就绪！");
                    //MessageBox.Show("所有文件均存在！");
                }
                else
                {
                    button4.Enabled = false;
                    // 具体提示哪个文件不存在
                    string missingFiles = "";
                    if (!File.Exists(coloExe)) missingFiles += $"colo-windows-{架构}.exe";
                    if (!File.Exists(ipsV4)) missingFiles += "ips-v4.txt ";
                    if (!File.Exists(ipsV6)) missingFiles += "ips-v6.txt ";
                    if (!File.Exists(locationsJson)) missingFiles += "locations.json ";
                    //log("以下文件不存在: " + missingFiles);
                    //MessageBox.Show("以下文件不存在: " + missingFiles);
                }
            }
            else
            {
                button4.Enabled = false;
                //log("colo文件夹不存在！");
                //MessageBox.Show("colo文件夹不存在！");
            }
        }

        private void log(string text)
        {
            outputTextBox.AppendText(text+"\r\n");
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "缓存IP库") { 
                button4.Text= "停止";
                button1.Enabled = false;
                log("执行colo生成缓存IP库");
                string 架构 = comboBox2.Text;
                string IP类型 = "4";
                if (comboBox3.Text == "IPv6") IP类型 = "6";
                string 随机IP = "true";
                if (checkBox2.Checked == false) 随机IP = "false";
                string 并发请求 = textBox9.Text;
                log($"生成 {textBox1.Text}缓存 IP库");
                await RunCommandAsync($"colo-windows-{架构}.exe -ips={IP类型} -random={随机IP}  -task={并发请求}","colo");
                string[] 数据中心数组 = textBox1.Text.Split(',');

                // 检测 colo/ip.csv 文件是否存在
                // 获取程序当前目录并拼接相对路径
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "colo", "ip.csv");

                if (File.Exists(filePath))
                {
                    StringBuilder IP库 = new StringBuilder(); // 用于存储符合条件的IP

                    // 读取 ip.csv 文件的内容
                    string[] lines = File.ReadAllLines(filePath);

                    // 跳过第一行（标题行），并逐行处理数据
                    foreach (var line in lines.Skip(1)) // Skip(1) 跳过标题行
                    {
                        string[] columns = line.Split(','); // 按逗号分割列
                        if (columns.Length >= 5)
                        {
                            string ip地址 = columns[0];    // IP地址
                            string 数据中心名 = columns[1]; // 数据中心

                            // 如果该IP的 数据中心 在数据中心数组中
                            if (数据中心数组.Contains(数据中心名))
                            {
                                IP库.AppendLine(ip地址); // 将符合条件的IP添加到IP库
                            }
                        }
                    }

                    // 将IP库内容写入到程序目录的 ips-v4.txt 文件中
                    string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"ips-v{IP类型}.txt");
                    File.WriteAllText(outputPath, IP库.ToString());

                    log("IP库已成功写入到 ips-v4.txt 文件中。");
                }
                else
                {
                    log("文件 colo/ip.csv 不存在。");
                }
                button4.Text = "缓存IP库";
            }
            else {
                button4.Text = "缓存IP库";
                button1.Enabled = true;
                log("停止colo生成缓存IP库");
                await StopCommandAsync();
            }
        }
    }
}
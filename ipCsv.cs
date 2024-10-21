using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace cfnat.win.gui
{
    public partial class ipCsv : Form
    {
        public ipCsv()
        {
            InitializeComponent();
        }

        // 右键菜单的复制功能
        private void CopyCellContent(object sender, DataGridView dataGridView)
        {
            if (dataGridView.CurrentCell != null)
            {
                Clipboard.SetText(dataGridView.CurrentCell.Value.ToString());
            }
        }

        // RowPostPaint 事件的处理函数，用于显示序号
        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(dataGridView1.RowHeadersDefaultCellStyle.ForeColor))
            {
                e.Graphics.DrawString((e.RowIndex + 1).ToString(),
                                      dataGridView1.DefaultCellStyle.Font,
                                      b,
                                      e.RowBounds.Location.X + 10,
                                      e.RowBounds.Location.Y + 4);
            }
        }

        // RowPostPaint 事件的处理函数，用于显示序号（dataGridView2）
        private void dataGridView2_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(dataGridView2.RowHeadersDefaultCellStyle.ForeColor))
            {
                e.Graphics.DrawString((e.RowIndex + 1).ToString(),
                                      dataGridView2.DefaultCellStyle.Font,
                                      b,
                                      e.RowBounds.Location.X + 10,
                                      e.RowBounds.Location.Y + 4);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 禁用列和行的拖拽
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;

            // 创建右键菜单
            ContextMenuStrip contextMenu1 = new ContextMenuStrip();
            ToolStripMenuItem copyItem1 = new ToolStripMenuItem("复制", null, (s, ev) => CopyCellContent(s, dataGridView1));
            contextMenu1.Items.Add(copyItem1);
            dataGridView1.ContextMenuStrip = contextMenu1;
            dataGridView1.RowPostPaint += new DataGridViewRowPostPaintEventHandler(dataGridView1_RowPostPaint);

            // 为 dataGridView2 创建右键菜单
            ContextMenuStrip contextMenu2 = new ContextMenuStrip();
            ToolStripMenuItem copyItem2 = new ToolStripMenuItem("复制", null, (s, ev) => CopyCellContent(s, dataGridView2));
            contextMenu2.Items.Add(copyItem2);
            dataGridView2.ContextMenuStrip = contextMenu2;
            dataGridView2.RowPostPaint += new DataGridViewRowPostPaintEventHandler(dataGridView2_RowPostPaint);

            try
            {
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(currentPath, "colo", "ip.csv");

                if (File.Exists(filePath))
                {
                    var csvLines = File.ReadAllLines(filePath);
                    DataTable dt = new DataTable();
                    // 手动定义列头
                    dt.Columns.Add("IP地址");
                    dt.Columns.Add("数据中心");
                    dt.Columns.Add("地区");
                    dt.Columns.Add("城市");
                    dt.Columns.Add("网络延迟");

                    for (int i = 1; i < csvLines.Length; i++)
                    {
                        string[] rowData = csvLines[i].Split(',');
                        dt.Rows.Add(rowData);
                    }

                    dataGridView1.DataSource = dt;
                    DateTime lastModified = File.GetLastWriteTime(filePath);
                    this.Text = "历史数据  最后更新时间: " + lastModified.ToString("yyyy-MM-dd HH:mm:ss");

                    // 统计数据中心信息
                    var dataList = new List<DataEntry>();

                    for (int i = 1; i < csvLines.Length; i++)
                    {
                        var rowData = csvLines[i].Split(',');
                        if (rowData.Length >= 5)
                        {
                            string dataCenter = rowData[1];
                            int latency = int.Parse(rowData[4].Split(' ')[0]);
                            string city = rowData[3];

                            dataList.Add(new DataEntry
                            {
                                DataCenter = dataCenter,
                                Latency = latency,
                                City = city
                            });
                        }
                    }

                    var stats = dataList.GroupBy(d => d.DataCenter)
                        .Select(g => new
                        {
                            DataCenter = g.Key,
                            Count = g.Count(),
                            AverageLatency = Math.Round(g.Average(d => d.Latency), 2),
                            MinLatency = g.Min(d => d.Latency),
                            City = g.First().City
                        })
                        .OrderByDescending(s => s.Count)
                        .ToList();

                    dataGridView2.DataSource = stats.Select(s => new
                    {
                        数据中心 = s.DataCenter,
                        出现次数 = s.Count,
                        城市 = s.City,
                        平均延迟 = s.AverageLatency.ToString("F0") + " ms",
                        最低延迟 = s.MinLatency + " ms"
                    }).ToList();

                    // 启用排序
                    foreach (DataGridViewColumn column in dataGridView2.Columns)
                    {
                        column.SortMode = DataGridViewColumnSortMode.Automatic;
                    }
                }
                else
                {
                    MessageBox.Show("未找到文件: ip.csv", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 用于存储每一行数据的类
        private class DataEntry
        {
            public string DataCenter { get; set; }
            public int Latency { get; set; }
            public string City { get; set; }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            button1_Click(sender, e);
        }
     }
}

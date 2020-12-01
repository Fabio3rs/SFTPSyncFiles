using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Renci.SshNet;
using System.IO;
using System.Threading;

namespace SyncDetect
{
    public partial class Form1 : Form
    {
        static conndata Datamgr = new conndata();
        List<Watcher> list_wa;
        Thread thr;
        DataTable dblistservers = new DataTable();

        void SyncDirectory(SftpClient client, string localPath, string remotePath)
        {
            client.BufferSize = 16 * 1024;
            Console.WriteLine("Uploading directory {0} to {1}", localPath, remotePath);

            IEnumerable<FileSystemInfo> infos =
                new DirectoryInfo(localPath).EnumerateFileSystemInfos();
            foreach (FileSystemInfo info in infos)
            {
                if (info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    string subPath = remotePath + "/" + info.Name;
                    if (!client.Exists(subPath))
                    {
                        client.CreateDirectory(subPath);
                    }
                    SyncDirectory(client, info.FullName, remotePath + "/" + info.Name);
                }
                else
                {
                    Renci.SshNet.Sftp.SftpFileAttributes attr;

                    bool supload = true;
                    try
                    {
                        if (client.Exists(remotePath + "/" + info.Name))
                        {
                            attr = client.GetAttributes(remotePath + "/" + info.Name);

                            FileInfo finfo = (FileInfo)info;

                            if (finfo.Length == attr.Size)
                            {
                                DateTime dt = finfo.LastWriteTime;
                                DateTime dt1 = attr.LastWriteTime;

                                //System.Windows.Forms.MessageBox.Show(info.Name + "  " + dt.ToString() + "   " + dt1.ToString());
                                if (dt <= dt1)
                                {
                                    supload = false;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show(ex.Message + "  " + info.Name);
                    }

                    if (supload)
                    {
                        using (Stream fileStream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            /*String s = String.Format(
                                "Uploading {0} ({1:N0} bytes)\n",
                                info.FullName, ((FileInfo)info).Length);

                            AppendTextBox(s);*/
                            client.UploadFile(fileStream, remotePath + "/" + info.Name);
                        }
                    }
                }
            }
        }

        public Form1()
        {
            list_wa = new List<Watcher>();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Hide();

            dblistservers = new DataTable();

            dblistservers.Columns.Add("id", typeof(Int64));
            dblistservers.Columns.Add("sourced", typeof(string));
            dblistservers.Columns.Add("targetd", typeof(string));
            dblistservers.Columns.Add("server", typeof(string));
            dblistservers.Columns.Add("port", typeof(int));
            dblistservers.Columns.Add("user", typeof(string));
            dblistservers.Columns.Add("passwd", typeof(string));

            Datamgr.FillDataTable(ref dblistservers);

            listaservers.DataSource = dblistservers;
            listaservers.Columns["id"].Visible = false;

            {
                DataGridViewColumn c = listaservers.Columns["sourced"];
                c.HeaderText = "Diretório local";
                c.MinimumWidth = 150;
                c.Width = 150;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                c.ReadOnly = false;
            }

            {
                DataGridViewColumn c = listaservers.Columns["targetd"];
                c.HeaderText = "Diretório servidor";
                c.MinimumWidth = 150;
                c.Width = 150;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                c.ReadOnly = false;
            }

            {
                DataGridViewColumn c = listaservers.Columns["server"];
                c.HeaderText = "Servidor";
                c.MinimumWidth = 55;
                c.Width = 55;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                c.ReadOnly = false;
            }

            {
                DataGridViewColumn c = listaservers.Columns["port"];
                c.HeaderText = "Porta";
                c.MinimumWidth = 35;
                c.Width = 35;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                c.ReadOnly = false;
            }

            {
                DataGridViewColumn c = listaservers.Columns["user"];
                c.HeaderText = "Usuário";
                c.MinimumWidth = 60;
                c.Width = 60;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                c.ReadOnly = false;
            }

            {
                DataGridViewColumn c = listaservers.Columns["passwd"];
                c.HeaderText = "Senha";
                c.MinimumWidth = 60;
                c.Width = 60;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                c.ReadOnly = false;
            }

            {
                DataGridViewTextBoxColumn txtb = new DataGridViewTextBoxColumn();
                txtb.HeaderText = "STATUS";
                txtb.CellTemplate.ToolTipText = "Status";
                txtb.Name = "STATUS";
                txtb.MinimumWidth = 120;
                listaservers.Columns.Add(txtb);
            }

            /*{
                DataRow dr = dblistservers.Rows.Add();
                Datamgr.get(dr);
                //dblistservers.Rows.Add();
            }*/
            //listaservers.Rows[0].Cells["status"].Value = list_wa[0].client.GetStatus("/").AvailableBlocks.ToString();

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                if (list_wa.Count == 0)
                {
                    foreach (DataRow dr in dblistservers.Rows)
                    {
                        list_wa.Add(new Watcher(dr["sourced"].ToString(),
                            this,
                            dr["server"].ToString(),
                            Convert.ToInt32(dr["port"]),
                            dr["user"].ToString(),
                            dr["passwd"].ToString(),
                            dr["targetd"].ToString()));
                    }
                }

                syncdir.Enabled = true;
                timer1.Interval = 5000;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            bool processing = false;
            bool allconnected = true;
            if (list_wa.Count > 0)
            {
                for (int i = 0; i < list_wa.Count; i++)
                {
                    if (list_wa[i].client != null)
                    {
                        if (!list_wa[i].client.IsConnected)
                        {
                            allconnected = false;
                        }
                    }
                    else
                    {
                        allconnected = false;
                    }

                    bool bmut = list_wa[i].mut.WaitOne(30);

                    if (bmut)
                    {
                        list_wa[i].mut.ReleaseMutex();

                        if (list_wa[i].blockedBySuspectActivity)
                        {
                            listaservers.Rows[i].Cells["status"].Value = "Detectou atividade suspeita";
                        }
                        else
                        {
                            listaservers.Rows[i].Cells["status"].Value = "Idle/Watching";
                        }
                    }
                    else
                    {
                        processing = true;

                        listaservers.Rows[i].Cells["status"].Value = "Processando";
                    }
                }

                actionsmutexstatus.Text = processing? "Processando..." : "Only watching";
            }
            else
            {
                actionsmutexstatus.Text = "Nenhum watcher ativo/Desconectado";
                allconnected = false;
            }

            connectall.Enabled = !allconnected;
        }

        public void addToTextBox(String text)
        {
            richTextBox1.Text += text;
        }

        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }

            richTextBox1.Text += value;
        }

        public static void UploadSFTPFile(string host, string username, string password, string sourcefile, string destinationpath, int port)
        {
            using (SftpClient client = new SftpClient(host, port, username, password))
            {
                client.Connect();
                client.ChangeDirectory(destinationpath);
                using (FileStream fs = new FileStream(sourcefile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    client.BufferSize = 16 * 1024;
                    client.UploadFile(fs, Path.GetFileName(sourcefile));
                }
            }
        }

        void synctest()
        {
            try
            {
                foreach (Watcher wa in list_wa)
                {
                    wa.mut.WaitOne();

                    if (wa.blockedBySuspectActivity)
                    {
                        AppendTextBox("Sincronização indisponível no momento devido a atividades estranhas\n\n\n\n");
                        continue;
                    }

                    try
                    {
                        AppendTextBox("Sincronizando " + wa.path + "\n");
                        SyncDirectory(wa.client, wa.path, wa.serverpath);
                        AppendTextBox("Finalizada sincronização " + wa.path + "\n");
                    }
                    catch(Exception ex)
                    {
                        AppendTextBox(ex.Message + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
                    }
                    wa.mut.ReleaseMutex();
                }
            }
            catch (Exception ex)
            {
                AppendTextBox(ex.Message);
            }
        }

        private void syncdir_Click(object sender, EventArgs e)
        {
            if (thr == null)
            {
                thr = new Thread(synctest);
            }

            if (thr.ThreadState == ThreadState.Unstarted)
            {
                thr.Start();
            }
            else if (thr.ThreadState == ThreadState.Stopped)
            {
                thr = new Thread(synctest);
                thr.Start();
            }
        }

        private void listaservers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (listaservers.Columns[e.ColumnIndex].Name == "passwd" && e.Value != null)
            {
                listaservers.Rows[e.RowIndex].Tag = e.Value;
                e.Value = new String('\u25CF', e.Value.ToString().Length);
            }
        }

        private void listaservers_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (listaservers.Columns[listaservers.CurrentCell.ColumnIndex].Name == "passwd")//select target column
            {
                TextBox textBox = e.Control as TextBox;
                if (textBox != null)
                {
                    textBox.UseSystemPasswordChar = true;
                }
            }
            else
            {
                TextBox textBox = e.Control as TextBox;
                if (textBox != null)
                {
                    textBox.UseSystemPasswordChar = false;
                }
            }
            //var txtBox = e.Control as TextBox;
            //txtBox.KeyDown -= new KeyEventHandler(underlyingTextBox_KeyDown);
            //txtBox.KeyDown += new KeyEventHandler(underlyingTextBox_KeyDown);
        }

        private void listaservers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == listaservers.NewRowIndex || e.RowIndex < 0)
                return;

            /*if (e.ColumnIndex == listaservers.Columns["REMOVER"].Index)
            {
                //MessageBox.Show(listaservers.Rows[e.RowIndex].Cells["passwd"].Value.ToString());
                //listaservers.Rows.RemoveAt(e.RowIndex);
            }*/
        }

        private void connectall_Click(object sender, EventArgs e)
        {
            if (list_wa.Count == 0)
            {
                foreach (DataRow dr in dblistservers.Rows)
                {
                    list_wa.Add(new Watcher(dr["sourced"].ToString(),
                        this,
                        dr["server"].ToString(),
                        Convert.ToInt32(dr["port"]),
                        dr["user"].ToString(),
                        dr["passwd"].ToString(),
                        dr["targetd"].ToString()));
                }
            }

            syncdir.Enabled = true;
            timer1.Interval = 1000;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
                Hide();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void aboutbtn_Click(object sender, EventArgs e)
        {
            About ab = new About();
            ab.Show();
            ab.BringToFront();
        }
    }
}

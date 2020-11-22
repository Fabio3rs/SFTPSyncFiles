using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using Renci.SshNet;
using System.Security.Permissions;
using System.Threading;

namespace SyncDetect
{
    class Watcher
    {
        private string _path;
        public string path
        {
            get
            {
                return _path;
            }
            private set
            {
                _path = value;
            }
        }

        public string _serverpath;
        public string serverpath
        {
            get
            {
                return _serverpath;
            }
            private set
            {
                _serverpath = value;
            }
        }

        Form1 interf;
        FileSystemWatcher watcher;
        string logpath;
        //StreamWriter sw;
        List<FileSystemEventArgs> fse;
        List<RenamedEventArgs> fsren;

        public SftpClient client;

        public Mutex mut = new Mutex();

        private System.Timers.Timer aTimer = new System.Timers.Timer(5000);
        private System.Timers.Timer aRenTimer = new System.Timers.Timer(4000);

        public void stop()
        {
            mut.WaitOne();

            fsren.Clear();
            fse.Clear();

            watcher.Dispose();

            watcher = null;

            client.Disconnect();
            client.Dispose();

            client = null;
            interf = null;

            mut.ReleaseMutex();
        }

        void addToFileListUnique(FileSystemEventArgs f)
        {
            bool exists = false;
            for (int i = 0; i < fse.Count; i++)
            {
                FileSystemEventArgs e = fse[i];
                if (e.FullPath == f.FullPath)
                {
                    if (f.ChangeType == WatcherChangeTypes.Deleted)
                    {
                        fse[i] = f;
                        exists = true;
                        break;
                    }
                    else if (e.ChangeType == f.ChangeType)
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
            {
                fse.Add(f);
            }
        }

        void DirSearch(string sDir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        Console.WriteLine(f);
                        System.Windows.Forms.MessageBox.Show(f);
                    }
                    DirSearch(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        void DirSearchF(string sDir)
        {
            try
            {
                try
                {
                    string svpath = sDir.Replace(path, "");
                    svpath = svpath.Replace("\\", "/");
                    string fullpath = serverpath + svpath;

                    RecursiveSVMkdir(fullpath);
                }
                catch(Exception ex)
                {
                    interf.AppendTextBox(ex.Message + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
                }

                foreach (string f in Directory.GetFiles(sDir))
                {
                    if (f == "." || f == "..")
                        continue;

                    string svpath = f.Replace(path, "");
                    svpath = svpath.Replace("\\", "/");
                    string fullfilepath = serverpath + svpath;
                    //System.Windows.Forms.MessageBox.Show(f);
                    try
                    {
                        using (FileStream fs = new FileStream(f, FileMode.Open))
                        {
                            client.BufferSize = 4 * 1024;
                            client.UploadFile(fs, fullfilepath);
                        }
                    }
                    catch (Exception ex)
                    {
                        interf.AppendTextBox(ex.Message + "   " + fullfilepath + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
                        continue;
                    }
                }

                foreach (string d in Directory.GetDirectories(sDir))
                {
                    DirSearchF(d);
                }
            }
            catch (System.Exception ex)
            {
                interf.AppendTextBox(ex.Message + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
            }
        }

        private void OnTimedRenameEvent(Object source, ElapsedEventArgs e)
        {
            mut.WaitOne();

            if (aTimer.Enabled)
            {
                aTimer.Stop();
                aTimer.Start();
            }

            if (!client.IsConnected)
            {
                try
                {
                    client.Connect();
                }
                catch (Exception ex)
                {
                    interf.AppendTextBox(ex.Message + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
                    mut.ReleaseMutex();
                    return;
                }
            }

            aRenTimer.Stop();
            // Specify what is done when a file is renamed.
            //sw.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");

            foreach (RenamedEventArgs f in fsren)
            {
                string fpath = f.FullPath.Replace(path, "");
                fpath = fpath.Replace("\\", "/");


                string foldpath = f.OldFullPath.Replace(path, "");
                foldpath = foldpath.Replace("\\", "/");


                string spath = serverpath + fpath;
                string soldpath = serverpath + foldpath;

                if (client.Exists(soldpath))
                {
                    try
                    {
                        client.RenameFile(soldpath, spath);
                    }catch (Exception ex)
                    {
                        interf.AppendTextBox(ex.Message + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(f.FullPath, FileMode.Open))
                    {
                        client.BufferSize = 4 * 1024;
                        client.UploadFile(fs, spath);
                    }
                }
            }

            fsren.Clear();
            mut.ReleaseMutex();
        }

        void RecursiveSVMkdir(string dir)
        {
            if (client.Exists(dir))
                return;

            dir = dir.Replace("//", "/");
            string[] paths = dir.Split('/');
            string nfullpath = "";

            int start = 0;

            if (dir.Length > 0
                && dir[0] == '/')
            {
                ++start;
            }

            for (int i = start; i < paths.Length; i++)
            {
                nfullpath += "/";
                nfullpath += paths[i];

                if (!client.Exists(nfullpath))
                    client.CreateDirectory(nfullpath);
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            mut.WaitOne();

            if (!client.IsConnected)
            {
                try
                {
                    client.Connect();
                }
                catch (Exception ex)
                {
                    interf.AppendTextBox(ex.Message + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
                    mut.ReleaseMutex();
                    return;
                }
            }

            aTimer.Stop();

            // Sorting the file list makes sure the actions are applied to the parent directory first
            List<FileSystemEventArgs> SortedList = fse.OrderBy(o => o.FullPath).ToList();

            for (int i = 0; i < SortedList.Count; i++)
            {
                FileSystemEventArgs f = SortedList[i];

                if (f == null)
                    continue;

                string fpath = f.FullPath.Replace(path, "");
                fpath = fpath.Replace("\\", "/");
                //System.Windows.Forms.MessageBox.Show(f.Name + "   " + fpath + "  " + f.ChangeType.ToString());
                
                if (f.ChangeType != WatcherChangeTypes.Deleted)
                {
                    FileAttributes attr = FileAttributes.Directory;
                    
                    try
                    {
                        attr = File.GetAttributes(f.FullPath);
                    }
                    catch(Exception ex)
                    {
                        interf.AppendTextBox(ex.Message);
                        continue;
                    }

                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        try
                        {
                            string spath = serverpath + fpath;
                            if (!client.Exists(spath))
                                client.CreateDirectory(spath);
                        }
                        catch(Exception ex)
                        {
                            interf.AppendTextBox(ex.Message);
                        }

                        DirSearchF(f.FullPath);

                        for (int j = i + 1; j < SortedList.Count; j++)
                        {
                            string bsl = "\\";

                            if (f.FullPath.Contains("/"))
                            {
                                bsl = "/";
                            }

                            if (f.FullPath.EndsWith(bsl))
                            {
                                bsl = "";
                            }

                            bool ischilddir = SortedList[j].FullPath == f.FullPath? true : SortedList[j].FullPath.StartsWith(f.FullPath + bsl);

                            if (ischilddir)
                            {
                                SortedList[j] = null;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            string fmtname = Path.GetDirectoryName(f.FullPath);
                            fmtname = fmtname.Replace(path, "");
                            fmtname = fmtname.Replace("\\\\", "/");
                            fmtname = fmtname.Replace("\\", "/");
                            //string lfname = StringUtils.RemoveFromEnd(fpath, fmtname);
                            string svfullpath = serverpath + fmtname;

                            RecursiveSVMkdir(svfullpath);

                            using (FileStream fs = new FileStream(f.FullPath, FileMode.Open))
                            {
                                client.BufferSize = 4 * 1024;
                                client.UploadFile(fs, serverpath + fpath);
                            }
                        }
                        catch(Exception ex)
                        {
                            interf.AppendTextBox(ex.Message + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
                        }
                    }
                }
                else
                {
                    try
                    {
                        string spath = serverpath + fpath;
                        if (client.Exists(spath))
                        {
                            bool isdirectory = client.GetAttributes(spath).IsDirectory;

                            client.RenameFile(spath, spath + ".file_deleted");

                            if (isdirectory)
                            {
                                for (int j = i + 1; j < SortedList.Count; j++)
                                {
                                    string bsl = "\\";

                                    if (f.FullPath.Contains("/"))
                                    {
                                        bsl = "/";
                                    }

                                    if (f.FullPath.EndsWith(bsl))
                                    {
                                        bsl = "";
                                    }

                                    bool ischilddir = SortedList[j].FullPath == f.FullPath ? true : SortedList[j].FullPath.StartsWith(f.FullPath + bsl);

                                    if (ischilddir)
                                    {
                                        SortedList[j] = null;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        interf.AppendTextBox(ex.Message);
                        continue;
                    }
                }
            }
            fse.Clear();
            mut.ReleaseMutex();
            /*System.Windows.Forms.MessageBox.Show("The Elapsed event was raised at {0:HH:mm:ss.fff}" +
                              e.SignalTime.ToString());*/
        }

        public Watcher(string wpath, Form1 forminst, string host, int port, string user, string password, string svpath)
        {
            fse = new List<FileSystemEventArgs>();
            fsren = new List<RenamedEventArgs>();
            interf = forminst;
            path = wpath;
            serverpath = svpath;

            logpath = wpath + "\\fwatcher.log";
            //sw = File.CreateText(logpath);

            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = false;

            aRenTimer.Elapsed += OnTimedRenameEvent;
            aRenTimer.AutoReset = true;
            aRenTimer.Enabled = false;


            client = new SftpClient(host, port, user, password);
            client.Connect();
            client.ChangeDirectory(serverpath);

            /*foreach (Renci.SshNet.Sftp.SftpFile fp in client.ListDirectory(serverpath))
            {
                interf.addToTextBox(fp.Attributes.Size.ToString() + "      " + fp.Attributes.LastWriteTime + "     " + fp.FullName + "\n");
            }*/

            Run();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void Run()
        {
            /*string[] args = Environment.GetCommandLineArgs();

            // If a directory is not specified, exit program.
            if (args.Length != 2)
            {
                // Display the proper way to call the program.
                Console.WriteLine("Usage: Watcher.exe (directory)");
                return;
            }*/

            // Create a new FileSystemWatcher and set its properties.
            watcher = new FileSystemWatcher();
            {
                watcher.Path = path;

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                watcher.NotifyFilter = NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.CreationTime;

                watcher.IncludeSubdirectories = true;

                // Only watch text files.
                watcher.Filter = "";

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                /*Console.WriteLine("Press 'q' to quit the sample.");
                while (Console.Read() != 'q') ;*/
            }
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                mut.WaitOne();
                addToFileListUnique(e);
                aTimer.Stop();

                int operationsCount = fse.Count + fsren.Count;

                if (operationsCount > 100)
                {
                    aTimer.Interval = 60000;
                }
                else if (operationsCount > 10)
                {
                    aTimer.Interval = 20000;
                }
                else if (operationsCount > 5)
                {
                    aTimer.Interval = 10000;
                }
                else
                {
                    aTimer.Interval = 5000;
                }

                aTimer.Start();
                aTimer.Enabled = true;
                interf.AppendTextBox($"{DateTime.Now.ToString()}    File:   {e.FullPath} {e.ChangeType}\n");
                mut.ReleaseMutex();
                //interf.addToTextBox($"File: {e.FullPath} {e.ChangeType}");
            }
            catch (Exception ex)
            {
                interf.AppendTextBox(ex.Message + "   " + ex.StackTrace + "   " + ex.Source + "\n\n\n\n");
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            mut.WaitOne();
            fsren.Add(e);
            aRenTimer.Stop();

            int operationsCount = fse.Count + fsren.Count;

            if (operationsCount > 100)
            {
                aTimer.Interval = 60000;
            }
            else if (operationsCount > 10)
            {
                aTimer.Interval = 20000;
            }
            else if (operationsCount > 5)
            {
                aTimer.Interval = 10000;
            }
            else
            {
                aTimer.Interval = 5000;
            }

            aRenTimer.Start();
            aRenTimer.Enabled = true;
            interf.AppendTextBox($"{DateTime.Now.ToString()}    File:   {e.OldFullPath} renamed to {e.FullPath}\n");
            mut.ReleaseMutex();
        }
    }
}

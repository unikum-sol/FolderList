// FolderList - schreibt die komplette Ordner-/Dateistruktur eines Ordners
// als lesbare TXT (Baumdarstellung) und als maschinenlesbare JSON.
// Kompatibel mit dem in Windows 10 enthaltenen csc.exe (.NET Framework 4.x, C# 5).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FolderList
{
    public class MainForm : Form
    {
        private TextBox txtSource;
        private TextBox txtTarget;
        private Button btnSource;
        private Button btnTarget;
        private Button btnStart;
        private Button btnCancel;
        private Button btnOpen;
        private Label lblStatus;
        private TextBox txtLog;

        private Thread worker;
        private Scanner scanner;

        public MainForm()
        {
            Text = "FolderList - Ordnerstruktur exportieren";
            Width = 780;
            Height = 520;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            Label l1 = new Label();
            l1.Text = "Quellordner:";
            l1.Left = 12; l1.Top = 15; l1.Width = 85;

            txtSource = new TextBox();
            txtSource.Left = 100; txtSource.Top = 12; txtSource.Width = 545;
            txtSource.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            btnSource = new Button();
            btnSource.Text = "Durchsuchen...";
            btnSource.Left = 652; btnSource.Top = 10; btnSource.Width = 100;
            btnSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSource.Click += delegate { PickFolder(txtSource, "Ordner auswählen, dessen Struktur exportiert werden soll"); };

            Label l2 = new Label();
            l2.Text = "Zielordner:";
            l2.Left = 12; l2.Top = 47; l2.Width = 85;

            txtTarget = new TextBox();
            txtTarget.Left = 100; txtTarget.Top = 44; txtTarget.Width = 545;
            txtTarget.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtTarget.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            btnTarget = new Button();
            btnTarget.Text = "Durchsuchen...";
            btnTarget.Left = 652; btnTarget.Top = 42; btnTarget.Width = 100;
            btnTarget.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTarget.Click += delegate { PickFolder(txtTarget, "Ordner auswählen, in den TXT und JSON gespeichert werden"); };

            btnStart = new Button();
            btnStart.Text = "Exportieren";
            btnStart.Left = 12; btnStart.Top = 80; btnStart.Width = 120; btnStart.Height = 30;
            btnStart.Click += BtnStart_Click;

            btnCancel = new Button();
            btnCancel.Text = "Abbrechen";
            btnCancel.Left = 140; btnCancel.Top = 80; btnCancel.Width = 120; btnCancel.Height = 30;
            btnCancel.Enabled = false;
            btnCancel.Click += delegate { if (scanner != null) scanner.Cancel = true; };

            btnOpen = new Button();
            btnOpen.Text = "Zielordner öffnen";
            btnOpen.Left = 268; btnOpen.Top = 80; btnOpen.Width = 130; btnOpen.Height = 30;
            btnOpen.Click += delegate
            {
                if (Directory.Exists(txtTarget.Text)) Process.Start("explorer.exe", "\"" + txtTarget.Text + "\"");
            };

            lblStatus = new Label();
            lblStatus.Left = 12; lblStatus.Top = 118; lblStatus.Width = 740; lblStatus.Height = 20;
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoEllipsis = true;
            lblStatus.Text = "Bereit.";

            txtLog = new TextBox();
            txtLog.Left = 12; txtLog.Top = 142; txtLog.Width = 740; txtLog.Height = 325;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Both;
            txtLog.WordWrap = false;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.Add(l1); Controls.Add(txtSource); Controls.Add(btnSource);
            Controls.Add(l2); Controls.Add(txtTarget); Controls.Add(btnTarget);
            Controls.Add(btnStart); Controls.Add(btnCancel); Controls.Add(btnOpen);
            Controls.Add(lblStatus); Controls.Add(txtLog);

            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (worker != null && worker.IsAlive)
                {
                    MessageBox.Show(this, "Export läuft noch. Bitte zuerst abbrechen.", "FolderList",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    e.Cancel = true;
                }
            };
        }

        private void PickFolder(TextBox target, string description)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = description;
                dlg.ShowNewFolderButton = true;
                if (Directory.Exists(target.Text)) dlg.SelectedPath = target.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK) target.Text = dlg.SelectedPath;
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            string source = txtSource.Text.Trim();
            string target = txtTarget.Text.Trim();

            if (source.Length == 0 || !Directory.Exists(source))
            {
                MessageBox.Show(this, "Quellordner existiert nicht.", "FolderList", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (target.Length == 0 || !Directory.Exists(target))
            {
                MessageBox.Show(this, "Zielordner existiert nicht.", "FolderList", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DirectoryInfo root = new DirectoryInfo(source);
            string baseName = SafeFileName(root) + "_" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
            string txtFile = Path.Combine(target, baseName + ".txt");
            string jsonFile = Path.Combine(target, baseName + ".json");

            txtLog.Clear();
            Log("Quelle: " + root.FullName);
            Log("TXT:    " + txtFile);
            Log("JSON:   " + jsonFile);
            Log("");

            scanner = new Scanner();
            scanner.Progress = delegate(string msg)
            {
                BeginInvoke((MethodInvoker)delegate { lblStatus.Text = msg; });
            };

            SetBusy(true);
            Scanner sc = scanner;
            worker = new Thread(delegate()
            {
                string result;
                bool ok = false;
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    sc.Run(root, txtFile, jsonFile);
                    ok = true;
                    result = "FERTIG nach " + sw.Elapsed.ToString(@"hh\:mm\:ss") + "\r\n" +
                             "Ordner: " + sc.Folders + "   Dateien: " + sc.Files +
                             "   Verknüpfungen (nicht verfolgt): " + sc.Links +
                             "   Fehler: " + sc.Errors;
                }
                catch (OperationCanceledException)
                {
                    result = "ABGEBROCHEN. Unvollständige Dateien wurden gelöscht.";
                    TryDelete(txtFile); TryDelete(jsonFile);
                }
                catch (Exception ex)
                {
                    result = "FEHLER: " + ex.Message + "\r\nUnvollständige Dateien wurden gelöscht.";
                    TryDelete(txtFile); TryDelete(jsonFile);
                }
                string final = result;
                bool finalOk = ok;
                BeginInvoke((MethodInvoker)delegate
                {
                    Log(final);
                    if (finalOk && sc.Errors > 0)
                    {
                        Log("");
                        Log("Hinweis: Ordner ohne Zugriff sind in beiden Dateien als Fehler markiert.");
                        Log("Ggf. Programm als Administrator starten.");
                    }
                    lblStatus.Text = finalOk ? "Fertig." : "Nicht abgeschlossen.";
                    SetBusy(false);
                });
            }, 64 * 1024 * 1024); // großer Stack für sehr tiefe Ordnerstrukturen
            worker.IsBackground = true;
            worker.Start();
        }

        private static string SafeFileName(DirectoryInfo root)
        {
            string name = root.Name;
            if (root.Parent == null) // Laufwerkswurzel, z. B. "D:\"
                name = "Laufwerk_" + name.TrimEnd('\\').TrimEnd(':');
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Trim();
            if (name.Length == 0) name = "Ordner";
            return "Struktur_" + name;
        }

        private static void TryDelete(string file)
        {
            try { if (File.Exists(file)) File.Delete(file); } catch { }
        }

        private void SetBusy(bool busy)
        {
            btnStart.Enabled = !busy;
            btnSource.Enabled = !busy;
            btnTarget.Enabled = !busy;
            txtSource.Enabled = !busy;
            txtTarget.Enabled = !busy;
            btnCancel.Enabled = busy;
        }

        private void Log(string text)
        {
            txtLog.AppendText(text + "\r\n");
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class Scanner
    {
        // Baumzeichen als Unicode-Escapes (unabhängig von der Quelltext-Codierung)
        private const string Branch = "\u251C\u2500\u2500 "; // ├──
        private const string LastBranch = "\u2514\u2500\u2500 "; // └──
        private const string Pipe = "\u2502   ";                // │
        private const string Space = "    ";

        public volatile bool Cancel;
        public Action<string> Progress;
        public long Folders, Files, Links, Errors;

        private StreamWriter txt;
        private StreamWriter json;
        private long counter;

        public void Run(DirectoryInfo root, string txtFile, string jsonFile)
        {
            string created = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

            using (txt = new StreamWriter(txtFile, false, new UTF8Encoding(true)))
            using (json = new StreamWriter(jsonFile, false, new UTF8Encoding(false)))
            {
                txt.NewLine = "\r\n";
                json.NewLine = "\n";

                txt.WriteLine("Ordnerstruktur von: " + root.FullName);
                txt.WriteLine("Erstellt am:        " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture));
                txt.WriteLine("Legende:            Ordner enden mit \\");
                txt.WriteLine(new string('=', 70));
                txt.WriteLine();
                txt.WriteLine(root.FullName.TrimEnd('\\') + "\\");

                json.Write("{\n");
                json.Write("  \"format\": \"FolderList\",\n");
                json.Write("  \"version\": 1,\n");
                json.Write("  \"root\": " + Q(root.FullName) + ",\n");
                json.Write("  \"created\": " + Q(created) + ",\n");
                json.Write("  \"pathSeparator\": \"\\\\\",\n");
                json.Write("  \"tree\": ");

                WriteFolder(root, root.Name, "", 1, "");

                json.Write(",\n");
                json.Write("  \"summary\": {\n");
                json.Write("    \"folders\": " + Folders + ",\n");
                json.Write("    \"files\": " + Files + ",\n");
                json.Write("    \"linksNotFollowed\": " + Links + ",\n");
                json.Write("    \"errors\": " + Errors + "\n");
                json.Write("  }\n");
                json.Write("}\n");

                txt.WriteLine();
                txt.WriteLine(new string('=', 70));
                txt.WriteLine("Ordner: " + Folders + "   Dateien: " + Files +
                              "   Verknüpfungen (nicht verfolgt): " + Links + "   Fehler: " + Errors);
            }
        }

        // Schreibt das JSON-Objekt eines Ordners (inkl. Inhalt) und die TXT-Zeilen seines Inhalts.
        private void WriteFolder(DirectoryInfo dir, string name, string rel, int depth, string childPrefix)
        {
            Folders++;
            string ind = Ind(depth);
            string ind1 = Ind(depth + 1);
            string ind2 = Ind(depth + 2);

            json.Write("{\n");
            json.Write(ind1 + "\"type\": \"folder\",\n");
            json.Write(ind1 + "\"name\": " + Q(name) + ",\n");
            json.Write(ind1 + "\"path\": " + Q(rel) + ",\n");

            List<FileSystemInfo> dirs = new List<FileSystemInfo>();
            List<FileSystemInfo> files = new List<FileSystemInfo>();
            string error = null;
            try
            {
                foreach (FileSystemInfo fsi in dir.EnumerateFileSystemInfos())
                {
                    if (Cancel) throw new OperationCanceledException();
                    if ((fsi.Attributes & FileAttributes.Directory) != 0) dirs.Add(fsi);
                    else files.Add(fsi);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (UnauthorizedAccessException) { error = "Zugriff verweigert"; }
            catch (Exception ex) { error = ex.Message; }

            if (error != null)
            {
                Errors++;
                json.Write(ind1 + "\"error\": " + Q(error) + ",\n");
                txt.WriteLine(childPrefix + "[FEHLER: " + error + "]");
            }

            Comparison<FileSystemInfo> byName = delegate(FileSystemInfo a, FileSystemInfo b)
            {
                return StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
            };
            dirs.Sort(byName);
            files.Sort(byName);

            int total = dirs.Count + files.Count;
            json.Write(ind1 + "\"children\": [");
            if (total == 0)
            {
                json.Write("]\n");
            }
            else
            {
                json.Write("\n");
                for (int i = 0; i < total; i++)
                {
                    if (Cancel) throw new OperationCanceledException();

                    bool last = (i == total - 1);
                    bool isDir = i < dirs.Count;
                    FileSystemInfo fsi = isDir ? dirs[i] : files[i - dirs.Count];
                    string crel = rel.Length == 0 ? fsi.Name : rel + "\\" + fsi.Name;
                    string line = childPrefix + (last ? LastBranch : Branch);

                    json.Write(ind2);
                    if (isDir && (fsi.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        // Junction / symbolischer Link: nicht verfolgen (Schutz vor Schleifen)
                        Links++;
                        txt.WriteLine(line + fsi.Name + "\\  [Verknüpfung - nicht verfolgt]");
                        json.Write("{ \"type\": \"link\", \"name\": " + Q(fsi.Name) + ", \"path\": " + Q(crel) + " }");
                    }
                    else if (isDir)
                    {
                        txt.WriteLine(line + fsi.Name + "\\");
                        WriteFolder((DirectoryInfo)fsi, fsi.Name, crel, depth + 2,
                                    childPrefix + (last ? Space : Pipe));
                    }
                    else
                    {
                        Files++;
                        txt.WriteLine(line + fsi.Name);
                        json.Write("{ \"type\": \"file\", \"name\": " + Q(fsi.Name) + ", \"path\": " + Q(crel) + " }");
                    }
                    json.Write(last ? "\n" : ",\n");

                    counter++;
                    if (counter % 500 == 0 && Progress != null)
                        Progress(counter.ToString("N0", CultureInfo.CurrentCulture) + " Einträge ... " + crel);
                }
                json.Write(ind1 + "]\n");
            }
            json.Write(ind + "}");
        }

        private static string Ind(int depth)
        {
            return new string(' ', depth * 2);
        }

        // JSON-String mit korrektem Escaping
        private static string Q(string s)
        {
            StringBuilder sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u" + ((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}

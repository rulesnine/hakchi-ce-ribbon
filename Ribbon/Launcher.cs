using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
namespace HakchiCE.Ribbon
{
    internal static class Launcher
    {
        [STAThread]
        private static void Main(string[] args)
        {
            try {
                string original = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hakchi.exe");
                if (!File.Exists(original)) throw new FileNotFoundException("Copia hakchi-ribbon.exe y su archivo .config junto al hakchi.exe de tu instalación portátil.");
                Version version = AssemblyName.GetAssemblyName(original).Version;
                if (version.Major != 3 || version.Minor != 9 || version.Build != 3)
                    throw new InvalidOperationException("Esta actualización requiere Hakchi CE v3.9.3.");
                using (Mutex existing = OpenExisting()) {
                    if (existing != null) {
                        MessageBox.Show("Cierra primero todas las ventanas de Hakchi y después abre Hakchi Ribbon.", "Hakchi Ribbon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                Assembly app = Assembly.LoadFrom(original);
                using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()) {
                    timer.Interval = 100;
                    timer.Tick += delegate {
                        foreach (Form form in Application.OpenForms) {
                            if (form.GetType().FullName == "com.clusterrr.hakchi_gui.MainForm") {
                                HakchiRibbon.Attach(form);
                                form.Text += " | Ribbon 1.0";
                                timer.Stop();
                                break;
                            }
                        }
                    };
                    timer.Start();
                    app.EntryPoint.Invoke(null, new object[] {args});
                }
            } catch (Exception ex) {
                Exception reason = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                MessageBox.Show(reason.Message, "Hakchi Ribbon: no se pudo iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private static Mutex OpenExisting()
        {
            try { return Mutex.OpenExisting("hakchi2"); }
            catch (WaitHandleCannotBeOpenedException) { return null; }
        }
    }
}

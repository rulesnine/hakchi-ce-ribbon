using System;
using System.IO;
using System.Windows.Forms;
using HakchiCE.Ribbon;
namespace com.clusterrr.hakchi_gui {
 public class MainForm:Form {
  public MainForm() {
   MainMenuStrip=new MenuStrip(); Controls.Add(MainMenuStrip);
   Text="Host startup regression";
  }
 }
}
class StartupTests {
 [STAThread] static int Main() {
  try {
   bool attached=false;
   Launcher.RunWithRibbon(delegate {
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    using(var form=new com.clusterrr.hakchi_gui.MainForm())
    using(var close=new Timer()) {
     close.Interval=250;
     close.Tick+=delegate {attached=form.Controls.ContainsKey("hakchiRibbon") && form.Text.Contains("1.1");form.Close();};
     form.Shown+=delegate {close.Start();};
     Application.Run(form);
    }
   });
   if(!attached) throw new Exception("Ribbon was not attached after host startup");
   Console.WriteLine("PASS: host SetCompatibleTextRenderingDefault succeeds; ribbon attaches after startup; message loop exits.");
   return 0;
  } catch(Exception ex) { Console.WriteLine(ex);return 1; }
 }
}


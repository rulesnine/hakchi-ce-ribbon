using System;
using System.Drawing;
using System.Windows.Forms;
using HakchiCE.Ribbon;
class RibbonTests {
 [STAThread] static int Main() {
  Application.EnableVisualStyles();
  using(Form form=new Form {Size=new Size(1000,700)}) {
   MenuStrip menu=new MenuStrip();
   ToolStripMenuItem view=new ToolStripMenuItem("Ver") {Name="viewToolStripMenuItem"};
   ToolStripMenuItem source=new ToolStripMenuItem("Instalar") {Name="installModulesToolStripMenuItem"};
   ToolStripMenuItem modules=new ToolStripMenuItem("Módulos"); modules.DropDownItems.Add(source);
   menu.Items.Add(view); menu.Items.Add(modules);
   Panel content=new Panel {Dock=DockStyle.Fill};
   Button add=new Button {Name="buttonAddGames",Text="Añadir",Dock=DockStyle.Bottom};
   content.Controls.Add(add); form.Controls.Add(content); form.Controls.Add(menu);
   int adds=0, installs=0; add.Click+=delegate {adds++;}; source.Click+=delegate {installs++;};
   form.Show(); HakchiRibbon.Attach(form); HakchiRibbon.Attach(form); Application.DoEvents();
   TabControl ribbon=(TabControl)form.Controls["hakchiRibbon"];
   Check(ribbon!=null && ribbon.TabCount==4,"four tabs and idempotent attach");
   Check(ribbon.Top>=menu.Bottom && content.Top>=ribbon.Bottom,"dock order");
   Button addAccess=(Button)ribbon.Controls.Find("ribbon_buttonAddGames",true)[0];
   addAccess.PerformClick(); Check(adds==1,"original button dispatch");
   add.Enabled=false; Pump(); Check(!addAccess.Enabled,"disabled source sync");
   addAccess.PerformClick(); Check(adds==1,"disabled action blocked");
   add.Enabled=true; Pump(); Check(addAccess.Enabled,"re-enabled source sync");
   add.Visible=false; Pump(); Check(!addAccess.Visible,"hidden source sync");
   ribbon.SelectedIndex=2; Pump();
   Button install=(Button)ribbon.Controls.Find("ribbon_installModulesToolStripMenuItem",true)[0];
   install.PerformClick(); Check(installs==1,"original menu dispatch");
   source.Enabled=false; Pump(); install.PerformClick(); Check(!install.Enabled && installs==1,"disabled menu action blocked");
   ((ToolStripMenuItem)view.DropDownItems[0]).PerformClick(); Check(!ribbon.Visible,"hide ribbon");
   ((ToolStripMenuItem)view.DropDownItems[0]).PerformClick(); Check(ribbon.Visible,"show ribbon");
   form.Width=420; Application.DoEvents();
   Check(((FlowLayoutPanel)ribbon.SelectedTab.Controls[0]).AutoScroll,"narrow window scroll");
   form.Close();
  }
  Console.WriteLine("PASS: dispatch, disabled and hidden states, duplicate attach, docking, toggle, narrow layout.");
  return 0;
 }
 static void Pump() {System.Threading.Thread.Sleep(300); Application.DoEvents();}
 static void Check(bool value,string label) {if(!value) throw new Exception(label); Console.WriteLine("PASS "+label);}
}

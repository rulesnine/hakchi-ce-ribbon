using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace HakchiCE.Ribbon
{
    public static class HakchiRibbon
    {
        public static void Attach(Form form)
        {
            if (form.Controls.ContainsKey("hakchiRibbon")) return;
            MenuStrip menu = form.MainMenuStrip;
            if (menu == null) foreach (Control control in form.Controls) if (control is MenuStrip) { menu = (MenuStrip)control; break; }
            if (menu == null) return;
            form.MainMenuStrip = menu;
            TabControl ribbon = new TabControl { Name = "hakchiRibbon", Dock = DockStyle.Top,
                Height = 140, Font = new Font("Segoe UI", 9F), AccessibleName = "Cinta de herramientas Hakchi" };
            List<Action> refresh = new List<Action>();
            List<Image> images = new List<Image>();
            AddTab(form, ribbon, refresh, images, "Inicio", new string[,] {
                {"Juegos", "buttonAddGames", "Añadir\njuegos", "+"},
                {"Juegos", "structureButton", "Organizar\ncarpetas", "F"},
                {"Transferir", "buttonStart", "Sincronizar\ncon mini", "S"},
                {"Transferir", "buttonExport", "Exportar\na USB", "U"},
                {"Herramientas", "saveStateManagerToolStripMenuItem", "Partidas\nguardadas", "G"},
                {"Herramientas", "explorerToolStripMenuItem", "Explorador\nde archivos", "F"}
            });
            AddTab(form, ribbon, refresh, images, "Juegos", new string[,] {
                {"Biblioteca", "buttonAddGames", "Añadir\njuegos", "+"},
                {"Biblioteca", "structureButton", "Organizar\ncarpetas", "F"},
                {"Selección", "selectEmulationCoreToolStripMenuItem", "Elegir\nemulador", "E"},
                {"Selección", "downloadBoxArtForSelectedGamesToolStripMenuItem", "Descargar\ncarátulas", "I"},
                {"Selección", "scrapeSelectedGamesToolStripMenuItem", "Buscar\ninformación", "?"}
            });
            AddTab(form, ribbon, refresh, images, "Módulos", new string[,] {
                {"Gestionar", "installModulesToolStripMenuItem", "Instalar\nmódulos", "+"},
                {"Gestionar", "uninstallModulesToolStripMenuItem", "Desinstalar\nmódulos", "-"},
                {"Repositorios", "manageModRepositoriesToolStripMenuItem", "Gestionar\nrepositorios", "F"},
                {"Diagnóstico", "generateModulesReportToolStripMenuItem", "Informe de\nmódulos", "I"}
            });
            AddTab(form, ribbon, refresh, images, "Consola", new string[,] {
                {"Archivos", "explorerToolStripMenuItem", "Explorador\nde archivos", "F"},
                {"Archivos", "openFTPInExplorerToolStripMenuItem", "Abrir\nFTP", "F"},
                {"Herramientas", "openTelnetToolStripMenuItem", "Terminal\nTelnet", ">"},
                {"Herramientas", "saveStateManagerToolStripMenuItem", "Partidas\nguardadas", "G"},
                {"Herramientas", "takeScreenshotToolStripMenuItem", "Capturar\npantalla", "I"}
            });
            Action update = delegate { foreach (Action action in refresh) action(); };
            Timer timer = new Timer { Interval = 250 };
            timer.Tick += delegate { update(); };
            ribbon.SelectedIndexChanged += delegate { update(); };
            ToolStripMenuItem toggle = new ToolStripMenuItem("Cinta de herramientas") {
                Name = "showHakchiRibbon", Checked = true, CheckOnClick = true };
            toggle.CheckedChanged += delegate { ribbon.Visible = toggle.Checked; };
            ToolStripMenuItem view = FindMenu(menu.Items, "viewToolStripMenuItem") as ToolStripMenuItem;
            if (view != null) view.DropDownItems.Add(toggle);
            form.SuspendLayout();
            form.Controls.Add(ribbon);
            // Dock order: menu, ribbon, then fill content.
            form.Controls.SetChildIndex(ribbon, form.Controls.GetChildIndex(menu));
            form.Height += ribbon.Height;
            form.ResumeLayout(true);
            update();
            timer.Start();
            form.FormClosed += delegate { timer.Dispose(); foreach (Image image in images) image.Dispose(); };
        }

        private static object FindSource(Form form, string name)
        {
            FieldInfo field = form.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null) return field.GetValue(form);
            Control[] controls = form.Controls.Find(name, true);
            if (controls.Length > 0) return controls[0];
            return FindMenu(form.MainMenuStrip.Items, name);
        }

        private static ToolStripItem FindMenu(ToolStripItemCollection items, string name)
        {
            foreach (ToolStripItem item in items) {
                if (item.Name == name) return item;
                ToolStripDropDownItem parent = item as ToolStripDropDownItem;
                if (parent != null) {
                    ToolStripItem found = FindMenu(parent.DropDownItems, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private static void AddTab(Form form, TabControl ribbon, List<Action> refresh,
            List<Image> images, string title, string[,] commands)
        {
            TabPage tab = new TabPage(title) { BackColor = Color.FromArgb(246, 248, 251) };
            FlowLayoutPanel row = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            tab.Controls.Add(row);
            ribbon.TabPages.Add(tab);
            FlowLayoutPanel buttons = null;
            string previous = null;
            ToolTip tips = new ToolTip();
            tab.Disposed += delegate { tips.Dispose(); };
            for (int i = 0; i < commands.GetLength(0); i++) {
                if (previous != commands[i, 0]) {
                    previous = commands[i, 0];
                    GroupBox group = new GroupBox { Text = previous, AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(4, 3, 4, 0), Margin = new Padding(4, 0, 0, 0) };
                    buttons = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        WrapContents = false, Dock = DockStyle.Fill };
                    group.Controls.Add(buttons);
                    row.Controls.Add(group);
                }
                object source = FindSource(form, commands[i, 1]);
                if (source == null) continue;
                Button sourceButton = source as Button;
                ToolStripItem sourceItem = source as ToolStripItem;
                Button button = new Button { Name = "ribbon_" + commands[i, 1], Text = commands[i, 2],
                    AccessibleName = commands[i, 2].Replace('\n', ' '), Size = new Size(91, 70),
                    Margin = new Padding(1), FlatStyle = FlatStyle.Flat, TextImageRelation = TextImageRelation.ImageAboveText };
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(218, 233, 250);
                Image icon = MakeIcon(commands[i, 3]);
                images.Add(icon);
                button.Image = icon;
                // Dispatch through the original control to retain its validation and confirmations.
                button.Click += delegate {
                    if (sourceButton != null && sourceButton.Enabled && sourceButton.Visible) sourceButton.PerformClick();
                    else if (sourceItem != null && sourceItem.Enabled && sourceItem.Available) sourceItem.PerformClick();
                };
                refresh.Add(delegate {
                    button.Enabled = sourceButton != null ? sourceButton.Enabled && sourceButton.Visible :
                        sourceItem != null && sourceItem.Enabled && sourceItem.Available;
                    button.Visible = sourceButton != null ? sourceButton.Visible : sourceItem != null && sourceItem.Available;
                });
                string original = sourceButton != null ? sourceButton.Text : sourceItem.Text;
                tips.SetToolTip(button, original.Replace("&", ""));
                buttons.Controls.Add(button);
            }
        }

        private static Image MakeIcon(string glyph)
        {
            Bitmap image = new Bitmap(26, 26);
            using (Graphics g = Graphics.FromImage(image))
            using (Pen pen = new Pen(Color.FromArgb(38, 95, 160), 2F))
            using (Brush brush = new SolidBrush(Color.FromArgb(38, 95, 160)))
            using (Font font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Pixel)) {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (glyph == "F") {
                    g.DrawLines(pen, new Point[] { new Point(3,21), new Point(3,6), new Point(11,6),
                        new Point(14,9), new Point(23,9), new Point(23,21), new Point(3,21) });
                } else if (glyph == "S" || glyph == "U") {
                    g.DrawLine(pen, 13, 3, 13, 17);
                    g.DrawLines(pen, new Point[] {new Point(7,11), new Point(13,17), new Point(19,11)});
                    g.DrawLines(pen, new Point[] {new Point(4,17), new Point(4,23), new Point(22,23), new Point(22,17)});
                } else {
                    g.DrawRectangle(pen, 3, 3, 20, 20);
                    using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString(glyph, font, brush, new RectangleF(3,3,20,20), format);
                }
            }
            return image;
        }
    }
}


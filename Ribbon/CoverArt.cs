using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace HakchiCE.Ribbon {
 public static class CoverArtSupport {
  public static void Attach(Form form) {
   Control[] found=form.Controls.Find("buttonGoogle",true);
   if(found.Length==0 || !(found[0] is Button)) return;
   Button button=(Button)found[0]; if(button.Tag as string=="ribbon-covers") return;
   MethodInfo old=form.GetType().GetMethod("buttonGoogle_Click",BindingFlags.Instance|BindingFlags.NonPublic);
   if(old!=null) button.Click-=(EventHandler)Delegate.CreateDelegate(typeof(EventHandler),form,old);
   button.Tag="ribbon-covers";button.Text="Carátulas";button.AccessibleName="Buscar y descargar carátulas";
   button.Click+=delegate {try {
    object game=form.GetType().GetMethod("GetSelectedGame",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,null);
    if(game==null) {MessageBox.Show(form,"Selecciona un solo juego.");return;}
    string name=Convert.ToString(game.GetType().GetProperty("Name").GetValue(game,null));
    object metadata=game.GetType().GetField("Metadata").GetValue(game);
    string system=Convert.ToString(metadata.GetType().GetField("System").GetValue(metadata));
    if(String.IsNullOrEmpty(system)) {object info=metadata.GetType().GetProperty("AppInfo").GetValue(metadata,null);system=Convert.ToString(info.GetType().GetField("Name").GetValue(info));}
    using(CoverArtForm picker=new CoverArtForm(name,system)) {
     if(picker.ShowDialog(form)==DialogResult.OK && picker.Result!=null) {
      game.GetType().GetProperty("Image").SetValue(game,picker.Result,null);
      form.GetType().GetMethod("ShowSelected").Invoke(form,null);
      FieldInfo timer=form.GetType().GetField("timerCalculateGames",BindingFlags.Instance|BindingFlags.NonPublic);
      if(timer!=null) ((System.Windows.Forms.Timer)timer.GetValue(form)).Enabled=true;
     }
    }
   } catch(Exception ex) {MessageBox.Show(form,(ex.InnerException??ex).Message,"No se pudo aplicar la carátula",MessageBoxButtons.OK,MessageBoxIcon.Error);}};
  }
 }
 internal static class CoverCatalog {
  public const string BaseUrl="https://thumbnails.libretro.com/";
  static readonly Dictionary<string,string[]> cache=new Dictionary<string,string[]>();
  internal static string Normalize(string text) {
   text=Regex.Replace(text??"",@"\([^)]*\)|\[[^\]]*\]"," ").Normalize(NormalizationForm.FormD);
   StringBuilder clean=new StringBuilder();foreach(char c in text) if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark) clean.Append(c);
   return Regex.Replace(clean.ToString().ToLowerInvariant(),@"[^a-z0-9]+"," ").Trim();
  }
  internal static string[] ParseIndex(string html) {
   List<string> names=new List<string>();
   foreach(Match match in Regex.Matches(html,"href=\"([^\"]+)\"",RegexOptions.IgnoreCase)) {
    string name=Uri.UnescapeDataString(WebUtility.HtmlDecode(match.Groups[1].Value));
    if(name.EndsWith(".png",StringComparison.OrdinalIgnoreCase) && name.IndexOfAny(new char[]{'/','\\'})<0) names.Add(name.Substring(0,name.Length-4));
   }
   return names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  }
  internal static string DirectoryUrl(string system) {return BaseUrl+Uri.EscapeDataString(system)+"/Named_Boxarts/";}
  internal static string ImageUrl(string system,string name) {return DirectoryUrl(system)+Uri.EscapeDataString(name+".png");}
  internal static string[] MatchNames(string[] names,string query) {
   string normal=Normalize(query);string[] words=normal.Split(new char[]{' '},StringSplitOptions.RemoveEmptyEntries);
   if(words.Length==0) return new string[0];
   return names.Where(n=>words.All(w=>Normalize(n).Split(' ').Contains(w))).OrderBy(n=>Normalize(n)==normal?0:1)
    .ThenBy(n=>n.Contains("(USA)")?0:n.Contains("(Europe)")?1:2).ThenBy(n=>n.Length).ThenBy(n=>n).Take(200).ToArray();
  }
  internal static string[] Search(string system,string query,CancellationToken token) {
   string[] names;lock(cache) cache.TryGetValue(system,out names);
   if(names==null) {
    names=ParseIndex(Encoding.UTF8.GetString(Download(DirectoryUrl(system),12*1024*1024,token)));
    if(names.Length==0) throw new InvalidOperationException("El catálogo no devolvió carátulas. Prueba Google o una imagen local.");
    lock(cache) cache[system]=names;
   }
   return MatchNames(names,query);
  }
  internal static byte[] Download(string url,int limit,CancellationToken token) {
   Uri uri;if(!Uri.TryCreate(url,UriKind.Absolute,out uri)||(uri.Scheme!="https" && uri.Scheme!="http")) throw new ArgumentException("Pega la dirección http o https de una imagen.");
   ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
   HttpWebRequest request=(HttpWebRequest)WebRequest.Create(uri);request.UserAgent="HakchiCE-Ribbon/1.1";request.Timeout=20000;request.ReadWriteTimeout=20000;
   using(token.Register(request.Abort)) using(WebResponse response=request.GetResponse()) using(Stream input=response.GetResponseStream()) using(MemoryStream output=new MemoryStream()) {
    if(response.ContentLength>limit) throw new InvalidOperationException("La descarga es demasiado grande.");
    byte[] buffer=new byte[16384];int count;
    while((count=input.Read(buffer,0,buffer.Length))>0) {token.ThrowIfCancellationRequested();if(output.Length+count>limit) throw new InvalidOperationException("La descarga es demasiado grande.");output.Write(buffer,0,count);}
    return output.ToArray();
   }
  }
  internal static Bitmap LoadImage(string url,CancellationToken token) {
   byte[] data=Download(url,12*1024*1024,token);
   using(MemoryStream stream=new MemoryStream(data)) using(Image image=Image.FromStream(stream)) {
    if(image.Width>4096||image.Height>4096) throw new InvalidOperationException("La imagen supera 4096 píxeles.");return new Bitmap(image);
   }
  }
 }
 public sealed class CoverArtForm:Form {
  readonly TextBox query=new TextBox{Dock=DockStyle.Fill},url=new TextBox{Dock=DockStyle.Fill};
  readonly ComboBox system=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
  readonly ListBox results=new ListBox{Dock=DockStyle.Fill,HorizontalScrollbar=true};
  readonly PictureBox preview=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(235,238,242)};
  readonly Label status=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};
  readonly Button apply=new Button{Text="Usar carátula",AutoSize=true,Enabled=false},search=new Button{Text="Buscar",AutoSize=true};
  readonly CancellationTokenSource lifetime=new CancellationTokenSource();
  int searchVersion,imageVersion;public Image Result{get;private set;}
  public CoverArtForm(string game,string gameSystem) {
   Text="Buscar carátula — "+game;Size=new Size(950,650);MinimumSize=new Size(720,500);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9F);query.Text=game;
   system.Items.AddRange(new object[]{"Nintendo - Nintendo Entertainment System","Nintendo - Family Computer Disk System","Nintendo - Super Nintendo Entertainment System","Nintendo - Nintendo 64","Nintendo - Game Boy","Nintendo - Game Boy Color","Nintendo - Game Boy Advance","Sega - Mega Drive - Genesis","Sega - Master System - Mark III","Sega - Game Gear","Sega - 32X","NEC - PC Engine - TurboGrafx 16","Sony - PlayStation","SNK - Neo Geo","Atari - 2600"});
   if(String.IsNullOrEmpty(gameSystem)||gameSystem=="Unknown System") gameSystem=(string)system.Items[0];
   if(!system.Items.Contains(gameSystem)) system.Items.Add(gameSystem);system.SelectedItem=gameSystem;
   TableLayoutPanel root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(10)};
   root.RowStyles.Add(new RowStyle(SizeType.Absolute,34));root.RowStyles.Add(new RowStyle(SizeType.Absolute,32));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));root.RowStyles.Add(new RowStyle(SizeType.Absolute,36));root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
   TableLayoutPanel top=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));top.Controls.Add(query,0,0);top.Controls.Add(search,1,0);root.Controls.Add(top,0,0);root.Controls.Add(system,0,1);
   SplitContainer split=new SplitContainer{Dock=DockStyle.Fill,Width=900,SplitterDistance=390};split.Panel1.Controls.Add(results);split.Panel2.Controls.Add(preview);root.Controls.Add(split,0,2);root.Controls.Add(status,0,3);
   TableLayoutPanel urls=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};urls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));urls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));Button load=new Button{Text="Cargar URL",AutoSize=true};url.AccessibleName="Dirección directa de una imagen";urls.Controls.Add(url,0,0);urls.Controls.Add(load,1,0);root.Controls.Add(urls,0,4);
   FlowLayoutPanel actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};Button google=new Button{Text="Google (navegador)",AutoSize=true},local=new Button{Text="Imagen local…",AutoSize=true},cancel=new Button{Text="Cancelar",AutoSize=true,DialogResult=DialogResult.Cancel};actions.Controls.Add(apply);actions.Controls.Add(google);actions.Controls.Add(local);actions.Controls.Add(cancel);root.Controls.Add(actions,0,5);Controls.Add(root);CancelButton=cancel;AcceptButton=search;
   search.Click+=async delegate{await SearchAsync();};Shown+=async delegate{await SearchAsync();};
   results.SelectedIndexChanged+=async delegate{if(results.SelectedItem!=null) await LoadAsync(CoverCatalog.ImageUrl(Convert.ToString(system.SelectedItem),Convert.ToString(results.SelectedItem)));};
   system.SelectedIndexChanged+=delegate{searchVersion++;imageVersion++;results.Items.Clear();SetPreview(null);search.Enabled=true;status.Text="Pulsa Buscar para consultar esta consola.";};
   load.Click+=async delegate{searchVersion++;search.Enabled=true;await LoadAsync(url.Text.Trim());};

   google.Click+=delegate{try{Process.Start(new ProcessStartInfo("https://www.google.com/search?tbm=isch&q="+Uri.EscapeDataString(query.Text+" "+system.Text+" box cover art")){UseShellExecute=true});status.Text="Copia la URL de la imagen o guárdala y usa Imagen local.";}catch(Exception ex){status.Text=ex.Message;}};
   local.Click+=delegate{using(OpenFileDialog dialog=new OpenFileDialog{Filter="Imágenes|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Todos los archivos|*.*"}) if(dialog.ShowDialog(this)==DialogResult.OK) try{searchVersion++;search.Enabled=true;imageVersion++;using(Image image=Image.FromFile(dialog.FileName)) SetPreview(new Bitmap(image));status.Text="Imagen local lista. Pulsa Usar carátula.";}catch(Exception ex){status.Text="No se pudo leer la imagen: "+ex.Message;}};
   apply.Click+=delegate{if(preview.Image!=null){Result=new Bitmap(preview.Image);DialogResult=DialogResult.OK;Close();}};
  }
  async Task SearchAsync() {
   int version=++searchVersion;imageVersion++;string name=query.Text.Trim(),console=Convert.ToString(system.SelectedItem);results.Items.Clear();SetPreview(null);search.Enabled=false;status.Text="Buscando carátulas en Libretro…";
   try{
    if(CoverCatalog.Normalize(name).Length==0){status.Text="Escribe el nombre del juego.";return;}
    string[] found=await Task.Run(()=>CoverCatalog.Search(console,name,lifetime.Token));if(IsDisposed||version!=searchVersion)return;
    results.Items.AddRange(found);status.Text=found.Length+" resultados — Libretro. Selecciona una carátula para verla.";
    if(found.Length==0)status.Text="Sin resultados. Prueba un nombre más corto, otra consola, Google o una imagen local.";
    if(found.Length>0)results.SelectedIndex=0;
   }catch(Exception ex){if(!IsDisposed&&!lifetime.IsCancellationRequested&&version==searchVersion)status.Text="No se pudo consultar Libretro: "+ex.Message+" Usa Google o Imagen local.";}
   finally{if(!IsDisposed&&version==searchVersion)search.Enabled=true;}
  }
  async Task LoadAsync(string imageUrl) {
   int version=++imageVersion;SetPreview(null);status.Text="Descargando vista previa…";
   try{Bitmap image=await Task.Run(()=>CoverCatalog.LoadImage(imageUrl,lifetime.Token));if(IsDisposed||version!=imageVersion){image.Dispose();return;}SetPreview(image);status.Text="Vista previa lista. Pulsa Usar carátula para guardarla en el juego.";}
   catch(Exception ex){if(!IsDisposed&&!lifetime.IsCancellationRequested&&version==imageVersion)status.Text="No se pudo cargar la imagen: "+ex.Message;}
  }
  void SetPreview(Image image){Image old=preview.Image;preview.Image=image;apply.Enabled=image!=null;if(old!=null)old.Dispose();}
  protected override void Dispose(bool disposing){if(disposing){lifetime.Cancel();SetPreview(null);if(Result!=null)Result.Dispose();}base.Dispose(disposing);}
 }
}


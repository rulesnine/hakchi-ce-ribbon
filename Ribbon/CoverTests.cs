using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using HakchiCE.Ribbon;
class CoverTests {
 [STAThread] static int Main() {
  try {
   Check(CoverCatalog.Normalize("Pokémon (USA) [Rev 1]")=="pokemon","normalize accents and region");
   string html="<a href=\"Mario%20%26%20Luigi%20(USA).png\">art</a><a href=\"../Bad.png\">bad</a><a href=\"https://bad/a.png\">bad</a>";
   string[] parsed=CoverCatalog.ParseIndex(html);
   Check(parsed.Length==1 && parsed[0]=="Mario & Luigi (USA)","directory parsing");
   Check(CoverCatalog.MatchNames(new[]{"Mario 2 (USA)","Mario 20 (USA)"},"Mario 2").Length==1,"whole number matching");
   string nes="Nintendo - Nintendo Entertainment System";
   string[] found=CoverCatalog.Search(nes,"Super Mario Bros.",CancellationToken.None);
   Check(found.Length>0,"live NES catalog: "+found.Length);
   using(Bitmap image=CoverCatalog.LoadImage(CoverCatalog.ImageUrl(nes,found[0]),CancellationToken.None))
    Check(image.Width>0 && image.Height>0,"live image download "+image.Width+"x"+image.Height);
   string[] gb=CoverCatalog.Search("Nintendo - Game Boy","Tetris",CancellationToken.None);
   Check(gb.Length>0,"live Game Boy catalog: "+gb.Length);
   bool invalid=false;try{CoverCatalog.Download("file:///test.png",100,CancellationToken.None);}catch(ArgumentException){invalid=true;}
   Check(invalid,"invalid URL rejected");
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   bool loaded=false;
   using(CoverArtForm form=new CoverArtForm("Super Mario Bros.",nes))
   using(System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer()) {
    int ticks=0;timer.Interval=300;
    FieldInfo preview=typeof(CoverArtForm).GetField("preview",BindingFlags.Instance|BindingFlags.NonPublic);
    timer.Tick+=delegate{
     PictureBox picture=(PictureBox)preview.GetValue(form);ticks++;
     if(picture.Image!=null) {
      using(Bitmap shot=new Bitmap(form.Width,form.Height)) {form.DrawToBitmap(shot,new Rectangle(Point.Empty,form.Size));shot.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"covers-preview.png"));}
      loaded=true;form.Close();
     } else if(ticks>100) form.Close();
    };
    form.Shown+=delegate{timer.Start();};Application.Run(form);
   }
   Check(loaded,"live dialog search and preview");
   Console.WriteLine("PASS: cover art tests");
   return 0;
  }catch(Exception ex){Console.WriteLine(ex);return 1;}
 }
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
}

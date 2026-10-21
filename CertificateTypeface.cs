using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
namespace ChiikawaDesktopPet {
static class CertificateTypeface {
 static readonly PrivateFontCollection fonts=new PrivateFontCollection();
 static readonly byte[] data;
 static readonly GCHandle pinned;
 static CertificateTypeface(){
  using(var stream=typeof(CertificateTypeface).Assembly.GetManifestResourceStream("Jua.ttf")){
   if(stream==null)throw new InvalidOperationException("Embedded Jua font missing");
   data=new byte[stream.Length];int offset=0;while(offset<data.Length){int count=stream.Read(data,offset,data.Length-offset);if(count==0)throw new InvalidOperationException("Incomplete Jua font");offset+=count;}
  }
  pinned=GCHandle.Alloc(data,GCHandleType.Pinned);fonts.AddMemoryFont(pinned.AddrOfPinnedObject(),data.Length);
 }
 public static Font Create(float size){return new Font(fonts.Families[0],size,FontStyle.Regular,GraphicsUnit.Point);}
}
}

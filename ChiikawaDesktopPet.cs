using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ChiikawaDesktopPet {
static class Program {
 [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
 [STAThread] static void Main(string[] args) {
  SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
  if(args.Length>0 && args[0]=="--self-test"){Tests.Run(args.Length>1?args[1]:"test-results.txt");return;}
  if(args.Length>0 && args[0]=="--preview"){Preview(args.Length>1?args[1]:"preview.png");return;}
  bool created; using(var mutex=new Mutex(true,"Local.ChiikawaClaudePet.v1",out created)) {
   if(!created){MessageBox.Show("이미 실행 중입니다. 작업 표시줄의 숨겨진 아이콘을 확인해 주세요.","치이카와 데스크톱 펫");return;}
   Application.Run(new PetApp(args.Length>0 && args[0]=="--smoke-test"));
  }
 }
 static void Preview(string file){using(var b=new Bitmap(960,340))using(var g=Graphics.FromImage(b)){
  g.Clear(Color.FromArgb(246,242,235));
  using(var f=new Font("Malgun Gothic",20,FontStyle.Bold))g.DrawString("치이카와 데스크톱 펫",f,Brushes.SaddleBrown,28,22);
  for(int i=0;i<4;i++){var s=g.Save();g.TranslateTransform(45+i*230,75);g.ScaleTransform(1.25f,1.25f);Art.Draw(g,i,0,false,false);g.Restore(s);using(var f=new Font("Malgun Gothic",12))g.DrawString(PetApp.Names[i],f,Brushes.DimGray,92+i*230,285);}
  b.Save(file,System.Drawing.Imaging.ImageFormat.Png);
 }}
}
class ClaudeStatus {
 public string State="offline",Label="클로드 연결 대기",Title="클로드";
 public event Action<string> Completed;
 DateTime next=DateTime.MinValue; Dictionary<string,string> seen=new Dictionary<string,string>(); bool initial=true;
 public void Poll(){if(DateTime.UtcNow<next)return;next=DateTime.UtcNow.AddMilliseconds(500);
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"status");if(!Directory.Exists(dir))return;
  DateTime newest=DateTime.MinValue,activeTime=DateTime.MinValue;string newestState="offline",detail="",activeState="",activeDetail="",chatTitle="",activeTitle="";int active=0;
  foreach(string file in Directory.GetFiles(dir,"*.status")){try{
   string[] row=File.ReadAllText(file).Split('\t');if(row.Length<4)continue;
   DateTime time;if(!DateTime.TryParse(row[0],null,System.Globalization.DateTimeStyles.RoundtripKind,out time))continue;
   if(DateTime.UtcNow-time.ToUniversalTime()>TimeSpan.FromHours(24))continue;
   string old;bool changed=!seen.TryGetValue(file,out old)||old!=row[0];seen[file]=row[0];
   if(!initial&&changed&&row[1]=="done"&&Completed!=null)Completed("클로드의 응답이 끝났어요. 결과를 확인해 주세요.");
   if(row[1]=="working"||row[1]=="waiting"){active++;if(time>activeTime){activeTime=time;activeState=row[1];activeDetail=row[2];activeTitle=row.Length>=5?row[4]:"";}}
   if(time>newest){newest=time;newestState=row[1];detail=row[2];chatTitle=row.Length>=5?row[4]:"";}
  }catch(IOException){}catch(UnauthorizedAccessException){}}
  initial=false;if(active>0){newestState=activeState;detail=activeDetail;chatTitle=activeTitle;}State=newestState;Title=chatTitle.Length>0?chatTitle:"클로드";
  string text=State=="working"?"클로드 작업 중":State=="waiting"?"승인·입력 기다리는 중":State=="done"?"응답 완료!":State=="error"?"오류 · 확인해 주세요":State=="idle"?"클로드 준비됨":"클로드 연결 대기";
  Label=text+(detail.Length>0?"\n"+detail:"")+(active>1?"\n진행 중 세션 "+active:"");
 }
}
static class Geometry {
 public static Point Clamp(Point p,Size size,Rectangle area){return new Point(Math.Max(area.Left,Math.Min(p.X,Math.Max(area.Left,area.Right-size.Width))),Math.Max(area.Top,Math.Min(p.Y,Math.Max(area.Top,area.Bottom-size.Height))));}
}
class PetApp:ApplicationContext {
 public static readonly string[] Names={"치이카와","하치와레","우사기","모몽가"};
 public List<Pet> Pets=new List<Pet>(); public bool Paused=true,Follow,ClickThrough; public int PetSize=160; public int SelectedCharacter;
 public QuestionController Questions; public ClaudeStatus Claude=new ClaudeStatus(); NotifyIcon tray; public EventWaitHandle StopSignal=new EventWaitHandle(false,EventResetMode.ManualReset,"Local.ChiikawaClaudePet.Stop.v1"); ContextMenuStrip menu; TimerHolder smoke; bool quitting;
 string settings=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.txt");
 public PetApp(bool test){
  Questions=new QuestionController(this);Load(); for(int i=0;i<1;i++){var p=new Pet(this,SelectedCharacter);Pets.Add(p);p.Show();}
  menu=new ContextMenuStrip();menu.Opening+=(s,e)=>FillMenu(menu);
  tray=new NotifyIcon{Text=Names[SelectedCharacter]+" · 클로드 펫",Icon=SystemIcons.Information,ContextMenuStrip=menu,Visible=true};
  tray.DoubleClick+=(s,e)=>Reset();
  Claude.Completed+=(label)=>{tray.ShowBalloonTip(6000,"클로드 응답 완료",label,ToolTipIcon.Info);System.Media.SystemSounds.Asterisk.Play();};
  if(test)smoke=new TimerHolder(2500,()=>Quit());
 }
 void Load(){try{if(!File.Exists(settings))return;var a=File.ReadAllLines(settings);int n;if(a.Length>0 && int.TryParse(a[0],out n)&&n>=100&&n<=240)PetSize=n;if(a.Length>1&&int.TryParse(a[1],out n)&&n>=0&&n<Names.Length)SelectedCharacter=n;}catch(IOException){}catch(UnauthorizedAccessException){}}
 void Save(){try{Directory.CreateDirectory(Path.GetDirectoryName(settings));File.WriteAllText(settings,PetSize.ToString()+Environment.NewLine+SelectedCharacter);}catch(IOException){}catch(UnauthorizedAccessException){}}
 ToolStripMenuItem Item(string name,bool check,Action action){var i=new ToolStripMenuItem(name){Checked=check};i.Click+=(s,e)=>action();return i;}
 public void FillMenu(ContextMenuStrip m){while(m.Items.Count>0)m.Items[0].Dispose();m.Items.Add(new ToolStripMenuItem("치이카와와 친구들"){Enabled=false});


  m.Items.Add(Item("클릭 통과 (해제: 트레이 메뉴)",ClickThrough,()=>{ClickThrough=!ClickThrough;foreach(var p in Pets)p.SetClickThrough(ClickThrough);}));
  var chars=new ToolStripMenuItem("캐릭터 선택");for(int i=0;i<Names.Length;i++){int value=i;var choice=Item(Names[i],SelectedCharacter==i,()=>SelectCharacter(value));chars.DropDownItems.Add(choice);}m.Items.Add(chars);
  var sizes=new ToolStripMenuItem("크기");foreach(int n in new[]{120,160,200}){int value=n;sizes.DropDownItems.Add(Item(n==120?"작게":n==160?"보통":"크게",PetSize==n,()=>{PetSize=value;foreach(var p in Pets)p.ResizePet();Save();}));}m.Items.Add(sizes);
  m.Items.Add("위치 초기화",null,(s,e)=>Reset());m.Items.Add(new ToolStripSeparator());
  m.Items.Add("사용 방법",null,(s,e)=>MessageBox.Show("드래그: 위치 이동\n클릭: 반응 보기\n두 번 클릭: 잠자기 / 깨우기\n우클릭: 설정\n\n클릭 통과 중에는 작업 표시줄의 트레이 아이콘에서 해제할 수 있습니다.\n종료도 트레이 메뉴에서 할 수 있습니다.","사용 방법"));
  m.Items.Add("종료",null,(s,e)=>Quit());
 }
 public void SelectCharacter(int index){if(index<0||index>=Names.Length)throw new ArgumentOutOfRangeException("index");SelectedCharacter=index;foreach(var pet in Pets)pet.ChangeCharacter(index);if(tray!=null)tray.Text=Names[index]+" · 클로드 펫";Save();}
 public void Reset(){foreach(var p in Pets){p.Rehome();p.SavePosition();p.Show();}}
 public void Quit(){if(quitting)return;quitting=true;if(Questions!=null)Questions.Dispose();Save();if(smoke!=null)smoke.Dispose();foreach(var p in Pets){p.AllowClose=true;p.Close();}tray.Visible=false;tray.Dispose();menu.Dispose();StopSignal.Dispose();ExitThread();}
}
class TimerHolder:IDisposable {System.Windows.Forms.Timer t; public TimerHolder(int ms,Action a){t=new System.Windows.Forms.Timer{Interval=ms};t.Tick+=(s,e)=>{t.Stop();a();};t.Start();}public void Dispose(){t.Dispose();}}
class Pet:Form {
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int GetWindowLong(IntPtr h,int n);
 [DllImport("user32.dll",EntryPoint="SetWindowLongW")] static extern int SetWindowLong(IntPtr h,int n,int v);
 [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
 int questionHeight; Point questionAnchor; public bool AllowClose; PetApp app; int kind; Random random; System.Windows.Forms.Timer timer; ContextMenuStrip menu;
 bool held,moved,sleeping,suppressRelease; Point origin,mouseDown; double x,y,vx,vy,phase; int changeIn,reaction,displayCheck; const int TickMs=33;
 public Pet(PetApp owner,int index){app=owner;kind=index;random=new Random(Environment.TickCount+index*971);
  FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;DoubleBuffered=false;StartPosition=FormStartPosition.Manual;
  Text=PetApp.Names[kind];AccessibleName=Text;Cursor=Cursors.Hand;Size=new Size(Math.Max(320,app.PetSize+20),app.PetSize+195+questionHeight);
  menu=new ContextMenuStrip();menu.Opening+=(s,e)=>app.FillMenu(menu);ContextMenuStrip=menu;
  MouseDown+=Down;MouseMove+=MovePet;MouseUp+=Up;MouseDoubleClick+=(s,e)=>{if(e.Button==MouseButtons.Left){sleeping=!sleeping;suppressRelease=true;reaction=0;Invalidate();}};
  MouseCaptureChanged+=(s,e)=>{if(held&&!Capture){held=false;x=Left;y=Top;}};
  FormClosing+=(s,e)=>{if(!AllowClose){e.Cancel=true;Hide();}};
  Rehome();try{if(File.Exists(PositionFile)){var parts=File.ReadAllText(PositionFile).Split(',');int px,py;if(parts.Length==2&&int.TryParse(parts[0],out px)&&int.TryParse(parts[1],out py)){Location=new Point(px,py);ClampHere();}}}catch(IOException){}catch(UnauthorizedAccessException){}timer=new System.Windows.Forms.Timer{Interval=TickMs};timer.Tick+=(s,e)=>Step();timer.Start();
 }
 public int CharacterIndex{get{return kind;}}
 public void ChangeCharacter(int index){if(index<0||index>=PetApp.Names.Length)throw new ArgumentOutOfRangeException("index");kind=index;sleeping=false;reaction=30;Text=PetApp.Names[index];AccessibleName=Text;Render();}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var c=base.CreateParams;c.ExStyle|=0x08080080;return c;}}
 public void SetClickThrough(bool enabled){int style=GetWindowLong(Handle,-20);SetWindowLong(Handle,-20,enabled?style|0x20:style&~0x20);SetWindowPos(Handle,IntPtr.Zero,0,0,0,0,0x37);}
 public void SetQuestionHeight(int height){if(questionHeight==height)return;if(questionHeight==0&&height>0)questionAnchor=Location;bool restore=questionHeight>0&&height==0;questionHeight=height;ResizePet();if(restore){Location=questionAnchor;ClampHere();}}
 public void ResizePet(){Size=new Size(Math.Max(320,app.PetSize+20),app.PetSize+195+questionHeight);ClampHere();Invalidate();}
 string PositionFile {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"position-0.txt");}}
 public void SavePosition(){if(questionHeight>0)questionAnchor=Location;try{File.WriteAllText(PositionFile,Left+","+Top);}catch(IOException){}catch(UnauthorizedAccessException){}}
 public void Rehome(){var a=Screen.PrimaryScreen.WorkingArea;var p=Geometry.Clamp(new Point(a.Left+40,a.Bottom-Height-20),Size,a);Location=p;x=Left;y=Top;ChooseDirection();}
 void ClampHere(){var p=Geometry.Clamp(Location,Size,Screen.FromRectangle(Bounds).WorkingArea);Location=p;x=Left;y=Top;}
 void ChooseDirection(){var angle=random.NextDouble()*Math.PI*2;double speed=.55+kind*.13;vx=Math.Cos(angle)*speed;vy=Math.Sin(angle)*speed*.65;changeIn=random.Next(75,220);}
 void Down(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;held=true;moved=false;origin=Location;mouseDown=Cursor.Position;Capture=true;}
 void MovePet(object s,MouseEventArgs e){if(!held)return;var d=new Point(Cursor.Position.X-mouseDown.X,Cursor.Position.Y-mouseDown.Y);if(Math.Abs(d.X)+Math.Abs(d.Y)>5)moved=true;if(moved){var proposed=new Point(origin.X+d.X,origin.Y+d.Y);var area=Screen.FromPoint(Cursor.Position).WorkingArea;Location=Geometry.Clamp(proposed,Size,area);x=Left;y=Top;}}
 void Up(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left||!held)return;held=false;Capture=false;ClampHere();if(moved)SavePosition();if(suppressRelease){suppressRelease=false;}else if(!moved){reaction=sleeping?0:60;}changeIn=45;Invalidate();}
 void Step(){if(app.StopSignal.WaitOne(0)){app.Quit();return;}app.Claude.Poll();app.Questions.Poll(this);if(!Visible)return;phase+=.12;if(reaction>0)reaction--;if(++displayCheck>=60){displayCheck=0;if(!held)ClampHere();}
  Render();
 }
 protected override void OnShown(EventArgs e){base.OnShown(e);Render();}
 protected override void OnPaint(PaintEventArgs e){Render();}
 public void DrawFrame(Graphics g){
  g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
  Color ink=Color.FromArgb(91,73,59),green=Color.FromArgb(105,151,96),paper=Color.FromArgb(255,253,245);
  float right=Width-5;
  using(var card=new GraphicsPath()){
   card.AddArc(4,5,14,14,180,90);card.AddArc(right-14,5,14,14,270,90);
   card.AddArc(right-14,151,14,14,0,90);card.AddArc(4,151,14,14,90,90);card.CloseFigure();
   using(var fill=new SolidBrush(paper))g.FillPath(fill,card);
   using(var pen=new Pen(ink,1.8f))g.DrawPath(pen,card);
  }
  using(var pen=new Pen(Color.FromArgb(188,203,166),1))g.DrawRectangle(pen,10,11,Width-21,148);
  using(var brush=new SolidBrush(Color.FromArgb(236,241,226)))g.FillEllipse(brush,19,23,34,34);
  PointF[] crown={new PointF(25,43),new PointF(24,32),new PointF(30,36),new PointF(36,29),new PointF(42,36),new PointF(48,32),new PointF(47,43)};
  using(var brush=new SolidBrush(green))g.FillPolygon(brush,crown);
  using(var pen=new Pen(green,1.8f)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLine(pen,27,47,45,47);}
  using(var brush=new SolidBrush(Color.FromArgb(68,94,61)))using(var font=CertificateTypeface.Create(13.5f))g.DrawString("제초 자격증",font,brush,61,20);
  using(var brush=new SolidBrush(Color.FromArgb(139,150,123)))using(var font=new Font("Malgun Gothic",9.5f,FontStyle.Regular))g.DrawString("풀뽑기 검정",font,brush,61,43);
  float badgeX=Width-123;
  using(var badge=new GraphicsPath()){
   badge.AddArc(badgeX,39,8,8,180,90);badge.AddArc(badgeX+32,39,8,8,270,90);
   badge.AddArc(badgeX+32,50,8,8,0,90);badge.AddArc(badgeX,50,8,8,90,90);badge.CloseFigure();
   using(var brush=new SolidBrush(Color.FromArgb(235,241,224)))g.FillPath(brush,badge);
  }
  using(var brush=new SolidBrush(Color.FromArgb(94,124,76)))using(var font=new Font("Malgun Gothic",8.5f,FontStyle.Bold))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
   g.DrawString("5급",font,brush,new RectangleF(badgeX,39,40,19),format);
  var portrait=new Rectangle(Width-67,17,48,42);
  using(var brush=new SolidBrush(Color.FromArgb(226,236,224)))g.FillRectangle(brush,portrait);
  var clip=g.Save();g.SetClip(portrait);g.TranslateTransform(Width-78,15);g.ScaleTransform(.46f,.46f);Art.Draw(g,kind,0,false,false);g.Restore(clip);
  using(var pen=new Pen(ink,1))g.DrawRectangle(pen,portrait);
  using(var pen=new Pen(Color.FromArgb(188,203,166),1))g.DrawLine(pen,19,66,Width-20,66);
  string label=app.Claude.Label;int newline=label.IndexOf('\n');
  string speech=newline<0?"클로드가 작업을 시작하면 여기서 이야기할게요.":label.Substring(newline+1);
  using(var font=new Font("Malgun Gothic",10,FontStyle.Bold))using(var brush=new SolidBrush(ink))using(var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})
   g.DrawString(app.Claude.Title,font,brush,new RectangleF(20,75,Width-40,23),format);
  using(var font=new Font("Malgun Gothic",9.5f))using(var brush=new SolidBrush(Color.FromArgb(137,143,132)))using(var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter})
   g.DrawString(speech,font,brush,new RectangleF(20,102,Width-40,51),format);
  PointF[] tail={new PointF(Width/2f-10,166),new PointF(Width/2f,178),new PointF(Width/2f+8,166)};
  using(var fill=new SolidBrush(paper))g.FillPolygon(fill,tail);
  var saved=g.Save();g.TranslateTransform((Width-app.PetSize)/2f,185+questionHeight);g.ScaleTransform(app.PetSize/160f,app.PetSize/160f);
  Art.Draw(g,kind,(float)phase,sleeping,held||reaction>0||app.Claude.State=="done");g.Restore(saved);
 }
 void Render(){if(!IsHandleCreated||IsDisposed)return;using(var bitmap=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb)){
  using(var g=Graphics.FromImage(bitmap))DrawFrame(g);
  Layered.Present(Handle,bitmap,Location);
 }}
 protected override void Dispose(bool disposing){if(disposing){if(timer!=null)timer.Dispose();if(menu!=null)menu.Dispose();}base.Dispose(disposing);}
}
static class Layered {
 [StructLayout(LayoutKind.Sequential)] struct Pos {public int X,Y;public Pos(int x,int y){X=x;Y=y;}}
 [StructLayout(LayoutKind.Sequential)] struct Extent {public int W,H;public Extent(int w,int h){W=w;H=h;}}
 [StructLayout(LayoutKind.Sequential,Pack=1)] struct Blend {public byte Op,Flags,Alpha,Format;}
 [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr h);
 [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr h,IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dest,ref Pos pos,ref Extent size,IntPtr source,ref Pos origin,int key,ref Blend blend,int flags);
 public static void Present(IntPtr window,Bitmap bitmap,Point location){IntPtr screen=GetDC(IntPtr.Zero),memory=CreateCompatibleDC(screen),image=IntPtr.Zero,old=IntPtr.Zero;
  try{image=bitmap.GetHbitmap(Color.FromArgb(0));old=SelectObject(memory,image);var pos=new Pos(location.X,location.Y);var size=new Extent(bitmap.Width,bitmap.Height);var origin=new Pos(0,0);var blend=new Blend{Op=0,Flags=0,Alpha=255,Format=1};
   if(!UpdateLayeredWindow(window,screen,ref pos,ref size,memory,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
  }finally{if(old!=IntPtr.Zero)SelectObject(memory,old);if(image!=IntPtr.Zero)DeleteObject(image);DeleteDC(memory);ReleaseDC(IntPtr.Zero,screen);}
 }
}
static class Art {
 static Bitmap atlas; static Rectangle[,] sprites=new Rectangle[4,3];
 static Art(){using(var stream=typeof(Art).Assembly.GetManifestResourceStream("characters.png")){if(stream==null)throw new InvalidOperationException("Character image missing");using(var original=new Bitmap(stream))atlas=new Bitmap(original);}
  for(int row=0;row<4;row++)for(int col=0;col<3;col++){
   int[] rowEdges={0,312,610,952,1254};
   int l=col*atlas.Width/3,r=(col+1)*atlas.Width/3,t=rowEdges[row]*atlas.Height/1254,b=rowEdges[row+1]*atlas.Height/1254;
   int minX=r,minY=b,maxX=l,maxY=t;
   for(int y=t;y<b;y++)for(int x=l;x<r;x++)if(atlas.GetPixel(x,y).A>60){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
   sprites[row,col]=Rectangle.FromLTRB(Math.Max(l,minX-2),Math.Max(t,minY-2),Math.Min(r,maxX+3),Math.Min(b,maxY+3));
  }
 }
 public static void Draw(Graphics g,int kind,float phase,bool sleep,bool happy){int state=sleep?2:happy?1:0;var src=sprites[kind,state];float scale=Math.Min(144f/src.Width,140f/src.Height);float w=src.Width*scale,h=src.Height*scale;float bob=0;
  g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
  using(var attributes=new System.Drawing.Imaging.ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(atlas,new Rectangle((int)((160-w)/2),(int)(147-h+bob),(int)w,(int)h),src.X,src.Y,src.Width,src.Height,GraphicsUnit.Pixel,attributes);}
 }
}
static class Tests {
 public static void Run(string file){QuestionTests.Run();NavigationAndCharacterTests.Navigation();int count=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);count++;};
  var size=new Size(160,195);var area=new Rectangle(-1920,0,1920,1040);
  check(Geometry.Clamp(new Point(-2000,-50),size,area)==new Point(-1920,0),"negative monitor origin");
  check(Geometry.Clamp(new Point(0,1040),size,area)==new Point(-160,845),"right bottom edges");
  check(Geometry.Clamp(new Point(-1000,200),size,area)==new Point(-1000,200),"inside stays");
  check(Geometry.Clamp(new Point(50,50),size,new Rectangle(0,0,80,80))==Point.Empty,"screen smaller than pet");
  var rand=new Random(41);for(int i=0;i<5000;i++){var a=new Rectangle(rand.Next(-3000,3000),rand.Next(-2000,2000),rand.Next(240,4000),rand.Next(240,2200));var p=Geometry.Clamp(new Point(rand.Next(-5000,5000),rand.Next(-5000,5000)),size,a);check(p.X>=a.Left&&p.Y>=a.Top&&p.X+size.Width<=a.Right&&p.Y+size.Height<=a.Bottom,"bounds property");}
  using(var b=new Bitmap(160,195))using(var g=Graphics.FromImage(b)){for(int k=0;k<4;k++)for(int s=0;s<3;s++){g.Clear(Color.Magenta);Art.Draw(g,k,1,s==1,s==2);check(b.GetPixel(80,80).ToArgb()!=Color.Magenta.ToArgb(),"character rendered");}}
  var app=new PetApp(false);try{
   var pet=app.Pets[0];QuestionTests.Preview(app,pet);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   Action<string,int> send=(name,clicks)=>typeof(Control).GetMethod(name,flags).Invoke(pet,new object[]{new MouseEventArgs(MouseButtons.Left,clicks,80,80,0)});
   var sleeping=typeof(Pet).GetField("sleeping",flags);
   for(int gesture=0;gesture<2;gesture++){
    send("OnMouseDown",1);send("OnMouseUp",1);send("OnMouseDown",2);send("OnMouseDoubleClick",2);send("OnMouseUp",2);
    check((bool)sleeping.GetValue(pet)==(gesture==0),"double click sleep/wake");
   }
   app.Claude.Title="이번 주 작업 정리";app.Claude.Label="클로드 작업 중\n이번 주 커밋·메모리 기록을 모아서 작업 내용을 정리하고 있어요. / README.md 내용을 살펴보고 있어요.";
   using(var preview=new Bitmap(pet.Width,pet.Height))using(var graphics=Graphics.FromImage(preview)){pet.DrawFrame(graphics);preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"chatty-preview.png"));}
   var fixedLocation=pet.Location;for(int step=0;step<120;step++)typeof(Pet).GetMethod("Step",flags).Invoke(pet,null);check(pet.Location==fixedLocation,"stationary during idle updates");
   NavigationAndCharacterTests.Characters(app,pet);app.FillMenu(new ContextMenuStrip());pet.SetClickThrough(true);pet.SetClickThrough(false);
   app.StopSignal.Set();typeof(Pet).GetMethod("Step",flags).Invoke(pet,null);check(pet.IsDisposed,"graceful shutdown signal closes pet");
  }finally{app.Quit();}
  File.WriteAllText(file,"PASS: "+count+" checks. Geometry (negative monitor, bounds, small screen) and 12 character/state render checks.");
 }
}
}
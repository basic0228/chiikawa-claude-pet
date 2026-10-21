using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace ChiikawaDesktopPet {
public class QuestionOption {public string label{get;set;} public string description{get;set;}}
public class PetQuestion {public string question{get;set;} public string header{get;set;} public QuestionOption[] options{get;set;} public bool multiSelect{get;set;}}
public class QuestionRequest {public string id{get;set;} public string sessionId{get;set;} public string title{get;set;} public string expiresAt{get;set;} public PetQuestion[] questions{get;set;}}
class QuestionController:IDisposable {
 readonly PetApp app;QuestionPanel panel;DateTime next=DateTime.MinValue;string activePath;
 public QuestionController(PetApp owner){app=owner;}
 public void Poll(Pet pet){
  if(panel!=null){
   if(!File.Exists(activePath)||panel.Expired){ClosePanel(pet);}
   else {Sync(pet);app.Claude.Title=panel.Request.title;app.Claude.Label="입력을 기다려요\n아래 질문에 답하면 작업을 이어갈게요.";app.Claude.State="waiting";}
  }
  if(DateTime.UtcNow<next||panel!=null)return;next=DateTime.UtcNow.AddMilliseconds(500);
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"questions");if(!Directory.Exists(dir))return;
  foreach(string path in Directory.GetFiles(dir,"*.request.json").OrderBy(File.GetCreationTimeUtc)){
   if(File.Exists(path.Replace(".request.json",".answer.json")))continue;
   try{
    var request=new JavaScriptSerializer().Deserialize<QuestionRequest>(File.ReadAllText(path));DateTime deadline;
    if(request==null||request.questions==null||request.questions.Length<1||request.questions.Length>4||!GuidId(request.id)||Path.GetFileName(path)!=request.id+".request.json")continue;
    if(!DateTime.TryParse(request.expiresAt,null,System.Globalization.DateTimeStyles.RoundtripKind,out deadline)||deadline.ToUniversalTime()<DateTime.UtcNow)continue;
    if(request.questions.Any(q=>q==null||String.IsNullOrWhiteSpace(q.question)))continue;
    activePath=path;panel=new QuestionPanel(request,path);panel.Finished+=()=>ClosePanel(pet);
    pet.SetQuestionHeight(panel.Height+8);Sync(pet);if(pet.Visible)panel.Show(pet);return;
   }catch(IOException){}catch(ArgumentException){}catch(InvalidOperationException){}
  }
 }
 public static bool GuidId(string id){Guid parsed;return id!=null&&id.Length==32&&Guid.TryParseExact(id,"N",out parsed);}
 public void SavePreview(Pet pet,string path){if(panel==null)throw new InvalidOperationException("No question panel");panel.PrepareLayout();using(var bitmap=new Bitmap(pet.Width,pet.Height))using(var g=Graphics.FromImage(bitmap)){pet.DrawFrame(g);using(var question=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(question,panel.ClientRectangle);g.DrawImageUnscaled(question,4,172);}bitmap.Save(path);}}
 void Sync(Pet pet){if(panel==null)return;pet.SetQuestionHeight(panel.Height+8);panel.Location=new Point(pet.Left+4,pet.Top+172);if(!pet.Visible)panel.Hide();else if(!panel.Visible)panel.Show(pet);}
 void ClosePanel(Pet pet){var old=panel;panel=null;if(old!=null)old.Dispose();activePath=null;if(!pet.IsDisposed)pet.SetQuestionHeight(0);}
 public void Dispose(){if(panel!=null){panel.Cancel();panel.Dispose();panel=null;}}
}
class QuestionPanel:Form {
 public readonly QuestionRequest Request;readonly string requestPath;readonly Dictionary<string,string> answers=new Dictionary<string,string>();
 readonly Dictionary<string,string[]> picks=new Dictionary<string,string[]>();readonly Dictionary<string,string> typed=new Dictionary<string,string>();readonly HashSet<string> otherQuestions=new HashSet<string>();
 public event Action Finished;int index;bool answered;FlowLayoutPanel content;List<Tuple<CheckBox,string>> checks;List<Tuple<RadioButton,string>> radios;CheckBox otherCheck;RadioButton otherRadio;TextBox otherText;Label validation;Button back,next;
 public string CurrentQuestion{get{return Request.questions[index].question;}}
 public bool Expired{get{DateTime date;return !DateTime.TryParse(Request.expiresAt,out date)||date.ToUniversalTime()<DateTime.UtcNow;}}
 protected override bool ShowWithoutActivation{get{return true;}}
 public QuestionPanel(QuestionRequest request,string path){
  Request=request;requestPath=path;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;
  BackColor=Color.FromArgb(255,253,245);Font=new Font("Malgun Gothic",9);Width=312;
  Paint+=(s,e)=>{using(var pen=new Pen(Color.FromArgb(188,203,166),1))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);};
  Build();FormClosing+=(s,e)=>{if(!answered)Cancel();};
  Shown+=(s,e)=>PrepareLayout();
 }
 public void PrepareLayout(){CreateControl();content.CreateControl();content.PerformLayout();foreach(Control control in content.Controls){control.CreateControl();control.Update();}PerformLayout();Refresh();}
 public static IEnumerable<Control> AllControls(Control root){foreach(Control child in root.Controls){yield return child;foreach(Control nested in AllControls(child))yield return nested;}}
 Label TextLabel(string text,int width,Font font,Color color){return new Label{Text=text,Width=width,AutoSize=false,Height=TextRenderer.MeasureText(text,font,new Size(width,1000),TextFormatFlags.WordBreak).Height+5,Font=font,ForeColor=color,Margin=new Padding(0,2,0,9)};}
 Button Button(string text,int width){return new SoftButton{Text=text,Width=width,Height=39,Primary=text!="이전",Margin=new Padding(0,1,8,0)};}
 void Build(){
  if(content!=null)content.Dispose();checks=new List<Tuple<CheckBox,string>>();radios=new List<Tuple<RadioButton,string>>();
  content=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(16,15,10,13),BackColor=Color.FromArgb(255,253,245)};Controls.Add(content);
  content.Paint+=(s,e)=>{e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var outline=SoftTheme.Round(new RectangleF(1,1,Width-3,Height-3),14))using(var pen=new Pen(Color.FromArgb(193,205,171),1.3f))e.Graphics.DrawPath(pen,outline);};
  var q=Request.questions[index];content.Controls.Add(new QuestionHeading{Width=280,Height=28,Counter=(index+1)+" / "+Request.questions.Length,Margin=new Padding(0,0,0,6)});
  content.Controls.Add(TextLabel(q.question,280,new Font(Font.FontFamily,10,FontStyle.Bold),Color.FromArgb(95,78,62)));
  string previous;answers.TryGetValue(q.question,out previous);
  foreach(var option in q.options??new QuestionOption[0]){
   if(option==null||String.IsNullOrWhiteSpace(option.label))continue;
   var label=option.label;int height=SoftTheme.ChoiceHeight(label,option.description);
   if(q.multiSelect){var check=new SoftCheck{Text=label,Description=option.description,Width=280,Height=height,Margin=new Padding(0,0,0,7)};content.Controls.Add(check);checks.Add(Tuple.Create((CheckBox)check,label));if(picks.ContainsKey(q.question)&&picks[q.question].Contains(label))check.Checked=true;}
   else {var radio=new SoftRadio{Text=label,Description=option.description,Width=280,Height=height,Margin=new Padding(0,0,0,7)};content.Controls.Add(radio);radios.Add(Tuple.Create((RadioButton)radio,label));if(picks.ContainsKey(q.question)&&picks[q.question].Contains(label))radio.Checked=true;}
  }
  if(q.multiSelect){otherCheck=new SoftCheck{Text="기타 · 직접 입력",Width=280,Height=40,IsOther=true,Margin=new Padding(0,0,0,7)};otherRadio=null;content.Controls.Add(otherCheck);}
  else {otherRadio=new SoftRadio{Text="기타 · 직접 입력",Width=280,Height=40,IsOther=true,Margin=new Padding(0,0,0,7)};otherCheck=null;content.Controls.Add(otherRadio);}
  var inputCard=new SoftInput{Width=280,Height=67,Margin=new Padding(0,0,0,0)};
  otherText=new TextBox{Multiline=true,Location=new Point(12,10),Width=256,Height=47,MaxLength=4000,Enabled=false,ScrollBars=ScrollBars.None,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(248,248,237),ForeColor=Color.FromArgb(95,78,62),Font=new Font(Font.FontFamily,9.2f)};
  inputCard.Controls.Add(otherText);inputCard.Input=otherText;content.Controls.Add(inputCard);
  Action toggleInput=()=>{bool selected=otherCheck!=null?otherCheck.Checked:otherRadio.Checked;otherText.Enabled=selected;otherText.BackColor=selected?Color.FromArgb(255,254,249):Color.FromArgb(248,248,237);inputCard.Invalidate();if(selected)otherText.Focus();};
  if(otherCheck!=null)otherCheck.CheckedChanged+=(s,e)=>toggleInput();
  if(otherRadio!=null)otherRadio.CheckedChanged+=(s,e)=>toggleInput();
  if(typed.ContainsKey(q.question))otherText.Text=typed[q.question];
  if(otherQuestions.Contains(q.question)){if(otherCheck!=null)otherCheck.Checked=true;else otherRadio.Checked=true;}
  validation=TextLabel("",280,Font,Color.FromArgb(181,108,91));validation.Height=19;validation.Margin=new Padding(0,2,0,2);content.Controls.Add(validation);
  otherText.TextChanged+=(s,e)=>validation.Text="";
  foreach(var radio in radios)radio.Item1.CheckedChanged+=(s,e)=>validation.Text="";
  foreach(var check in checks)check.Item1.CheckedChanged+=(s,e)=>validation.Text="";
  var actions=new FlowLayoutPanel{Width=288,Height=42,Margin=new Padding(0)};
  back=Button("이전",65);back.Enabled=index>0;back.Click+=(s,e)=>{SaveDraft();index--;Build();};if(index>0)actions.Controls.Add(back);
  next=Button(index==Request.questions.Length-1?"답변 보내기":"다음",index>0?207:280);next.Click+=(s,e)=>SubmitCurrent();actions.Controls.Add(next);content.Controls.Add(actions);
  var fallback=new LinkLabel{Text="클로드 창에서 답할게요",Width=280,Height=24,TextAlign=ContentAlignment.MiddleCenter,LinkColor=Color.FromArgb(145,151,128),ActiveLinkColor=Color.FromArgb(102,130,84),LinkBehavior=LinkBehavior.HoverUnderline,Margin=new Padding(0,7,0,0)};fallback.LinkClicked+=(s,e)=>{Cancel();if(Finished!=null)Finished();};content.Controls.Add(fallback);
  int preferred=content.Controls.Cast<Control>().Sum(c=>c.Height+c.Margin.Vertical)+content.Padding.Vertical;
  Height=Math.Min(Math.Max(310,preferred+6),Math.Max(310,Screen.PrimaryScreen.WorkingArea.Height-410));
  using(var shape=SoftTheme.Round(new RectangleF(0,0,Width,Height),15)){var old=Region;Region=new Region(shape);if(old!=null)old.Dispose();}
 }
 void SaveDraft(){picks[CurrentQuestion]=checks.Where(c=>c.Item1.Checked).Select(c=>c.Item2).Concat(radios.Where(c=>c.Item1.Checked).Select(c=>c.Item2)).ToArray();typed[CurrentQuestion]=otherText.Text;bool other=otherCheck!=null?otherCheck.Checked:otherRadio.Checked;if(other)otherQuestions.Add(CurrentQuestion);else otherQuestions.Remove(CurrentQuestion);}
 public void SubmitCurrent(){
  validation.Text="";
  string answer="";bool other=otherCheck!=null?otherCheck.Checked:otherRadio.Checked;
  var selected=checks.Where(c=>c.Item1.Checked).Select(c=>c.Item2).ToList();selected.AddRange(radios.Where(c=>c.Item1.Checked).Select(c=>c.Item2));
  var selectedLabels=selected.ToArray();
  if(other){if(String.IsNullOrWhiteSpace(otherText.Text)){validation.Text="기타 답변을 입력해 주세요.";return;}selected.Add(otherText.Text.Trim());}
  answer=String.Join(", ",selected);if(answer.Length==0){validation.Text="선택지를 고르거나 직접 입력해 주세요.";return;}
  answers[CurrentQuestion]=answer;
  SaveDraft();
  if(index<Request.questions.Length-1){index++;Build();return;}
  try{WriteResponse(false);}catch(IOException){validation.Text="전송하지 못했어요. 다시 눌러 주세요.";return;}catch(UnauthorizedAccessException){validation.Text="폴더 쓰기 권한을 확인해 주세요.";return;}answered=true;if(Finished!=null)Finished();
 }
 void WriteResponse(bool cancelled){
  string output=requestPath.Replace(".request.json",".answer.json");if(File.Exists(output))return;
  string temp=output+".tmp";var response=new {id=Request.id,cancelled=cancelled,answers=answers};
  File.WriteAllText(temp,new JavaScriptSerializer().Serialize(response),new System.Text.UTF8Encoding(false));File.Move(temp,output);
 }
 public void Cancel(){if(answered)return;try{WriteResponse(true);}catch(IOException){}catch(UnauthorizedAccessException){}answered=true;}
}
static class QuestionTests {
 public static void Preview(PetApp app,Pet pet){
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"questions");Directory.CreateDirectory(dir);string id=Guid.NewGuid().ToString("N"),path=Path.Combine(dir,id+".request.json");
  var request=new QuestionRequest{id=id,title="와이스 라이브 상품 탭 분석",expiresAt=DateTime.UtcNow.AddMinutes(3).ToString("o"),questions=new[]{new PetQuestion{question="어떤 방식으로 진행할까요?",options=new[]{new QuestionOption{label="추천 방식",description="기존 구성을 유지하며 다듬어요."},new QuestionOption{label="새 구성",description="새로운 배치로 바꿔요."}}}}};
  File.WriteAllText(path,new JavaScriptSerializer().Serialize(request));try{app.Questions.Poll(pet);Application.DoEvents();app.Questions.SavePreview(pet,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"interactive-preview.png"));}finally{File.Delete(path);app.Questions.Poll(pet);}
 }
 public static void Run(){
  string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"question-tests");Directory.CreateDirectory(dir);
  Func<string,bool,QuestionRequest> request=(id,multi)=>new QuestionRequest{id=id,title="상품 탭 분석",expiresAt=DateTime.UtcNow.AddMinutes(5).ToString("o"),questions=new[]{new PetQuestion{question="어떤 방식으로 진행할까요?",header="진행 방식",multiSelect=multi,options=new[]{new QuestionOption{label="추천 방식",description="기존 구성을 유지해요."},new QuestionOption{label="새 구성",description="새로운 배치를 사용해요."}}}}};
  Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("Question test: "+name);};
  for(int scenario=0;scenario<4;scenario++){
   string id=Guid.NewGuid().ToString("N"),path=Path.Combine(dir,id+".request.json"),answer=path.Replace(".request.json",".answer.json");
   var req=request(id,scenario==2);using(var form=new QuestionPanel(req,path)){
    var controls=QuestionPanel.AllControls(form).ToArray();
    form.SubmitCurrent();check(!File.Exists(answer),"empty answer cannot submit");
    if(scenario==0){controls.OfType<RadioButton>().First(r=>r.Text=="추천 방식").Checked=true;form.SubmitCurrent();}
    if(scenario==1){controls.OfType<RadioButton>().First(r=>r.Text.StartsWith("기타")).Checked=true;var box=controls.OfType<TextBox>().Single();check(box.Enabled,"other input enabled");form.SubmitCurrent();check(!File.Exists(answer),"blank other cannot submit");box.Text="직접 정한 답변";form.SubmitCurrent();}
    if(scenario==2){foreach(var c in controls.OfType<CheckBox>().Where(c=>!c.Text.StartsWith("기타")))c.Checked=true;form.SubmitCurrent();}
    if(scenario==3)form.Cancel();
    check(File.Exists(answer),"response file exists");var value=File.ReadAllText(answer);
    check(scenario!=0||value.Contains("추천 방식"),"single selection");check(scenario!=1||value.Contains("직접 정한 답변"),"free text");check(scenario!=2||value.Contains("추천 방식, 새 구성"),"multi selection");check(scenario!=3||value.Contains("\"cancelled\":true"),"fallback cancellation");
    if(scenario==1){form.Show();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,form.ClientRectangle);bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"questions-preview.png"));}form.Hide();}
   }
   File.Delete(answer);
  }
  check(QuestionController.GuidId(Guid.NewGuid().ToString("N")),"valid id");check(!QuestionController.GuidId("../invalid"),"invalid id");
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"question-test-results.txt"),"PASS: single choice, multi choice, Other text, empty validation, cancellation and request ID checks.");
 }
}
}

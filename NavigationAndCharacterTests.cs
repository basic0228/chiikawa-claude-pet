using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;
namespace ChiikawaDesktopPet {
static class NavigationAndCharacterTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception("Navigation/character test: "+name);}
 static IEnumerable<Control> Controls(QuestionPanel panel){return QuestionPanel.AllControls(panel);}
 static void Select(QuestionPanel panel,string label){Controls(panel).OfType<RadioButton>().Single(c=>c.Text==label).Checked=true;}
 static void Click(QuestionPanel panel,string text){Controls(panel).OfType<Button>().Single(c=>c.Text==text).PerformClick();Application.DoEvents();}
 static TextBox Input(QuestionPanel panel){return Controls(panel).OfType<TextBox>().Single();}
 static bool Selected(QuestionPanel panel,string label){return Controls(panel).OfType<RadioButton>().Single(c=>c.Text==label).Checked;}
 public static void Navigation(){
  string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"question-tests");Directory.CreateDirectory(folder);
  for(int scenario=0;scenario<5;scenario++){
   string id=Guid.NewGuid().ToString("N"),path=Path.Combine(folder,id+".request.json");
   var questions=new[]{new PetQuestion{question="첫 번째 질문",multiSelect=scenario==2,options=new[]{new QuestionOption{label="첫 답 A"},new QuestionOption{label="첫 답 B"}}},new PetQuestion{question="두 번째 질문",options=new[]{new QuestionOption{label="둘째 답 A"},new QuestionOption{label="둘째 답 B"}}}};
   var request=new QuestionRequest{id=id,title="이전 버튼 상태 유지 테스트",expiresAt=DateTime.UtcNow.AddMinutes(5).ToString("o"),questions=questions};
   using(var panel=new QuestionPanel(request,path)){
    panel.Show();Application.DoEvents();
    if(scenario==1){Select(panel,"기타 · 직접 입력");Input(panel).Text="첫 질문의 한글 답변\r\n두 번째 줄!";}
    else if(scenario==2){foreach(var c in Controls(panel).OfType<CheckBox>())c.Checked=true;Input(panel).Text="복수 선택과 기타 답변";}
    else Select(panel,"첫 답 B");
    Click(panel,"다음");
    if(scenario==3||scenario==4){Select(panel,"기타 · 직접 입력");Input(panel).Text="아직 보내지 않은 입력\r\n한글과 공백 123!";if(scenario==4)Select(panel,"둘째 답 B");}
    else Select(panel,"둘째 답 B");
    Click(panel,"이전");
    if(scenario==1){Check(Selected(panel,"기타 · 직접 입력")&&Input(panel).Enabled,"Other selection restored");Check(Input(panel).Text=="첫 질문의 한글 답변\r\n두 번째 줄!","submitted Other text restored");}
    else if(scenario==2){Check(Controls(panel).OfType<CheckBox>().All(c=>c.Checked),"multi selections restored");Check(Input(panel).Text=="복수 선택과 기타 답변","multi Other text restored");}
    else Check(Selected(panel,"첫 답 B"),"single selection restored");
    Click(panel,"다음");
    if(scenario==3){Check(Selected(panel,"기타 · 직접 입력")&&Input(panel).Enabled,"unsent Other selection restored");Check(Input(panel).Text=="아직 보내지 않은 입력\r\n한글과 공백 123!","unsent text restored");}
    else if(scenario==4){Check(Selected(panel,"둘째 답 B")&&!Input(panel).Enabled,"standard choice preserved after switching from Other");Check(Input(panel).Text=="아직 보내지 않은 입력\r\n한글과 공백 123!","inactive Other draft retained");Select(panel,"기타 · 직접 입력");Check(Input(panel).Enabled,"Other can be re-enabled with text intact");}
    else Check(Selected(panel,"둘째 답 B"),"unsent standard selection restored");
    Click(panel,"답변 보내기");string output=path.Replace(".request.json",".answer.json");
    var response=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(output));var answers=(Dictionary<string,object>)response["answers"];
    Check(answers.Count==2,"both answers delivered");
    if(scenario==1)Check((string)answers["첫 번째 질문"]=="첫 질문의 한글 답변\r\n두 번째 줄!","final output retains multiline first answer");
    if(scenario==2)Check((string)answers["첫 번째 질문"]=="첫 답 A, 첫 답 B, 복수 선택과 기타 답변","final output retains multi and Other answers");
    if(scenario==3||scenario==4)Check((string)answers["두 번째 질문"]=="아직 보내지 않은 입력\r\n한글과 공백 123!","final output includes restored draft");
    File.Delete(output);
   }
  }
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"navigation-test-results.txt"),"PASS: five round-trip scenarios: single, Other multiline, multi-select+Other, unsent draft, inactive Other draft; final answers verified.");
 }
 public static void Characters(PetApp app,Pet pet){
  int original=app.SelectedCharacter;Point position=pet.Location;string settings=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.txt");
  using(var preview=new Bitmap(pet.Width*4,pet.Height))using(var g=Graphics.FromImage(preview)){
   g.Clear(Color.FromArgb(247,246,237));
   for(int i=0;i<PetApp.Names.Length;i++){
    using(var menu=new ContextMenuStrip()){
     app.FillMenu(menu);var choices=menu.Items.OfType<ToolStripMenuItem>().Single(c=>c.Text=="캐릭터 선택");
     choices.DropDownItems.OfType<ToolStripMenuItem>().Single(c=>c.Text==PetApp.Names[i]).PerformClick();
    }
    Check(pet.CharacterIndex==i&&pet.Text==PetApp.Names[i],"character changed via menu");Check(pet.Location==position,"switch preserves position");
    Check(File.ReadAllLines(settings)[1]==i.ToString(),"choice persisted");
    app.SelectedCharacter=-1;typeof(PetApp).GetMethod("Load",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);Check(app.SelectedCharacter==i,"saved choice reloads");
    using(var cell=new Bitmap(pet.Width,pet.Height))using(var cellGraphics=Graphics.FromImage(cell)){pet.DrawFrame(cellGraphics);g.DrawImageUnscaled(cell,i*pet.Width,0);}
    using(var font=new Font("Malgun Gothic",10,FontStyle.Bold))g.DrawString(PetApp.Names[i],font,Brushes.SaddleBrown,i*pet.Width+12,pet.Height-20);
   }
   preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"characters-preview.png"));
  }
  app.SelectCharacter(original);
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"character-test-results.txt"),"PASS: all four menu selections, character/window title updates, position preservation, persistence and reload.");
 }
}
}

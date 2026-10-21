using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ChiikawaDesktopPet {
static class SoftTheme {
 public static Color Paper=Color.FromArgb(255,253,245),Green=Color.FromArgb(116,145,92),Ink=Color.FromArgb(95,78,62);
 public static GraphicsPath Round(RectangleF r,float radius){var path=new GraphicsPath();float d=radius*2;path.AddArc(r.Left,r.Top,d,d,180,90);path.AddArc(r.Right-d,r.Top,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.Left,r.Bottom-d,d,d,90,90);path.CloseFigure();return path;}
 public static int ChoiceHeight(string label,string description){using(var title=new Font("Malgun Gothic",9.2f,FontStyle.Bold))using(var note=new Font("Malgun Gothic",8.3f)){int h=TextRenderer.MeasureText(label,title,new Size(225,1000),TextFormatFlags.WordBreak).Height;int detail=String.IsNullOrEmpty(description)?0:TextRenderer.MeasureText(description,note,new Size(225,1000),TextFormatFlags.WordBreak).Height+5;return Math.Max(48,h+detail+22);}}
 public static void Choice(Graphics g,Control control,bool selected,bool hover,string description,bool multiple,bool other){
  g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
  var bounds=new RectangleF(1,1,control.Width-3,control.Height-3);
  using(var shape=Round(bounds,10)){
   Color fill=selected?(other?Color.FromArgb(255,241,226):Color.FromArgb(237,244,223)):(hover?Color.FromArgb(248,249,237):Color.FromArgb(255,254,249));
   using(var brush=new SolidBrush(fill))g.FillPath(brush,shape);using(var pen=new Pen(selected?(other?Color.FromArgb(205,166,124):Green):Color.FromArgb(225,222,203),selected?1.5f:1))g.DrawPath(pen,shape);
  }
  float cy=control.Height/2f;var mark=new RectangleF(13,cy-7,14,14);
  using(var pen=new Pen(selected?Green:Color.FromArgb(185,192,167),1.4f)){
   if(multiple){using(var shape=Round(mark,3)){g.FillPath(Brushes.White,shape);g.DrawPath(pen,shape);}}else{g.FillEllipse(Brushes.White,mark);g.DrawEllipse(pen,mark);}
  }
  if(selected){if(multiple){using(var pen=new Pen(Green,2)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLines(pen,new[]{new PointF(16,cy),new PointF(19,cy+3),new PointF(24,cy-3)});}}else{using(var brush=new SolidBrush(Green))g.FillEllipse(brush,17,cy-3,6,6);}}
  using(var title=new Font("Malgun Gothic",9.2f,FontStyle.Bold))using(var brush=new SolidBrush(Ink)){
   int h=TextRenderer.MeasureText(control.Text,title,new Size(225,1000),TextFormatFlags.WordBreak).Height;
   float y=String.IsNullOrEmpty(description)?(control.Height-h)/2f:10;
   g.DrawString(control.Text,title,brush,new RectangleF(39,y,control.Width-51,h+4));
   if(!String.IsNullOrEmpty(description))using(var note=new Font("Malgun Gothic",8.3f))using(var muted=new SolidBrush(Color.FromArgb(151,155,137)))g.DrawString(description,note,muted,new RectangleF(39,13+h,control.Width-51,control.Height-h-17));
  }
  if(control.Focused){using(var pen=new Pen(Color.FromArgb(173,188,145),1)){pen.DashStyle=DashStyle.Dot;using(var shape=Round(new RectangleF(4,4,control.Width-9,control.Height-9),8))g.DrawPath(pen,shape);}}
 }
}
class SoftRadio:RadioButton {
 public string Description;public bool IsOther;bool hover;
 public SoftRadio(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AutoSize=false;Cursor=Cursors.Hand;BackColor=SoftTheme.Paper;}
 protected override void OnPaint(PaintEventArgs e){SoftTheme.Choice(e.Graphics,this,Checked,hover,Description,false,IsOther);}
 protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
}
class SoftCheck:CheckBox {
 public string Description;public bool IsOther;bool hover;
 public SoftCheck(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AutoSize=false;Cursor=Cursors.Hand;BackColor=SoftTheme.Paper;}
 protected override void OnPaint(PaintEventArgs e){SoftTheme.Choice(e.Graphics,this,Checked,hover,Description,true,IsOther);}
 protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
}
class SoftButton:Button {
 public bool Primary;bool hover,pressed;
 public SoftButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;BackColor=SoftTheme.Paper;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
  Color fill=Primary?(pressed?Color.FromArgb(100,128,79):hover?Color.FromArgb(122,150,99):Color.FromArgb(139,164,112)):Color.FromArgb(241,241,226);
  using(var shape=SoftTheme.Round(new RectangleF(1,1,Width-3,Height-3),11))using(var brush=new SolidBrush(fill))g.FillPath(brush,shape);
  using(var font=new Font("Malgun Gothic",9.3f,FontStyle.Bold))using(var brush=new SolidBrush(Primary?Color.FromArgb(255,255,245):Color.FromArgb(125,136,105)))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(Text,font,brush,new RectangleF(0,0,Width,Height-1),format);
  if(Primary&&Width>130)using(var pen=new Pen(Color.FromArgb(240,247,222),1.4f)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;float x=Width-28,y=Height/2f;g.DrawLines(pen,new[]{new PointF(x-4,y-4),new PointF(x+3,y),new PointF(x-4,y+4)});}
  if(Focused){using(var pen=new Pen(Color.FromArgb(224,234,203),1)){pen.DashStyle=DashStyle.Dot;using(var shape=SoftTheme.Round(new RectangleF(4,4,Width-9,Height-9),8))g.DrawPath(pen,shape);}}
 }
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
}
class SoftInput:Panel {
 public TextBox Input;
 public SoftInput(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=SoftTheme.Paper;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;bool active=Input!=null&&Input.Enabled;using(var shape=SoftTheme.Round(new RectangleF(1,1,Width-3,Height-3),10)){using(var brush=new SolidBrush(active?Color.FromArgb(255,254,249):Color.FromArgb(248,248,237)))g.FillPath(brush,shape);using(var pen=new Pen(active?Color.FromArgb(205,174,137):Color.FromArgb(226,225,207),1))g.DrawPath(pen,shape);}}
}
class QuestionHeading:Control {
 public string Counter;
 public QuestionHeading(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=SoftTheme.Paper;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
  using(var fill=new SolidBrush(Color.FromArgb(235,242,222)))using(var shape=SoftTheme.Round(new RectangleF(0,2,118,23),10))g.FillPath(fill,shape);
  using(var pen=new Pen(SoftTheme.Green,1.5f)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLine(pen,13,18,15,10);g.DrawArc(pen,9,8,8,7,200,210);}
  using(var font=new Font("Malgun Gothic",8.4f,FontStyle.Bold))using(var brush=new SolidBrush(Color.FromArgb(107,132,85)))g.DrawString("골라 주세요",font,brush,26,5);
  using(var font=new Font("Malgun Gothic",8.2f))using(var brush=new SolidBrush(Color.FromArgb(159,166,142)))using(var format=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center})g.DrawString(Counter,font,brush,new RectangleF(200,1,78,25),format);
 }
}
}

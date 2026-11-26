using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal static class DrawingTheme {
    public static Color Rgb(int hex) { return Color.FromArgb((hex>>16)&255,(hex>>8)&255,hex&255); }

    private static GraphicsPath Round(RectangleF r,float radius) {
        var p = new GraphicsPath();
        float d = radius*2;
        p.AddArc(r.X,r.Y,d,d,180,90);
        p.AddArc(r.Right-d,r.Y,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);
        p.AddArc(r.X,r.Bottom-d,d,d,90,90);
        p.CloseFigure();
        return p;
    }

    public static void Shape(Graphics g,RectangleF rect,float radius,Color top,Color bottom,Color border) {
        if (rect.Width<2 || rect.Height<2) return;
        radius = Math.Min(radius,Math.Min(rect.Width,rect.Height)/2);
        using (var p=Round(rect,radius))
        using (var b=new LinearGradientBrush(rect,top,bottom,LinearGradientMode.Vertical)) {
            g.FillPath(b,p);
            if (border.A>0)
                using (var pen=new Pen(border)) g.DrawPath(pen,p);
        }
    }
}

internal class PaintedPanel : Panel {
    protected PaintedPanel() { DoubleBuffered = true; }
    protected override void OnPaintBackground(PaintEventArgs e) {
        base.OnPaintBackground(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        PaintSurface(e.Graphics);
    }
    protected virtual void PaintSurface(Graphics g) { }
}

internal sealed class Surface : PaintedPanel {
    private readonly bool green;
    public Surface(bool mint) { green=mint; }
    protected override void PaintSurface(Graphics g) {
        var outer = new RectangleF(2,5,Width-5,Height-7);
        var rect = new RectangleF(1,1,Width-4,Height-7);
        DrawingTheme.Shape(g,outer,17,Color.FromArgb(18,72,57,107),Color.FromArgb(32,72,57,107),Color.Transparent);
        DrawingTheme.Shape(g,rect,17,Color.White,DrawingTheme.Rgb(green?0xF2FBF8:0xFCFAFF),DrawingTheme.Rgb(green?0xCFE9E0:0xE9E1F5));
    }
}

internal sealed class Badge : Label {
    private readonly bool mint;
    public Badge(string value,bool green) {
        Text=value; mint=green;
        BackColor=Color.Transparent;
        Font=new Font("Segoe UI",11,FontStyle.Bold);
    }
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        var rectangle = new Rectangle(1,1,Width-3,Height-4);
        DrawingTheme.Shape(e.Graphics,rectangle,10,DrawingTheme.Rgb(mint?0xCFF5E6:0xEEE5FF),
            DrawingTheme.Rgb(mint?0xB3E9D6:0xDFD0FB),DrawingTheme.Rgb(mint?0xB5E4D4:0xD8C8F3));
        TextRenderer.DrawText(e.Graphics,Text,Font,rectangle,DrawingTheme.Rgb(mint?0x225B4E:0x523395),
            TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
    }
}

internal sealed class FieldSurface : PaintedPanel {
    public static readonly Color Fill=DrawingTheme.Rgb(0xF6F4FB);
    public FieldSurface() { BackColor=Color.Transparent; }
    protected override void PaintSurface(Graphics g) {
        DrawingTheme.Shape(g,new RectangleF(1,1,Width-3,Height-3),8,Fill,Fill,
            DrawingTheme.Rgb(ContainsFocus?0x9171DB:0xD8CEE8));
    }
}

internal sealed class Hero : PaintedPanel {
    protected override void PaintSurface(Graphics g) {
        var r = new RectangleF(1,1,Width-4,Height-5);
        DrawingTheme.Shape(g,r,22,DrawingTheme.Rgb(0xEEE7FF),DrawingTheme.Rgb(0xDED1FA),DrawingTheme.Rgb(0xD9CBF0));
        var state=g.Save();
        float scale=Height/125f;
        g.TranslateTransform(Width-105*scale,Height/2f);
        g.ScaleTransform(scale,scale);
        using (var disk=new SolidBrush(Color.FromArgb(80,255,255,255))) g.FillEllipse(disk,-58,-49,116,98);
        using (var ring=new Pen(Color.FromArgb(145,255,255,255),1.3f)) g.DrawEllipse(ring,-69,-55,138,111);
        g.RotateTransform(12);
        DrawingTheme.Shape(g,new RectangleF(-19,-50,39,102),14,DrawingTheme.Rgb(0xB9F1DF),DrawingTheme.Rgb(0x8AD5C0),DrawingTheme.Rgb(0x83C9B8));
        DrawingTheme.Shape(g,new RectangleF(24,-11,13,23),4,DrawingTheme.Rgb(0xAA90EC),DrawingTheme.Rgb(0x805BC8),DrawingTheme.Rgb(0x9D7EDF));
        DrawingTheme.Shape(g,new RectangleF(-29,-30,59,62),26,DrawingTheme.Rgb(0xA285E5),DrawingTheme.Rgb(0x6C4BB6),DrawingTheme.Rgb(0x7957BE));
        DrawingTheme.Shape(g,new RectangleF(-24,-26,49,49),23,DrawingTheme.Rgb(0x554073),DrawingTheme.Rgb(0x34265C),DrawingTheme.Rgb(0xBD9EF9));
        using (var b=new SolidBrush(DrawingTheme.Rgb(0xFFF1BF))) g.FillEllipse(b,-10,4,12,8);
        using (var p=new Pen(DrawingTheme.Rgb(0xFFF1BF),3) { StartCap=LineCap.Round, EndCap=LineCap.Round }) {
            g.DrawLine(p,0,7,0,-11);
            g.DrawLine(p,0,-11,12,-7);
        }
        using (var b=new SolidBrush(DrawingTheme.Rgb(0x9D79DB))) {
            g.FillEllipse(b,-72,-9,6,6);
            g.FillEllipse(b,59,17,5,5);
        }
        using (var p=new Pen(Color.White,2.4f) { StartCap=LineCap.Round, EndCap=LineCap.Round }) {
            g.DrawLine(p,51,-34,64,-34); g.DrawLine(p,57,-40,57,-28);
            g.DrawLine(p,-58,24,-50,24); g.DrawLine(p,-54,20,-54,28);
        }
        g.Restore(state);
    }
}

internal sealed class RaisedButton : Button {
    private readonly bool primary;
    private bool hovering;
    private bool pushed;
    public RaisedButton(bool strong) {
        primary=strong;
        SetStyle(ControlStyles.SupportsTransparentBackColor,true);
        BackColor=Color.Transparent;
        FlatStyle=FlatStyle.Flat;
        FlatAppearance.BorderSize=0;
        Cursor=Cursors.Hand;
        DoubleBuffered=true;
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovering=true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovering=false; pushed=false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if(e.Button==MouseButtons.Left) pushed=true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pushed=false; Invalidate(); }
    protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if(e.KeyCode==Keys.Space) pushed=true; Invalidate(); }
    protected override void OnKeyUp(KeyEventArgs e) { base.OnKeyUp(e); pushed=false; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;

        int shift=pushed?3:0;
        var face=new Rectangle(1,1+shift,Width-4,Height-7);
        var foot=new RectangleF(2,7,Width-4,Height-9);
        Color top=DrawingTheme.Rgb(!Enabled?0xF0EDF4:primary?(hovering?0x8C6BD9:0x7955C1):0xFFFFFF);
        Color bottom=DrawingTheme.Rgb(!Enabled?0xEAE5F1:primary?(hovering?0x6441B5:0x57359E):hovering?0xE9DDFA:0xF0E8FB);
        Color edge=DrawingTheme.Rgb(!Enabled?0xDFD7E8:primary?0x6748A4:0xDACCEB);
        DrawingTheme.Shape(e.Graphics,foot,11,DrawingTheme.Rgb(!Enabled?0xDED7E8:primary?0x4C2A86:0xB9A2D6),
            DrawingTheme.Rgb(!Enabled?0xE7E0EF:primary?0x45267E:0xCEBDE6),Color.Transparent);
        DrawingTheme.Shape(e.Graphics,face,11,top,bottom,edge);
        Color ink=DrawingTheme.Rgb(!Enabled?0x736983:primary?0xFFFFFF:0x513688);
        TextRenderer.DrawText(e.Graphics,Text,Font,face,ink,TextFormatFlags.HorizontalCenter|
            TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(face,-6,-5),ink,top);
    }
}

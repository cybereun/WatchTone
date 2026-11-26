using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("워치벨 설치 도우미")]
[assembly: AssemblyCompany("Lebi Studio")]
[assembly: AssemblyFileVersion("1.0.2.0")]

internal sealed partial class Installer : Form {
    private const string ApkName = "WatchTone-watch-1.0.0.apk";
    private static readonly Color Paper = Color.FromArgb(249,247,244);
    private static readonly Color Ink = Color.FromArgb(41,35,70);
    private static readonly Color Muted = Color.FromArgb(83,77,106);
    private readonly TextBox apk = new TextBox { ReadOnly = true };
    private readonly TextBox pair = new TextBox();
    private readonly TextBox code = new TextBox { MaxLength = 6, UseSystemPasswordChar = true };
    private readonly TextBox address = new TextBox();
    private readonly ComboBox watches = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "설치할 워치" };
    private readonly Label status = new Label();
    private readonly Panel body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
    private Button install;
    private string apkPath = string.Empty;
    private bool busy;

    [STAThread]
    private static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Installer());
    }

    private Installer() {
        Text = "워치벨 설치 도우미";
        Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleDimensions = new SizeF(96,96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Malgun Gothic",10);
        ForeColor = Ink;
        BackColor = Paper;
        ClientSize = new Size(864,695);
        MinimumSize = new Size(690,510);
        Controls.Add(body);
        var layout = new TableLayoutPanel {
            Bounds = new Rectangle(24,20,816,655), ColumnCount = 2, RowCount = 5,
            BackColor = Paper, Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        foreach (int height in new[] { 139,188,226,63,33 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
        body.Controls.Add(layout);
        body.Resize += (s,e) => CenterLayout(layout);

        var hero = new Hero { Dock = DockStyle.Fill, Margin = new Padding(4,0,4,12) };
        layout.Controls.Add(hero,0,0);
        layout.SetColumnSpan(hero,2);
        AddText(hero,"워치벨 설치 도우미",26,Ink,true,24,18,589,49);
        AddText(hero,"워치용 APK를 선택해 갤럭시 워치에 설치하세요.",10,Muted,false,27,75,500,26);

        var file = Card(layout,0,1,"1","워치용 APK 선택",false);
        AddText(file,"다운로드한 워치벨 v1.0.0의 워치용 파일을 고르세요.",9,Muted,false,19,61,367,37);
        AddField(file,apk,"워치용 APK",20,106,221);
        AddButton(file,"APK 선택",Choose,246,125,126);

        var prep = Card(layout,1,1,"2","워치와 PC 준비",true);
        AddText(prep,"PC와 워치를 같은 Wi-Fi에 연결하세요.\n워치 정보 → 소프트웨어 정보에서 버전을 5번 누르고,\n개발자 옵션에서 ADB·무선 디버깅을 켜세요.",9,Muted,false,19,55,367,67);
        AddButton(prep,"이미 연결된 워치 찾기",() => Run(Find),20,125,233);

        var pairing = Card(layout,0,2,"3","첫 연결 페어링",false);
        AddText(pairing,"처음 연결하는 PC에서만 필요합니다.\n워치의 ‘새 기기 페어링’ 화면을 열어 주세요.",9,Muted,false,19,55,367,44);
        AddField(pairing,pair,"페어링 주소",20,106,211);
        AddField(pairing,code,"6자리 코드",242,106,130);
        AddButton(pairing,"페어링",() => Run(Pair),20,169,151);

        var connection = Card(layout,1,2,"4","워치 연결과 설치",true);
        AddText(connection,"워치가 목록에 없으면 ‘무선 디버깅’ 첫 화면의\n연결 주소를 입력하세요.",9,Muted,false,19,54,367,40);
        AddField(connection,address,"연결 주소",20,97,215);
        AddButton(connection,"워치 연결",() => Run(Connect),246,116,126);
        watches.Bounds = new Rectangle(20,175,178,30);
        watches.ForeColor = Ink;
        watches.DrawMode = DrawMode.OwnerDrawFixed;
        watches.DropDownWidth = 300;
        watches.ItemHeight = 19;
        watches.DrawItem += (s,e) => {
            e.DrawBackground();
            var watch = e.Index < 0 ? null : watches.Items[e.Index] as Watch;
            bool collapsed = (e.State & DrawItemState.ComboBoxEdit) != 0;
            string text = watch == null ? "설치할 워치 선택" : collapsed ? watch.Model : watch.ToString();
            var textBounds = Rectangle.Inflate(e.Bounds,-6,0);
            bool highlighted = (e.State & DrawItemState.Selected) != 0;
            TextRenderer.DrawText(e.Graphics,text,watches.Font,textBounds,
                highlighted ? SystemColors.HighlightText : Ink,
                TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        };
        watches.AccessibleDescription = "연결된 워치 목록";
        connection.Controls.Add(watches);
        install = AddButton(connection,"선택한 워치에 설치",() => Run(Install),208,169,164,true);
        watches.SelectedIndexChanged += (s,e) => UpdateInstall();

        var feedback = new Surface(false) { Dock = DockStyle.Fill, Margin = new Padding(4,3,4,3) };
        layout.Controls.Add(feedback,0,3);
        layout.SetColumnSpan(feedback,2);
        AddText(feedback,"진행 상태",10,Color.FromArgb(84,61,164),true,16,9,90,39);
        status.ForeColor = Ink;
        status.BackColor = Color.Transparent;
        status.TextAlign = ContentAlignment.MiddleLeft;
        status.Bounds = new Rectangle(107,7,665,45);
        status.Text = "APK를 선택하고 워치를 연결하세요.";
        feedback.Controls.Add(status);

        var credit = new Label {
            Text = "@2026 Lebi_Cybereun", TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font(Font.FontFamily,9),
            Margin = new Padding(4,0,4,0)
        };
        layout.Controls.Add(credit,0,4);
        layout.SetColumnSpan(credit,2);


        var sibling = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,ApkName);
        if (File.Exists(sibling)) SelectApk(sibling);
        UpdateInstall();
        FormClosing += (s,e) => e.Cancel = busy;
    }

    private void CenterLayout(Control layout) {
        int side = layout.Top + 4;
        layout.Left = Math.Max(side,(body.ClientSize.Width-layout.Width)/2);
    }

    private Surface Card(TableLayoutPanel layout,int col,int row,string number,string title,bool green) {
        var card = new Surface(green) { Dock = DockStyle.Fill, Margin = new Padding(4,0,4,8) };
        layout.Controls.Add(card,col,row);
        var badge = new Badge(number,green) { Bounds = new Rectangle(19,15,31,31) };
        card.Controls.Add(badge);
        AddText(card,title,14,Ink,true,59,13,304,36);
        return card;
    }

    private void AddText(Control parent,string value,int size,Color color,bool bold,int x,int y,int width,int height) {
        parent.Controls.Add(new Label {
            Text = value, ForeColor = color, BackColor = Color.Transparent,
            Font = new Font(Font.FontFamily,size,bold ? FontStyle.Bold : FontStyle.Regular),
            Bounds = new Rectangle(x,y,width,height), TextAlign = ContentAlignment.MiddleLeft
        });
    }

    private void AddField(Control parent,TextBox field,string name,int x,int y,int width) {
        AddText(parent,name,9,Muted,true,x,y,width,20);
        var frame = new FieldSurface { Bounds = new Rectangle(x,y+21,width,35) };
        field.AccessibleName = name;
        field.BorderStyle = BorderStyle.None;
        field.Bounds = new Rectangle(10,8,width-20,21);
        field.ForeColor = Ink;
        field.BackColor = FieldSurface.Fill;
        field.GotFocus += (s,e) => frame.Invalidate();
        field.LostFocus += (s,e) => frame.Invalidate();
        frame.Controls.Add(field);
        frame.Click += (s,e) => field.Focus();
        parent.Controls.Add(frame);
    }

    private Button AddButton(Control parent,string text,Action action,int x,int y,int width,bool primary=false) {
        var button = new RaisedButton(primary) {
            Text = text, Bounds = new Rectangle(x,y,width,42),
            Font = new Font(Font.FontFamily,9,FontStyle.Bold)
        };
        button.Click += (s,e) => action();
        parent.Controls.Add(button);
        return button;
    }

    private void Choose() {
        using (var dialog = new OpenFileDialog { Filter = "Android APK (*.apk)|*.apk", Title = "워치용 APK 선택" }) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            SelectApk(dialog.FileName);
            status.Text = "APK를 선택했습니다.";
            UpdateInstall();
        }
    }

    private void SelectApk(string path) {
        apkPath = path;
        apk.Text = Path.GetFileName(path);
    }

    private void UpdateInstall() {
        if (install != null) install.Enabled = !busy && File.Exists(apkPath) && watches.SelectedItem is Watch;
    }

    private async void Run(Func<Task> action) {
        if (busy) return;
        busy = true;
        body.Enabled = false;
        UseWaitCursor = true;
        status.Text = "처리 중입니다. 잠시 기다려 주세요.";
        try { await action(); }
        catch (Exception ex) { status.Text = ex.Message; }
        finally { UseWaitCursor = false; body.Enabled = true; busy = false; UpdateInstall(); }
    }
}

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("워치벨 설치 도우미")]
[assembly: AssemblyCompany("Lebi Studio")]
[assembly: AssemblyFileVersion("1.0.0.0")]

internal sealed partial class Installer : Form {
    private const string ApkName = "WatchTone-watch-1.0.0.apk";
    private static readonly Color Mint = Color.FromArgb(164,244,207);
    private static readonly Color Panel = Color.FromArgb(24,48,41);
    private static readonly Color Ink = Color.FromArgb(236,247,240);
    private readonly TextBox apk = new TextBox { ReadOnly = true };
    private readonly TextBox pair = new TextBox();
    private readonly TextBox code = new TextBox { MaxLength = 6, UseSystemPasswordChar = true };
    private readonly TextBox address = new TextBox();
    private readonly ComboBox watches = new ComboBox { Width = 345, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "설치할 워치" };
    private readonly Label status = new Label { AutoSize = true, MaximumSize = new Size(610,0), ForeColor = Mint };
    private readonly FlowLayoutPanel body = new FlowLayoutPanel {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
        AutoScroll = true, Padding = new Padding(20,12,12,15)
    };
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
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Malgun Gothic",10);
        ForeColor = Ink;
        BackColor = Color.FromArgb(11,25,22);
        Size = new Size(715,760);
        MinimumSize = new Size(675,470);
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(body);
        Label("워치벨 설치 도우미",23,true);
        Label("PC에서 갤럭시 워치에 워치벨을 설치합니다.",10,false);
        Label("1. 워치용 APK 선택",13,true);
        Row(Field(apk,"워치용 APK",425), Button("APK 선택",Choose));
        var sibling = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,ApkName);
        if (File.Exists(sibling)) SelectApk(sibling);

        Label("2. 워치와 PC 준비",13,true);
        Label("같은 Wi-Fi에 연결하세요. 워치 설정 → 워치 정보 → 소프트웨어 정보에서\n소프트웨어 버전을 5번 누른 뒤, 개발자 옵션에서 ADB 디버깅과 무선 디버깅을 켭니다.",9,false);
        Row(Button("이미 연결된 워치 찾기", () => Run(Find)));

        Label("3. 처음 연결할 때만 페어링",13,true);
        Label("워치의 ‘새 기기 페어링’ 화면에서 IP 주소:포트와 6자리 코드를 확인하세요.",9,false);
        Row(Field(pair,"페어링 주소",260), Field(code,"6자리 코드",125), Button("페어링",() => Run(Pair)));

        Label("4. 워치 연결과 설치",13,true);
        Label("목록에 워치가 없으면 ‘무선 디버깅’ 첫 화면의 연결 주소:포트를 입력하세요.",9,false);
        Row(Field(address,"연결 주소",260), Button("워치 연결", () => Run(Connect)));
        install = Button("선택한 워치에 설치", () => Run(Install));
        install.BackColor = Mint;
        install.ForeColor = Color.Black;
        Row(watches,install);
        watches.SelectedIndexChanged += (s,e) => UpdateInstall();
        status.Margin = new Padding(3,8,0,4);
        status.Text = "APK를 선택하고 워치를 연결하세요.";
        body.Controls.Add(status);
        UpdateInstall();
        FormClosing += (s,e) => e.Cancel = busy;
    }

    private void Label(string value,int size,bool title) {
        body.Controls.Add(new Label {
            Text = value, ForeColor = title ? Mint : Ink, AutoSize = true, MaximumSize = new Size(610,0),
            Font = new Font(Font.FontFamily,size,title ? FontStyle.Bold : FontStyle.Regular),
            Margin = new Padding(3,title ? 8 : 0,3,4)
        });
    }
    private void Row(params Control[] controls) {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0,0,0,4) };
        row.Controls.AddRange(controls);
        body.Controls.Add(row);
    }
    private Control Field(TextBox box,string name,int width) {
        var group = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown };
        group.Controls.Add(new Label { Text = name, AutoSize = true });
        box.AccessibleName = name; box.Width = width; box.BackColor = Panel; box.ForeColor = Ink;
        group.Controls.Add(box);
        return group;
    }
    private Button Button(string text,Action action) {
        var button = new Button { Text = text, AutoSize = true, Height = 32, FlatStyle = FlatStyle.Flat,
            ForeColor = Ink, BackColor = Panel, Margin = new Padding(4,7,4,2) };
        button.Click += (s,e) => action();
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

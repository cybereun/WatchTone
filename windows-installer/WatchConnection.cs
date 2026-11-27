using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal sealed partial class Installer {
    private const string WatchHash = "9dcec31179586f5f70db471fa9f42edb64f0b42c09b8a2416bfe34c47bc4d0a1";

    private static string Endpoint(string text) {
        var match = Regex.Match(text.Trim(),@"^(\d{1,3}(?:\.\d{1,3}){3}|\[[0-9a-fA-F:]+\]):(\d{1,5})$");
        IPAddress ip;
        int port;
        if (!match.Success || !IPAddress.TryParse(match.Groups[1].Value.Trim('[',']'),out ip) ||
            !int.TryParse(match.Groups[2].Value,out port) || port < 1 || port > 65535)
            throw new InvalidOperationException("워치 화면에 표시된 IP 주소:포트를 함께 입력하세요. 예시 192.168.0.20:38011");
        return text.Trim();
    }
    private async Task Pair() {
        string host = Endpoint(pair.Text);
        string pin = code.Text.Trim();
        if (!Regex.IsMatch(pin,@"^[0-9]{6}$"))
            throw new InvalidOperationException("워치에 표시된 페어링 코드 6자리를 입력하세요.");
        string adb = await PrepareTools();
        var result = await Adb(adb,"pair "+host,20,pin);
        if (result.Exit != 0 || !result.Text.Contains("Successfully paired"))
            throw new InvalidOperationException("페어링에 실패했습니다. 워치의 ‘새 기기 페어링’을 다시 열고 새 주소와 코드를 입력하세요.");
        code.Clear();
        await Task.Delay(1000);
        await ListWatches(adb,"페어링했습니다. ‘무선 디버깅’ 첫 화면의 연결 주소를 입력하세요.");
    }
    private async Task Connect() {
        string host = Endpoint(address.Text);
        string adb = await PrepareTools();
        await Adb(adb,"connect "+host,20);
        Watch watch = await GetWatch(adb,host);
        if (watch == null)
            throw new InvalidOperationException("워치에 연결하지 못했습니다. 같은 Wi-Fi인지 확인하고 무선 디버깅 첫 화면의 최신 주소를 입력하세요.");
        watches.Items.Clear();
        watches.Items.Add(watch);
        watches.SelectedIndex = 0;
        status.Text = "워치가 연결됐습니다. ‘선택한 워치에 설치’를 누르세요.";
    }
    private async Task Find() {
        await ListWatches(await PrepareTools(),"연결된 워치를 찾지 못했습니다. 처음이라면 페어링부터 진행하세요.");
    }
    private async Task ListWatches(string adb,string emptyMessage) {
        var output = await Adb(adb,"devices",15);
        var candidates = new List<Watch>();
        foreach (Match entry in Regex.Matches(output.Text,@"(?m)^([A-Za-z0-9.\-:\[\]_]+)[\t ]+device[\t \r]*$")) {
            var watch = await GetWatch(adb,entry.Groups[1].Value);
            if (watch != null) candidates.Add(watch);
        }
        watches.Items.Clear();
        foreach (var watch in candidates.GroupBy(w => w.HardwareId).Select(g => g.First())) watches.Items.Add(watch);
        if (watches.Items.Count == 1) watches.SelectedIndex = 0;
        status.Text = watches.Items.Count > 0 ? "워치를 찾았습니다. 설치할 워치를 선택하세요." : emptyMessage;
    }
    private static async Task<Watch> GetWatch(string adb,string serial) {
        string prefix = "-s "+serial+" shell ";
        var result = await Adb(adb,prefix+"pm has-feature android.hardware.type.watch",9);
        if (result.Exit != 0 || result.Text.Trim() != "true") return null;
        string model = (await Adb(adb,prefix+"getprop ro.product.model",9)).Text.Trim();
        string id = (await Adb(adb,prefix+"getprop ro.serialno",9)).Text.Trim();
        return new Watch { Serial = serial, HardwareId = id.Length == 0 ? serial : id, Model = model };
    }
    private async Task Install() {
        var target = watches.SelectedItem as Watch;
        string filePath = apkPath;
        if (target == null || !File.Exists(filePath))
            throw new InvalidOperationException("워치용 APK와 연결된 워치를 선택하세요.");
        using (var file = File.Open(filePath,FileMode.Open,FileAccess.Read,FileShare.Read)) {
            string hash = await Task.Run(() => {
                using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
            });
            if (hash != WatchHash)
                throw new InvalidOperationException("워치벨 v1.0.2 공식 워치용 APK를 선택하세요. 휴대폰용 APK도 파일 이름을 확인해 주세요.");
            string adb = await PrepareTools();
            var current = await GetWatch(adb,target.Serial);
            if (current == null || current.HardwareId != target.HardwareId)
                throw new InvalidOperationException("워치 연결이 바뀌었습니다. 워치를 다시 찾으세요.");
            status.Text = target.Model+"에 워치벨을 설치하고 있습니다.";
            var result = await Adb(adb,"-s "+target.Serial+" install -r \""+filePath+"\"",75);
            if (result.Exit != 0 || !Regex.IsMatch(result.Text,@"(?m)^Success\s*$")) {
                if (result.Text.Contains("INSTALL_FAILED_UPDATE_INCOMPATIBLE"))
                    throw new InvalidOperationException("워치에 다른 서명으로 설치된 워치벨이 있습니다. 기존 앱과 설치 파일을 확인하세요.");
                throw new InvalidOperationException("설치하지 못했습니다. 워치 연결과 저장 공간을 확인하고 다시 시도하세요.");
            }
        }
        status.Text = "설치 완료! 워치 앱 목록에서 ‘워치벨’을 한 번 열어 주세요.";
    }
    private sealed class Watch {
        public string Serial, HardwareId, Model;
        public override string ToString() { return Model+" · "+Serial; }
    }
}

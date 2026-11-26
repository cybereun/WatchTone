using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed partial class Installer {
    private async Task<string> PrepareTools() {
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LebiStudio","WatchToneInstaller");
        string adb = Path.Combine(root,"tools","adb.exe");
        if (File.Exists(adb)) return adb;
        if (MessageBox.Show(this,"처음 한 번 Google에서 워치 연결 도구를 다운로드합니다.\n계속할까요?",
            "워치 연결 도구 준비",MessageBoxButtons.YesNo,MessageBoxIcon.Information) != DialogResult.Yes)
            throw new InvalidOperationException("도구 다운로드를 취소했습니다. 연결할 때 다시 시도할 수 있습니다.");
        status.Text = "Google의 워치 연결 도구를 준비하고 있습니다.";
        try {
            await Task.Run(() => DownloadTools(root,adb));
            return adb;
        } catch (Exception) {
            throw new InvalidOperationException("연결 도구를 준비하지 못했습니다. PC 인터넷 연결을 확인하고 다시 시도하세요.");
        }
    }
    private static void DownloadTools(string root,string adb) {
        var names = new[] { "adb.exe","AdbWinApi.dll","AdbWinUsbApi.dll" };
        string stage = Path.Combine(root,"download-"+Guid.NewGuid().ToString("N"));
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45), MaxResponseContentBufferSize = 48000000 })
        using (var input = new MemoryStream(client.GetByteArrayAsync("https://dl.google.com/android/repository/platform-tools-latest-windows.zip").GetAwaiter().GetResult()))
        using (var zip = new ZipArchive(input,ZipArchiveMode.Read)) {
            var entries = Array.ConvertAll(names,name => zip.GetEntry("platform-tools/"+name));
            if (Array.Exists(entries,entry => entry == null || entry.Length < 1 || entry.Length > 40000000))
                throw new InvalidDataException("Unexpected SDK archive");
            Directory.CreateDirectory(stage);
            for (int i=0;i<names.Length;i++)
                using (var from = entries[i].Open())
                using (var to = File.Create(Path.Combine(stage,names[i]))) from.CopyTo(to);
        }
        try { Directory.Move(stage,Path.GetDirectoryName(adb)); }
        catch (IOException) { if (!File.Exists(adb)) throw; }
    }
}

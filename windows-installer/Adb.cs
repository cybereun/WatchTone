using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

internal sealed partial class Installer {
    private static async Task<AdbResult> Adb(string executable,string arguments,int seconds,string input = null) {
        var options = new ProcessStartInfo(executable,arguments) {
            WorkingDirectory = Path.GetDirectoryName(executable), CreateNoWindow = true, UseShellExecute = false,
            RedirectStandardInput = input != null, RedirectStandardOutput = true, RedirectStandardError = true
        };
        using (var process = Process.Start(options)) {
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (input != null) {
                process.StandardInput.WriteLine(input);
                process.StandardInput.Close();
            }
            if (!await Task.Run(() => process.WaitForExit(seconds*1000)))
                throw new InvalidOperationException("워치 응답이 늦습니다. 워치 화면과 연결을 확인하고 다시 시도하세요.");
            return new AdbResult { Exit = process.ExitCode, Text = (await output)+(await error) };
        }
    }
    private sealed class AdbResult {
        public int Exit;
        public string Text;
    }
}

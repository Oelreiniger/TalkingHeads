using System.Diagnostics;
using System.Text;

namespace TalkingHeads.Commands
{
    class CmdController : THCommand, IDisposable
    {
        Process CMDProcess = new Process();

        public CmdController()
        {
            InitProcess();
        }

        private void InitProcess()
        {
            if (CMDProcess.StartInfo.RedirectStandardError != true)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                CMDProcess.StartInfo = startInfo;
            }

            CMDProcess.Start();
        }

        public async Task<string> Execute(string command)
        {
            if (CMDProcess.HasExited)
                return string.Empty;

            StreamWriter cmdInput = CMDProcess.StandardInput;
            string markerStart = "---START---";
            string markerEnd = "---END---";

            cmdInput.WriteLine($"echo {markerStart}");
            cmdInput.WriteLine(command);
            cmdInput.WriteLine($"echo {markerEnd}");
            cmdInput.Flush();

            Stopwatch timer = Stopwatch.StartNew();
            bool inOutput = false;
            StringBuilder outputBuilder = new();

            _ = Task.Run(async () =>
            {
                string? errLine;
                while ((errLine = await ReadLineWithTimeout(CMDProcess.StandardError, 200)) != null)
                {
                    if (!string.IsNullOrWhiteSpace(errLine) && errLine != "konnte nicht gefunden werden.")
                    {
                        outputBuilder.AppendLine("[stderr] " + errLine.Trim());
                        return;
                    }
                }
            });

            await Task.Delay(200);

            if (outputBuilder.Length == 0)
            {
                while (timer.ElapsedMilliseconds < 3000)
                {
                    Task<string?> stdOutTask = ReadLineWithTimeout(CMDProcess.StandardOutput, 3000);
                    if (await stdOutTask != null)
                    {
                        string? trimmedLine = stdOutTask.Result?.Trim();
                        if (trimmedLine == null)
                            continue;

                        if (trimmedLine == "---START---")
                        {
                            inOutput = true;
                            continue;
                        }
                        if (trimmedLine == "---END---")
                            break;
                        if (!inOutput || string.IsNullOrWhiteSpace(trimmedLine))
                            continue;

                        outputBuilder.AppendLine(trimmedLine);
                        timer.Restart();
                    }
                }
            }

            List<string> lines = outputBuilder.ToString()
            .Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.TrimEnd('\r'))
            .ToList();

            if (lines.Count >= 2)
            {
                lines.RemoveAt(0);
                lines.RemoveAt(lines.Count - 1);
            }

            string cleanedOutput = string.Join("\n", lines);
            if (string.IsNullOrWhiteSpace(cleanedOutput) && outputBuilder.Length != 0)
            {
                cleanedOutput = outputBuilder.ToString().Split(' ').FirstOrDefault();
            }
            else if (string.IsNullOrWhiteSpace(cleanedOutput))
            {
                cleanedOutput = "[No output]";
            }

            Console.WriteLine(cleanedOutput);
            return cleanedOutput;
        }

        async Task<string?> ReadLineWithTimeout(StreamReader reader, int timeoutMs)
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            var readTask = reader.ReadLineAsync();
            var timeoutTask = Task.Delay(timeoutMs, cts.Token);

            var completed = await Task.WhenAny(readTask, timeoutTask);

            if (completed == readTask)
            {
                cts.Cancel();
                return await readTask;
            }
            else
            {
                return null;
            }
        }

        public void Terminate()
        {
            if (CMDProcess != null && !CMDProcess.HasExited)
            {
                CMDProcess.Kill();
            }
        }

        public void Dispose()
        {
            Terminate();
            CMDProcess.Dispose();
            GC.SuppressFinalize(this);
        }

        ~CmdController()
        {
            Dispose();
        }
    }
}

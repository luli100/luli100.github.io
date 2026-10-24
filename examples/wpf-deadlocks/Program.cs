using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace WpfDeadlocks;

internal enum Expected { Complete, UiBlocked, OperationBlocked }
internal sealed record Scenario(string Name, Expected Expected, Func<Probe, Task> Run);

internal sealed class Probe
{
    public readonly Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;
    public readonly Window Window = new();
    public readonly ManualResetEventSlim ReachedWait = new();
    public readonly ManualResetEventSlim Finished = new();
    public int Pulses;
    public Exception? Error;

    public void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--case")
        {
            RunChild(Scenarios.All.Single(x => x.Name == args[1]));
            return 1; // 子进程由独立监控线程给出结果并退出。
        }

        if (args.Length > 0)
        {
            Console.Error.WriteLine("Usage: dotnet run [-- --case <name>]");
            return 2;
        }

        int failures = 0;
        foreach (var scenario in Scenarios.All)
        {
            var start = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            start.ArgumentList.Add("--case");
            start.ArgumentList.Add(scenario.Name);
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(8000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                Console.Error.WriteLine($"FAIL {scenario.Name}: child exceeded timeout");
                failures++;
            }
            else if (process.ExitCode != 0)
            {
                failures++;
            }
            Console.Write(stdout.GetAwaiter().GetResult());
            Console.Error.Write(stderr.GetAwaiter().GetResult());
        }
        Console.WriteLine($"{Scenarios.All.Length - failures}/{Scenarios.All.Length} scenarios passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void RunChild(Scenario scenario)
    {
        var probe = new Probe();
        var heartbeat = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(15)
        };
        heartbeat.Tick += (_, _) => Interlocked.Increment(ref probe.Pulses);
        heartbeat.Start();

        // 不依赖被测 Dispatcher 或线程池的独立监控线程。
        var monitor = new Thread(() =>
        {
            try
            {
                if (scenario.Expected == Expected.Complete)
                {
                    probe.Require(probe.Finished.Wait(4000), "operation did not finish");
                    if (probe.Error is not null) throw probe.Error;
                }
                else
                {
                    probe.Require(probe.ReachedWait.Wait(4000), "did not reach controlled wait");
                    Thread.Sleep(100); // 只用于采样；依赖关系由场景的信号确定。
                    int before = Volatile.Read(ref probe.Pulses);
                    Thread.Sleep(250);
                    int after = Volatile.Read(ref probe.Pulses);
                    probe.Require(!probe.Finished.IsSet, "blocked operation unexpectedly finished");
                    probe.Require(probe.Error is null, "operation failed instead of waiting");
                    probe.Require(scenario.Expected == Expected.UiBlocked ? after == before : after > before,
                        $"unexpected UI progress: {before} -> {after}");
                }
                Console.WriteLine($"PASS {scenario.Name}: {scenario.Expected}; UI pulses={probe.Pulses}");
                Environment.Exit(0);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"FAIL {scenario.Name}: {error}");
                Environment.Exit(1);
            }
        }) { IsBackground = true, Name = "Independent verifier" };
        monitor.Start();

        probe.Dispatcher.BeginInvoke(new Action(async () =>
        {
            try { await scenario.Run(probe); }
            catch (Exception error) { probe.Error = error; }
            finally { probe.Finished.Set(); }
        }));
        Dispatcher.Run();
    }
}

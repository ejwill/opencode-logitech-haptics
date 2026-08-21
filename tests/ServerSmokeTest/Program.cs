using System.Diagnostics;
using System.Reflection;
using System.Collections.Concurrent;

var pluginPath = Environment.GetEnvironmentVariable("PLUGIN_DLL_PATH")
    ?? Path.GetFullPath("logitech-plugin/OpenCodeHapticsPlugin/bin/Release/bin/OpenCodeCompanionPlugin.dll");

if (!File.Exists(pluginPath))
{
    Console.Error.WriteLine($"Plugin DLL not found: {pluginPath}");
    return 1;
}

var assembly = Assembly.LoadFrom(pluginPath);
var serverType = assembly.GetType("Loupedeck.OpenCodeCompanionPlugin.OpenCodeCompanionServer", throwOnError: true)!;
var raised = new ConcurrentQueue<String>();
var port = Int32.Parse(Environment.GetEnvironmentVariable("HAPTICS_TEST_PORT") ?? "18744");
using var server = (IDisposable)Activator.CreateInstance(
    serverType,
    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
    binder: null,
    args: [port, new Action<String>(raised.Enqueue)],
    culture: null)!
;

var start = serverType.GetMethod("Start", BindingFlags.Instance | BindingFlags.Public)!;
var stop = serverType.GetMethod("Stop", BindingFlags.Instance | BindingFlags.Public)!;
if (!(Boolean)start.Invoke(server, null)!) throw new InvalidOperationException("Listener did not report startup.");

(String Code, String Body) Curl(params String[] args)
{
    var psi = new ProcessStartInfo("curl") { RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var arg in new[] { "-sS", "-o", "-", "-w", "\n%{http_code}" }.Concat(args)) psi.ArgumentList.Add(arg);

    using var process = Process.Start(psi)!;
    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0) throw new Exception(error);
    var split = output.LastIndexOf('\n');
    return (output[(split + 1)..].Trim(), output[..split]);
}

var url = $"http://127.0.0.1:{port}/haptic";
var complete = Curl("-X", "POST", url, "-H", "Content-Type: application/json", "--data", "{\"event\":\"complete\"}");
var missing = Curl("-X", "POST", url, "-H", "Content-Type: application/json", "--data", "{}");
var invalid = Curl("-X", "POST", url, "-H", "Content-Type: application/json", "--data", "nope");
var unknown = Curl("-X", "POST", url, "-H", "Content-Type: application/json", "--data", "{\"event\":\"unknown\"}");
var invalidEvent = Curl("-X", "POST", url, "-H", "Content-Type: application/json", "--data", "{\"event\":42}");
var tooLarge = Curl("-X", "POST", url, "-H", "Content-Type: application/json", "--data", $"{{\"event\":\"complete\",\"message\":\"{new String('x', 16 * 1024)}\"}}");
var wrongMethod = Curl(url);
var notFound = Curl($"http://127.0.0.1:{port}/wrong");
var concurrent = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Curl("-X", "POST", url, "-H", "Content-Type: application/json", "--data", "{\"event\":\"test\"}"))));

Console.WriteLine($"complete={complete.Code}:{complete.Body}");
Console.WriteLine($"missing={missing.Code}:{missing.Body}");
Console.WriteLine($"invalid={invalid.Code}:{invalid.Body}");
Console.WriteLine($"unknown={unknown.Code}:{unknown.Body}");
Console.WriteLine($"invalid-event={invalidEvent.Code}:{invalidEvent.Body}");
Console.WriteLine($"too-large={tooLarge.Code}:{tooLarge.Body}");
Console.WriteLine($"wrong-method={wrongMethod.Code}:{wrongMethod.Body}");
Console.WriteLine($"get={notFound.Code}:{notFound.Body}");
Console.WriteLine($"raised={String.Join(',', raised)}");

if (complete != ("202", "accepted")) return 1;
if (missing != ("400", "missing event")) return 1;
if (invalid != ("400", "invalid json")) return 1;
if (unknown.Code != "204") return 1;
if (invalidEvent != ("400", "invalid event")) return 1;
if (tooLarge != ("413", "request too large")) return 1;
if (wrongMethod != ("405", "method not allowed")) return 1;
if (notFound != ("404", "not found")) return 1;
if (concurrent.Any(result => result != ("202", "accepted"))) return 1;
if (!raised.OrderBy(value => value).SequenceEqual(Enumerable.Repeat("opencodeTest", 8).Append("opencodeComplete").OrderBy(value => value))) return 1;

stop.Invoke(server, null);
using var restartedServer = (IDisposable)Activator.CreateInstance(
    serverType,
    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
    binder: null,
    args: [port, new Action<String>(raised.Enqueue)],
    culture: null)!
;
if (!(Boolean)start.Invoke(restartedServer, null)!) return 1;
var restarted = Curl("-X", "POST", url, "-H", "Content-Type: application/json", "--data", "{\"event\":\"complete\"}");
if (restarted != ("202", "accepted")) return 1;
return 0;

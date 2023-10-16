using System.Text;
using System.Text.Json;
using Extism.Sdk;
using Extism.Sdk.Native;

var manifest = new Manifest(new PathWasmSource("../Plugin/bin/Debug/net8.0/wasi-wasm/AppBundle/Plugin.wasm"))
{
    Config = new Dictionary<string, string>
    {
        { "weather-api-key", Environment.GetEnvironmentVariable("weather-api-key") }
    },
    AllowedHosts = ["api.weatherapi.com"]
};

var functions = new[]
{
    HostFunction.FromMethod("send_message", IntPtr.Zero, (CurrentPlugin plugin, long offset) =>
    {
        var message = plugin.ReadString(offset);
        Console.WriteLine($"bot says: {message}");
    }),

    HostFunction.FromMethod("user_info", IntPtr.Zero, (CurrentPlugin plugin) =>
    {
        var json = JsonSerializer.Serialize(new UserInfo
        {
            FullName = "John Smith",
            City = "New York"
        });

        return plugin.WriteString(json);
    })
};

var plugin = new Plugin(manifest, functions, withWasi: true);

var botName = plugin.Call("bot_name", Array.Empty<byte>());
Console.WriteLine($"Bot Name: {Encoding.UTF8.GetString(botName)}");

plugin.Call("respond", Encoding.UTF8.GetBytes(""));

while (true)
{
    Console.Write("> ");
    var message = Console.ReadLine();

    // Easily cancel plugin calls
    var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(1));

    plugin.Call("respond", Encoding.UTF8.GetBytes(message), cts.Token);
}

class UserInfo
{
    public string FullName { get; set; }
    public string City { get; set; }
}
using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using Extism;

// Note: this is needed to ensure that we export a main but the main is not used so it throws an exception
throw new NotImplementedException("a main export is needed but not supported");

static class Plugin
{
    [UnmanagedCallersOnly(EntryPoint = "bot_name")]
    public static void BotName()
    {
        Pdk.SetOutput("weather bot");
    }

    [UnmanagedCallersOnly(EntryPoint = "respond")]
    public static void Respond()
    {
        var message = Pdk.GetInputString();

        if (message.Contains("hi", StringComparison.OrdinalIgnoreCase))
        {
            Reply("Hello :-)");
        }
        else if (message.Contains("weather", StringComparison.OrdinalIgnoreCase))
        {
            // Get secrets and configuration from the Host
            if (!Pdk.TryGetConfig("weather-api-key", out var apiKey))
            {
                throw new Exception("Beep boop malfunction detected: API Key is not configured!");
            }

            var block = MemoryBlock.Find(Env.GetUserInfo());
            var json = block.ReadString();
            var userInfo = JsonSerializer.Deserialize<UserInfo>(json);

            // Call HTTP APIs
            var query = $"https://api.weatherapi.com/v1/current.json?key={apiKey}&q={userInfo.City}&aqi=no";
            var response = Pdk.SendRequest(new HttpRequest(query));
            var responseJson = response.Body.ReadString();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseJson);

            Reply($"The current temparature in {userInfo.City} is {apiResponse.current.temp_c}°C");
        }
        else
        {
            Reply("""
                Hi, I am the weather bot. Commands:
                1. Hi
                2. How's the weather?
                """);
        }
    }

    static void Reply(string message)
    {
        var block = Pdk.Allocate(message);
        Env.SendMessage(block.Offset);
    }
}

static class Env
{
    [DllImport("env", EntryPoint = "send_message")]
    public static extern void SendMessage(ulong offset);

    [DllImport("env", EntryPoint = "user_info")]
    public static extern ulong GetUserInfo();
}

class UserInfo
{
    public string FullName { get; set; }
    public string City { get; set; }
}

class ApiResponse
{
    public Current current { get; set; }
}

class Current
{
    public double temp_c { get; set; }
}
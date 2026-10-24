using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using PlateWatch.Core;

namespace PlateWatch.App.Services;

/// <summary>
/// Real printer connection over Bambu's LAN MQTT feed (ssl:8883, user "bblp",
/// access code as password) - the same channel Bambu Studio uses in LAN mode.
/// No cloud, no account. Parses push reports into PrinterState and sends
/// control commands (pause/resume/stop/speed/light).
/// </summary>
public sealed class LanBambuClient : IBambuClient, IDisposable
{
    private readonly IMqttClient _mqtt;
    private readonly string _accessCode;
    private readonly CancellationTokenSource _stop = new();
    private int _retrying;

    public LanBambuClient(PrinterIdentity identity, string accessCode)
    {
        Identity = identity;
        _accessCode = accessCode;
        State.Status = PrinterStatus.Offline; // until the first report says otherwise
        _mqtt = new MqttFactory().CreateMqttClient();
        _mqtt.ApplicationMessageReceivedAsync += OnMessage;
        _mqtt.DisconnectedAsync += OnDisconnected;
    }

    public PrinterIdentity Identity { get; }
    public PrinterState State { get; } = new();
    public event EventHandler? StateChanged;

    public async Task StartAsync()
    {
        try
        {
            await ConnectAndSubscribe();
        }
        catch
        {
            MarkOffline();
            _ = RetryLoop();
        }
    }

    private async Task ConnectAndSubscribe()
    {
        // bounded: a powered-off printer must fail fast, not hang a retry slot
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(Identity.IpAddress, 8883)
                .WithCredentials("bblp", _accessCode)
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = true,
                    AllowUntrustedCertificates = true,
                    IgnoreCertificateChainErrors = true,
                    IgnoreCertificateRevocationErrors = true,
                })
                .Build();
            await _mqtt.ConnectAsync(options, timeout.Token);
            await _mqtt.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic($"device/{Identity.Serial}/report"))
                .Build(), timeout.Token);
            await SendAsync("{\"pushing\":{\"sequence_id\":\"0\",\"command\":\"pushall\"}}");
        }
    }

    private void MarkOffline()
    {
        if (_stop.IsCancellationRequested) return;
        if (State.Status == PrinterStatus.Offline) return;
        State.Status = PrinterStatus.Offline;
        State.LastUpdated = DateTimeOffset.UtcNow;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private Task OnDisconnected(MqttClientDisconnectedEventArgs args)
    {
        if (!_stop.IsCancellationRequested)
        {
            MarkOffline();
            _ = RetryLoop();
        }
        return Task.CompletedTask;
    }

    // single-flight: only one retry loop ever runs, no matter how many
    // disconnect events fire (previously every failure spawned another loop,
    // and they multiplied while the printer stayed off)
    private async Task RetryLoop()
    {
        if (Interlocked.Exchange(ref _retrying, 1) == 1) return;
        try
        {
            while (!_stop.IsCancellationRequested && !_mqtt.IsConnected)
            {
                await Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ => { });
                if (_stop.IsCancellationRequested) return;
                try { await ConnectAndSubscribe(); }
                catch { MarkOffline(); }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _retrying, 0);
        }
    }

    private Task OnMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var seg = e.ApplicationMessage.PayloadSegment;
            var json = Encoding.UTF8.GetString(seg.Array!, seg.Offset, seg.Count);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("print", out var print))
                ApplyPrint(print);
        }
        catch
        {
            // malformed report: ignore, next push fills state
        }
        return Task.CompletedTask;
    }

    // Report messages after the initial pushall dump are diffs of changed
    // fields, so every field is guarded: only overwrite state the printer
    // actually sent, otherwise a partial report would zero unchanged values.
    private void ApplyPrint(JsonElement p)
    {
        if (p.TryGetProperty("gcode_state", out var gs))
            State.Status = gs.GetString() switch
            {
                "RUNNING" or "PREPARE" or "SLICING" => PrinterStatus.Printing,
                "PAUSE" => PrinterStatus.Paused,
                "FINISH" => PrinterStatus.Finished,
                "FAILED" => PrinterStatus.Error,
                _ => State.Status == PrinterStatus.Printing ? State.Status : PrinterStatus.Idle,
            };
        State.JobName = Str(p, "subtask_name") ?? Str(p, "gcode_file") ?? State.JobName;
        if (p.TryGetProperty("mc_percent", out var v)) State.Progress = Num(v) / 100.0;
        if (p.TryGetProperty("layer_num", out v)) State.LayerCurrent = (int)Num(v);
        if (p.TryGetProperty("total_layer_num", out v)) State.LayerTotal = (int)Num(v);
        if (p.TryGetProperty("mc_remaining_time", out v)) State.SecondsLeft = Num(v) * 60;
        if (p.TryGetProperty("nozzle_temper", out v)) State.NozzleC = Num(v);
        if (p.TryGetProperty("nozzle_target_temper", out v)) State.NozzleTargetC = Num(v);
        if (p.TryGetProperty("bed_temper", out v)) State.BedC = Num(v);
        if (p.TryGetProperty("bed_target_temper", out v)) State.BedTargetC = Num(v);
        if (p.TryGetProperty("chamber_temper", out var ch)) State.ChamberC = Num(ch);
        if (p.TryGetProperty("cooling_fan_speed", out v)) State.FanPartPct = Num(v);
        if (p.TryGetProperty("aux_part_fan", out v)) State.FanAuxPct = Num(v);
        if (p.TryGetProperty("spd_lvl", out v))
            State.Speed = (int)Num(v) switch
            {
                1 => PrintSpeed.Silent,
                3 => PrintSpeed.Sport,
                4 => PrintSpeed.Ludicrous,
                _ => PrintSpeed.Standard,
            };
        if (p.TryGetProperty("lights_report", out var lights) && lights.ValueKind == JsonValueKind.Array)
            foreach (var light in lights.EnumerateArray())
                if (Str(light, "node") == "chamber_light")
                    State.LightOn = Str(light, "mode") == "on";
        if (p.TryGetProperty("ams", out var ams))
            ApplyAms(ams);
        State.LastUpdated = DateTimeOffset.UtcNow;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyAms(JsonElement ams)
    {
        var trayNow = Str(ams, "tray_now");
        if (!ams.TryGetProperty("ams", out var units) || units.ValueKind != JsonValueKind.Array) return;
        State.AmsUnits = new List<AmsUnit>();
        foreach (var unit in units.EnumerateArray())
        {
            var amsUnit = new AmsUnit { Name = $"AMS {(int)Num(unit, "id") + 1}" };
            if (double.TryParse(Str(unit, "humidity"), out var hum))
                amsUnit.HumidityPct = hum switch { 1 => 20, 2 => 40, 3 => 60, 4 => 80, 5 => 100, _ => hum };
            if (unit.TryGetProperty("temp", out var t))
                amsUnit.TemperatureC = Num(t);
            if (unit.TryGetProperty("tray", out var trays) && trays.ValueKind == JsonValueKind.Array)
                foreach (var tray in trays.EnumerateArray())
                {
                    var remain = Num(tray, "remain");
                    var id = Str(tray, "id") ?? "0";
                    amsUnit.Spools.Add(new Spool
                    {
                        Slot = (int)Num(tray, "id") + 1,
                        ColorHex = ToHexColor(Str(tray, "tray_color")),
                        Material = Str(tray, "tray_type") ?? "",
                        Remaining01 = remain < 0 ? 1 : Math.Clamp(remain / 100.0, 0, 1),
                        Active = trayNow is not null && trayNow == id,
                    });
                }
            if (amsUnit.Spools.Count > 0)
                State.AmsUnits.Add(amsUnit);
        }
    }

    public Task PauseAsync() => SendAsync("{\"print\":{\"sequence_id\":\"1\",\"command\":\"pause\"}}");
    public Task ResumeAsync() => SendAsync("{\"print\":{\"sequence_id\":\"1\",\"command\":\"resume\"}}");
    public Task StopAsync() => SendAsync("{\"print\":{\"sequence_id\":\"1\",\"command\":\"stop\"}}");

    public Task SetSpeedAsync(PrintSpeed speed) => SendAsync(
        "{\"print\":{\"sequence_id\":\"1\",\"command\":\"print_speed\",\"param\":\"" +
        speed switch
        {
            PrintSpeed.Silent => "1",
            PrintSpeed.Sport => "3",
            PrintSpeed.Ludicrous => "4",
            _ => "2",
        } + "\"}}");

    public Task SetLightAsync(bool on) => SendAsync(
        "{\"system\":{\"sequence_id\":\"1\",\"command\":\"ledctrl\",\"led_node\":\"chamber_light\",\"led_mode\":\"" +
        (on ? "on" : "off") + "\"}}");

    private async Task SendAsync(string json)
    {
        if (!_mqtt.IsConnected) return;
        await _mqtt.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"device/{Identity.Serial}/request")
            .WithPayload(json)
            .Build());
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static double Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) ? Num(v) : 0;

    private static double Num(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Number => v.GetDouble(),
        JsonValueKind.String when double.TryParse(v.GetString(), out var d) => d,
        _ => 0,
    };

    private static string ToHexColor(string? rrggbbaa)
    {
        if (string.IsNullOrEmpty(rrggbbaa) || rrggbbaa.Length < 6) return "#9AA0A6";
        return "#" + rrggbbaa[..6];
    }

    public void Dispose()
    {
        _stop.Cancel();
        _mqtt.Dispose();
    }
}

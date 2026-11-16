using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;

const int packetCount = 20_000;
const int payloadLengthBytes = 1466;

Console.WriteLine(JsonSerializer.Serialize(new
{
    Runtime = RuntimeInformation.FrameworkDescription,
    OS = RuntimeInformation.OSDescription,
    Transport = "IPv4 loopback; no physical Ethernet link or PAUSE test"
}));

bool unreadCasePassed = RunUnreadCase();
RunClosedPortCase();
bool oversizedCasePassed = RunOversizedCase();
return unreadCasePassed && oversizedCasePassed ? 0 : 1;

bool RunUnreadCase()
{
    using var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
    receiver.ReceiveBufferSize = 8192;
    receiver.ReceiveTimeout = 1000;
    EndPoint destination = receiver.LocalEndPoint ?? throw new InvalidOperationException("Receiver is not bound.");
    using var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    sender.Connect(destination);
    var payload = new byte[payloadLengthBytes];

    // Check that the loopback path works before deliberately pausing application reads.
    sender.Send(payload);
    receiver.Receive(new byte[payloadLengthBytes]);

    SendObservation observation = SendPackets(sender, payload);
    Thread.Sleep(50);
    int receivedAfterSending = 0;
    var receiveBuffer = new byte[payloadLengthBytes];
    while (receiver.Poll(20_000, SelectMode.SelectRead))
    {
        receiver.Receive(receiveBuffer);
        receivedAfterSending++;
    }

    bool passed = observation.SuccessfulSends == packetCount &&
        receivedAfterSending > 0 && receivedAfterSending < observation.SuccessfulSends;
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        Scenario = "bound_receiver_does_not_read_during_sending",
        TargetPackets = packetCount,
        PayloadLengthBytes = payloadLengthBytes,
        ActualReceiveBufferBytes = receiver.ReceiveBufferSize,
        observation.SuccessfulSends,
        observation.ElapsedMs,
        observation.Error,
        ReceivedAfterSending = receivedAfterSending,
        Passed = passed
    }));
    return passed;
}

void RunClosedPortCase()
{
    IPEndPoint destination;
    using (var reservation = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
    {
        reservation.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        destination = (IPEndPoint)(reservation.LocalEndPoint ?? throw new InvalidOperationException("Missing endpoint."));
    }

    using var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    sender.Connect(destination);
    SendObservation observation = SendPackets(sender, new byte[payloadLengthBytes]);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        Scenario = "receiver_port_closed",
        observation.SuccessfulSends,
        observation.ElapsedMs,
        observation.Error,
        Note = "Observation only: ICMP reporting differs by OS and socket API; the reserved port has been released."
    }));
}

bool RunOversizedCase()
{
    using var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
    using var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    sender.Connect(receiver.LocalEndPoint ?? throw new InvalidOperationException("Receiver is not bound."));
    const int oversizedLengthBytes = 65_508;
    try
    {
        int sentBytes = sender.Send(new byte[oversizedLengthBytes]);
        Console.WriteLine(JsonSerializer.Serialize(new { Scenario = "oversized_ipv4_datagram", SentBytes = sentBytes, Passed = false }));
        return false;
    }
    catch (SocketException exception)
    {
        bool passed = exception.SocketErrorCode == SocketError.MessageSize;
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Scenario = "oversized_ipv4_datagram",
            PayloadLengthBytes = oversizedLengthBytes,
            Error = exception.SocketErrorCode.ToString(),
            exception.NativeErrorCode,
            Passed = passed
        }));
        return passed;
    }
}

SendObservation SendPackets(Socket sender, byte[] payload)
{
    int successfulSends = 0;
    string? error = null;
    var elapsed = Stopwatch.StartNew();
    for (int index = 0; index < packetCount; index++)
    {
        try
        {
            int sentBytes = sender.Send(payload);
            if (sentBytes != payload.Length)
            {
                error = $"Unexpected send length: {sentBytes}";
                break;
            }
            successfulSends++;
        }
        catch (SocketException exception)
        {
            error = $"{exception.SocketErrorCode} ({exception.NativeErrorCode})";
            break;
        }
    }
    return new SendObservation(successfulSends, elapsed.ElapsedMilliseconds, error);
}

internal sealed record SendObservation(int SuccessfulSends, long ElapsedMs, string? Error);

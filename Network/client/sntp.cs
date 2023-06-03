using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class Sntp
{
    public static volatile int offsetTime = 0;
    private static Thread _sntpThread;

    public static void Start()
    {
        if (_sntpThread != null)
            return;
        _sntpThread = new Thread(() =>
        {
            while (true)
            {
                var r = GetNetworkTime();
                Thread.Sleep(r ? 65000 : 15000);
            }
        });
        _sntpThread.Start();
    }

    public static long GetTime()
    {
        var r = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return (long)(r + offsetTime);
    }

    private static bool GetNetworkTime()
    {

        try
        {
            const string ntpServer = "time.google.com";

            var ntpData = new byte[48];
            ntpData[0] = 0x1B;

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            using (var socket = new Socket(addresses[0].AddressFamily, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            const byte serverReplyTime = 40;

            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds).ToLocalTime();

            var currentLocal = DateTime.Now;
            offsetTime = (int)((networkDateTime - currentLocal).TotalMilliseconds);

            return true;
        }
        catch
        {
            return false;
        }
    }

    static uint SwapEndianness(ulong x)
    {
        return (uint)(((x & 0x000000ff) << 24) +
                       ((x & 0x0000ff00) << 8) +
                       ((x & 0x00ff0000) >> 8) +
                       ((x & 0xff000000) >> 24));
    }
}
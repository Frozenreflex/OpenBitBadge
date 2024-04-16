using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HidSharp;

namespace OpenBitBadge;

public static class USBManager
{
    public const int PacketSize = 64;

    public static void PushToBadge(BadgePacket packet)
    {
        var bytes = packet.ToByteArray();
        if (bytes.Length > 4096) return;
        Task.Run(() => { Push(bytes); });
    }

    private static void Push(byte[] bytes)
    {
        var devices = DeviceList.Local.GetAllDevices();
        foreach (var d in devices)
        {
            if (d is not HidDevice hid) continue;
            if (hid.VendorID != MagicConstants.VendorID || hid.ProductID != MagicConstants.ProductID) continue;
            if (!hid.TryOpen(out var stream)) continue;

            GD.Print(stream.GetType().ToString());

            var num = hid.GetMaxOutputReportLength();
            MainManager.ActionQueue.Add(() => { GD.Print(num); });


            var chunks = (bytes.Length / PacketSize) + 1;
            for (var i = 0; i < chunks; i++)
            {
                var start = i * PacketSize;
                var end = Math.Min(start + PacketSize, bytes.Length);

                var buffer = new List<byte>(bytes[start..end]);
                if (buffer.Count != PacketSize)
                {
                    var padding = new byte[PacketSize - buffer.Count];
                    buffer.AddRange(padding);
                }

                //HACK: fix HID bug that removes the first 0 in a packet
                //TODO: figure out if this is linux only
                if (buffer.First() == 0) buffer = buffer.Prepend((byte)0).ToList();
                stream.Write(buffer.ToArray());
                stream.Read();
                var str = $"{buffer.Count}, {i}, {buffer.First():X2}, {buffer.Last():X2}";
                MainManager.ActionQueue.Add(() => { GD.Print(str); });
                Thread.Sleep(10);
            }
        }
    }
}
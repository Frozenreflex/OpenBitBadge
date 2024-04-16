using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace OpenBitBadge;
public enum Brightness
{
    Full,
    ThreeQuarters,
    Half,
    Quarter
}

public enum ShowMode
{
    Left,
    Right,
    Up,
    Down,
    Freeze,
    Animation,
    Piling,
    Split,
    Laser,
    Smooth,
    Rotate
}
public class BadgePacket
{
    /// <summary>
    /// The list of messages in the packet. There should always be exactly 8 messages in the array.
    /// </summary>
    [JsonInclude]
    public Message[] Messages = new Message[8]
    {
        new()
        {
            Text = "Text",
            Used = true,
        },
        new(), new(), new(), new(), new(), new(), new()
    };

    [JsonInclude]
    public int ImageHeight = 11;
    [JsonInclude]
    public int ImageWidth = 44;

    [JsonIgnore]
    public bool Valid => Messages.Sum(i => i.MaxLength) < 2560 && Messages.Any(i => i.Used);

    [JsonIgnore]
    public byte FlashByte => Messages.Reverse()
        .Aggregate<Message, byte>(0, (current, message) => (byte)(current << 1 | (message.Flash ? 1 : 0)));

    [JsonIgnore]
    public byte LampByte => Messages.Reverse()
        .Aggregate<Message, byte>(0, (current, message) => (byte)(current << 1 | (message.Lamp ? 1 : 0)));

    public byte[] ToByteArray()
    {
        var messageLengths = new int[8];
        var list = new List<byte>();

        list.AddRange(MagicConstants.Wang);
        list.Add(0);

        //TODO: find out how this actually works, this is essentially the same as the original program
        //but i have no idea how this would work there
        //this is storing the brightness data in one byte
        //brightness has 4 possible values, so they can be stored in 2 bits
        //there are 8 messages
        //thus, it requires 16 bits total
        //this thing is also ANDing it with the top half, so it actually only stores 4 bits
        //is it actually only two options, bright and dim?
        byte brightnessByte = 0;
        for (var index = 0; index < 8; index++)
            brightnessByte = (byte)((int)Messages[index].Brightness << 4 & 0b11110000);
        list.Add(brightnessByte);

        list.Add(FlashByte);
        list.Add(LampByte);

        for (var index = 0; index < 8; index++)
        {
            var modeSpeedByte = (byte)((byte)Messages[index].ShowMode | (byte)(Messages[index].Speed << 4));
            list.Add(modeSpeedByte);
        }

        var currentOffset = 0;
        for (var index = 0; index < 8; index++)
        {
            var currentMessage = Messages[index];
            var byteLength = (currentMessage.Used ? currentMessage.MaxLength : 0);
            var length = currentOffset + byteLength > 0xA00 ? 0xA00 - currentOffset : byteLength;
            messageLengths[index] = length;
            currentOffset += length;
            list.Add((byte)(length >> 8));
            list.Add((byte)length);
        }

        list.AddRange(new byte[2]);
        
        //TODO: figure out what WriteAddress and SendToAddress do
        list.AddRange(new byte[3]);

        list.Add(0);

        var now = DateTime.Now;
        list.Add((byte)(now.Year / 100));
        list.Add((byte)now.Month);
        list.Add((byte)now.Day);
        list.Add((byte)now.Hour);
        list.Add((byte)now.Minute);
        list.Add((byte)now.Second);

        //TODO: figure out what SecurityCode does
        list.Add(0);

        var padding = new byte[64 - list.Count];
        list.AddRange(padding);

        for (var message = 0; message < 8; message++)
        {
            if (messageLengths[message] <= 0) continue;
            var currentMessage = Messages[message];
            var xWidth = messageLengths[message];
            var byteData = new List<byte>();
            for (var x = 0; x < xWidth; x++)
            for (var y = 0; y < ImageHeight; y++) 
                byteData.Add(currentMessage.Image[y, x]);

            list.AddRange(byteData);
        }
        
        //print the packet to the console, for debugging
        /*
        var stringBuilder = new StringBuilder();
        for (var i = 0; i < list.Count; i++)
        {
            stringBuilder.Append(list[i].ToString("X2") + " ");
            if (i % 16 == 15)
            {
                stringBuilder.Append('\n');
            }
            else if (i % 8 == 7)
            {
                stringBuilder.Append(' ');
            }
        }
        GD.Print(stringBuilder.ToString());
        */

        return list.ToArray();
    }
}
using System;
using System.Text.Json.Serialization;
using Godot;

namespace OpenBitBadge;

public class Message
{
    [JsonInclude]
    public bool Used;
    [JsonInclude]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Brightness Brightness;
    [JsonInclude]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ShowMode ShowMode;
    [JsonInclude]
    public bool Flash;
    [JsonInclude]
    public bool Lamp;
    [JsonInclude]
    public bool Draw;
    [JsonInclude]
    public string Text;
    [JsonInclude]
    public string FontFile;
    [JsonInclude]
    public float FontScale = 1;
    [JsonInclude]
    public Vector2 TextOffset;

    [JsonIgnore]
    public int MaxLength
    {
        get
        {
            var maxX = 0;
            for (var y = 0; y < MagicConstants.MaxMessageHeight; y++)
            {
                for (var x = 0; x < MagicConstants.MaxMessageLength; x++)
                {
                    var b = Image[y, x];
                    if (b == 0) continue;
                    var pos = (x + 1);
                    if (maxX < pos) maxX = pos;
                }
            }
            return maxX;
        }
    }
    public readonly byte[,] Image = new byte[MagicConstants.MaxMessageHeight, MagicConstants.MaxMessageLength];

    /// <summary>
    /// Image as a single dimensional array, for serialization. Do not use.
    /// </summary>
    [JsonInclude]
    public byte[] ImageArray
    {
        get
        {
            if (!Draw) return null;
            var array = new byte[MagicConstants.MaxMessageHeight * MagicConstants.MaxMessageLength];
            for (var y = 0; y < MagicConstants.MaxMessageHeight; y++)
            for (var x = 0; x < MagicConstants.MaxMessageLength; x++) 
                array[(y * MagicConstants.MaxMessageLength) + x] = Image[y,x];
            return array;
        }
        set
        {
            if (!Draw) return;
            for (var y = 0; y < MagicConstants.MaxMessageHeight; y++)
            for (var x = 0; x < MagicConstants.MaxMessageLength; x++) 
                Image[y, x] = value[(y * MagicConstants.MaxMessageLength) + x];
        }
    }
    
    /// <summary>
    /// Speed, 0 is the slowest, 7 is the fastest
    /// </summary>
    [JsonInclude]
    public byte Speed
    {
        get => _speed;
        set => _speed = Math.Clamp(value, (byte)0, (byte)7);
    }

    private byte _speed = 3;
}
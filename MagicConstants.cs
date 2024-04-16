namespace OpenBitBadge;

public static class MagicConstants
{
    /// <summary>
    /// The word "hello", in lower case
    /// </summary>
    public static readonly byte[] Hello = { 0x68, 0x65, 0x6C, 0x6C, 0x6F }; //hello

    /// <summary>
    /// The word "wang", in lower case
    /// </summary>
    public static readonly byte[] Wang = { 0x77, 0x61, 0x6E, 0x67 }; //wang

    public const int VendorID = 0x0416;
    public const int ProductID = 0x5020;
    public const int MaxMessageHeight = 16;

    public const int
        MaxMessageLength =
            512; //each block is 8 pixels long, 512 block limit individually but a limit of 2560 blocks overall i think?
}
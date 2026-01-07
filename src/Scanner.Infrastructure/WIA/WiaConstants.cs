namespace Scanner.Infrastructure.WIA;

/// <summary>
/// WIA property constants.
/// </summary>
public static class WiaConstants
{
    // Device types
    public const int ScannerDeviceType = 1;

    // Property IDs
    public const int DeviceId = 2;
    public const int DeviceName = 7;
    public const int DeviceManufacturer = 3;

    // Scanner item properties
    public const int HorizontalResolution = 6147;
    public const int VerticalResolution = 6148;
    public const int HorizontalStartPosition = 6149;
    public const int VerticalStartPosition = 6150;
    public const int HorizontalExtent = 6151;
    public const int VerticalExtent = 6152;
    public const int CurrentIntent = 6146;
    public const int DataType = 4103;
    public const int BitsPerPixel = 4104;

    // Intent values
    public const int IntentImageTypeColor = 1;
    public const int IntentImageTypeGrayscale = 2;
    public const int IntentImageTypeText = 4;

    // Data types
    public const int DataTypeColor = 3;
    public const int DataTypeGrayscale = 2;
    public const int DataTypeBlackAndWhite = 0;

    // Document handling
    public const int DocumentHandlingCapabilities = 3086;
    public const int DocumentHandlingStatus = 3087;
    public const int DocumentHandlingSelect = 3088;
    public const int Pages = 3096;

    // Document handling flags
    public const int FlatbedReady = 1;
    public const int FeederReady = 2;
    public const int DuplexReady = 4;
    public const int FlatbedAvailable = 1;
    public const int FeederAvailable = 2;
    public const int DuplexAvailable = 4;
    public const int FilmAvailable = 16;      // Transparency/film adapter
    public const int Feeder = 1;
    public const int Flatbed = 2;
    public const int Duplex = 4;
    public const int AutoAdvance = 8;
    public const int FrontFirst = 8;
    public const int BackFirst = 16;
    public const int FrontOnly = 32;
    public const int BackOnly = 64;
    public const int NextPage = 128;
    public const int Prefeed = 256;
    public const int AutoSource = 0x8000;

    // Brightness and contrast
    public const int Brightness = 6154;
    public const int Contrast = 6155;

    // Format GUIDs
    public const string FormatBmp = "{B96B3CAB-0728-11D3-9D7B-0000F81EF32E}";
    public const string FormatPng = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";
    public const string FormatJpeg = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";
    public const string FormatTiff = "{B96B3CB1-0728-11D3-9D7B-0000F81EF32E}";
}

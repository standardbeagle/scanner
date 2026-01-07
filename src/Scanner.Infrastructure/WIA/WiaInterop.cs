using System.Runtime.InteropServices;

namespace Scanner.Infrastructure.WIA;

/// <summary>
/// WIA COM interop helper using dynamic late binding.
/// </summary>
public static class WiaInterop
{
    private const string WiaDeviceManagerProgId = "WIA.DeviceManager";

    /// <summary>
    /// Creates a WIA DeviceManager COM object.
    /// </summary>
    public static dynamic CreateDeviceManager()
    {
        var type = Type.GetTypeFromProgID(WiaDeviceManagerProgId)
            ?? throw new InvalidOperationException("WIA is not available on this system.");

        var instance = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Failed to create WIA DeviceManager.");

        return instance;
    }

    /// <summary>
    /// Safely gets a property value from a WIA properties collection.
    /// </summary>
    public static object? GetPropertyValue(dynamic properties, int propertyId)
    {
        try
        {
            foreach (dynamic prop in properties)
            {
                if (prop.PropertyID == propertyId)
                {
                    return prop.Value;
                }
            }
        }
        catch
        {
            // Ignore property access errors
        }
        return null;
    }

    /// <summary>
    /// Safely sets a property value in a WIA properties collection.
    /// </summary>
    public static void SetPropertyValue(dynamic properties, int propertyId, object value)
    {
        try
        {
            foreach (dynamic prop in properties)
            {
                if (prop.PropertyID == propertyId)
                {
                    prop.Value = value;
                    return;
                }
            }
        }
        catch
        {
            // Ignore property set errors - some scanners don't support all properties
        }
    }

    /// <summary>
    /// Gets the image data from a WIA ImageFile object.
    /// </summary>
    public static byte[] GetImageData(dynamic imageFile)
    {
        dynamic fileData = imageFile.FileData;
        return (byte[])fileData.BinaryData;
    }
}

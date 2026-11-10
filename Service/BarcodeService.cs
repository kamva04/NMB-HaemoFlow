using NetBarcode;

namespace NMB_HLabSys.Services;

public interface IBarcodeService
{
    string GenerateCode128DataUrl(string value, int height = 80, bool showLabel = false);
}

public class BarcodeService : IBarcodeService
{
    public string GenerateCode128DataUrl(string value, int height = 80, bool showLabel = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var barcode = new Barcode(value, NetBarcode.Type.Code128, showLabel);
        var base64 = barcode.GetBase64Image();
        return $"data:image/png;base64,{base64}";
    }
}
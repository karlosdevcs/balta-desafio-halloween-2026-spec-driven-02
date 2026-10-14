using System.Net;
using System.Net.Sockets;
using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Application;

// RN-02 e D-06: http ou https, até 2048 caracteres e host publicamente acessível, sem resolver DNS.
public static class TargetUrlValidator
{
    public static string? Validate(string? targetUrl)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
            return "A URL de destino é obrigatória.";

        if (targetUrl.Length > Link.MaxTargetUrlLength)
            return $"A URL de destino deve ter no máximo {Link.MaxTargetUrlLength} caracteres.";

        // No Linux, "/caminho" vira file:///caminho; só aceita file: quando escrito explicitamente.
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri)
            || (uri.IsFile && !targetUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase)))
            return "A URL de destino deve ser uma URL absoluta válida.";

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return "A URL de destino deve usar http ou https.";

        if (string.IsNullOrEmpty(uri.Host))
            return "A URL de destino deve ser uma URL absoluta válida.";

        if (!IsPublicHost(uri))
            return "A URL de destino deve apontar para um endereço publicamente acessível.";

        return null;
    }

    private static bool IsPublicHost(Uri uri)
    {
        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
            return IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address) && IsPublicAddress(address);

        var host = uri.IdnHost.TrimEnd('.');
        return host.Contains('.')
            && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 0
                || b[0] == 10
                || b[0] == 127
                || (b[0] == 169 && b[1] == 254)
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168));
        }

        var bytes = address.GetAddressBytes();
        return !(address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.IPv6Loopback)
            || address.IsIPv6LinkLocal
            || (bytes[0] & 0xFE) == 0xFC);
    }
}

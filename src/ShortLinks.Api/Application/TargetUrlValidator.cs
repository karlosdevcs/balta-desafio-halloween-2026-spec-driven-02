using System.Diagnostics.CodeAnalysis;
using System.Net;
using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Application;

// RN-02 e D-06: http ou https, até 2048 caracteres e host publicamente acessível, sem resolver DNS.
public static class TargetUrlValidator
{
    private static readonly IPNetwork[] NonPublicNetworks =
    [
        IPNetwork.Parse("0.0.0.0/8"),      // não especificado
        IPNetwork.Parse("10.0.0.0/8"),     // rede privada
        IPNetwork.Parse("127.0.0.0/8"),    // loopback
        IPNetwork.Parse("169.254.0.0/16"), // link-local
        IPNetwork.Parse("172.16.0.0/12"),  // rede privada
        IPNetwork.Parse("192.168.0.0/16"), // rede privada
        IPNetwork.Parse("::/128"),         // não especificado
        IPNetwork.Parse("::1/128"),        // loopback
        IPNetwork.Parse("fc00::/7"),       // rede privada (unique local)
        IPNetwork.Parse("fe80::/10"),      // link-local
    ];

    public static ValidationResult<string> Validate(string? targetUrl)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
            return ValidationResult<string>.Failure("A URL de destino é obrigatória.");

        if (targetUrl.Length > Link.MaxTargetUrlLength)
            return ValidationResult<string>.Failure(
                $"A URL de destino deve ter no máximo {Link.MaxTargetUrlLength} caracteres.");

        if (!TryParseAbsoluteUri(targetUrl, out var uri))
            return ValidationResult<string>.Failure("A URL de destino deve ser uma URL absoluta válida.");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return ValidationResult<string>.Failure("A URL de destino deve usar http ou https.");

        if (string.IsNullOrEmpty(uri.Host))
            return ValidationResult<string>.Failure("A URL de destino deve ser uma URL absoluta válida.");

        if (!IsPublicHost(uri))
            return ValidationResult<string>.Failure(
                "A URL de destino deve apontar para um endereço publicamente acessível.");

        return ValidationResult<string>.Success(targetUrl);
    }

    // No Linux, "/caminho" vira file:///caminho; só aceita file: quando escrito explicitamente.
    private static bool TryParseAbsoluteUri(string value, [NotNullWhen(true)] out Uri? uri) =>
        Uri.TryCreate(value, UriKind.Absolute, out uri)
        && (!uri.IsFile || value.StartsWith("file:", StringComparison.OrdinalIgnoreCase));

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

        return !NonPublicNetworks.Any(network => network.Contains(address));
    }
}

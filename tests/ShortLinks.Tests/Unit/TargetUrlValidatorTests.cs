using ShortLinks.Api.Application;

namespace ShortLinks.Tests.Unit;

public class TargetUrlValidatorTests
{
    [Theory]
    [InlineData("http://balta.io")]
    [InlineData("https://balta.io/cursos?id=1#topo")]
    [InlineData("https://8.8.8.8/dns")]
    [InlineData("https://[2001:4860:4860::8888]/")]
    public void Accepts_public_http_and_https_urls(string url)
    {
        Assert.Null(TargetUrlValidator.Validate(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_missing_url(string? url)
    {
        Assert.Equal("A URL de destino é obrigatória.", TargetUrlValidator.Validate(url));
    }

    [Theory]
    [InlineData("ftp://balta.io/arquivo")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///c:/windows")]
    [InlineData("mailto:contato@balta.io")]
    public void Rejects_schemes_other_than_http_and_https(string url)
    {
        Assert.Equal("A URL de destino deve usar http ou https.", TargetUrlValidator.Validate(url));
    }

    [Theory]
    [InlineData("/caminho/relativo")]
    [InlineData("balta.io")]
    [InlineData("http://")]
    public void Rejects_relative_or_malformed_urls(string url)
    {
        Assert.Equal("A URL de destino deve ser uma URL absoluta válida.", TargetUrlValidator.Validate(url));
    }

    [Fact]
    public void Accepts_url_with_exactly_2048_characters()
    {
        var url = "https://balta.io/" + new string('a', 2048 - "https://balta.io/".Length);

        Assert.Null(TargetUrlValidator.Validate(url));
    }

    [Fact]
    public void Rejects_url_longer_than_2048_characters()
    {
        var url = "https://balta.io/" + new string('a', 2049 - "https://balta.io/".Length);

        Assert.Equal("A URL de destino deve ter no máximo 2048 caracteres.", TargetUrlValidator.Validate(url));
    }

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("http://api.localhost")]
    [InlineData("http://intranet/")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://2130706433/")]
    [InlineData("http://0.0.0.0")]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://172.16.0.1")]
    [InlineData("http://172.31.255.255")]
    [InlineData("http://192.168.0.10")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://[::1]/")]
    [InlineData("http://[::]/")]
    [InlineData("http://[fe80::1]/")]
    [InlineData("http://[fd00::1]/")]
    [InlineData("http://[::ffff:192.168.0.1]/")]
    public void Rejects_hosts_that_are_not_publicly_accessible(string url)
    {
        Assert.Equal(
            "A URL de destino deve apontar para um endereço publicamente acessível.",
            TargetUrlValidator.Validate(url));
    }

    [Theory]
    [InlineData("http://172.15.0.1")]
    [InlineData("http://172.32.0.1")]
    public void Accepts_addresses_next_to_private_ranges(string url)
    {
        Assert.Null(TargetUrlValidator.Validate(url));
    }
}

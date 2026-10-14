using Microsoft.AspNetCore.Http;
using ShortLinks.Api.Endpoints;

namespace ShortLinks.Tests.Unit;

public class ShortUrlOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://sho.rt")]
    [InlineData("http://localhost:5059/")]
    public void Accepts_empty_or_absolute_http_base_url(string? baseUrl)
    {
        Assert.True(new ShortUrlOptions { BaseUrl = baseUrl }.HasValidBaseUrl());
    }

    [Theory]
    [InlineData("sho.rt")]
    [InlineData("/links")]
    [InlineData("ftp://sho.rt")]
    public void Rejects_relative_or_non_http_base_url(string baseUrl)
    {
        Assert.False(new ShortUrlOptions { BaseUrl = baseUrl }.HasValidBaseUrl());
    }

    [Fact]
    public void Uses_configured_base_url_without_trailing_slash()
    {
        var options = new ShortUrlOptions { BaseUrl = "https://sho.rt/" };

        Assert.Equal("https://sho.rt", options.ResolveBaseUrl(new DefaultHttpContext().Request));
    }

    [Fact]
    public void Falls_back_to_request_scheme_and_host()
    {
        var request = new DefaultHttpContext().Request;
        request.Scheme = "https";
        request.Host = new HostString("links.balta.io");
        request.PathBase = "/app";

        Assert.Equal("https://links.balta.io/app", new ShortUrlOptions().ResolveBaseUrl(request));
    }
}

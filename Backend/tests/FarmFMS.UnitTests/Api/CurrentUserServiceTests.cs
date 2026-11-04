using System.Security.Claims;
using FarmFMS.API.Common.Identity;
using Microsoft.AspNetCore.Http;

namespace FarmFMS.UnitTests.Api;

public class CurrentUserServiceTests
{
    private static CurrentUserService Create(ClaimsPrincipal? user)
    {
        var accessor = new HttpContextAccessor();
        if (user is not null)
            accessor.HttpContext = new DefaultHttpContext { User = user };

        return new CurrentUserService(accessor);
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public void Reads_user_id_from_sub_claim()
    {
        var service = Create(Authenticated(new Claim("sub", "42"), new Claim(ClaimTypes.Name, "admin")));

        Assert.True(service.IsAuthenticated);
        Assert.Equal(42, service.UserId);
        Assert.Equal("admin", service.UserName);
    }

    [Fact]
    public void Falls_back_to_name_identifier_claim()
    {
        var service = Create(Authenticated(new Claim(ClaimTypes.NameIdentifier, "7")));

        Assert.Equal(7, service.UserId);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    public void Invalid_user_id_is_null(string value)
    {
        Assert.Null(Create(Authenticated(new Claim("sub", value))).UserId);
    }

    [Fact]
    public void Anonymous_request_has_no_user()
    {
        var service = Create(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
        Assert.Null(service.UserName);
    }

    [Fact]
    public void No_http_context_has_no_user()
    {
        var service = Create(null);

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using GatekeeperHQ.Tests.Common.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace GatekeeperHQ.API.IntegrationTests.Security;

public class TokenSecurityTests : IntegrationTestBase
{
	public TokenSecurityTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	[Fact]
	public async Task ExpiredToken_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// Generate an expired token
		var expiredToken = GenerateExpiredToken(testData.SuperAdmin.Id, testData.SuperAdmin.Email);
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task TamperedToken_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// Generate a valid token and tamper with it
		var validToken = GenerateToken(testData.SuperAdmin);
		var tamperedToken = validToken.Substring(0, validToken.Length - 10) + "TAMPERED!!";
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task TokenSignedWithDifferentKey_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// Generate a token with a different secret key
		var wrongKeyToken = GenerateTokenWithDifferentKey(testData.SuperAdmin.Id, testData.SuperAdmin.Email);
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongKeyToken);

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task TokenWithWrongIssuer_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// Generate a token with wrong issuer
		var wrongIssuerToken = GenerateTokenWithWrongIssuer(testData.SuperAdmin.Id, testData.SuperAdmin.Email);
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongIssuerToken);

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task TokenWithWrongAudience_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// Generate a token with wrong audience
		var wrongAudienceToken = GenerateTokenWithWrongAudience(testData.SuperAdmin.Id, testData.SuperAdmin.Email);
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongAudienceToken);

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task MalformedToken_ReturnsUnauthorized()
	{
		// Arrange
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt.token");

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task EmptyToken_ReturnsUnauthorized()
	{
		// Arrange
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task RandomString_ReturnsUnauthorized()
	{
		// Arrange
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "random-string-not-jwt");

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	private string GenerateExpiredToken(int userId, string email)
	{
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
			new Claim(JwtRegisteredClaimNames.Email, email),
			new Claim("is_super_admin", "true")
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.TestJwtSecretKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: CustomWebApplicationFactory.TestJwtIssuer,
			audience: CustomWebApplicationFactory.TestJwtAudience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(-10),  // Expired 10 minutes ago
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private string GenerateTokenWithDifferentKey(int userId, string email)
	{
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
			new Claim(JwtRegisteredClaimNames.Email, email),
			new Claim("is_super_admin", "true")
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("CompletelyDifferentSecretKey12345678"));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: CustomWebApplicationFactory.TestJwtIssuer,
			audience: CustomWebApplicationFactory.TestJwtAudience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(60),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private string GenerateTokenWithWrongIssuer(int userId, string email)
	{
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
			new Claim(JwtRegisteredClaimNames.Email, email),
			new Claim("is_super_admin", "true")
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.TestJwtSecretKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: "WrongIssuer",
			audience: CustomWebApplicationFactory.TestJwtAudience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(60),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private string GenerateTokenWithWrongAudience(int userId, string email)
	{
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
			new Claim(JwtRegisteredClaimNames.Email, email),
			new Claim("is_super_admin", "true")
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.TestJwtSecretKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: CustomWebApplicationFactory.TestJwtIssuer,
			audience: "WrongAudience",
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(60),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}

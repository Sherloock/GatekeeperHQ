using GatekeeperHQ.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace GatekeeperHQ.API.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly PostgreSqlContainer _dbContainer;
	
	public const string TestJwtSecretKey = "TestSecretKeyForIntegrationTesting1234567890!!";
	public const string TestJwtIssuer = "GatekeeperHQ.Test";
	public const string TestJwtAudience = "GatekeeperHQ.Test.Client";

	public CustomWebApplicationFactory()
	{
		_dbContainer = new PostgreSqlBuilder()
			.WithImage("postgres:16-alpine")
			.WithDatabase("gatekeeperhq_test")
			.WithUsername("test_user")
			.WithPassword("test_password")
			.Build();
	}

	public async Task InitializeAsync()
	{
		await _dbContainer.StartAsync();
	}

	public new async Task DisposeAsync()
	{
		await _dbContainer.DisposeAsync();
		await base.DisposeAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureTestServices(services =>
		{
			// Remove the existing DbContext registration
			services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
			services.RemoveAll(typeof(AppDbContext));

			// Add the test database context
			services.AddDbContext<AppDbContext>(options =>
			{
				options.UseNpgsql(_dbContainer.GetConnectionString());
			});

			// Replace Redis cache with in-memory distributed cache
			services.RemoveAll(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
			services.AddDistributedMemoryCache();
		});

		builder.ConfigureAppConfiguration((context, config) =>
		{
			// Override configuration for tests
			var testConfig = new Dictionary<string, string?>
			{
				["Jwt:SecretKey"] = TestJwtSecretKey,
				["Jwt:Issuer"] = TestJwtIssuer,
				["Jwt:Audience"] = TestJwtAudience,
				["Jwt:ExpirationMinutes"] = "60",
				["RateLimiting:EnableRateLimiting"] = "false"
			};

			config.AddInMemoryCollection(testConfig);
		});
	}

	public string GetConnectionString() => _dbContainer.GetConnectionString();
}

using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Tests.Common.Fixtures;

public static class InMemoryDbContextFactory
{
	/// <summary>
	/// Creates an in-memory AppDbContext for unit testing.
	/// Each call creates a unique database to ensure test isolation.
	/// </summary>
	public static AppDbContext Create()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		var context = new AppDbContext(options);
		context.Database.EnsureCreated();
		return context;
	}

	/// <summary>
	/// Creates an in-memory AppDbContext with a specific database name.
	/// Use this when you need to share the database across multiple contexts.
	/// </summary>
	public static AppDbContext Create(string databaseName)
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase(databaseName: databaseName)
			.Options;

		var context = new AppDbContext(options);
		context.Database.EnsureCreated();
		return context;
	}
}

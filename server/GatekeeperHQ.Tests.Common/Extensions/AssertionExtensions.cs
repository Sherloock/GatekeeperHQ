using System.Net;
using FluentAssertions;

namespace GatekeeperHQ.Tests.Common.Extensions;

public static class AssertionExtensions
{
	/// <summary>
	/// Asserts that the response has a successful status code (2xx).
	/// </summary>
	public static void ShouldBeSuccessful(this HttpResponseMessage response)
	{
		response.IsSuccessStatusCode.Should().BeTrue(
			$"Expected success status code but got {(int)response.StatusCode} {response.StatusCode}. " +
			$"Content: {response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
	}

	/// <summary>
	/// Asserts that the response has the expected status code.
	/// </summary>
	public static void ShouldHaveStatusCode(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
	{
		response.StatusCode.Should().Be(expectedStatusCode,
			$"Expected {(int)expectedStatusCode} {expectedStatusCode} but got {(int)response.StatusCode} {response.StatusCode}. " +
			$"Content: {response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
	}

	/// <summary>
	/// Asserts that the response is 401 Unauthorized.
	/// </summary>
	public static void ShouldBeUnauthorized(this HttpResponseMessage response)
	{
		response.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);
	}

	/// <summary>
	/// Asserts that the response is 403 Forbidden.
	/// </summary>
	public static void ShouldBeForbidden(this HttpResponseMessage response)
	{
		response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
	}

	/// <summary>
	/// Asserts that the response is 404 Not Found.
	/// </summary>
	public static void ShouldBeNotFound(this HttpResponseMessage response)
	{
		response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
	}

	/// <summary>
	/// Asserts that the response is 400 Bad Request.
	/// </summary>
	public static void ShouldBeBadRequest(this HttpResponseMessage response)
	{
		response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
	}

	/// <summary>
	/// Asserts that the response is 409 Conflict.
	/// </summary>
	public static void ShouldBeConflict(this HttpResponseMessage response)
	{
		response.ShouldHaveStatusCode(HttpStatusCode.Conflict);
	}

	/// <summary>
	/// Asserts that the response is 201 Created.
	/// </summary>
	public static void ShouldBeCreated(this HttpResponseMessage response)
	{
		response.ShouldHaveStatusCode(HttpStatusCode.Created);
	}

	/// <summary>
	/// Asserts that the response is 204 No Content.
	/// </summary>
	public static void ShouldBeNoContent(this HttpResponseMessage response)
	{
		response.ShouldHaveStatusCode(HttpStatusCode.NoContent);
	}

	/// <summary>
	/// Asserts that the response is 200 OK.
	/// </summary>
	public static void ShouldBeOk(this HttpResponseMessage response)
	{
		response.ShouldHaveStatusCode(HttpStatusCode.OK);
	}
}

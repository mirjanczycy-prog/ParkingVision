namespace ParkingVision.Api;

public static class ApiKeyFilter
{
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> Create(string? expectedKey) =>
        async (ctx, next) =>
        {
            if (string.IsNullOrEmpty(expectedKey) ||
                !ctx.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var provided) ||
                provided != expectedKey)
                return Results.Unauthorized();
            return await next(ctx);
        };
}

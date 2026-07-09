using FluentValidation;
using MediatR;

namespace BuildingBlocks.Common.Application.Behaviors;

/// <summary>
/// Runs all registered FluentValidation validators for the request before the handler executes.
/// If any fail, short-circuits and returns a failed Result (never calls the handler).
/// Registered once in DI for the whole module/pipeline — handlers never validate manually.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(request, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
        var error = Error.Validation(errorMessage);

        // TResponse is always Result or Result<T> by convention in this codebase.
        var resultType = typeof(TResponse);
        if (resultType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        var valueType = resultType.GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
            .MakeGenericMethod(valueType);

        return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
    }
}

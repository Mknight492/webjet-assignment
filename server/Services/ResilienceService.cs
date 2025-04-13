using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoviePriceComparison.Configuration;
using Polly;

namespace MoviePriceComparison.Services;

public interface IResilienceService
{
    Task<IEnumerable<TResult>> ExecuteInParallelWithResilienceAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<TResult>> itemProcessor,
        Func<TItem, string> itemIdentifier,
        CancellationToken cancellationToken = default,
        Action<TResult> resultHandler = null);
        
    Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<Task<TResult>> operation,
        string operationName,
        CancellationToken cancellationToken = default);
}

public class ResilienceService : IResilienceService
{
    private readonly ResilienceOptions _options;
    private readonly ILogger<ResilienceService> _logger;

    public ResilienceService(
        IOptions<ResilienceOptions> options,
        ILogger<ResilienceService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<TResult>> ExecuteInParallelWithResilienceAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<TResult>> itemProcessor,
        Func<TItem, string> itemIdentifier,
        CancellationToken cancellationToken = default,
        Action<TResult> resultHandler = null)
    {
        var results = new List<TResult>();
        using var semaphore = new SemaphoreSlim(_options.MaxParallelism);
        var runningTasks = new List<Task<TResult>>();
        var itemQueue = new Queue<TItem>(items);

        // Helper method to process a single item with retry
        async Task<TResult> ProcessItemAsync(TItem item)
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken);
                string itemId = itemIdentifier(item);
                
                // Use the retry policy from the other method
                return await ExecuteWithRetryAsync(
                    () => itemProcessor(item, cancellationToken),
                    itemId,
                    cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        // Start initial batch of tasks
        while (itemQueue.Count > 0 && runningTasks.Count < _options.MaxParallelism && !cancellationToken.IsCancellationRequested)
        {
            var item = itemQueue.Dequeue();
            runningTasks.Add(ProcessItemAsync(item));
        }

        // Process tasks as they complete and start new ones
        while (runningTasks.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var completedTask = await Task.WhenAny(runningTasks);
            runningTasks.Remove(completedTask);

            var result = await completedTask;
            
            // Handle the result if a handler was provided
            if (result != null)
            {
                results.Add(result);
                resultHandler?.Invoke(result);
            }

            // Start a new task if there are more items
            if (itemQueue.Count > 0)
            {
                var item = itemQueue.Dequeue();
                runningTasks.Add(ProcessItemAsync(item));
            }
        }

        return results;
    }
    
    public async Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<Task<TResult>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var policy = Policy<TResult>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _options.MaxRetries,
                retryAttempt => TimeSpan.FromMilliseconds(
                    _options.InitialRetryDelayMs * Math.Pow(_options.RetryBackoffFactor, retryAttempt)),
                onRetry: (outcome, timeSpan, retryCount, context) => 
                {
                    // OnRetry for Policy<TResult> gives us an 'outcome' which has the exception
                    if (outcome.Exception != null)
                    {
                        _logger.LogWarning(
                            outcome.Exception, 
                            "Error performing operation {OperationName}, retry attempt {RetryCount} after {Delay}ms", 
                            operationName, retryCount, timeSpan.TotalMilliseconds);
                    }
                });
                
        return await policy.ExecuteAsync(operation);
    }
} 
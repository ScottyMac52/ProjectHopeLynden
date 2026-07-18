using System.Diagnostics.CodeAnalysis;
using ProjectHopeLynden.Application.IncomingOrders;

namespace ProjectHopeLynden.Web.HostedServices;

[ExcludeFromCodeCoverage]
public sealed class IncomingOrderWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<IncomingOrderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReceiveDueOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Automatic incoming-order processing failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ReceiveDueOrdersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IIncomingOrderService>();
        var result = await service.ReceiveDueAsync(
            DateOnly.FromDateTime(DateTime.Now),
            DateTime.UtcNow,
            cancellationToken);

        if (result.ReceivedOrderCount > 0)
        {
            logger.LogInformation(
                "Received {OrderCount} due incoming order lines and added {Quantity} units to inventory.",
                result.ReceivedOrderCount,
                result.AddedQuantity);
        }
    }
}

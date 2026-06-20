using Teltonika.Avl.Server;

namespace Teltonika.Avl.AspNetCore;

public interface ITeltonikaDeviceHandler
{
    Task OnDeviceConnectedAsync(DeviceConnectedEventArgs e, CancellationToken ct) => Task.CompletedTask;

    Task OnAvlDataReceivedAsync(AvlDataReceivedEventArgs e, CancellationToken ct) => Task.CompletedTask;

    Task OnDeviceDisconnectedAsync(DeviceDisconnectedEventArgs e, CancellationToken ct) => Task.CompletedTask;

    Task OnCommandReceivedAsync(CommandReceivedEventArgs e, CancellationToken ct) => Task.CompletedTask;

    ValueTask<bool> ValidateImeiAsync(string imei, CancellationToken ct) => new(true);
}

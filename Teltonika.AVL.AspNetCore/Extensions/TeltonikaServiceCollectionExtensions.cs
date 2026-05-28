using Microsoft.Extensions.DependencyInjection;
using Teltonika.AVL.Networking;
using Teltonika.AVL.Profiles;
using Teltonika.AVL.Protocol;

namespace Teltonika.AVL.AspNetCore.Extensions;

public static class TeltonikaServiceCollectionExtensions
{
    public static IServiceCollection AddTeltonikaTcpServer(this IServiceCollection services, int port = 1234)
    {
        // Parsers
        services.AddSingleton<ICodecParser, Codec8Parser>();
        services.AddSingleton<ICodecParser, Codec8ExtendedParser>();
        services.AddSingleton<IIoElementsParser, IoElementsParser>();

        // Profiles
        services.AddSingleton<IIoProfile, GenericProfile>();
        services.AddSingleton<IIoProfile, Fmc234Profile>();

        // TCP Server
        services.AddSingleton(sp => new TcpPipelineServer(
            port,
            sp.GetServices<ICodecParser>(),
            sp.GetServices<IPacketHandler>()));

        services.AddHostedService<TeltonikaTcpHostedService>();
        return services;
    }
}
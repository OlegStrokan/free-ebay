using Protos.Auth;
using Protos.Inventory;
using Protos.Order;
using Protos.Payment;
using Protos.Product;
using Protos.Role;
using Protos.Search;
using Protos.User;

namespace Gateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        var grpc = configuration.GetSection("GrpcServices");

        services.AddGrpcClient<AuthService.AuthServiceClient>(o =>
            o.Address = new Uri(grpc["AuthUrl"]!));

        services.AddGrpcClient<UserServiceProto.UserServiceProtoClient>(o =>
            o.Address = new Uri(grpc["UserUrl"]!));

        services.AddGrpcClient<RoleService.RoleServiceClient>(o =>
            o.Address = new Uri(grpc["UserUrl"]!));

        services.AddGrpcClient<ProductService.ProductServiceClient>(o =>
            o.Address = new Uri(grpc["ProductUrl"]!));

        services.AddGrpcClient<OrderService.OrderServiceClient>(o =>
            o.Address = new Uri(grpc["OrderUrl"]!));

        services.AddGrpcClient<B2BOrderService.B2BOrderServiceClient>(o =>
            o.Address = new Uri(grpc["OrderUrl"]!));

        services.AddGrpcClient<RecurringOrderService.RecurringOrderServiceClient>(o =>
            o.Address = new Uri(grpc["OrderUrl"]!));

        services.AddGrpcClient<PaymentService.PaymentServiceClient>(o =>
            o.Address = new Uri(grpc["PaymentUrl"]!));

        services.AddGrpcClient<InventoryService.InventoryServiceClient>(o =>
            o.Address = new Uri(grpc["InventoryUrl"]!));

        services.AddGrpcClient<SearchService.SearchServiceClient>(o =>
            o.Address = new Uri(grpc["SearchUrl"]!));

        return services;
    }
}

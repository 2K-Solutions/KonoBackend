namespace Kono.Infrastructure.Contracts.Orders;

public enum ResponseError
{
    None,
    BadRequest

}

public sealed record CreateOrderResponse(Guid orderId);

public sealed record CreateOrderResult(CreateOrderResponse? response, ResponseError error, string message)
{
    public static CreateOrderResult Ok(CreateOrderResponse response) => new(response, ResponseError.None, "Order created successfully");
    public static CreateOrderResult Fail(ResponseError error, string message) => new(null, error, message);
}
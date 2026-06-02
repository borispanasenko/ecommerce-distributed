using Payment.Application.Payments;

namespace Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var payments = app.MapGroup("/api/payments")
            .WithTags("Payments");

        payments.MapGet("/", async (
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            var result = await paymentService.GetPaymentsAsync(cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetPayments")
        .WithOpenApi();

        payments.MapGet("/{paymentId:guid}", async (
            Guid paymentId,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            var payment = await paymentService.GetPaymentByIdAsync(paymentId, cancellationToken);

            if (payment is null)
            {
                return Results.NotFound(new
                {
                    error = "payment_not_found",
                    message = "Payment was not found."
                });
            }

            return Results.Ok(payment);
        })
        .WithName("GetPaymentById")
        .WithOpenApi();

        payments.MapPost("/", async (
            CreatePaymentRequest request,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            var result = await paymentService.CreatePaymentAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Created($"/api/payments/{result.Value!.Id}", result.Value);
        })
        .WithName("CreatePayment")
        .WithOpenApi();

        payments.MapPost("/{paymentId:guid}/succeed", async (
            Guid paymentId,
            CompletePaymentRequest request,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            var result = await paymentService.MarkPaymentSucceededAsync(
                paymentId,
                request,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Ok(result.Value);
        })
        .WithName("SucceedPayment")
        .WithOpenApi();

        payments.MapPost("/{paymentId:guid}/fail", async (
            Guid paymentId,
            FailPaymentRequest request,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            var result = await paymentService.MarkPaymentFailedAsync(
                paymentId,
                request,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Results.Ok(result.Value);
        })
        .WithName("FailPayment")
        .WithOpenApi();

        return app;
    }
}

using JasperFx.Core;

using Serilog;

using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.Services.AddSerilog(o => o.WriteTo.Console());

builder.UseWolverine(opts =>
	{
		const string commandqueue = "command_queue";
		const string triggerqueue  = "trigger_queue";
		opts.UseRabbitMq("amqp://admin:admin@localhost:5672")
			.EnableEnhancedDeadLettering()
			.DeclareExchange(
				"test.exchange",
				e =>
				{
					e.BindTopic("test.exchange.command").ToQueue(commandqueue);
					e.BindTopic("test.exchange.event").ToQueue(triggerqueue);
				}
			)
			.AutoProvision();
		
		opts.PublishMessagesToRabbitMqExchange<TriggerEvent>("test.exchange", _ => "test.exchange.event");
		opts.PublishMessagesToRabbitMqExchange<TestCommand>("test.exchange", _ => "test.exchange.command");
		opts.ListenToRabbitQueue(triggerqueue);

		opts.OnAnyException()
			.Requeue()
			.AndPauseProcessing(30.Seconds())
			.AndPauseSending(30.Seconds());

		opts.Policies.OnAnyException()
			.Requeue()
			.AndPauseProcessing(30.Seconds())
			.AndPauseSending(30.Seconds());

		opts.OnException<TimeoutException>()
			.Requeue()
			.AndPauseSending(30.Seconds())
			.AndPauseProcessing(30.Seconds());
	}
);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.MapGet(
	"/",
	async (IMessageBus bus, ILoggerFactory loggerFactory) =>
	{
		await bus.PublishAsync(new TriggerEvent());
		var logger = loggerFactory.CreateLogger<TriggerEventHandler>();
		logger.LogInformation("Sent TriggerEvent");
		TypedResults.Ok();
	}
);

await app.RunAsync();

public record TriggerEvent();

public record TestCommand();

public record TestResponse();

public class TriggerEventHandler
{
	public async Task Handle(TriggerEvent t, ILogger<TriggerEventHandler> logger, IMessageBus bus, CancellationToken ct)
	{
		logger.LogInformation("Recieved TriggerEvent");
		await bus.InvokeAsync<TestResponse>(new TestCommand());
	}
}

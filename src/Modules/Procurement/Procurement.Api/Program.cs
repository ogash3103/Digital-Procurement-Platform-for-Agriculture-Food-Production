using Microsoft.EntityFrameworkCore;
using Procurement.Infrastructure.Persistence;
using Procurement.Application.Abstractions;
using Procurement.Application.Commands.CreateRfq;
using Procurement.Infrastructure.Persistence.Outbox;
using Procurement.Infrastructure.Messaging;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddDbContext<ProcurementDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("ProcurementDb");
    opt.UseNpgsql(cs);
    //opt.UseSnakeCaseNamingConvention();
});

builder.Services.AddSingleton(new RabbitMqOptions
{
    HostName = "localhost",
    Port = 5672,
    UserName = "agri",
    Password = "agri_pwd",
    Exchange = "agri.events"
});

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services.AddScoped<IRfqRepository, RfqRepository>();
builder.Services.AddScoped<CreateRfqHandler>();
builder.Services.AddHostedService<OutboxProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
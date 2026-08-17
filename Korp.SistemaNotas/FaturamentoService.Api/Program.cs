using FaturamentoService.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuração de Controllers e Banco de Dados
builder.Services.AddControllers();
builder.Services.AddDbContext<FaturamentoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração do HttpClient resiliente para comunicação com o microsserviço de Estoque
builder.Services.AddHttpClient("EstoqueService", client =>
{
    var url = builder.Configuration["ServicesUrls:EstoqueApi"]
              ?? builder.Configuration["Services:EstoqueServiceUrl"]
              ?? "http://localhost:5000";

    client.BaseAddress = new Uri(url);
});

// Política de CORS liberada para o frontend Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger com suporte a documentação XML
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Korp - Faturamento Service API",
        Version = "v1",
        Description = "Microsserviço responsável pela emissão de notas fiscais e comunicação com o estoque para baixa de itens."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Executa migrations pendentes com retry aguardando o SQL Server
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<FaturamentoDbContext>();

    for (int retry = 0; retry < 10; retry++)
    {
        try
        {
            dbContext.Database.Migrate();
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Aguardando SQL Server iniciar... Tentativa {retry + 1}/10. Detalhes: {ex.Message}");
            System.Threading.Thread.Sleep(5000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");

app.UseAuthorization();
app.MapControllers();

app.Run();
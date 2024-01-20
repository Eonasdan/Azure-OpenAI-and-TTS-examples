using TalkToMe.Components;
using TalkToMe.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//builder.Services.AddSingleton(builder.Configuration);
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<ISpeechSynthesisService, SpeechSynthesisServiceService>();
builder.Services.AddScoped<ISpeechAvatarService, SpeechAvatarService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
using EasyLogin.Application.Interfaces;
using System.Reflection;

namespace EasyLogin.Infrastructure.Services;

public class EmbeddedEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Assembly _assembly = typeof(EmbeddedEmailTemplateRenderer).Assembly;

    public async Task<string> RenderAsync(string templateName, Dictionary<string, string> placeholders)
    {
        var resourceName = $"EasyLogin.Infrastructure.EmailTemplates.{templateName}.html";
        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Email template '{templateName}' not found.");
        using var reader = new StreamReader(stream);
        var template = await reader.ReadToEndAsync();

        foreach (var (key, value) in placeholders)
            template = template.Replace($"{{{{{key}}}}}", value);

        return template;
    }
}

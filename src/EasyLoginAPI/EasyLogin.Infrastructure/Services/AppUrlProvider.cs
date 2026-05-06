using EasyLogin.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EasyLogin.Infrastructure.Services;

public class AppUrlProvider(IConfiguration config) : IAppUrlProvider
{
    public string FrontendBaseUrl => config["App:FrontendBaseUrl"] ?? "http://localhost:4200";
}

using Microsoft.AspNetCore.Mvc;
using SG.CentralCrypto.MODALS;
using System.Net.Http.Json;

namespace SG.CentralCrypto.IAPI.Controllers;

[ApiController]
[Route("api/crypto")]
public sealed class CryptoController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CryptoController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("encrypt")]
    public Task<IActionResult> Encrypt(
        [FromBody] CryptoRequest request,
        CancellationToken cancellationToken)
    {
        return ForwardAsync("encrypt", request, cancellationToken);
    }

    [HttpPost("decrypt")]
    public Task<IActionResult> Decrypt(
        [FromBody] CryptoRequest request,
        CancellationToken cancellationToken)
    {
        return ForwardAsync("decrypt", request, cancellationToken);
    }

    private async Task<IActionResult> ForwardAsync(
        string operation,
        CryptoRequest request,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("CryptoApi");

        using var message = new HttpRequestMessage(
            HttpMethod.Post, $"api/crypto/{operation}")
        {
            Content = JsonContent.Create(request)
        };

        // Forward the complete Authorization header when supplied.
        if (Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            message.Headers.TryAddWithoutValidation(
                "Authorization", authorization.ToArray());
        }

        try
        {
            using var response = await client.SendAsync(
                message, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString()
                              ?? "application/json",
                Content = body
            };
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new
            {
                error = "The encryption API is unavailable."
            });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                error = "The encryption API request timed out."
            });
        }
    }
}
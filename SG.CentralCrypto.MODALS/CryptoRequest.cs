using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SG.CentralCrypto.MODALS;

public sealed class CryptoRequest
{
    [Required]
    public string Key { get; set; } = string.Empty;

    public JsonElement Input { get; set; }
}
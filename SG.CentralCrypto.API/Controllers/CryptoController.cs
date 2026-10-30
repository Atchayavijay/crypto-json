using Microsoft.AspNetCore.Mvc;
using SG.CentralCrypto.MODALS;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SG.CentralCrypto.API.Controllers;

[ApiController]
[Route("api/crypto")]
public sealed class CryptoController : ControllerBase
{
    private static readonly HashSet<string> SkipFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ENK_Key",
            "ENK_oldapikey",
            "ENK_olduikey"
        };




    private static bool ShouldSkip(string parentPath, string propertyName)
    {
        // Existing flattened ENK fields.
        if (SkipFields.Contains(propertyName))
            return true;

        // Plaintext sections in the supplied configuration.
        if (parentPath == "$" &&
            (propertyName.Equals("Logging", StringComparison.OrdinalIgnoreCase) ||
             propertyName.Equals("ENK", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }



    [HttpPost("encrypt")]
    public IActionResult Encrypt([FromBody] CryptoRequest request)
    {
        return Process(request, encrypt: true);
    }

    [HttpPost("decrypt")]
    public IActionResult Decrypt([FromBody] CryptoRequest request)
    {
        return Process(request, encrypt: false);
    }

    private IActionResult Process(CryptoRequest request, bool encrypt)
    {
        if (request.Input.ValueKind != JsonValueKind.Object)
        {
            return BadRequest(new
            {
                error = "Input must be a JSON object."
            });
        }


        if (encrypt && request.Key.Length != 10)
        {
            return BadRequest(new
            {
                error = "Encryption key must be exactly 10 characters."
            });
        }

        SGCrypto.Encryption crypto;

        try
        {
            crypto = new SGCrypto.Encryption(request.Key);
        }
        catch (Exception)
        {
            return BadRequest(new
            {
                error = "Unable to initialize encryption with the supplied key."
            });
        }

        var input = JsonNode.Parse(request.Input.GetRawText())!;
        var failedFields = new List<string>();

        var output = Transform(
            input, crypto, encrypt, "$", failedFields);

        if (failedFields.Count > 0)
        {
            return BadRequest(new
            {
                error = encrypt
                    ? "Encryption failed for one or more fields."
                    : "Decryption failed for one or more fields. Check the key and values.",
                failedFields
            });
        }

        return Ok(output);
    }

    private static JsonNode? Transform(
        JsonNode? node,
        SGCrypto.Encryption crypto,
        bool encrypt,
        string path,
        List<string> failedFields)
    {
        if (node is JsonObject obj)
        {
            var result = new JsonObject();

            foreach (var property in obj)
            {
                var fieldPath =
                    $"{path}[{JsonSerializer.Serialize(property.Key)}]";

                //result[property.Key] = SkipFields.Contains(property.Key)
                result[property.Key] = ShouldSkip(path, property.Key)
                    ? property.Value?.DeepClone()
                    : Transform(
                        property.Value,
                        crypto,
                        encrypt,
                        fieldPath,
                        failedFields);
            }

            return result;
        }

        if (node is JsonArray array)
        {
            var result = new JsonArray();

            for (var index = 0; index < array.Count; index++)
            {
                result.Add(Transform(
                    array[index],
                    crypto,
                    encrypt,
                    $"{path}[{index}]",
                    failedFields));
            }

            return result;
        }

        if (node is JsonValue value &&
            value.TryGetValue<string>(out var text))
        {
            if (string.IsNullOrEmpty(text) || text == "*")
            {
                return JsonValue.Create(text);
            }

            try
            {
                var processed = encrypt
                    ? crypto.Encrypt(text)
                    : crypto.Decrypt(text);

                if (processed is null ||
        string.Equals(
            processed,
            "Encryption key length should be 10 characters.",
            StringComparison.Ordinal) ||
        string.Equals(
            processed,
            "Padding is invalid and cannot be removed.",
            StringComparison.Ordinal))
                {
                    failedFields.Add(path);
                    return null;
                }

                return JsonValue.Create(processed);
            }
            catch (Exception)
            {
                failedFields.Add(path);
                return null;
            }
        }

        return node?.DeepClone();
    }
}
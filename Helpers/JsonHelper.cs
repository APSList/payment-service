namespace payment_service.Helpers;

using System.Text.Json;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static byte[] Serialize<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, Options);
}


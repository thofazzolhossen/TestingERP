namespace CRM.Messenger;

public static class MessengerMime
{
    public static string TypeFromMime(string? mime)
    {
        mime = (mime ?? "").ToLowerInvariant();
        if (mime.StartsWith("image/")) return "image";
        if (mime.StartsWith("video/")) return "video";
        if (mime.StartsWith("audio/")) return "audio";
        return "file";
    }

    public static string ExtFromMime(string? mime) => (mime ?? "").ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "video/mp4" => ".mp4",
        "audio/mpeg" => ".mp3",
        "audio/mp4" => ".m4a",
        "audio/ogg" => ".ogg",
        "audio/aac" => ".aac",
        "application/pdf" => ".pdf",
        _ => ""
    };

    public static string Preview(string? type, string? name, string? text) => type switch
    {
        "image" => "📷 Photo",
        "video" => "🎥 Video",
        "audio" => "🎵 Audio",
        "file" => "📎 " + name,
        _ => text ?? ""
    };
}
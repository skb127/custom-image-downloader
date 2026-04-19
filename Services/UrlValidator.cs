namespace custom_image_downloader.Services;

public class UrlValidator : IUrlValidator
{
    public (string[] ValidUrls, string[] InvalidLines) Validate(string[] lines)
    {
        var valid = new List<string>();
        var invalid = new List<string>();

        foreach (string line in lines)
        {
            // any line containing whitespace is therefore malformed
            if (!line.Any(char.IsWhiteSpace)
                && Uri.TryCreate(line, UriKind.Absolute, out Uri? uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                valid.Add(line);
            else
                invalid.Add(line);
        }

        return ([..valid], [..invalid]);
    }
}

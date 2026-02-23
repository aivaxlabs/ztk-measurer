namespace CountTokens_Tester.Aivax;

internal sealed class AivaxApiException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public AivaxApiException(int statusCode, string responseBody)
        : base($"Aivax API request failed (HTTP {statusCode}).")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

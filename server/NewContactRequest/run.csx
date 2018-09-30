#r "System.Web"
#r "SendGrid"

using System.Web;
using System.Net;
using SendGrid.Helpers.Mail;

public static HttpResponseMessage Run(
    HttpRequestMessage req, 
    ICollector<ContactRequest> outputTable, 
    TraceWriter log,
    out Mail emailMessage)
{
    log.Info("C# HTTP trigger function processed a request.");

    var contactRequest = req.Content.ReadAsAsync<ContactRequest>().Result;

    if(contactRequest == null)
    {
        emailMessage = null;
        return req.CreateResponse(HttpStatusCode.BadRequest, "Request is null");
    }

    contactRequest.PartitionKey = "ContactRequest";
    contactRequest.RowKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString();
    contactRequest.UserAgent = req.Headers.UserAgent.ToString();
    contactRequest.UserIp = GetClientIp(req);

    log.Info($"Request received: {contactRequest}");

    outputTable.Add(contactRequest);

    emailMessage = new Mail()
    {
        Subject = "New Contact Request"
    };

    var content = new Content
    {
        Type = "text/plain",
        Value = contactRequest.ToString()
    };

    emailMessage.AddContent(content);

    return req.CreateResponse(HttpStatusCode.OK, $"Contact request from: {contactRequest.Name} ({contactRequest.Email}) is successfully submitted.");
}

private static string Truncate(string str, int maxLength)
{
    if (string.IsNullOrEmpty(str)) return str;
    return str.Length <= maxLength 
        ? str 
        : str.Substring(0, maxLength) + " (truncated, total length: " + str.Length + ")"; 
}

private static string GetClientIp(HttpRequestMessage request)
{
    if (request.Properties.ContainsKey("MS_HttpContext"))
    {
        return ((HttpContextWrapper)request.Properties["MS_HttpContext"])?.Request?.UserHostAddress;
    }

    return null;
}

public sealed class ContactRequest
{  
    private string _name;
    private string _email;
    private string _message;
    private string _userAgent;
    private string _userIp;

    public string PartitionKey { get; set; }
    public string RowKey { get; set;}

    public string Name { get => _name; set => _name = Truncate(value, 100); }
    public string Email { get => _email; set => _email = Truncate(value, 100); }
    public string Message { get => _message; set => _message = Truncate(value, 2000); }
    public string UserAgent { get => _userAgent; set => _userAgent = Truncate(value, 1000); }   
    public string UserIp { get => _userIp; set => _userIp = Truncate(value, 20); }

    public override string ToString()
    {
        var lf = Environment.NewLine;
        return $"name: {Name}{lf} email: {Email}{lf}{lf} message:{lf}{Message}{lf}{lf}### userAgent: {UserAgent}{lf}{lf}{lf}### userIP: {UserIp}";
    }
}
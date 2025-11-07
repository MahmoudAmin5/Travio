namespace Travio.API.Errors
{
    public class ApiExceptionErrorResponse : ApiResponse
    {
        public string? Details { get; set; }
        public ApiExceptionErrorResponse(int statuscode, string? message = null, string? details=null) : base(statuscode, message)
        {
            Details = details;

        }
    }
}

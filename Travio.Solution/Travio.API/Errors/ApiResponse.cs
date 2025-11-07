namespace Travio.API.Errors
{
    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public ApiResponse(int statuscode, string? message = null)
        {
            StatusCode = statuscode;
            ErrorMessage = message ?? GetDefaultMassageForStatusCode(statuscode);
        }
        private string? GetDefaultMassageForStatusCode(int Statuscode)
        {
            return Statuscode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                500 => "Errors are the path to the dark side, Errors lead to anger, Anger leads to hate, hate leads to career shift ",
                _ => null
            };

        }
    }
}

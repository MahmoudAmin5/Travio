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
                400 => "I'm gonna make you an offer you can't refuse: Fix your request.",
                401 => "You are not on the guest list for this party.",
                404 => "I looked under the bed, in the closet, and in the database. It's gone.",
                500 => "Errors are the path to the dark side, Errors lead to anger, Anger leads to hate, hate leads to career shift ",
                _ => null
            };

        }
    }
}

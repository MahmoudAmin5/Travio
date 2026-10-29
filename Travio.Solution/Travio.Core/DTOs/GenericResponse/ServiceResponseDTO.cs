namespace Travio.Core.DTOs.GenericResponse
{
    public interface IServiceResult
    {
        bool Success { get; }
        bool BusinessRuleFailed => !Success;
    }

    public class ServiceResponse<T> : IServiceResult
    {
        public T? Data { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; }

        public ServiceResponse()
        {
            Errors = new List<string>();
        }

        public ServiceResponse(T data, string message = "")
        {
            Success = true;
            Data = data;
            Message = message;
            Errors = new List<string>();
        }

        public ServiceResponse(string message)
        {
            Success = false;
            Message = message;
            Errors = new List<string>();
        }
    }
}

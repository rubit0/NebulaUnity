namespace Nebula.Runtime.API
{
    public class WebResponse<T>
    {
        public T Content { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public static WebResponse<T> Success(T content)
        {
            return new WebResponse<T> {IsSuccess = true, Content = content};
        }

        public static WebResponse<T> Failed(string errorMessage = null)
        {
            return new WebResponse<T> {IsSuccess = false, ErrorMessage = errorMessage};
        }
    }

    public class WebResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public static WebResponse Success()
        {
            return new WebResponse {IsSuccess = true};
        }

        public static WebResponse Failed(string errorMessage = null)
        {
            return new WebResponse {IsSuccess = false, ErrorMessage = errorMessage};
        }
    }
}
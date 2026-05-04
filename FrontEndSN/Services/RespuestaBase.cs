public class RespuestaBase<T>
{
	public bool IsSuccess { get; set; }
	public T Data { get; set; }
	public string Mensaje { get; set; }
}
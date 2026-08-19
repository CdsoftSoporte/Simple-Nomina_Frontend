using System.Diagnostics.CodeAnalysis;

public class RespuestaError
{
	public bool? IsSuccess { get; set; }
	public string? Message { get; set; }
	[MaybeNull]
	public string? Details { get; set; }
}
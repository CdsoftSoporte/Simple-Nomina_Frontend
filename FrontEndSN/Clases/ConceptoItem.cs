using System.Text.Json.Serialization;

namespace FrontEndSN.Clases
{
	public class ConceptoItem
	{
		public int Id {  get; set; }
		public string Descripcion { get; set; } = String.Empty;
		[JsonPropertyName("descripcion_concepto")]
		public string DescripcionConcepto { get; set; }=String.Empty; 

	}
}

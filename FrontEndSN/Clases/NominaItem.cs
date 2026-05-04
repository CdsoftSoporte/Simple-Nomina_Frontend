using System.Text.Json.Serialization;
using static DevExpress.Utils.MVVM.Internal.ILReader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FrontEndSN.Clases
{
	public class NominaItem
	{
		public int Id {  get; set; }
		public string Descripcion { get; set; }=string.Empty;

		[JsonPropertyName("id_patron")]
		public int IdPatron { get; set; }
		public char Tipo {  get; set; }
		public string DescripcionCompleta => $"{Descripcion} {"-"} {Tipo}".Trim();
	}
}

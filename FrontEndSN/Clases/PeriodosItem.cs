using System.Text.Json.Serialization;

namespace FrontEndSN.Clases
{
	public class PeriodosItem
	{
		public int id {  get; set; }

		[JsonPropertyName("id_nomina")]
		public int IdNomina { get; set; }
		public int Numero { get; set; }
		public char Naturaleza { get; set; }
		public DateOnly Desde { get; set; }
		public DateOnly Hasta { get; set; }
		public string Descripcion {  get; set; }

		[JsonPropertyName("ultima_nomina_mes")]
		public char UltimaNominaMes {  get; set; }

		public string DescripcionCompleta => $"{Numero} {"-"} {Desde} {" al "} {Hasta} {Descripcion}".Trim();

	}
}

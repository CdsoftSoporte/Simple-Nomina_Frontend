using System.Diagnostics.CodeAnalysis;

namespace FrontEndSN.Clases
{
	public class MovimientoAfiliatorioModel
	{
		public int id { get; set; }

		public int id_empleado { get; set; }
		public int id_patron { get; set; }
		public DateOnly fecha { get; set; }
		public char tipo_movimiento { get; set; }
		public decimal salario_diario { get; set; }
		public decimal salario_integrado { get; set; }
		public byte dias_vacaciones { get; set; }
		public byte dias_aguinaldo { get; set; }
		public decimal prima_vacacional { get; set; }
		public decimal factor_integracion { get; set; }

		[MaybeNull]
		public string? cuenta_infonavit { get; set; }
		[MaybeNull]
		public byte? tipo_descuento_infonavit { get; set; }
		[MaybeNull]
		public DateOnly? fecha_inicio_descuento_infonavit { get; set; }
		[MaybeNull]
		public decimal? importe_descuento_infonavit { get; set; }

	}
}

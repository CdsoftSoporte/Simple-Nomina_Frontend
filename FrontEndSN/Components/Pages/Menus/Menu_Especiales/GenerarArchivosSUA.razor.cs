using FrontEndSN.Clases;
using static FrontEndSN.Components.Pages.Menus.Catalogos.MovimientosImss;

namespace FrontEndSN.Components.Pages.Menus.Menu_Especiales
{
	public partial class GenerarArchivosSUA:VentanaBase
	{
		private bool cargando = false;
		private int IdPatronSeleccionado = 0;
		private char MovimientoSeleccionado;

		private DateOnly FechaDesdeConsulta=DateOnly.FromDateTime(DateTime.Today);
		private DateOnly FechaHastaConsulta=DateOnly.FromDateTime(DateTime.Today);

		private List<dynamic> ListaMovimientos = new() {
		new { Valor = 'T', Texto = "Todos"},
		new { Valor = 'A', Texto = "Alta" },
		new { Valor = 'B', Texto = "Baja" },
		new { Valor = 'M', Texto = "Modificación de Salario" },
		new { Valor = 'R', Texto = "Reingreso"},
		new { Valor = 'F', Texto = "Ausentismo / Falta"},
		new { Valor = 'I', Texto = "Incapacidad"}
		};


		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarPatronesParaCombo(),
					CargarEmpleadosParaCombos()
						);

			cargando = false;
			StateHasChanged();
		}
	}
}

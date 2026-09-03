namespace FrontEndSN.Components.Pages.Menus.Menu_Reportes
{
	public partial class KardexVacaciones
	{
		private bool cargando = false;
		
		private int IdEmpleadoSeleccionado =0;

		private DateOnly FechaCorte = DateOnly.FromDateTime(DateTime.Now);


		private List<dynamic> TipoReporte = new() {
		new { Valor = 'V', Texto = "Días vacaciones"},
		new { Valor = 'P', Texto = "Prima vacacional" }
		};

		private char TipoReporteSeleccionado = 'V';


		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarEmpleadosParaCombos()
						);

			cargando = false;
			StateHasChanged();
		}



		private async Task OnEmpleadoChanged(int nuevoEmpleado)
		{
			IdEmpleadoSeleccionado = nuevoEmpleado;
		}

		private async Task OnTipoReporteChanged(char nuevoValor)
		{
			TipoReporteSeleccionado = nuevoValor;
		}

		private async Task GenerarReporte()
		{

		}


	}
}

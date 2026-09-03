using FrontEndSN.Components.Modales;
using Microsoft.JSInterop;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class IncidenciasVacaciones
	{
		private bool cargando = false;

		private CapturaVacacionesRequest ModeloCaptura = new CapturaVacacionesRequest();

		// Variables globales que asumo ya manejas en tu vista
		private int IdEmpleadoSeleccionado;
		private int IdPatronSeleccionado;

		private HistoricoSolicitudesVacaciones ModalHistorico { get; set; }

		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarPatronesParaCombo(),
					CargarEmpleadosParaCombos('N')
						);

			cargando = false;
			StateHasChanged();
		}



		// tipoFecha: 1 = Inicio, 2 = Fin
		private void AlCambiarFechas(DateTime? nuevaFecha, int tipoFecha)
		{
			if (tipoFecha == 1) ModeloCaptura.fecha_inicio = nuevaFecha;
			if (tipoFecha == 2) ModeloCaptura.fecha_fin = nuevaFecha;

			CalcularSugerencias();
		}




		private void CalcularSugerencias()
		{
			if (ModeloCaptura.fecha_inicio.HasValue && ModeloCaptura.fecha_fin.HasValue)
			{
				if (ModeloCaptura.fecha_fin >= ModeloCaptura.fecha_inicio)
				{
					TimeSpan diferencia = ModeloCaptura.fecha_fin.Value - ModeloCaptura.fecha_inicio.Value;
					int diasCalculados = diferencia.Days + 1;

					// Sugerimos los días a disfrutar
					ModeloCaptura.dias_a_disfrutar = diasCalculados;

					// Sugerimos la misma cantidad para la prima vacacional (el usuario puede editarlo)
					ModeloCaptura.dias_prima_a_pagar = diasCalculados;

					// Magia UX: Sugerimos la fecha de reingreso sumando 1 día a la fecha final
					ModeloCaptura.fecha_reingreso = ModeloCaptura.fecha_fin.Value.AddDays(1);
				}
			}
		}

		private async Task GuardarIncidencia()
		{
			try
			{
				if (!ModeloCaptura.fecha_inicio.HasValue || !ModeloCaptura.fecha_fin.HasValue || !ModeloCaptura.fecha_reingreso.HasValue)
				{
					await JS.InvokeVoidAsync("notifier.error", "Por favor completa las fechas de inicio, fin y reingreso.");
					return;
				}

				ModeloCaptura.id_empleado = IdEmpleadoSeleccionado;
				ModeloCaptura.id_patron = IdPatronSeleccionado;

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				var request = new HttpRequestMessage(HttpMethod.Post, "api/IncidenciasVacaciones/AgregarIncidencia");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(ModeloCaptura);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success", "Incidencia registrada con éxito.");
					ModeloCaptura = new CapturaVacacionesRequest(); // Limpia la forma
					StateHasChanged();
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					await JS.InvokeVoidAsync("notifier.error", error);
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task HistoricoSolicitudes()
		{

			var NombreEmpleado = ListaEmpleados.FirstOrDefault(e => e.Id == IdEmpleadoSeleccionado).NombreCompleto;
			ModalHistorico.Mostrar(NombreEmpleado, IdPatronSeleccionado, IdEmpleadoSeleccionado);
		}


		// 2. DTO del Request
		public class CapturaVacacionesRequest
		{
			public int id_empleado { get; set; }
			public int id_patron { get; set; }
			public DateTime? fecha_inicio { get; set; }
			public DateTime? fecha_fin { get; set; }
			public DateTime? fecha_reingreso { get; set; }
			public int dias_a_disfrutar { get; set; }
			public decimal? dias_prima_a_pagar { get; set; }
			public string? notas { get; set; }
		}


	}
}

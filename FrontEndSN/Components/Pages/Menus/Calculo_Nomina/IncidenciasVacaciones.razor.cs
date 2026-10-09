using FrontEndSN.Components.Modales;
using Microsoft.JSInterop;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class IncidenciasVacaciones
	{
		private bool cargando = false;

		private CapturaVacacionesRequest ModeloCaptura = new CapturaVacacionesRequest();

		private int IdEmpleadoSeleccionado;
		private int IdPatronSeleccionado;
		private int IdNominaSeleccionada;

		private HistoricoSolicitudesVacaciones ModalHistorico { get; set; }

		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarPatronesParaCombo(),
					CargarEmpleadosParaCombos('N'),
					CargarNominasParaCombo()
						);

			cargando = false;
			StateHasChanged();
		}

		private string ValidarDatos()
		{
			if (!ModeloCaptura.fecha_inicio.HasValue || !ModeloCaptura.fecha_fin.HasValue || !ModeloCaptura.fecha_reingreso.HasValue)
			{
				return "Capture las fechas de inicio, fin y reingreso.";
			}

			if (ModeloCaptura.dias_a_disfrutar <= 0) 
			{
				return "Capture los días de vacaciones a disfrutar";
			}

			if(ModeloCaptura.id_periodo == 0)
			{
				return "Capture un periodo de nómina para aplicar las vacaciones";
			}



			return "";
		}

		private async Task GuardarIncidencia()
		{
			try
			{

				string mensaje_error = ValidarDatos();

				if (mensaje_error.Length > 0)
				{
					await JS.InvokeVoidAsync("notifier.error", mensaje_error);
					return;
				}

				cargando = true;
				StateHasChanged();


				ModeloCaptura.id_empleado = IdEmpleadoSeleccionado;
				ModeloCaptura.id_patron = IdPatronSeleccionado;

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				var request = new HttpRequestMessage(HttpMethod.Post, "api/IncidenciasVacaciones/AgregarIncidencia");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(ModeloCaptura);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					await Task.Delay(800);
					cargando = false;
					await JS.InvokeVoidAsync("notifier.success", "Incidencia registrada con éxito.");
					ModeloCaptura = new CapturaVacacionesRequest(); // Limpia la forma
					IdEmpleadoSeleccionado = 0;
					IdPatronSeleccionado = 0;
					IdNominaSeleccionada = 0;
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

		protected async Task OnNominaChanged(int nuevoIdNomina)
		{
			IdNominaSeleccionada = nuevoIdNomina;
			ModeloCaptura.id_periodo = 0;

			if (IdNominaSeleccionada > 0)
			{
				// Cargar los periodos de la nómina seleccionada
				await CargarPeriodosParaCombo(IdNominaSeleccionada);
			}
			else
			{
				ListaPeriodos.Clear();
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
			public int id_periodo { get; set; }
		}


	}
}

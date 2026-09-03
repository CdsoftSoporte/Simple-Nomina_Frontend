using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Uma;

namespace FrontEndSN.Components.Modales
{
	public partial class HistoricoSolicitudesVacaciones
	{
		private bool IsVisible { get; set; } = false;

		private int IdPatron;
		private int IdEmpleado;

		private string TituloModal;
		private string NombreEmpleado;

		private List<RegistroHistoricoVacaciones> ListaRegistros = new();


		private List<OpcionEstatus> ListaOpcionesEstatus = new() {
			new OpcionEstatus { Valor = "Pen", Texto = "Pendiente" },
			new OpcionEstatus { Valor = "Rec", Texto = "Rechazada" },
			new OpcionEstatus { Valor = "Aut", Texto = "Autorizada" }
};

		public async Task Mostrar(string NombreEmpleado, int Idpatron,int Idempleado)
		{
			TituloModal = $"Histórico de empleado {NombreEmpleado}";
			IsVisible = true;
			IdPatron = Idpatron;
			IdEmpleado = Idempleado;
			this.NombreEmpleado = NombreEmpleado;
			await ConsultaHistorial();
			StateHasChanged();
		}


		private async Task ConsultaHistorial()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/IncidenciasVacaciones/ObtenerIncidencias/{IdPatron}/{IdEmpleado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<RegistroHistoricoVacaciones>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaRegistros = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message+ ex.InnerException);
			}
		}

		private async Task ConfirmarActualizar(RegistroHistoricoVacaciones registro,string nuevoEstatus)
		{
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
			"¿Confirmar Cambios?",
			 $"¿Desea cambiar el estado de la solicitud?");
			if (confirmed)
			{
				await ActualizarEstatus(registro,nuevoEstatus);
				await ConsultaHistorial();
			}
			else
			{
				//regresar el color y el texto del combo al estado original
				var estatusOriginal = registro.Estatus;

				registro.Estatus = string.Empty;
				StateHasChanged();

				registro.Estatus = estatusOriginal;
				StateHasChanged();
			}
		}

		private async Task ActualizarEstatus(RegistroHistoricoVacaciones registro, string nuevoEstatus)
		{

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Patch, $"api/IncidenciasVacaciones/ModificarEstatus");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var parametros = new
				{
					id = registro.Id,
					estatus=nuevoEstatus
				};

				request.Content = JsonContent.Create(parametros);


				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					registro.Estatus = nuevoEstatus;
					StateHasChanged();
					await JS.InvokeVoidAsync("notifier.success", "Actualización registrada con éxito");
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "[BE]No se pudo cargar la lista: " + (errorData?.Message + errorData.Details ?? "Error inesperado"));
				}

			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message + ":" + ex.InnerException);
			}
			finally
			{
				await Task.Delay(500);
			}

		}

		private async Task GenerarFormato(RegistroHistoricoVacaciones registro)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/IncidenciasVacaciones/ExcelSolicitudVacaciones");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


				var parametros = new
				{
					id_patron = IdPatron,
					id_empleado = IdEmpleado,
					fecha_corte=registro.FechaInicio,
					dias_solicitados=registro.DiasADisfrutar,
					inicio_vacaciones=registro.FechaInicio,
					fin_vacaciones=registro.FechaFin,
					fecha_reingreso=registro.FechaReingreso,
					nombre_empleado=this.NombreEmpleado,
					estado_solicitud=registro.Estatus

				};

				request.Content = JsonContent.Create(parametros);

				var response = await Http.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					var errorReal = await response.Content.ReadAsStringAsync();

					await JS.InvokeVoidAsync("notifier. error", errorReal);

					return;
				}

				using var fileStream = await response.Content.ReadAsStreamAsync();

				using var streamRef = new DotNetStreamReference(stream: fileStream);

				await JS.InvokeVoidAsync("DescargarArchivos", streamRef, $"Solicitud Vacaciones.xlsx");
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}





	}

	public sealed class OpcionEstatus
	{
		public string Valor { get; set; }
		public string Texto { get; set; }
	}
	public sealed class RegistroHistoricoVacaciones
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("id_empleado")]
		public int IdEmpleado { get; set; }

		[JsonPropertyName("fecha_inicio")]
		public DateTime FechaInicio { get; set; }

		[JsonPropertyName("fecha_fin")]
		public DateTime FechaFin { get; set; }

		[JsonPropertyName("fecha_reingreso")]
		public DateTime FechaReingreso { get; set; }

		[JsonPropertyName("dias_a_disfrutar")]
		public int DiasADisfrutar { get; set; }

		[JsonPropertyName("dias_prima_a_pagar")]
		public decimal DiasPrimaAPagar { get; set; }

		[JsonPropertyName("estatus")]
		public string Estatus { get; set; }

		[JsonPropertyName("fecha_solicitud")]
		public DateTime FechaSolicitud { get; set; }

		[JsonPropertyName("notas")]
		public string Notas { get; set; }

	}

}

using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text.Json;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class MovimientosImss:VentanaBase
	{
		private List<RegistroPatronalItem> PatronesList = new();
		private List<MovimientoAfiliatorioModel> Historial = new();
		private MovimientoAfiliatorioModel Modelo = new();

		private EditContext editContext;
		private bool cargando = false;
		private int IdEmpleadoSeleccionado=0;
		private int IdPatronSeleccionado=0;

		private List<dynamic> tiposOpciones = new() {
		new { Valor = 'A', Texto = "Alta" },
		new { Valor = 'B', Texto = "Baja" },
		new { Valor = 'M', Texto = "Modificación de Salario" }
		};

		private List<dynamic> infonavitOpciones = new() {
		new { Valor = (byte)1, Texto = "Porcentaje (%)" },
		new { Valor = (byte)2, Texto = "Cuota Fija ($)" },
		new { Valor = (byte)3, Texto = "Veces UMA" }
		};
		protected override async Task OnInitializedAsync()
		{
			await Task.WhenAll(
							CargarEmpleadosParaCombos(),
							CargarPatrones()
						);
		}

		private async Task CargarPatrones()
		{
			cargando = true;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/RegistrosPatronales/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<RegistroPatronalItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					PatronesList = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
			finally
			{
				cargando = false;
				StateHasChanged();
			}
		}

		private async Task OnEmpleadoChanged(int id)
		{
			IdEmpleadoSeleccionado = id;
			if (id > 0 && IdPatronSeleccionado>0 ) await CargarHistorialEmpleadoYPatron(IdEmpleadoSeleccionado,IdPatronSeleccionado);
		}

		private async Task OnPatronChanged(int id)
		{
			IdPatronSeleccionado = id;
			if (id > 0 && IdEmpleadoSeleccionado>0) await CargarHistorialEmpleadoYPatron(IdEmpleadoSeleccionado,IdPatronSeleccionado);
		}

		private async Task CargarHistorialEmpleadoYPatron(int IdEmpleado,int IdPatron)
		{
			cargando = true;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/MovimientoAfiliatorio/ObtenerPorEmpleadoYPatron/{IdEmpleado}/{IdPatron}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<MovimientoAfiliatorioModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Historial = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
			finally
			{
				cargando = false;
				StateHasChanged();
			}
		}

		private async Task AbrirFormulario(MovimientoAfiliatorioModel? item = null)
		{
			if (item == null)
			{
				Modelo = new MovimientoAfiliatorioModel
				{
					id = 0,
					id_empleado = IdEmpleadoSeleccionado,
					fecha = DateOnly.FromDateTime(DateTime.Now),
					tipo_movimiento = 'M',
					dias_aguinaldo = 15,
					dias_vacaciones = 12,
					prima_vacacional = 0.25m
					
				};
			}
			else
			{
				Modelo = item;
			}

			CalcularSBC();
			editContext = new EditContext(Modelo);
			await PrepararVentana(600);
			StateHasChanged();
		}



		private void OnSdChanged(decimal v)
		{
			Modelo.salario_diario = v;
			CalcularSBC();
		}

		private void OnVacChanged(byte v)
		{
			Modelo.dias_vacaciones = v;
			CalcularSBC();
		}

		private void OnAguinaldoChanged(byte v)
		{
			Modelo.dias_aguinaldo = v;
			CalcularSBC();
		}

		private void OnPrimaChanged(decimal v)
		{
			Modelo.prima_vacacional = v;
			CalcularSBC();
		}

		private void CalcularSBC()
		{
			// Aseguramos que no haya división por cero y calculamos factor
			decimal factor = (365m + Modelo.dias_aguinaldo + (Modelo.dias_vacaciones * Modelo.prima_vacacional)) / 365m;
			Modelo.factor_integracion = Math.Round(factor, 6);
			Modelo.salario_integrado = Math.Round(Modelo.salario_diario * Modelo.factor_integracion, 2);

			StateHasChanged();
		}

		private async Task Guardar()
		{
			if (editContext == null || !editContext.Validate()) return;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var url = Modelo.id == 0
					? "api/MovimientoAfiliatorio/Agregar"
					: $"api/MovimientoAfiliatorio/Editar/{Modelo.id}";

				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(Modelo);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success",
						Modelo.id == 0 ? "Movimiento registrado exitosamente." : "Movimiento actualizado correctamente.");
					MostrarFormulario = false;
					await CargarHistorialEmpleadoYPatron(IdEmpleadoSeleccionado,IdPatronSeleccionado);
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo guardar: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task ConfirmarEliminar(MovimientoAfiliatorioModel item)
		{
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
			"¿Eliminar Registro?",
			$"¿Deseas borrar el movimiento afiliatorio {item.tipo_movimiento} del {item.fecha:dd/MM/yyyy}? Esta acción no se puede deshacer");
			if (confirmed)
			{
				await Eliminar(item.id);
				await CargarHistorialEmpleadoYPatron(IdEmpleadoSeleccionado,IdPatronSeleccionado);
			}
		}

		private async Task Eliminar(int id)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/MovimientoAfiliatorio/Borrar/{id}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{

					if (response.IsSuccessStatusCode)
					{
						await JS.InvokeVoidAsync("notifier.success", "Registro eliminado correctamente");
					}
					else
					{
						await JS.InvokeVoidAsync("notifier.error", response.Content);
					}

					await CargarHistorialEmpleadoYPatron(IdEmpleadoSeleccionado, IdPatronSeleccionado);
				}
				else
				{

					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();

					string mensajePrincipal = errorData?.Message ?? "Error inesperado";
					string detalleTecnico = errorData?.Details ?? "No hay detalles adicionales.";
					await JS.InvokeVoidAsync("notifier.error", "No se pudo guardar la información: " + mensajePrincipal + " " + detalleTecnico);

				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", $"Ocurrió un error: " + ex.InnerException);
			}
		}

		private void CerrarFormulario() => MostrarFormulario = false;

		private string GetTipoMovimientoTexto(char t) => t switch { 'A' => "Alta", 'B' => "Baja", 'M' => "Modif.", _ => "Mov. Inválido" };

		public class RegistroPatronalItem
		{
			public int id {  get; set; }
			public string razon_social {  get; set; }
		}

	}
}
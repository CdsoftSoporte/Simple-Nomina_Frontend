using DevExpress.XtraReports.Diagnostics;
using FrontEndSN.Clases;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Calculo_Nomina.CalculoAguinaldos;
using static FrontEndSN.Components.Pages.Menus.Catalogos.MovimientosImss;

namespace FrontEndSN.Components.Pages.Menus.Menu_Especiales
{
	public partial class GenerarArchivosSUA:VentanaBase
	{
		private bool cargando = false;
		private int IdPatronSeleccionado = 0;
		private char MovimientoSeleccionado='T';
		private int IdEmpleadoSeleccionado = 0;

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

		private List<ListaMovimientosSUADTO> ListaMovimientosSUA = new();


		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarPatronesParaCombo(),
					CargarEmpleadosParaCombos('S')
						);

			cargando = false;
			StateHasChanged();
		}

		protected async Task OnRegistroPatronalChanged(int nuevoIdPatron)
		{
			IdPatronSeleccionado = nuevoIdPatron;
			await LimpiarGrid();
		}

		protected async Task OnEmpleadoChanged(int nuevoIdEmpleado)
		{
			IdEmpleadoSeleccionado = nuevoIdEmpleado;
			await LimpiarGrid();
		}

		protected async Task OnMovimientoChanged(char nuevoValorMov)
		{
			MovimientoSeleccionado=nuevoValorMov;
			await LimpiarGrid();
		}

		protected Task LimpiarGrid()
		{
			ListaMovimientosSUA.Clear();
			StateHasChanged();
			return Task.CompletedTask;
		}

		private async Task<bool> ValidarFechas()
		{
			if (FechaDesdeConsulta > FechaHastaConsulta)
			{
				await JS.InvokeVoidAsync("notifier.error", "La fecha Desde es mayor que la fecha Hasta, verifique e intente de nuevo");
				return false;
			}
			return true;
		}

		private async Task ConsultarMovimientos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				if (!await ValidarFechas())
				{
					return;
				}
				cargando = true;
				StateHasChanged();

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/GeneradorSUA/ObtenerMovimientos");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var parametros = new
				{
					id_patron = IdPatronSeleccionado,
					id_empleado = IdEmpleadoSeleccionado,
					tipo_movimiento = MovimientoSeleccionado,
					fecha_desde=FechaDesdeConsulta,
					fecha_hasta=FechaHastaConsulta
				};

				request.Content = JsonContent.Create(parametros);


				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<ListaMovimientosSUADTO>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaMovimientosSUA = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message + errorData.Details ?? "Error inesperado"));
				}

			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message+":"+ex.InnerException);
			}
			finally
			{
				await Task.Delay(800);
				cargando = false;
				StateHasChanged();
			}
		}

		private async Task DescargarExcel()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var registro_patronal = ListaPatrones.FirstOrDefault(p => p.id == IdPatronSeleccionado).razon_social;

				// 2. Construimos el objeto maestro
				var datosParaExcel = new
				{
					registro_patronal = registro_patronal, 
					desde = FechaDesdeConsulta,       
					hasta = FechaHastaConsulta,

					// 3. Transformamos la lista del grid a la lista del Excel usando LINQ
					movimientos = ListaMovimientosSUA.Select(x => new
					{
						nombre_empleado = x.Nombre,
						nss = x.Nss,

						diario_integrado = x.Integrado,

						tipo_movimiento = x.TipoMov,

						fecha = x.Fecha.ToString("yyyy-MM-dd"),

						folio_incapacidad = x.FolioIncapacidad == "N/A" ? "N/A" : x.FolioIncapacidad,

						dias_incidencia = x.DiasIncidencia == 0 ? 0 : (int?)x.DiasIncidencia

					}).ToList()
				};

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/GeneradorSUA/ExcelMovimientosSUA");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(datosParaExcel);

				var response = await Http.SendAsync(request);

				// Quitamos temporalmente el EnsureSuccessStatusCode y leemos el error real
				if (!response.IsSuccessStatusCode)
				{
					var errorReal = await response.Content.ReadAsStringAsync();

					// Lo mandamos a la consola del navegador para que lo leas con F12
					await JS.InvokeVoidAsync("notifier.error", errorReal);

					return;
				}

				using var fileStream = await response.Content.ReadAsStreamAsync();

				using var streamRef = new DotNetStreamReference(stream: fileStream);

				await JS.InvokeVoidAsync("DescargarArchivos", streamRef, $"Lista_Movimientos_{registro_patronal}_{FechaDesdeConsulta}_al{FechaHastaConsulta}.xlsx");
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}


		private async Task GenerarMovimientos()
		{
			try
			{
				// 1. Validamos que el grid tenga datos
				if (ListaMovimientosSUA == null || !ListaMovimientosSUA.Any())
				{
					await JS.InvokeVoidAsync("notifier.error", "No hay movimientos cargados en el grid para exportar.");
					return;
				}

				// 2. Armamos la lista de IDs extrayéndolos directamente de la clase de tu grid
				var idsParaExportar = ListaMovimientosSUA.Select(m => m.IdMovimiento).ToList();

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				// 3. Preparamos la petición
				var request = new HttpRequestMessage(HttpMethod.Post, "api/GeneradorSUA/GenerarArchivosMovimientos");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				// Enviamos la lista de IDs que acabamos de armar
				request.Content = JsonContent.Create(idsParaExportar);

				var response = await Http.SendAsync(request);

				// 4. Manejo de errores
				if (!response.IsSuccessStatusCode)
				{
					var errorReal = await response.Content.ReadAsStringAsync();
					await JS.InvokeVoidAsync("console.log", "Error del backend:", errorReal);
					await JS.InvokeVoidAsync("notifier.error", "No se pudo generar el archivo. Revisa la consola.");
					return;
				}

				// 5. Detectar nombre y extensión del archivo
				string nombreArchivoFinal = $"MovimientosSUA_{DateTime.Now:yyyyMMdd}.txt";

				var contentDisposition = response.Content.Headers.ContentDisposition;
				if (contentDisposition != null && !string.IsNullOrEmpty(contentDisposition.FileName))
				{
					nombreArchivoFinal = contentDisposition.FileName.Trim('"');
				}
				else
				{
					var contentType = response.Content.Headers.ContentType?.MediaType;
					if (contentType == "application/zip")
					{
						nombreArchivoFinal = $"MovimientosSUA_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
					}
				}

				// 6. Descargar el archivo físicamente
				using var fileStream = await response.Content.ReadAsStreamAsync();
				using var streamRef = new DotNetStreamReference(stream: fileStream);

				await JS.InvokeVoidAsync("DescargarArchivos", streamRef, nombreArchivoFinal);
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}
	}
}

	public sealed class ListaMovimientosSUADTO
	{
		[JsonPropertyName("id_movimiento")]
		public int IdMovimiento { get; set; }

		[JsonPropertyName("id_empleado")]
		public int IdEmpleado { get; set; }

		[JsonPropertyName("nombre")]
		[MaybeNull]
		public string Nombre { get; set; }

		[JsonPropertyName("nss")]
		[MaybeNull]
		public string Nss { get; set; }

		[JsonPropertyName("integrado")]
		public decimal Integrado { get; set; }

		[JsonPropertyName("tipo_mov")]
		public char TipoMov { get; set; }

		[JsonPropertyName("fecha")]
		public DateTime Fecha { get; set; }

		[JsonPropertyName("folio_incapacidad")]
		public string? FolioIncapacidad { get; set; }

		[JsonPropertyName("dias_incidencia")]
		public int? DiasIncidencia { get; set; }
	}

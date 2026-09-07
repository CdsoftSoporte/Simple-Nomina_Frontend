using DevExpress.Blazor;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontEndSN.Components.Pages.Menus.Menu_Reportes
{
	public partial class KardexVacaciones
	{
		private bool cargando = false;
		
		private int IdEmpleadoSeleccionado=0;
		private int IdPatronSeleccionado=0;

		RespuestaKardexVacaciones Registro;
		public DateTime FecInicioAntiguedad=DateTime.Today;

		//List<RegistrosKardexVacaciones> ListaRegistros => Registro?.Registros ?? new List<RegistrosKardexVacaciones>();
		List<RegistrosKardexVacaciones> ListaRegistros = new List<RegistrosKardexVacaciones>();

		private DateOnly FechaCorte = DateOnly.FromDateTime(DateTime.Now);

		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarEmpleadosParaCombos(),
					CargarPatronesParaCombo()
						);

			cargando = false;
			StateHasChanged();
		}

		private async Task OnEmpleadoChanged(int nuevoEmpleado)
		{
			IdEmpleadoSeleccionado = nuevoEmpleado;
			await LimpiarGrid();
		}

		private async Task OnRegistroPatronalChanged(int nuevoPatron)
		{
			IdPatronSeleccionado = nuevoPatron;
			await LimpiarGrid();
		}

		protected Task LimpiarGrid()
		{
			ListaRegistros.Clear();
			StateHasChanged();
			return Task.CompletedTask;
		}



		private async Task GenerarReporte()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				cargando = true;
				StateHasChanged();

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/KardexVacaciones/ObtenerKardex");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var parametros = new
				{
					idpatron = IdPatronSeleccionado,
					idempleado = IdEmpleadoSeleccionado,
					fechacorte=this.FechaCorte.ToString("yyyy-MM-dd")
				};

				request.Content = JsonContent.Create(parametros);


				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<RespuestaKardexVacaciones>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Registro = resultado?.Data ?? new();
					FecInicioAntiguedad = resultado.Data.FechaInicioAntiguedad;
					ListaRegistros=resultado.Data.Registros;
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message + errorData.Details ?? "Error inesperado"));
				}

			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message + ":" + ex.InnerException);
			}
			finally
			{
				await Task.Delay(800);
				cargando = false;
				StateHasChanged();
			}
		}

		private async Task ExportarExcel()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var registro_patronal = ListaPatrones.FirstOrDefault(p => p.id == IdPatronSeleccionado).razon_social;
				var nombre_empleado = ListaEmpleados.FirstOrDefault(p => p.Id == IdEmpleadoSeleccionado).NombreCompleto;

				var datosParaExcel = new
				{
					NombrePatron = registro_patronal,
					NombreEmpleado=nombre_empleado,
					FechaInicioAntiguedad=FecInicioAntiguedad.ToString("yyyy-MM-dd"),


					// Transformamos la lista del grid a la lista del Excel usando LINQ
					Registros = ListaRegistros.Select(x => new
					{
						FechaMovimiento = x.FechaMovimiento.ToString("yyyy-MM-dd"),
						
						Concepto = x.Concepto,

						DiasAbono = x.DiasAbono,

						DiasCargo = x.DiasCargo,

						SaldoDias = x.SaldoDias,

						PrimaAbono = x.PrimaAbono,

						PrimaCargo = x.PrimaCargo,

						SaldoPrima = x.SaldoPrima,

					}).ToList()
				};

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/KardexVacaciones/GenerarKardexExcel");
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

				await JS.InvokeVoidAsync("DescargarArchivos", streamRef, $"Kardex_Vacaciones_{registro_patronal}_{nombre_empleado}_corte_al_{FecInicioAntiguedad.ToString("yyyy-MM-dd")}.xlsx");
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message+ " "+ex.InnerException);
			}
		}

		private void OnCustomizeElement(GridCustomizeElementEventArgs e)
		{
			// 1. Validamos que sea una fila de datos
			if (e.ElementType == GridElementType.DataRow)
			{
				// 2. Extraemos el objeto usando el Grid y el VisibleIndex
				var registro = (RegistrosKardexVacaciones)e.Grid.GetDataItem(e.VisibleIndex);

				// 3. Aplicamos los estilos
				if (registro != null && registro.Concepto.Contains("Vencimiento", StringComparison.OrdinalIgnoreCase))
				{
					e.Style = "background-color: #820000; font-weight: 500;";
				}
				else if (registro != null && registro.Concepto.Contains("Aniversario", StringComparison.OrdinalIgnoreCase))
				{
					e.Style = "background-color: #006400;";
				}
			}
		}


	}

	public class RespuestaKardexVacaciones
	{
		public DateTime FechaInicioAntiguedad { get; set; }
		public List<RegistrosKardexVacaciones> Registros { get; set; } = new();
		

	}

	public class RegistrosKardexVacaciones
	{
		[JsonPropertyName("fechaMovimiento")]
		public DateTime FechaMovimiento { get; set; }

		[JsonPropertyName("concepto")]
		public string Concepto { get; set; }

		[JsonPropertyName("diasAbono")]
		public int DiasAbono { get; set; }

		[JsonPropertyName("diasCargo")]
		public int DiasCargo { get; set; }

		[JsonPropertyName("saldoDias")]
		public int SaldoDias { get; set; }

		[JsonPropertyName("primaAbono")]
		public Decimal PrimaAbono { get; set; }

		[JsonPropertyName("primaCargo")]
		public Decimal PrimaCargo { get; set; }

		[JsonPropertyName("saldoPrima")]
		public Decimal SaldoPrima { get; set; }

	}






}

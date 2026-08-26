using DevExpress.Blazor;
using FrontEndSN.Clases;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontEndSN.Components.Pages.Menus.Menu_Especiales
{
	public partial class AumentosSalariales:VentanaBase
	{
		private bool cargando = false;

		private int IdPatronSeleccionado = 0;
		private int IdNominaSeleccionada = 0;
		private int IdCentroDeCostosSeleccionado = 0;
		private int IdDepartamentoSeleccionado = 0;
		private int IdPuestoSeleccionado = 0;

		private decimal RangoMinimo = 0;
		private decimal RangoMaximo = 0;
		private decimal ImporteAumento = 0;

		private DateOnly FechaDeAplicacion = DateOnly.FromDateTime(DateTime.Now);

		private char TipoAumentoSeleccionado = 'P';

		private string ValorMascara = "P2";

		private List<RegistrosAumento> ListaRegistros = new();

		IReadOnlyList<object> SeleccionadosPorAumentar { get; set; } = new List<object>();
		IGrid GridAumentos { get; set; }





		private List<dynamic> ListaTiposAumento = new() {
			new { Valor = 'P', Texto = "Porcentaje (%)" },
			new { Valor = 'S', Texto = "Sumar Cantidad"},
			new { Valor = 'N', Texto = "Nuevo Salario General" },
		};

		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarPatronesParaCombo(),
					CargarNominasParaCombo(),
					CargarCentrosDeCostosParaCombo('S'),
					CargarDepartamentosParaCombo('S'),
					CargarPuestosParaCombo('S')
						);

			cargando = false;
			StateHasChanged();
		}


		private async Task SeleccionarTodos() => await GridAumentos.SelectAllAsync();
		private async Task SeleccionarNinguno() => await GridAumentos.DeselectAllAsync();


		protected async Task OnRegistroPatronalChanged(int nuevoIdPatron)
		{
			IdPatronSeleccionado = nuevoIdPatron;
			await LimpiarGrid();
		}


		protected async Task OnNominaChanged(int nuevaNomina)
		{
			IdNominaSeleccionada = nuevaNomina;
			await LimpiarGrid();
		}


		protected async Task OnCentroCostosChanged(int nuevoCentro)
		{
			IdCentroDeCostosSeleccionado = nuevoCentro;
			await LimpiarGrid();
		}

		protected async Task OnDepartamentoChanged(int nuevoDepartamento)
		{
			IdDepartamentoSeleccionado = nuevoDepartamento;
			await LimpiarGrid();
		}

		protected async Task OnPuestoChanged(int nuevoPuesto)
		{
			IdPuestoSeleccionado = nuevoPuesto;
			await LimpiarGrid();
		}

		protected async Task OnTipoAumentoChanged(char nuevoValorMov)
		{
			TipoAumentoSeleccionado = nuevoValorMov;
			ValorMascara = (TipoAumentoSeleccionado == 'P' ? "P2" : "C2");
			ImporteAumento = 0;
			await LimpiarGrid();
		}

		protected Task LimpiarGrid()
		{
			ListaRegistros.Clear();
			SeleccionadosPorAumentar = new List<object>();
			StateHasChanged();
			return Task.CompletedTask;
		}

		private async Task DescargarExcel()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var registro_patronal = ListaPatrones?.FirstOrDefault(p => p.id == IdPatronSeleccionado).razon_social;

				var nomina = ListaNominas?.FirstOrDefault(p => p.Id == IdNominaSeleccionada).DescripcionCompleta;

				var centro_costos = ListaCentros?.FirstOrDefault(p => p.Id == IdCentroDeCostosSeleccionado).Nombre;

				var departamento = ListaDepartamentos?.FirstOrDefault(p => p.Id == IdDepartamentoSeleccionado).Nombre;

				var puesto = ListaPuestos?.FirstOrDefault(p => p.Id == IdPuestoSeleccionado).Nombre;

				var TipoAumento = ListaTiposAumento?.FirstOrDefault(p => p.Valor == TipoAumentoSeleccionado).Texto;

				var listaRegistrosExcel = SeleccionadosPorAumentar.Cast<RegistrosAumento>().ToList();

				// 2. Construimos el objeto
				var datosParaExcel = new
				{
					patron = registro_patronal,
					nomina = nomina,
					centro_de_costos=centro_costos,
					departamento=departamento,
					puesto=puesto,
					tipo_aumento=TipoAumento,
					rango_min=RangoMinimo.ToString(),
					rango_max=RangoMaximo.ToString(),
					importe=ImporteAumento.ToString(),
					registros=listaRegistrosExcel
				};

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/MovimientoAfiliatorio/ExcelAumentos");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(datosParaExcel);

				var response = await Http.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					var errorReal = await response.Content.ReadAsStringAsync();


					await JS.InvokeVoidAsync("notifier.error", errorReal);

					return;
				}

				using var fileStream = await response.Content.ReadAsStreamAsync();

				using var streamRef = new DotNetStreamReference(stream: fileStream);

				await JS.InvokeVoidAsync("DescargarArchivos", streamRef, $"Lista_Aumentos_{registro_patronal}_{nomina}.xlsx");
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}

		}

		private async Task CalcularAumentos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				cargando = true;
				StateHasChanged();

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/MovimientoAfiliatorio/ObtenerCalculoAumentoSalario");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var parametros = new
				{
					id_patron = IdPatronSeleccionado,
					id_nomina = IdNominaSeleccionada,
					id_centro_costos = IdCentroDeCostosSeleccionado,
					id_departamento = IdDepartamentoSeleccionado,
					id_puesto = IdPuestoSeleccionado,
					rango_minimo = RangoMinimo,
					rango_maximo = RangoMaximo,
					tipo_aumento = TipoAumentoSeleccionado,
					importe_aumento = ImporteAumento,

				};

				request.Content = JsonContent.Create(parametros);


				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<RegistrosAumento>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaRegistros = resultado?.Data ?? new();
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

		private async Task AplicarAumentos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				bool confirmado = await JS.InvokeAsync<bool>("notifier.confirm", "Aplicar Aumentos", "¿Está seguro de aplicar un aumento de salario a los empleados seleccionados?");

				if (!confirmado) return;

				cargando = true;
				StateHasChanged();

				var listaSeleccionados = SeleccionadosPorAumentar.Cast<RegistrosAumento>().ToList();

				
				var detallesAumento = listaSeleccionados.Select(emp => new
				{
					id_empleado = emp.IdEmpleado,

					// Mandamos los NUEVOS salarios, no los actuales
					salario_diario = emp.NuevoDiario,
					salario_integrado = emp.NuevoIntegrado,

					dias_vacaciones = (byte?)emp.DiasVacaciones,
					dias_aguinaldo = (byte?)emp.DiasAguinaldo,
					prima_vacacional = emp.PorcentajePrima,
					factor_integracion = emp.FactorIntegracion
				}).ToList();

				var requestBody = new 
				{
					id_patron = IdPatronSeleccionado,
					id_nomina = IdNominaSeleccionada,

					fecha = FechaDeAplicacion,
					tipo_movimiento = 'M',

					registros = detallesAumento
				};

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/MovimientoAfiliatorio/GenerarAumentosMasivos");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(requestBody);

				var response = await Http.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					var errorReal = await response.Content.ReadAsStringAsync();
					await JS.InvokeVoidAsync("notifier.error", errorReal);
					return;
				}

				SeleccionadosPorAumentar = new List<object>();
				await Task.Delay(800);
				cargando = false;
				await JS.InvokeVoidAsync("notifier.success", "¡Las modificaciones salariales se guardaron con éxito!");

				await LimpiarGrid();
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

	}

	public class RegistrosAumento 
	{

		[JsonPropertyName("id_empleado")]
		public int IdEmpleado { get; set; }

		[JsonPropertyName("nombre_empleado")]
		public string NombreEmpleado { get; set; }
		
		[JsonPropertyName("fecha_ultimo_movimiento")]
		public DateTime FechaUltimoMov { get; set; }

		[JsonPropertyName("diario_actual")]
		public decimal DiarioActual { get; set; }

		[JsonPropertyName("integrado_actual")]
		public decimal IntegradoActual { get; set; }

		[JsonPropertyName("dias_vacaciones")]
		public int DiasVacaciones { get; set; }

		[JsonPropertyName("dias_aguinaldo")]
		public int DiasAguinaldo { get; set; }

		[JsonPropertyName("porcentaje_prima")]
		public decimal PorcentajePrima { get; set; }

		[JsonPropertyName("factor_integracion")]
		public decimal FactorIntegracion { get; set; }

		[JsonPropertyName("nuevo_diario")]
		public decimal NuevoDiario { get; set; }

		[JsonPropertyName("nuevo_integrado")]
		public decimal NuevoIntegrado { get; set; }




	}


}

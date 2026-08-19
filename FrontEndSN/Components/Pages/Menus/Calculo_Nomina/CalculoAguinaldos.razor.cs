using DevExpress.XtraReports.Diagnostics;
using FrontEndSN.Components.Pages.Menus.Catalogos;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class CalculoAguinaldos
	{

		private bool cargando = false;
		private bool isModalVisible = false;
		private int IdPatronSeleccionado = 1;
		protected int IdNominaSeleccionada { get; set; } = 0;
		protected int IdCentroDeCostosSeleccionado { get; set; }

		protected int IdPeriodoSeleccionado;

		protected List<RegistrosAguinaldo> ListaAguinaldos { get; set; }=new List<RegistrosAguinaldo>();
		
		private bool Considerar360Dias = false;
		private bool TomarDiasSinCalcular = false;
		private bool ConsiderarFaltas = false;

		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarPatronesParaCombo(),
					CargarNominasParaCombo(),
					CargarCentrosDeCostosParaCombo('S')
						);

			cargando = false;
			StateHasChanged();
		}

		protected async Task OnRegistroPatronalChanged(int nuevoIdPatron)
		{
			IdPatronSeleccionado = nuevoIdPatron;
			IdNominaSeleccionada = 0;
			IdCentroDeCostosSeleccionado=0;
			await LimpiarGrid();
		}

		protected async Task OnNominaChanged(int nuevaNomina)
		{
			IdNominaSeleccionada = nuevaNomina;
			IdCentroDeCostosSeleccionado = 0;
			await LimpiarGrid();
			await CargarPeriodosParaCombo(IdNominaSeleccionada);
		}

		protected async Task OnCentroCostosChanged(int nuevoCentro)
		{
			IdCentroDeCostosSeleccionado = nuevoCentro;
			await LimpiarGrid();
		}

		protected async Task OnPeriodoChanged(int nuevoPeriodo)
		{
			IdPeriodoSeleccionado = nuevoPeriodo;
			StateHasChanged();
		}

		protected Task LimpiarGrid()
		{
			ListaAguinaldos.Clear();
			StateHasChanged();
			return Task.CompletedTask;
		}

		protected async Task CargarDatosAguinaldo()
		{
			if (IdNominaSeleccionada == 0)
				return;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/CalculoAguinaldo/ConsultarDatosAguinaldo");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var parametros = new
				{
					AnioCalculo = ListaNominas.FirstOrDefault(n => n.Id == IdNominaSeleccionada)?.Ejercicio ?? "2026",
					BaseDias = !Considerar360Dias?365:360,
					DescontarFaltas = ConsiderarFaltas?"N":"S",
					ProyectarRestante = TomarDiasSinCalcular?"S":"N",
					IdNomina = IdNominaSeleccionada,
					IdCentroCostos = IdCentroDeCostosSeleccionado
				};

				request.Content = JsonContent.Create(parametros);


				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<RegistrosAguinaldo>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaAguinaldos = resultado?.Data ?? new();
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
				StateHasChanged();
			}
		}


		private async Task DescargarExcel()
		{
			try {
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				string ejercicio = ListaNominas.FirstOrDefault(n => n.Id == IdNominaSeleccionada)?.Ejercicio ?? "2026";
				string nomina = ListaNominas.FirstOrDefault(n => n.Id == IdNominaSeleccionada)?.DescripcionCompleta ?? "Sin nombre de nómina encontrado";

				var datosParaExcel = ListaAguinaldos.Select(x => new
				{
					nombre_empleado = x.NombreEmpleado,
					ultimo_mov = x.UltimoMov,
					ultima_alta_reingreso = x.UltimaAlta,
					sueldo_diario = x.Diario,
					dias_aguinaldo = x.DiasAguinaldo,
					dias_trabajados = x.DiasTrabajados,
					faltas = x.Faltas,
					dias_netos = x.DiasNetos,
					dias_proporcionales = x.DiasProporcionales,
					aguinaldo_anterior = x.AguinaldoAnterior,
					aguinaldo_restante = x.AguinaldoAPagar,
					isr_restante = x.ImporteIsrAguinaldo
				}).ToList();

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/CalculoAguinaldo/ExcelDatosAguinaldo/{ejercicio}/{nomina}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(datosParaExcel);

				var response = await Http.SendAsync(request);

				response.EnsureSuccessStatusCode();

				using var fileStream = await response.Content.ReadAsStreamAsync();

				using var streamRef = new DotNetStreamReference(stream: fileStream);

				await JS.InvokeVoidAsync("DescargarArchivos", streamRef, $"Datos_aguinaldo_{ejercicio}_{nomina}.xlsx");
			}
			catch(Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task GuardarIncidenciasAguinaldo()
		{
			try
			{
				isModalVisible = false;
				cargando = true;
				StateHasChanged();
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var DatosIncidencias = ListaAguinaldos.Select(x => new
				{
					id_empleado = x.IdEmpleado,
					id_periodo = IdPeriodoSeleccionado,
					fecha = ListaPeriodos.FirstOrDefault(n => n.id == IdPeriodoSeleccionado)?.Desde,
					id_concepto = 3,
					importe = x.AguinaldoAPagar,

				}).ToList();

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/CalculoAguinaldo/CrearIncidenciasAguinaldo");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(DatosIncidencias);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					await Task.Delay(800);
					await JS.InvokeVoidAsync("notifier.success", "Incidencias creadas en el periodo correctamente");
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
				IdPeriodoSeleccionado = 0;
				await OnNominaChanged(0);
				await OnCentroCostosChanged(0);
				StateHasChanged();
			}
			
		}

		private void MostrarModal()
		{
			isModalVisible = true;
			StateHasChanged();
		}


		public class RegistrosAguinaldo
		{
			[JsonPropertyName("id_empleado")]
			public int IdEmpleado {  get; set; }

			[JsonPropertyName("nombre_empleado")]
			public string NombreEmpleado { get; set; }

			[JsonPropertyName("ultima_alta_reingreso")]
			public DateTime? UltimaAlta { get; set; }

			[JsonPropertyName("fecha_ultimo_mov_imss")]
			public DateTime UltimoMov {  get; set; }

			[JsonPropertyName("salario_diario")]
			public decimal Diario { get; set; }

			[JsonPropertyName("dias_aguinaldo")]
			public decimal DiasAguinaldo { get; set; }

			[JsonPropertyName("dias_trabajados")]
			public int DiasTrabajados { get; set; }

			[JsonPropertyName("faltas")]
			public int Faltas {  get; set; }

			[JsonPropertyName("dias_trabajados_netos")]
			public int DiasNetos { get; set; }

			[JsonPropertyName("dias_proporcionales")]
			public decimal DiasProporcionales { get; set; }

			[JsonPropertyName("importe_aguinaldo_gravado_anterior")]
			public decimal AguinaldoGravadoAnterior { get; set; }

			[JsonPropertyName("importe_aguinaldo_exento_anterior")]
			public decimal AguinaldoExentoAnterior { get; set; }

			[JsonPropertyName("importe_aguinaldo")]
			public decimal AguinaldoRestanteAPagar { get; set; }

			[JsonPropertyName("importe_aguinaldo_gravado")]
			public decimal AguinaldoGravadoAPagar { get; set; }

			[JsonPropertyName("importe_aguinaldo_exento")]
			public decimal AguinaldoExentoAPagar { get; set; }

			[JsonPropertyName("importe_isr_aguinaldo")]
			public decimal ImporteIsrAguinaldo { get; set; }

			public decimal AguinaldoAnterior  => AguinaldoGravadoAnterior+AguinaldoExentoAnterior;
			public decimal AguinaldoAPagar => AguinaldoGravadoAPagar + AguinaldoExentoAPagar;
		}





	}
}

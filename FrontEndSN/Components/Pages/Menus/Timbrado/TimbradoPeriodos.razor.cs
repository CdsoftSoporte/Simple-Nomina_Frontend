using DevExpress.Blazor;
using DevExpress.XtraReports.Diagnostics;
using FrontEndSN.Clases;
using FrontEndSN.Components.Modales;
using FrontEndSN.Components.Pages.Menus.Calculo_Nomina;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontEndSN.Components.Pages.Menus.Timbrado
{
	public partial class TimbradoPeriodos:VentanaBase
	{

		protected bool cargando = false;
		protected int IdNominaSeleccionada { get; set; }
		protected int IdPeriodoSeleccionado { get; set; }
		protected int TotalEmpleados { get; set; }
		protected int TotalEmpleadosPendientes { get; set; }
		protected int TotalEmpleadosTimbrados { get; set; }

		IGrid GridPendientes { get; set; }
		IGrid GridTimbrados { get; set; }
		IReadOnlyList<object> SeleccionadosPorTimbrar { get; set; } = new List<object>();
		IReadOnlyList<object> SeleccionadosTimbrados { get; set; } = new List<object>();
		List<EmpleadoTimbrado> Empleados = new();


		private VisorDatosTimbrado ModalDatosTimbrado { get; set; }

		// Eventos para el Toolbar superior
		private async Task SeleccionarTodos() => await GridPendientes.SelectAllAsync();
		private async Task SeleccionarNinguno() => await GridPendientes.DeselectAllAsync();
		private async Task SeleccionarTodosTimbrados() => await GridTimbrados.SelectAllAsync();
		private async Task SeleccionarNingunoTimbrados() => await GridTimbrados.DeselectAllAsync();
		protected override async Task OnInitializedAsync()
		{
			await CargarNominasParaCombo();
			cargando = false;
			StateHasChanged();

		}

		private void LimpiarTotales()
		{
			TotalEmpleados = 0;
			TotalEmpleadosPendientes = 0;
			TotalEmpleadosTimbrados = 0;
			SeleccionadosPorTimbrar = new List<object>();
			SeleccionadosTimbrados = new List<object>();
			Empleados.Clear();

			 
			
		}


		protected async Task TimbrarSeleccionados()
		{
			if (SeleccionadosPorTimbrar == null || !SeleccionadosPorTimbrar.Any()) return;
			try
			{

				cargando = true;

				//  Extraemos solo los ids de la selección
				var IdsSeleccionados = SeleccionadosPorTimbrar
				.Cast<EmpleadoTimbrado>()
				.Select(e => e.IdEmpleado)
				.ToList();

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
		
				var request = new HttpRequestMessage(HttpMethod.Post, $"api/Timbrado/TimbrarPeriodo/{IdPeriodoSeleccionado}")
				{
					Content = JsonContent.Create(IdsSeleccionados)
				};
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				response.EnsureSuccessStatusCode();

				// 1. Revisamos qué tipo de contenido nos regresó el servidor
				var contentType = response.Content.Headers.ContentType?.MediaType;

				if (contentType == "application/json")
				{

					await JS.InvokeVoidAsync("notifier.success", $"{IdsSeleccionados.Count} empleado(s) timbrado(s) correctamente.");
				}
				else
				{
				
					using var fileStream = await response.Content.ReadAsStreamAsync();
					using var streamRef = new DotNetStreamReference(stream: fileStream);

					await JS.InvokeVoidAsync("DescargarArchivos", streamRef, "Errores_Timbrado.xlsx");
					await JS.InvokeVoidAsync("notifier.error", "Al menos un empleado tuvo un error al momento de timbrarse, se le descargará un archivo de Excel con los errores mencionados");
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
			finally
			{
				await CargaEstadoTimbrado();
				cargando = false;
				StateHasChanged();
			}
		}

		private async Task DescargarZipSeleccionados()
		{
			if (SeleccionadosTimbrados == null || !SeleccionadosTimbrados.Any()) return;


			var uuidsSeleccionados = SeleccionadosTimbrados
				.Cast<EmpleadoTimbrado>()
				.Select(e => e.UUID)
				.ToList();


			var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
			var request = new HttpRequestMessage(HttpMethod.Post, "api/Excel/DescargarXmlMasivo")
			{
				Content = JsonContent.Create(uuidsSeleccionados)
			};
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


			var response = await Http.SendAsync(request);

			if (response.IsSuccessStatusCode)
			{
				using var fileStream = await response.Content.ReadAsStreamAsync();
				using var streamRef = new DotNetStreamReference(stream: fileStream);

				await JS.InvokeVoidAsync("DescargarArchivos", streamRef, $"XMLs_Masivos_{DateTime.Now:dd-MM-yyyy}.zip");

				SeleccionadosTimbrados = new List<VisorDatosTimbradoDTO>();
			}
			else
			{
				Console.WriteLine("Error al descargar el ZIP.");
			}
		}

		protected async Task OnNominaChanged(int nuevoIdNomina)
		{
			IdNominaSeleccionada = nuevoIdNomina;
			IdPeriodoSeleccionado = 0;

			if (IdNominaSeleccionada > 0)
			{

				await CargarPeriodosParaCombo(IdNominaSeleccionada);
				LimpiarTotales();
			}
			else
			{
				ListaPeriodos.Clear();
				LimpiarTotales();

			}
		}



		protected async Task OnPeriodoChanged(int nuevoIdPeriodo)
		{
			IdPeriodoSeleccionado = nuevoIdPeriodo;
			LimpiarTotales();
			if (IdPeriodoSeleccionado > 0 && IdNominaSeleccionada > 0)
			{
				CargaEstadoTimbrado();
			}
		}

		protected async Task CargaEstadoTimbrado()
		{
			if (IdPeriodoSeleccionado == 0 || IdNominaSeleccionada == 0)
				return;
			try
			{
				cargando = true;
				LimpiarTotales();
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/Timbrado/ObtenerDatosDeTimbrado/{IdPeriodoSeleccionado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<EmpleadoTimbrado>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Empleados = resultado?.Data ?? new();
					TotalEmpleadosPendientes = Empleados.Where(e => e.Estado == "Pendiente").Count();
					TotalEmpleadosTimbrados = Empleados.Where(e => e.Estado == "Timbrado").Count();
					TotalEmpleados = Empleados.Count();


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
				Thread.Sleep(500);
				cargando = false;
				StateHasChanged();
			}
		}


		protected void EnviarTimbrePorCorreo()
		{

		}

		protected void AbrirModalDatosTimbrado(int IdEmpleado,int IdPeriodo)
		{
			ModalDatosTimbrado.Mostrar(IdEmpleado,IdPeriodo);
		}

		private async Task ExportarAnaliticoDeNomina()
		{

			var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
			if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

			var request = new HttpRequestMessage(HttpMethod.Get, $"api/Excel/ExcelDatosDeTimbrado/{IdPeriodoSeleccionado}");
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await Http.SendAsync(request);

			response.EnsureSuccessStatusCode();

			using var fileStream = await response.Content.ReadAsStreamAsync();

			using var streamRef = new DotNetStreamReference(stream: fileStream);

			await JS.InvokeVoidAsync("DescargarArchivos", streamRef, "Datos de Timbrado.xlsx");
		}

		public class ConceptoDesglose
		{
			[JsonPropertyName("descripcion_concepto")] 
			public string Descripcion { get; set; }

			[JsonPropertyName("importe_total")]
			public decimal Importe { get; set; }

			[JsonPropertyName("tipo_concepto")]
			public string TipoConcepto { get; set; }
		}

		public class EmpleadoTimbrado
		{
			[JsonPropertyName("id_empleado")]
			public int IdEmpleado { get; set; }

			[JsonPropertyName("id_periodo")]
			public int IdPeriodo { get; set; }

			[JsonPropertyName("nombre_empleado")]
			public string Nombre { get; set; }

			[JsonPropertyName("nombre_puesto")]
			public string Puesto { get; set; }

			[JsonPropertyName("nombre_departamento")]
			public string Departamento { get; set; }

			[JsonPropertyName("total_percepciones")]
			public decimal Percepciones { get; set; }

			[JsonPropertyName("total_deducciones")]
			public decimal Deducciones { get; set; }

			[JsonPropertyName("neto_a_pagar")]
			public decimal Neto { get; set; }

			[JsonPropertyName("estado_timbrado")]
			public string Estado { get; set; }

			[JsonPropertyName("folio")]
			public string? Folio { get; set; }

			[JsonPropertyName("serie")]
			public string? Serie{get;set;}

			[JsonPropertyName("uuid")]
			public string? UUID { get; set; }

			private List<ConceptoDesglose> _conceptos = new();

			[JsonPropertyName("conceptos")]
			public List<ConceptoDesglose> Conceptos { get; set; } = new();
			// Estas propiedades ya no necesitan JsonPropertyName porque se alimentan solas aquí arriba
			public List<ConceptoDesglose> DesglosePercepciones =>
		Conceptos?.Where(c => c.TipoConcepto == "P").ToList() ?? new();

			public List<ConceptoDesglose> DesgloseDeducciones =>
				Conceptos?.Where(c => c.TipoConcepto == "D").ToList() ?? new();
		}

	}
}


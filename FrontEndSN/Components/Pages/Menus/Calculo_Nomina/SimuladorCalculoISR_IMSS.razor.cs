using FrontEndSN.Clases;
using Microsoft.JSInterop;
using System.Text.Json;
using static FrontEndSN.Components.Pages.Menus.Calculo_Nomina.CalculoAguinaldos;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class SimuladorCalculoISR_IMSS:VentanaBase
	{

		public bool cargando=false;
		public int IdPatronSeleccionado = 0;
		public int IdEmpleadoSeleccionado = 0;
		public int IdNominaSeleccionada = 0;
		public int IdPeriodoSeleccionado = 0;
		private DatosSimulacion datosSimulacion=new();


		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarPatronesParaCombo(),
					CargarEmpleadosParaCombos(),
					CargarNominasParaCombo()
						);

			cargando = false;
			StateHasChanged();
		}


		private async Task OnEmpleadoChanged(int NuevoEmpleado)
		{
			IdEmpleadoSeleccionado = NuevoEmpleado;
		}

		private async Task OnPatronChanged(int NuevoPatron)
		{
			IdPatronSeleccionado = NuevoPatron;
		}

		protected async Task OnNominaChanged(int nuevaNomina)
		{
			IdNominaSeleccionada = nuevaNomina;
			await CargarPeriodosParaCombo(IdNominaSeleccionada);
		}

		protected async Task OnPeriodoChanged(int nuevoPeriodo)
		{
			IdPeriodoSeleccionado = nuevoPeriodo;
			await CargarPeriodosParaCombo(IdNominaSeleccionada);
		}

		private async Task Simular()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/Calculo/SimularNomina/{IdPeriodoSeleccionado}/{IdEmpleadoSeleccionado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<RespuestaSimulacion>();
					if (result != null && result.IsSuccess)
					{
						datosSimulacion = result.Data;
					}
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

		private async Task ExcelISR()
		{

		}

		private async Task ExcelIMSS()
		{

		}

		private async Task ExcelInfonavit()
		{

		}





	}

	public class RespuestaSimulacion
	{
		public bool IsSuccess { get; set; }
		public string Message { get; set; }
		public DatosSimulacion Data { get; set; }
	}

	public class DatosSimulacion
	{
		public List<IngresoDto> Ingresos { get; set; } = new();
		public IsrDto Isr { get; set; } = new();
		public ImssDto Imss { get; set; } = new();
		public List<InfonavitDto> Infonavit { get; set; } = new();
	}

	public class IngresoDto
	{
		public int ClaveConcepto { get; set; }
		public string Concepto { get; set; }
		public decimal ImporteGravado { get; set; }
		public decimal ImporteExento { get; set; }
		public decimal ImporteTotal { get; set; }
	}

	public class IsrDto
	{
		public decimal BaseGravadaPeriodo { get; set; }
		public decimal BaseMensualizada { get; set; }
		public decimal LimiteInferior { get; set; }
		public decimal ExcedenteLimite { get; set; }
		public decimal PorcentajeAplicable { get; set; }
		public decimal ImpuestoMarginal { get; set; }
		public decimal CuotaFija { get; set; }
		public decimal ISRBrutoMensual { get; set; }
		public decimal SubsidioMensual { get; set; }
		public decimal SubsidioDelPeriodo { get; set; }
		public decimal ISRNetoRetenidoPeriodo { get; set; }
	}

	public class ImssDto
	{
		public decimal SalarioDiarioIntegrado { get; set; }
		public decimal DiasCotizados { get; set; }
		public decimal ValorUMA { get; set; }
		public decimal BaseExcedenteUMA { get; set; }
		public string PctExcedente { get; set; }
		public decimal RetencionExcedente { get; set; }
		public string PctPrestacionesDinero { get; set; }
		public decimal RetencionPrestacionesDinero { get; set; }
		public string PctGastosMedicos { get; set; }
		public decimal RetencionGastosMedicos { get; set; }
		public string PctInvalidezVida { get; set; }
		public decimal RetencionInvalidezVida { get; set; }
		public string PctCesantiaVejez { get; set; }
		public decimal RetencionCesantiaVejez { get; set; }
		public decimal TotalRetencionIMSS { get; set; }
	}

	public class InfonavitDto
	{
		public int ClaveConcepto { get; set; }
		public string Descripcion { get; set; }
		public decimal ImporteDescontado { get; set; }
	}






}

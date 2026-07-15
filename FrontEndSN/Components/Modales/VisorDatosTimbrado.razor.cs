using FrontEndSN.Components.Pages.Menus.Calculo_Nomina;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FrontEndSN.Components.Modales
{
	public partial class VisorDatosTimbrado
	{
		private VisorDatosTimbradoDTO DatosEmpleado=new VisorDatosTimbradoDTO();
		private bool IsVisible = false;
		private int _IdEmpleado = 0;
		private int _IdPeriodo = 0;
		

		public async Task Mostrar(int IdEmpleado,int IdPeriodo)
		{
			await CargarDatosTimbrado(IdEmpleado,IdPeriodo);
			IsVisible = true;
			StateHasChanged();
		}

		private async Task CargarDatosTimbrado(int IdEmpleado, int IdPeriodo)
		{
			if (IdPeriodo == 0 || IdEmpleado == 0)
				return;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/Timbrado/ObtenerDatosDeTimbradoPorEmpleado/{IdPeriodo}/{IdEmpleado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<VisorDatosTimbradoDTO>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					DatosEmpleado = resultado?.Data ?? new();
					_IdEmpleado = IdEmpleado;
					_IdPeriodo = IdPeriodo;

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

		private async Task DescargarXML()
		{
			if (DatosEmpleado == null || string.IsNullOrEmpty(DatosEmpleado.XmlFirmado))
			{
				return;
			}

			byte[] xmlBytes = System.Text.Encoding.UTF8.GetBytes(DatosEmpleado.XmlFirmado);

			using var memoryStream = new MemoryStream(xmlBytes);

			using var streamRef = new DotNetStreamReference(stream: memoryStream);

			string nombreLimpio = DatosEmpleado.NombreEmpleado.Replace(" ", "_");
			string nombreArchivo = $"XML_{nombreLimpio}_{DatosEmpleado.UUID.Substring(0, 8)}.xml";

			await JS.InvokeVoidAsync("DescargarArchivos", streamRef, nombreArchivo);
		}

		private async Task GenerarPDFReciboNomina()
		{
			var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
			if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

			var request = new HttpRequestMessage(HttpMethod.Get, $"api/Excel/ImprimirReciboNomina/{_IdPeriodo}/{_IdEmpleado}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await Http.SendAsync(request);
			response.EnsureSuccessStatusCode();

			var htmlContent = await response.Content.ReadAsStringAsync();

			await JS.InvokeVoidAsync("AbrirImpresion", htmlContent);
		}



	}


	sealed class VisorDatosTimbradoDTO 
	{
		[JsonPropertyName("id_empleado")]
		public int IdEmpleado { get; set; }

		[JsonPropertyName("nombre_empleado")]
		public string NombreEmpleado { get; set; }

		[JsonPropertyName("nombre_departamento")]
		public string NombreDepartamento { get; set; }

		[JsonPropertyName("nombre_puesto")]
		public string NombrePuesto { get; set; }

		[JsonPropertyName("id_periodo")]
		public int IdPeriodo { get; set; }

		[JsonPropertyName("id_timbrado")]
		public int IdTimbrado { get; set; }

		[JsonPropertyName("certificado_sat")]
		public string CertificadoSat { get; set; }

		[JsonPropertyName("sello_sat")]
		public string SelloSat { get; set; }

		[JsonPropertyName("sello_digital")]
		public string SelloDigital { get; set; }

		[JsonPropertyName("cadena_original")]
			public string CadenaOriginal { get; set; }

		[JsonPropertyName("xml_firmado")]
		public string XmlFirmado { get; set; }

		[JsonPropertyName("qr_timbre")]
		public byte[] QrTimbre { get; set; }

		[JsonPropertyName("uuid")]
		public string UUID { get; set; }
		
		[JsonPropertyName("fecha_timbrado")]
		public DateTime FechaTimbrado { get; set; }

		[JsonPropertyName("total_percepciones")]
		public decimal TotalPercepciones { get; set; }
		[JsonPropertyName("total_deducciones")]
		public decimal TotalDeducciones { get; set; }
		[JsonPropertyName("neto_a_pagar")]
		public decimal NetoAPagar { get; set; }
	}



}



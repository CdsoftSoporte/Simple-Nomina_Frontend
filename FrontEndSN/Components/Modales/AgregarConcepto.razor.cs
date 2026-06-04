using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json.Serialization;

namespace FrontEndSN.Components.Modales
{
	public partial class AgregarConcepto:VentanaBase
	{
		private int IdConceptoSeleccionado = 0;
		private int IdEmpleado = 0;
		private int IdPeriodo = 0;
		private int IdNomina = 0;
		private decimal Importe = 0;
		private string NombreEmp = "";
		private string TituloModal = "";
		private bool IsVisible { get; set; } = false;
		private bool IsEdicion { get; set; } = false;

		[Parameter] public EventCallback OnConceptoAgregado { get; set; }


		protected override async Task OnInitializedAsync()
		{
			await CargarConceptosParaCombo();
		}

		public void Mostrar(int idnomina, int idperiodo, int idempleado,string NombreEmpleado)
		{
			IsVisible = true;
			IdConceptoSeleccionado = 0;
			IdPeriodo = idperiodo;
			IdNomina = idnomina;
			IdEmpleado= idempleado;
			NombreEmp = NombreEmpleado;
			Importe = 0;
			TituloModal = "Agregar concepto a ";
			IsEdicion = false;
			StateHasChanged();
		}

		public void Mostrar(int idnomina,int idperiodo,int idempleado,int idconcepto,decimal importe,string NombreEmpleado)
		{
			IsVisible = true;
			IdConceptoSeleccionado = idconcepto;
			IdPeriodo = idperiodo;
			IdNomina = idnomina;
			IdEmpleado = idempleado;
			Importe = importe;
			NombreEmp = NombreEmpleado;
			TituloModal = "Modificar concepto a ";
			IsEdicion = true;
			StateHasChanged();
		}

		public void Cerrar()
		{
			IsVisible = false;
			StateHasChanged();
		}

		private async Task Guardar()
		{
			try
			{
				if (IdConceptoSeleccionado == 0)
					return;

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var url = $"api/Periodos/AgregarConceptoAEmpleado";

				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var objeto = new AgregarConceptoRequest
				{
					IdNomina = IdNomina,
					IdPeriodo = IdPeriodo,
					IdEmpleado = IdEmpleado,
					IdConcepto = IdConceptoSeleccionado,
					Importe = Importe
				};

				request.Content = JsonContent.Create(objeto);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success", "Concepto agregado exitosamente.");
					await OnConceptoAgregado.InvokeAsync();
					Cerrar();
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
			finally
			{
				StateHasChanged();
			}
		}


		private async Task OnConceptoChanged(int id)
		{
			IdConceptoSeleccionado = id;

		}
		public class AgregarConceptoRequest
		{
			public int IdNomina { get; set; }
			public int IdPeriodo { get; set; }
			public int IdEmpleado { get; set; }
			public int IdConcepto { get; set; }
			public decimal Importe { get; set; }
		}

	}
}


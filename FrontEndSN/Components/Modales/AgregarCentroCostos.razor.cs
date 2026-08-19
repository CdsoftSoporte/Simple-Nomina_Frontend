using DevExpress.Blazor;
using FrontEndSN.Components.Pages.Menus.Catalogos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using static FrontEndSN.Components.Modales.AgregarConcepto;

namespace FrontEndSN.Components.Modales
{
	public partial class AgregarCentroCostos
	{

		private bool IsVisible { get; set; } = false;
		private bool IsEdicion { get; set; } = false;
		
		private int IdCentroSeleccionado { get; set; }
		private string NombreCentro { get; set; }
		private string Descripcion { get; set; }
		private string TituloModal { get; set; }

		[Parameter] public EventCallback OnCentroAgregado { get; set; }




		public void Mostrar()
		{
			IdCentroSeleccionado = 0;

			IsEdicion = false;
			NombreCentro = "";
			Descripcion = "";
			TituloModal = "Nuevo Centro de Costos";
			IsVisible = true;
			StateHasChanged();
		}

		public void Mostrar(int IdCentro,string Nombre, string Descripcion)
		{
			IdCentroSeleccionado = IdCentro;

			IsEdicion = true;
			NombreCentro = Nombre;
			this.Descripcion = Descripcion;
			TituloModal = "Editando Centro de Costos";
			IsVisible = true;
			StateHasChanged();
		}

		private async Task Guardar()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				string url = !IsEdicion ? "api/CentroDeCostos/Agregar" : $"api/CentroDeCostos/Editar/{IdCentroSeleccionado}";
				var metodo = HttpMethod.Post;

				var request = new HttpRequestMessage(metodo, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var objeto = new CentrosDeCostos
				{
					Id=IdCentroSeleccionado,
					Nombre=NombreCentro,
					Descripcion=Descripcion
				};

				request.Content = JsonContent.Create(objeto);
				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<object>>();

					if (resultado != null && resultado.IsSuccess)
					{

						string mensajeExito = !IsEdicion ? "Registro creado con éxito" : "Registro actualizado correctamente";
						await JS.InvokeVoidAsync("notifier.success", mensajeExito);
						await OnCentroAgregado.InvokeAsync();
						Cerrar();
					}
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
			finally
			{
				StateHasChanged();
			}
		}

		public void Cerrar()
		{
			IsVisible = false;
			StateHasChanged();
		}



	}
}
